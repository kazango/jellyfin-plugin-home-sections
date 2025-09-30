using Jellyfin.Plugin.HomeScreenSections.Configuration;
using Jellyfin.Plugin.HomeScreenSections.Library;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using Jellyfin.Plugin.HomeScreenSections.Services;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HomeScreenSections.HomeScreen.Sections
{
    public abstract class UpcomingSectionBase<T>(
        IUserManager userManager,
        IDtoService dtoService,
        ArrApiService arrApiService,
        ILogger logger) : IHomeScreenSection where T : class
    {
        public abstract string? Section { get; }
        public abstract string? DisplayText { get; set; }
        public virtual int? Limit => 1;
        public virtual string? Route => null;
        public string? AdditionalData { get; set; }
        public object? OriginalPayload { get; set; } = null;
        
        protected readonly IUserManager UserManager = userManager;
        protected readonly IDtoService DtoService = dtoService;
        protected readonly ArrApiService ArrApiService = arrApiService;
        protected readonly ILogger Logger = logger;

        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload, IQueryCollection queryCollection)
        {
            try
            {
                PluginConfiguration? config = HomeScreenSectionsPlugin.Instance?.Configuration;
                if (config == null)
                {
                    Logger.LogWarning("Plugin configuration not available");
                    return new QueryResult<BaseItemDto>();
                }

                // Check if service is configured
                (string? url, string? apiKey) = GetServiceConfiguration(config);
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
                {
                    Logger.LogWarning("{ServiceName} URL or API key not configured, skipping {SectionName}", GetServiceName(), GetSectionName());
                    return new QueryResult<BaseItemDto>();
                }

                DateTime startDate = DateTime.UtcNow;
                (int timeframeValue, TimeframeUnit timeframeUnit) = GetTimeframeConfiguration(config);
                DateTime endDate = ArrApiService.CalculateEndDate(startDate, timeframeValue, timeframeUnit);
                
                Logger.LogDebug("Fetching {SectionName} from {StartDate} to {EndDate}", GetSectionName(), startDate, endDate);

                T[] calendarItems = GetCalendarItems(startDate, endDate);
                
                if (calendarItems == null || calendarItems.Length == 0)
                {
                    Logger.LogDebug("No {SectionName} found from {ServiceName}", GetSectionName(), GetServiceName());
                    return new QueryResult<BaseItemDto>();
                }

                T[] upcomingItems = [.. FilterAndSortItems(calendarItems).Take(16)];

                Logger.LogDebug("Found {Count} upcoming items after filtering", upcomingItems.Length);

                BaseItemDto[] dtoItems = [.. upcomingItems.Select(item => CreateDto(item, config))];

                return new QueryResult<BaseItemDto>(dtoItems);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching {SectionName} from {ServiceName}", GetSectionName(), GetServiceName());
                return new QueryResult<BaseItemDto>();
            }
        }

        protected string CalculateCountdown(DateTime releaseDate, PluginConfiguration config)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSpan = releaseDate.ToLocalTime() - now;
            int totalDays = (int)Math.Ceiling(timeSpan.TotalDays);
            
            string countdownText = totalDays switch
            {
            <= 0 => "Today!",
            < 7 => $"{totalDays} {(totalDays == 1 ? "Day" : "Days")}",
            < 30 => FormatTimeUnit(totalDays / 7, totalDays % 7, "Week", "Day"),
            < 365 => FormatTimeUnit(totalDays / 30, (totalDays % 30) / 7, "Month", "Week"),
            _ => FormatTimeUnit(totalDays / 365, (totalDays % 365) / 30, "Year", "Month")
            };

            string formattedDate = ArrApiService.FormatDate(releaseDate.ToLocalTime(), config.DateFormat, config.DateDelimiter);
            return $"{countdownText} - {formattedDate}";
        }

        private static string FormatTimeUnit(int primaryValue, int secondaryValue, string primaryUnit, string secondaryUnit)
        {
            string primaryText = $"{primaryValue} {(primaryValue == 1 ? primaryUnit : $"{primaryUnit}s")}";
            
            if (secondaryValue > 0)
            {
                string secondaryText = $"{secondaryValue} {(secondaryValue == 1 ? secondaryUnit : $"{secondaryUnit}s")}";
            return $"{primaryText}, {secondaryText}";
            }
            
            return primaryText;
        }

        protected string GetFallbackCoverUrl(T missingItem)
        {
            (string? size, string? title, string? additionalInfo) = missingItem switch
            {
                SonarrCalendarDto sonarr => ("250x400", sonarr.Series?.Title, sonarr.Title),
                RadarrCalendarDto radarr => ("250x400", radarr.Title, null),
                LidarrCalendarDto lidarr => ("300x300", lidarr.Title, lidarr.Artist?.ArtistName),
                ReadarrCalendarDto readarr => ("250x400", readarr.Title, readarr.Author?.AuthorName),
                _ => ("400x250", "Unknown Item", null)
            };
            // Generate a darker random color for good contrast with white text
            string randomBgColor = $"{Random.Shared.Next(0, 128):X2}{Random.Shared.Next(0, 128):X2}{Random.Shared.Next(0, 128):X2}";
            string formattedText = string.IsNullOrEmpty(additionalInfo) ? title ?? "Unknown Item" : $"{title}\n{additionalInfo}" + "\nImage Not Found";
            return $"https://placehold.co/{size}/{randomBgColor}/FFF?text={Uri.EscapeDataString(formattedText)}";
        }

        // Abstract methods that subclasses must implement
        protected abstract (string? url, string? apiKey) GetServiceConfiguration(PluginConfiguration config);
        protected abstract (int value, TimeframeUnit unit) GetTimeframeConfiguration(PluginConfiguration config);
        protected abstract T[] GetCalendarItems(DateTime startDate, DateTime endDate);
        protected abstract IOrderedEnumerable<T> FilterAndSortItems(T[] items);
        protected abstract BaseItemDto CreateDto(T item, PluginConfiguration config);
        protected abstract string GetServiceName();
        protected abstract string GetSectionName();

        public abstract IHomeScreenSection CreateInstance(Guid? userId, IEnumerable<IHomeScreenSection>? otherInstances = null);
        public abstract HomeScreenSectionInfo GetInfo();
    }
}