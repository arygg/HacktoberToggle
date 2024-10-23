using System.Net.Http;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Cli.Contracts;

namespace Cli.Clients;

public class AzureAppConfigurationClient
{
    private readonly string _credential;
    private readonly string _secret;
    private readonly string _hostName;

    private const string AzureAppConfigCredentialEnvVariableName = "APPCS_CREDENTIAL";
    private const string AzureAppConfigSecretEnvVariableName = "APPCS_SECRET";
    private const string AzureAppConfigHostEnvVariableName = "APPCS_HOST";
    private const string NotSet = "Not set";

    private readonly JsonSerializerOptions _defaultWebOptions;

    public AzureAppConfigurationClient(string hostName, string credential, string secret)
    {
        _hostName = hostName ?? throw new ArgumentNullException(nameof(hostName), "Missing value for host. Please set environment variable using setup");
        _credential = credential ?? throw new ArgumentNullException(nameof(credential), "Missing value for credential. Please set environment variable using setup");
        _secret = secret ?? throw new ArgumentNullException(nameof(secret), "Missing value for secret. Please set environment variable using setup");
        _defaultWebOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public AzureAppConfigurationClient() : 
        this(ReadEnvironmentVariable(AzureAppConfigHostEnvVariableName), ReadEnvironmentVariable(AzureAppConfigCredentialEnvVariableName), ReadEnvironmentVariable(AzureAppConfigSecretEnvVariableName))
    {
    }

    public string GetConnectionString()
    {
        return $"Endpoint=https://{_hostName}.azconfig.io;Id={_credential};Secret={_secret}";
    }

    public static void SetEnvironmentVariables(string hostName, string credential, string secret)
    {
        Environment.SetEnvironmentVariable(AzureAppConfigHostEnvVariableName, hostName, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(AzureAppConfigCredentialEnvVariableName, credential, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(AzureAppConfigSecretEnvVariableName, secret, EnvironmentVariableTarget.Process);
    }

    public static (bool exists, string message) GetConfiguredConnectionDetails()
    {
        var host = ReadEnvironmentVariable(AzureAppConfigHostEnvVariableName) ?? NotSet;
        var credentials = ReadEnvironmentVariable(AzureAppConfigCredentialEnvVariableName) ?? NotSet;
        var secret = ReadEnvironmentVariable(AzureAppConfigSecretEnvVariableName) ?? NotSet;

        if (host == NotSet && credentials == NotSet && secret == NotSet)
        {
            return (false, "No AppConfig connection settings found");
        }

        return (true, $"HostName: {host}, Credentials: {credentials}, Secret: {secret}");
    }

    private static string ReadEnvironmentVariable(string key)
    {
        return Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
    }

    public async Task<KeyValueResponse> GetAllKeys()
    {
        using var client = new HttpClient();
        var request = CreateSignedRequest(HttpMethod.Get);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
            
        return DeserializeAppSettingsResponse(body, false);
    }

    public async Task<KeyValueResponse> GetFilteredToggles(string toggleNameFilter, EnvironmentFilter environment)
    {
        var identifySalesChannelStatusFeatures = toggleNameFilter.Contains(Constants.SalesChannelMarker);

        var partialKey = $"{Constants.FeatureMarker}{toggleNameFilter}";
        return await GetFilteredKeys(partialKey, environment, identifySalesChannelStatusFeatures);
    }

    public async Task<KeyValueResponse> GetFilteredKeys(string partialKey, EnvironmentFilter environment, bool identifySalesChannelStatusFeatures = false)
    {
        //  https://appcs-onlinesales.azconfig.io/kv/UseFallbackInternalShopAccessToken?api-version=1.0&label=itest
        using var client = new HttpClient();
        var request = CreateSignedRequest(HttpMethod.Get, null, null, $"key={partialKey}", GetLabelUrlParameter(environment));
        
        var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine("Not found");            
            return KeyValueResponse.Empty;
        }

        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Status code : {response.StatusCode}");

        return DeserializeAppSettingsResponse(body, identifySalesChannelStatusFeatures);
    }

    public async Task<(bool success, string message)> AddSalesChannelStatus(string salesProcessId, string nautilusId, EnvironmentFilter environment)
    {
        var name = new SalesChannelStatusId(salesProcessId, nautilusId).Id;
        return await AddFeatureFlag(name, "Sales channel status", false, environment);
    }

    public async Task<(bool success, string message)> AddFeatureFlag(string name, string description, bool initialValue, EnvironmentFilter environment)
    {
        return await AddOrUpdateFeatureFlag(name, description, initialValue, environment, isNewEntry:true);
    }

    public async Task<(bool success, string message)> ToggleFeatureFlag(string name, bool value, EnvironmentFilter environment)
    {
        if (environment == EnvironmentFilter.All)
        {
            throw new ArgumentException("All is not a valid option for environment filter in this context");
        }

        return await AddOrUpdateFeatureFlag(name, "This should not be sent", value, environment, isNewEntry:false);
    }

    private KeyValueResponse DeserializeAppSettingsResponse(string body, bool deserializeSalesChannelFeatures)
    {
        var appSettingsResponse = JsonSerializer.Deserialize<KeyValueResponse>(body, _defaultWebOptions);

        foreach (var appSetting in appSettingsResponse.Items)
        {
            if (appSetting.IsToggle)
            {
                if (deserializeSalesChannelFeatures && SalesChannelStatusId.IsSalesChannelId(appSetting.Key))
                {
                    appSetting.Toggle = JsonSerializer.Deserialize<SalesChannelStatus>(appSetting.Value, _defaultWebOptions);
                }
                else
                {
                    appSetting.Toggle = JsonSerializer.Deserialize<FeatureToggle>(appSetting.Value, _defaultWebOptions);
                }
                appSetting.Toggle.Label = appSetting.Label;
            }
        }

        return appSettingsResponse;
    }

    private async Task<(bool success, string message)> AddOrUpdateFeatureFlag(string name, string description, bool value, EnvironmentFilter environment, bool isNewEntry)
    {
        using var client = new HttpClient();

        var urlParameters = GetLabelUrlParameter(environment);
        var jsonContent = GetJsonstringContent(name, description, value, isNewEntry);
        var content = new StringContent(jsonContent, Encoding.UTF8, AppSetting.FeatureFlagContentType);
        var request = CreateSignedRequest(HttpMethod.Put, $"/.appconfig.featureflag%2F{name}", content, urlParameters);
        if (isNewEntry)
        {
            request.Headers.TryAddWithoutValidation("If-None-Match", "*");
        }
        
        try
        {
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return (true, "Success");
            }

            if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return (true, "Resource already exists");
            }
            
            return (false, $"Unknown result. Response code was {response.StatusCode} with content:\n\r {body}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private static string GetLabelUrlParameter(EnvironmentFilter environment)
    {
        return environment == EnvironmentFilter.All ? "" : $"label={environment.ToString().ToLower()}";
    }

    private string GetJsonstringContent(string name, string description, bool value, bool isNewEntry)
    {
        var valueString = isNewEntry ? GetJsonStringForCreateFlag(name, description, value) : GetJsonStringForUpdateFlag(name, value);
        return $"{{\"content_type\": \"application/vnd.microsoft.appconfig.ff+json;charset=utf-8\", \"value\": \"{valueString}\", \"tags\": {{}}}}";
    }

    private string GetJsonStringForCreateFlag(string name, string description, bool value)
    {
        return $"{{\\\"id\\\":\\\"{name}\\\",\\\"description\\\":\\\"{description}\\\",\\\"enabled\\\":{value.ToString().ToLower()},\\\"conditions\\\":{{\\\"client_filters\\\":[]}}}}";
    }

    private string GetJsonStringForUpdateFlag(string name, bool value)
    {
        return $"{{\\\"id\\\":\\\"{name}\\\",\\\"enabled\\\":{value.ToString().ToLower()}}}";
    }

    private HttpRequestMessage CreateSignedRequest(HttpMethod httpMethod, string additionalUriSegment = "", StringContent content = null, params string[] urlParameters)
    {
        var requestUri = $"https://{_hostName}.azconfig.io/kv{additionalUriSegment}";
        var parameters = (urlParameters ?? []).Concat(new[]{"api-version=1.0"}).ToArray();

        for (var i = 0; i < parameters.Count(); i++)
        { 
            if (string.IsNullOrWhiteSpace(parameters[i]))
                continue;

            var delimiter = (i == 0) ? "?" : "&";
            requestUri += delimiter + parameters[i];
        }

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(requestUri),
            Method = httpMethod,
            Content = content
        };

        request.SignRequest(_credential, _secret);

        Console.WriteLine("Url: " + request.RequestUri);
        return request;
    }
}