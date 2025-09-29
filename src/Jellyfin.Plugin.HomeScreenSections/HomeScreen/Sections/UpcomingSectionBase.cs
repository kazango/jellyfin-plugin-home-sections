using Jellyfin.Data.Enums;
using Jellyfin.Plugin.HomeScreenSections.Configuration;
using Jellyfin.Plugin.HomeScreenSections.Library;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using Jellyfin.Plugin.HomeScreenSections.Services;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HomeScreenSections.HomeScreen.Sections
{
    public abstract class UpcomingSectionBase<T> : IHomeScreenSection where T : class
    {
        public abstract string? Section { get; }
        public abstract string? DisplayText { get; set; }
        public virtual int? Limit => 1;
        public virtual string? Route => null;
        public string? AdditionalData { get; set; }
        public object? OriginalPayload { get; set; } = null;
        
        protected readonly IUserManager UserManager;
        protected readonly IDtoService DtoService;
        protected readonly ArrApiService ArrApiService;
        protected readonly ILogger Logger;

        protected UpcomingSectionBase(
            IUserManager userManager,
            IDtoService dtoService,
            ArrApiService arrApiService,
            ILogger logger)
        {
            UserManager = userManager;
            DtoService = dtoService;
            ArrApiService = arrApiService;
            Logger = logger;
        }
        
        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload, IQueryCollection queryCollection)
        {
            try
            {
                var config = HomeScreenSectionsPlugin.Instance?.Configuration;
                if (config == null)
                {
                    Logger.LogWarning("Plugin configuration not available");
                    return new QueryResult<BaseItemDto>();
                }

                // Check if service is configured
                var (url, apiKey) = GetServiceConfiguration(config);
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
                {
                    Logger.LogWarning("{ServiceName} URL or API key not configured, skipping {SectionName}", GetServiceName(), GetSectionName());
                    return new QueryResult<BaseItemDto>();
                }

                var startDate = DateTime.UtcNow;
                var (timeframeValue, timeframeUnit) = GetTimeframeConfiguration(config);
                var endDate = ArrApiService.CalculateEndDate(startDate, timeframeValue, timeframeUnit);
                
                Logger.LogDebug("Fetching {SectionName} from {StartDate} to {EndDate}", GetSectionName(), startDate, endDate);

                var calendarItems = GetCalendarItems(startDate, endDate);
                
                if (calendarItems == null || calendarItems.Length == 0)
                {
                    Logger.LogDebug("No {SectionName} found from {ServiceName}", GetSectionName(), GetServiceName());
                    return new QueryResult<BaseItemDto>();
                }

                var upcomingItems = FilterAndSortItems(calendarItems)
                    .Take(16)
                    .ToArray();

                Logger.LogDebug("Found {Count} upcoming items after filtering", upcomingItems.Length);

                var dtoItems = upcomingItems.Select(item => CreateDto(item, config)).ToArray();

                return new QueryResult<BaseItemDto>(dtoItems);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching {SectionName} from {ServiceName}", GetSectionName(), GetServiceName());
                return new QueryResult<BaseItemDto>();
            }
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