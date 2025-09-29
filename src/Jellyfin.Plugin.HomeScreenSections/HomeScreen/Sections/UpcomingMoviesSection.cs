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
    public class UpcomingMoviesSection : IHomeScreenSection
    {
        public string? Section => "UpcomingMovies";
        
        public string? DisplayText { get; set; } = "Upcoming Movies";
        
        public int? Limit => 1;
        
        public string? Route { get; }
        
        public string? AdditionalData { get; set; }

        public object? OriginalPayload { get; set; } = null;
        
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly ArrApiService _arrApiService;
        private readonly ILogger<UpcomingMoviesSection> _logger;

        public UpcomingMoviesSection(
            IUserManager userManager,
            IDtoService dtoService,
            ArrApiService arrApiService,
            ILogger<UpcomingMoviesSection> logger)
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

                // Check if Radarr is configured
                if (string.IsNullOrEmpty(config.RadarrUrl) || string.IsNullOrEmpty(config.RadarrApiKey))
                {
                    _logger.LogWarning("Radarr URL or API key not configured, skipping upcoming movies");
                    return new QueryResult<BaseItemDto>();
                }

                var startDate = DateTime.UtcNow;
                var endDate = _arrApiService.CalculateEndDate(startDate, config.UpcomingMoviesTimeframeValue, config.UpcomingMoviesTimeframeUnit);
                
                _logger.LogDebug("Fetching upcoming movies from {StartDate} to {EndDate}", startDate, endDate);

                var calendarItems = _arrApiService.GetRadarrCalendarAsync(startDate, endDate).GetAwaiter().GetResult();
                
                if (calendarItems == null || calendarItems.Length == 0)
                {
                    _logger.LogDebug("No upcoming movies found from Radarr");
                    return new QueryResult<BaseItemDto>();
                }

                var upcomingItems = calendarItems
                    .Where(item => item.Monitored && !item.HasFile && item.DigitalRelease.HasValue)
                    .OrderBy(item => item.DigitalRelease)
                    .Take(16)
                    .ToArray();

                _logger.LogDebug("Found {Count} upcoming movies after filtering", upcomingItems.Length);

                var dtoItems = upcomingItems.Select(item => CreateUpcomingMovieDto(item, config)).ToArray();

                return new QueryResult<BaseItemDto>(dtoItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching upcoming movies from Radarr");
                return new QueryResult<BaseItemDto>();
            }
        }

        private BaseItemDto CreateUpcomingMovieDto(RadarrCalendarDto calendarItem, PluginConfiguration config)
        {
            var formattedDate = _arrApiService.FormatDate(
                calendarItem.DigitalRelease?.ToLocalTime() ?? DateTime.Now,
                config.DateFormat,
                config.DateDelimiter);

            var yearInfo = calendarItem.Year > 0 ? $" ({calendarItem.Year})" : "";

            var posterImage = calendarItem.Images?.FirstOrDefault(img => 
                string.Equals(img.CoverType, "poster", StringComparison.OrdinalIgnoreCase));

            // Create provider IDs to store external image URL and metadata
            var providerIds = new Dictionary<string, string>
            {
                { "RadarrMovieId", calendarItem.Id.ToString() },
                { "YearInfo", yearInfo },
                { "FormattedDate", formattedDate }
            };

            if (posterImage?.RemoteUrl != null)
            {
                providerIds.Add("RadarrPoster", posterImage.RemoteUrl);
            }

            return new BaseItemDto
            {
                Id = Guid.NewGuid(),
                Name = calendarItem.Title ?? "Unknown Movie",
                Type = BaseItemKind.Movie,
                ServerId = Guid.NewGuid().ToString(),
                PremiereDate = calendarItem.DigitalRelease,
                
                // Movie information
                ProductionYear = calendarItem.Year > 0 ? calendarItem.Year : null,
                
                // Store external image URL and metadata in ProviderIds
                ProviderIds = providerIds,
                
                // Store formatted display information in UserData
                UserData = new UserItemDataDto
                {
                    Key = $"upcoming-movie-{calendarItem.Id}",
                    PlaybackPositionTicks = 0,
                    IsFavorite = false
                }
            };
        }

        public IHomeScreenSection CreateInstance(Guid? userId, IEnumerable<IHomeScreenSection>? otherInstances = null)
        {
            return new UpcomingMoviesSection(_userManager, _dtoService, _arrApiService, _logger)
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
                ViewMode = SectionViewMode.Portrait,
                AllowViewModeChange = false,
                ContainerClass = "upcoming-movies-section"
            };
        }
    }
}