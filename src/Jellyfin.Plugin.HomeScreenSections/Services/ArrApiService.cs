using System.Text.Json;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.HomeScreenSections.Configuration;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;

namespace Jellyfin.Plugin.HomeScreenSections.Services
{
    public enum ArrServiceType
    {
        Sonarr,
        Radarr,
        Lidarr,
        Readarr
    }

    public class ArrApiService
    {
        private readonly ILogger<ArrApiService> _logger;
        private readonly HttpClient _httpClient;

        public ArrApiService(ILogger<ArrApiService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        private PluginConfiguration Config => HomeScreenSectionsPlugin.Instance?.Configuration ?? new PluginConfiguration();

        public async Task<T[]?> GetArrCalendarAsync<T>(ArrServiceType serviceType, DateTime startDate, DateTime endDate)
        {
            (string? url, string? apiKey, string? serviceName) = GetServiceConfig(serviceType);
            
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("{ServiceName} URL or API key not configured", serviceName);
                return null;
            }

            try
            {
                string startParam = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string endParam = endDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                (string? queryParams, string? apiVersion) = serviceType switch
                {
                    ArrServiceType.Sonarr => ($"includeSeries=true&start={startParam}&end={endParam}", "v3"),
                    ArrServiceType.Radarr => ($"start={startParam}&end={endParam}", "v3"),
                    ArrServiceType.Lidarr => ($"start={startParam}&end={endParam}", "v1"),
                    ArrServiceType.Readarr => ($"includeAuthor=true&start={startParam}&end={endParam}", "v1"),
                    _ => ($"start={startParam}&end={endParam}", "v3")
                };
                string requestUrl = $"{url.TrimEnd('/')}/api/{apiVersion}/calendar?{queryParams}";

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("X-API-KEY", apiKey);

                _logger.LogDebug("Fetching {ServiceName} calendar from {Url}", serviceName, requestUrl);

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch {ServiceName} calendar. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                        serviceName, response.StatusCode, response.ReasonPhrase);
                    return null;
                }

                string jsonContent = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(jsonContent))
                {
                    _logger.LogWarning("Empty response from {ServiceName} calendar API", serviceName);
                    return Array.Empty<T>();
                }

                T[]? calendarItems = JsonSerializer.Deserialize<T[]>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogDebug("Successfully fetched {Count} calendar items from {ServiceName}", calendarItems?.Length ?? 0, serviceName);
                return calendarItems ?? Array.Empty<T>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching {ServiceName} calendar", serviceName);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while processing {ServiceName} calendar response", serviceName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching {ServiceName} calendar", serviceName);
                return null;
            }
        }

        private (string? url, string? apiKey, string serviceName) GetServiceConfig(ArrServiceType serviceType)
        {
            return serviceType switch
            {
                ArrServiceType.Sonarr => (Config.SonarrUrl, Config.SonarrApiKey, "Sonarr"),
                ArrServiceType.Radarr => (Config.RadarrUrl, Config.RadarrApiKey, "Radarr"),
                ArrServiceType.Lidarr => (Config.LidarrUrl, Config.LidarrApiKey, "Lidarr"),
                ArrServiceType.Readarr => (Config.ReadarrUrl, Config.ReadarrApiKey, "Readarr"),
                _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, "Unsupported service type")
            };
        }

        public DateTime CalculateEndDate(DateTime startDate, int timeframeValue, TimeframeUnit timeframeUnit)
        {
            return timeframeUnit switch
            {
                TimeframeUnit.Days => startDate.AddDays(timeframeValue),
                TimeframeUnit.Weeks => startDate.AddDays(timeframeValue * 7),
                TimeframeUnit.Months => startDate.AddMonths(timeframeValue),
                TimeframeUnit.Years => startDate.AddYears(timeframeValue),
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