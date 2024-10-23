
using Microsoft.FeatureManagement;

namespace Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Create an instance of AzureAppConfigurationClient
            var appConfigClient = new Cli.Clients.AzureAppConfigurationClient();

            // Get the connection string from AzureAppConfigurationClient
            string connectionString = appConfigClient.GetConnectionString();

            // Load configuration from Azure App Configuration using the connection string
            //builder.Configuration.AddAzureAppConfiguration(connectionString);

            // Load configuration from Azure App Configuration
            builder.Configuration.AddAzureAppConfiguration(options =>
            {
                options.Connect(connectionString)
                      // Load all keys that start with `TestApp:` and have no label
                      .Select("API:*", "test")
                      // Configure to reload configuration if the registered sentinel key is modified
                      .ConfigureRefresh(refreshOptions =>
                            refreshOptions.Register("API:Sentinel", refreshAll: true));
                            //.SetRefreshInterval(TimeSpan.FromSeconds(30)));

                options.UseFeatureFlags(featureFlagOptions =>
                {
                    featureFlagOptions.Select("API*", "test");
                });
            });

            // Add Azure App Configuration middleware to the container of services.
            builder.Services.AddAzureAppConfiguration();

            // Add feature management to the container of services.
            builder.Services.AddFeatureManagement();

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            // Use Azure App Configuration middleware to refresh settings on each request.
            app.UseAzureAppConfiguration();

            // Configure routes
            app.AddApiEndpoints(); 

            app.Run();
        }
    }
}
