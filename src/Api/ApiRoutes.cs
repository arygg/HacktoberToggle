using Microsoft.FeatureManagement;

namespace Api;

public static class ApiRoutes
{
    public static void AddApiEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api");

        apiGroup.MapGet("/weatherforecast", GetWeatherForecasts)
            .WithName("GetWeatherForecast")
            .WithOpenApi();
    }

    static readonly string[] _summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];
    private static async Task<WeatherForecast[]> GetWeatherForecasts(IFeatureManager featureManager)
    {
        WeatherForecast[] forecast;

        if (await featureManager.IsEnabledAsync("API.TestFlag"))
        {
            // Run the following code
            forecast = [new WeatherForecast()];

        } else {
          forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = _summaries[Random.Shared.Next(_summaries.Length)]
                })
            .ToArray();
        }

        return forecast;
    }
}