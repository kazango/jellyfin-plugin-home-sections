using System.Text.Json;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.HomeScreenSections.Configuration;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;

namespace Jellyfin.Plugin.HomeScreenSections.Services
{
    public class ArrApiService
    {
        private readonly ILogger<ArrApiService> _logger;
        private readonly HttpClient _httpClient;
        private readonly PluginConfiguration _config;

        public ArrApiService(ILogger<ArrApiService> logger, HttpClient httpClient, PluginConfiguration config)
        {
            _logger = logger;
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<SonarrCalendarDto[]?> GetSonarrCalendarAsync(DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrEmpty(_config.SonarrUrl) || string.IsNullOrEmpty(_config.SonarrApiKey))
            {
                _logger.LogWarning("Sonarr URL or API key not configured");
                return null;
            }

            try
            {
                var startParam = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var endParam = endDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var url = $"{_config.SonarrUrl.TrimEnd('/')}/api/v3/calendar?includeSeries=true&start={startParam}&end={endParam}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-API-KEY", _config.SonarrApiKey);

                _logger.LogDebug("Fetching Sonarr calendar from {Url}", url);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch Sonarr calendar. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(jsonContent))
                {
                    _logger.LogWarning("Empty response from Sonarr calendar API");
                    return Array.Empty<SonarrCalendarDto>();
                }

                var calendarItems = JsonSerializer.Deserialize<SonarrCalendarDto[]>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogDebug("Successfully fetched {Count} calendar items from Sonarr", calendarItems?.Length ?? 0);
                return calendarItems ?? Array.Empty<SonarrCalendarDto>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching Sonarr calendar");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while processing Sonarr calendar response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching Sonarr calendar");
                return null;
            }
        }

        public DateTime CalculateEndDate(DateTime startDate, int timeframeValue, string timeframeUnit)
        {
            return timeframeUnit.ToLowerInvariant() switch
            {
                "days" => startDate.AddDays(timeframeValue),
                "weeks" => startDate.AddDays(timeframeValue * 7),
                "months" => startDate.AddMonths(timeframeValue),
                "years" => startDate.AddYears(timeframeValue),
                _ => startDate.AddDays(timeframeValue)
            };
        }

        public string FormatDate(DateTime date, string format, string delimiter)
        {
            return format.ToUpperInvariant() switch
            {
                "YYYY/MM/DD" => date.ToString($"yyyy{delimiter}MM{delimiter}dd"),
                "DD/MM/YYYY" => date.ToString($"dd{delimiter}MM{delimiter}yyyy"),
                "MM/DD/YYYY" => date.ToString($"MM{delimiter}dd{delimiter}yyyy"),
                "DD/MM" => date.ToString($"dd{delimiter}MM"),
                "MM/DD" => date.ToString($"MM{delimiter}dd"),
                _ => date.ToString($"yyyy{delimiter}MM{delimiter}dd")
            };
        }
    }
}