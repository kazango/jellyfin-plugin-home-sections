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
    public class UpcomingShowsSection : UpcomingSectionBase<SonarrCalendarDto>
    {
        public override string? Section => "UpcomingShows";
        
        public override string? DisplayText { get; set; } = "Upcoming Shows";

        public UpcomingShowsSection(
            IUserManager userManager,
            IDtoService dtoService,
            ArrApiService arrApiService,
            ILogger<UpcomingShowsSection> logger)
            : base(userManager, dtoService, arrApiService, logger)
        {
        }

        protected override (string? url, string? apiKey) GetServiceConfiguration(PluginConfiguration config)
        {
            return (config.SonarrUrl, config.SonarrApiKey);
        }

        protected override (int value, TimeframeUnit unit) GetTimeframeConfiguration(PluginConfiguration config)
        {
            return (config.UpcomingShowsTimeframeValue, config.UpcomingShowsTimeframeUnit);
        }

        protected override SonarrCalendarDto[] GetCalendarItems(DateTime startDate, DateTime endDate)
        {
            return ArrApiService.GetSonarrCalendarAsync(startDate, endDate).GetAwaiter().GetResult() ?? Array.Empty<SonarrCalendarDto>();
        }

        protected override IOrderedEnumerable<SonarrCalendarDto> FilterAndSortItems(SonarrCalendarDto[] items)
        {
            return items
                .Where(item => item.Monitored && !item.HasFile && item.AirDateUtc.HasValue)
                .OrderBy(item => item.AirDateUtc);
        }

        protected override BaseItemDto CreateDto(SonarrCalendarDto calendarItem, PluginConfiguration config)
        {
            var airDate = calendarItem.AirDateUtc ?? DateTime.Now;
            var countdownText = CalculateCountdown(airDate, config);

            var episodeInfo = $"S{calendarItem.SeasonNumber:D2}E{calendarItem.EpisodeNumber:D2} - {calendarItem.Title}";

            var posterImage = calendarItem.Series?.Images?.FirstOrDefault(img => 
                string.Equals(img.CoverType, "poster", StringComparison.OrdinalIgnoreCase));

            // Create provider IDs to store external image URL and metadata
            var providerIds = new Dictionary<string, string>
            {
                { "SonarrSeriesId", calendarItem.SeriesId.ToString() },
                { "SonarrEpisodeId", calendarItem.Id.ToString() },
                { "EpisodeInfo", episodeInfo },
                { "FormattedDate", countdownText }
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

        protected override string GetServiceName() => "Sonarr";

        protected override string GetSectionName() => "upcoming shows";

        public override IHomeScreenSection CreateInstance(Guid? userId, IEnumerable<IHomeScreenSection>? otherInstances = null)
        {
            return new UpcomingShowsSection(UserManager, DtoService, ArrApiService, (ILogger<UpcomingShowsSection>)Logger)
            {
                DisplayText = DisplayText,
                AdditionalData = AdditionalData,
                OriginalPayload = OriginalPayload
            };
        }
        
        public override HomeScreenSectionInfo GetInfo()
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
                ContainerClass = "upcoming-shows-section"
            };
        }
    }
}
