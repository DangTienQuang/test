using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AutoWashPro.BLL.Services.Interface;
using AutoWashPro.DAL.Entities;
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

        public async Task<bool> IsProlongedRainAsync(Branch branch)
        {
            try
            {
                var apiKey = _configuration["OpenWeatherMap:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("OpenWeatherMap API Key is not configured.");
                    return false;
                }

                string city = ExtractCityFromAddress(branch.Address);
                string query = Uri.EscapeDataString(city);

                // 1. Check current weather
                var currentUrl = $"https://api.openweathermap.org/data/2.5/weather?q={query}&appid={apiKey}";
                var currentResponse = await _httpClient.GetAsync(currentUrl);
                if (!currentResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch current weather data for branch {BranchId}. StatusCode: {StatusCode}", branch.BranchId, currentResponse.StatusCode);
                    return false;
                }

                var currentContent = await currentResponse.Content.ReadAsStringAsync();
                var currentWeatherData = JsonSerializer.Deserialize<JsonElement>(currentContent);
                bool isCurrentlyRaining = false;

                if (currentWeatherData.TryGetProperty("weather", out var currentWeatherArray) && currentWeatherArray.ValueKind == JsonValueKind.Array && currentWeatherArray.GetArrayLength() > 0)
                {
                    var mainStatus = currentWeatherArray[0].GetProperty("main").GetString();
                    if (!string.IsNullOrEmpty(mainStatus) &&
                        (mainStatus.Contains("Rain", StringComparison.OrdinalIgnoreCase) ||
                         mainStatus.Contains("Thunderstorm", StringComparison.OrdinalIgnoreCase)))
                    {
                        isCurrentlyRaining = true;
                    }
                }

                if (!isCurrentlyRaining)
                {
                    return false; // Not raining now
                }

                // 2. Check 3-hour forecast
                var forecastUrl = $"https://api.openweathermap.org/data/2.5/forecast?q={query}&appid={apiKey}&cnt=1"; // cnt=1 to get only the first forecast step
                var forecastResponse = await _httpClient.GetAsync(forecastUrl);
                if (!forecastResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch forecast data for branch {BranchId}. StatusCode: {StatusCode}", branch.BranchId, forecastResponse.StatusCode);
                    return false; // Better to return false if we can't confirm forecast
                }

                var forecastContent = await forecastResponse.Content.ReadAsStringAsync();
                var forecastData = JsonSerializer.Deserialize<JsonElement>(forecastContent);

                if (forecastData.TryGetProperty("list", out var listArray) && listArray.ValueKind == JsonValueKind.Array && listArray.GetArrayLength() > 0)
                {
                    var firstForecast = listArray[0];
                    if (firstForecast.TryGetProperty("weather", out var forecastWeatherArray) && forecastWeatherArray.ValueKind == JsonValueKind.Array && forecastWeatherArray.GetArrayLength() > 0)
                    {
                        var mainStatus = forecastWeatherArray[0].GetProperty("main").GetString();
                        if (!string.IsNullOrEmpty(mainStatus) &&
                            (mainStatus.Contains("Rain", StringComparison.OrdinalIgnoreCase) ||
                             mainStatus.Contains("Thunderstorm", StringComparison.OrdinalIgnoreCase)))
                        {
                            return true; // Raining now AND forecast is rain
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching prolonged rain weather data for branch {BranchId}.", branch.BranchId);
                return false;
            }
        }

        private string ExtractCityFromAddress(string? address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return "Ho Chi Minh";
            }

            var parts = address.Split(',');
            var lastPart = parts[parts.Length - 1].Trim();
            if (string.IsNullOrWhiteSpace(lastPart))
            {
                return "Ho Chi Minh";
            }

            return lastPart;
        }
    }
}
