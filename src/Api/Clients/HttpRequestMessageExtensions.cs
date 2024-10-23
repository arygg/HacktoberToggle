using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Cli.Clients;

static class HttpRequestMessageExtensions
{
    public static HttpRequestMessage SignRequest(this HttpRequestMessage request, string credential, string secret)
    {
        var host = request.RequestUri.Authority;
        var verb = request.Method.ToString().ToUpper();
        var utcNow = DateTimeOffset.UtcNow;
        var contentHash = Convert.ToBase64String(request.Content.ComputeSha256Hash());

        var signedHeaders = "date;host;x-ms-content-sha256"; // Semicolon separated header names

        var stringToSign = $"{verb}\n{request.RequestUri.PathAndQuery}\n{utcNow.ToString("r")};{host};{contentHash}";

        string signature;
        using (var hmac = new HMACSHA256(Convert.FromBase64String(secret)))
        {
            signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(stringToSign)));
        }

        request.Headers.Date = utcNow;
        request.Headers.Add("x-ms-content-sha256", contentHash);
        request.Headers.Authorization = new AuthenticationHeaderValue("HMAC-SHA256", $"Credential={credential}&SignedHeaders={signedHeaders}&Signature={signature}");

        return request;
    }
}