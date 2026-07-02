using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AutoWashPro.BLL.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoWashPro.BLL.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> IsRainingNowAsync()
        {
            try
            {
                var apiKey = _configuration["OpenWeatherMap:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("OpenWeatherMap API Key is not configured.");
                    return false;
                }

                // Hardcoded city Ho Chi Minh as per requirements.
                var url = $"https://api.openweathermap.org/data/2.5/weather?q=Ho Chi Minh&appid={apiKey}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch weather data. StatusCode: {StatusCode}", response.StatusCode);
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<JsonElement>(content);

                if (weatherData.TryGetProperty("weather", out var weatherArray) && weatherArray.ValueKind == JsonValueKind.Array && weatherArray.GetArrayLength() > 0)
                {
                    var mainStatus = weatherArray[0].GetProperty("main").GetString();

                    if (!string.IsNullOrEmpty(mainStatus) &&
                        (mainStatus.Contains("Rain", StringComparison.OrdinalIgnoreCase) ||
                         mainStatus.Contains("Thunderstorm", StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching weather data.");
                return false;
            }
        }
    }
}