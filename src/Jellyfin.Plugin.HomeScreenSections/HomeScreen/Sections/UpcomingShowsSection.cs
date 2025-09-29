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
    public class UpcomingShowsSection : IHomeScreenSection
    {
        public string? Section => "UpcomingShows";
        
        public string? DisplayText { get; set; } = "Upcoming Shows";
        
        public int? Limit => 1;
        
        public string? Route { get; }
        
        public string? AdditionalData { get; set; }

        public object? OriginalPayload { get; set; } = null;
        
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly ArrApiService _arrApiService;
        private readonly ILogger<UpcomingShowsSection> _logger;

        public UpcomingShowsSection(
            IUserManager userManager,
            IDtoService dtoService,
            ArrApiService arrApiService,
            ILogger<UpcomingShowsSection> logger)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _arrApiService = arrApiService;
            _logger = logger;
        }
        
        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload, IQueryCollection queryCollection)
        {
            try
            {
                var config = HomeScreenSectionsPlugin.Instance?.Configuration;
                if (config == null)
                {
                    _logger.LogWarning("Plugin configuration not available");
                    return new QueryResult<BaseItemDto>();
                }

                // Check if Sonarr is configured
                if (string.IsNullOrEmpty(config.SonarrUrl) || string.IsNullOrEmpty(config.SonarrApiKey))
                {
                    _logger.LogWarning("Sonarr URL or API key not configured, skipping upcoming shows");
                    return new QueryResult<BaseItemDto>();
                }

                var startDate = DateTime.UtcNow;
                var endDate = _arrApiService.CalculateEndDate(startDate, config.UpcomingTimeframeValue, config.UpcomingTimeframeUnit);
                
                _logger.LogDebug("Fetching upcoming shows from {StartDate} to {EndDate}", startDate, endDate);

                var calendarItems = _arrApiService.GetSonarrCalendarAsync(startDate, endDate).GetAwaiter().GetResult();
                
                if (calendarItems == null || calendarItems.Length == 0)
                {
                    _logger.LogDebug("No upcoming shows found from Sonarr");
                    return new QueryResult<BaseItemDto>();
                }

                var upcomingItems = calendarItems
                    .Where(item => item.Monitored && !item.HasFile && item.AirDateUtc.HasValue)
                    .OrderBy(item => item.AirDateUtc)
                    .Take(16)
                    .ToArray();

                _logger.LogDebug("Found {Count} upcoming episodes after filtering", upcomingItems.Length);

                var dtoItems = upcomingItems.Select(item => CreateUpcomingShowDto(item, config)).ToArray();

                return new QueryResult<BaseItemDto>(dtoItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching upcoming shows from Sonarr");
                return new QueryResult<BaseItemDto>();
            }
        }

        private BaseItemDto CreateUpcomingShowDto(SonarrCalendarDto calendarItem, PluginConfiguration config)
        {
            var formattedDate = _arrApiService.FormatDate(
                calendarItem.AirDateUtc?.ToLocalTime() ?? DateTime.Now,
                config.DateFormat,
                config.DateDelimiter);

            var episodeInfo = $"S{calendarItem.SeasonNumber:D2}E{calendarItem.EpisodeNumber:D2} - {calendarItem.Title}";

            var posterImage = calendarItem.Series?.Images?.FirstOrDefault(img => 
                string.Equals(img.CoverType, "poster", StringComparison.OrdinalIgnoreCase));

            // Create provider IDs to store external image URL and metadata
            var providerIds = new Dictionary<string, string>
            {
                { "SonarrSeriesId", calendarItem.SeriesId.ToString() },
                { "SonarrEpisodeId", calendarItem.Id.ToString() },
                { "EpisodeInfo", episodeInfo },
                { "FormattedDate", formattedDate }
            };

            if (posterImage?.RemoteUrl != null)
            {
                providerIds.Add("SonarrPoster", posterImage.RemoteUrl);
            }

            return new BaseItemDto
            {
                Id = Guid.NewGuid(),
                Name = calendarItem.Series?.Title ?? "Unknown Series",
                Type = BaseItemKind.Episode,
                ServerId = Guid.NewGuid().ToString(),
                PremiereDate = calendarItem.AirDateUtc,
                
                // Series information
                SeriesName = calendarItem.Series?.Title,
                
                // Episode information  
                IndexNumber = calendarItem.EpisodeNumber,
                ParentIndexNumber = calendarItem.SeasonNumber,
                
                // Store external image URL and metadata in ProviderIds
                ProviderIds = providerIds,
                
                // Store formatted display information in UserData
                UserData = new UserItemDataDto
                {
                    Key = $"upcoming-{calendarItem.Id}",
                    PlaybackPositionTicks = 0,
                    IsFavorite = false
                }
            };
        }

        public IHomeScreenSection CreateInstance(Guid? userId, IEnumerable<IHomeScreenSection>? otherInstances = null)
        {
            return new UpcomingShowsSection(_userManager, _dtoService, _arrApiService, _logger)
            {
                DisplayText = DisplayText,
                AdditionalData = AdditionalData,
                OriginalPayload = OriginalPayload
            };
        }
        
        public HomeScreenSectionInfo GetInfo()
        {
            return new HomeScreenSectionInfo
            {
                Section = Section,
                DisplayText = DisplayText,
                AdditionalData = AdditionalData,
                Route = Route,
                Limit = Limit ?? 1,
                OriginalPayload = OriginalPayload,
                ViewMode = SectionViewMode.Landscape,
                ContainerClass = "upcoming-shows-section"
            };
        }
    }
}
