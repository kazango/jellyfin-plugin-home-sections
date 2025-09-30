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
    public class UpcomingMusicSection : UpcomingSectionBase<LidarrCalendarDto>
    {
        public override string? Section => "UpcomingMusic";
        
        public override string? DisplayText { get; set; } = "Upcoming Music";

        public UpcomingMusicSection(
            IUserManager userManager,
            IDtoService dtoService,
            ArrApiService arrApiService,
            ILogger<UpcomingMusicSection> logger)
            : base(userManager, dtoService, arrApiService, logger)
        {
        }

        protected override (string? url, string? apiKey) GetServiceConfiguration(PluginConfiguration config)
        {
            return (config.LidarrUrl, config.LidarrApiKey);
        }

        protected override (int value, TimeframeUnit unit) GetTimeframeConfiguration(PluginConfiguration config)
        {
            return (config.UpcomingMusicTimeframeValue, config.UpcomingMusicTimeframeUnit);
        }

        protected override LidarrCalendarDto[] GetCalendarItems(DateTime startDate, DateTime endDate)
        {
            return ArrApiService.GetLidarrCalendarAsync(startDate, endDate).GetAwaiter().GetResult() ?? Array.Empty<LidarrCalendarDto>();
        }

        protected override IOrderedEnumerable<LidarrCalendarDto> FilterAndSortItems(LidarrCalendarDto[] items)
        {
            return items
                .Where(item => item.Monitored && !item.HasFile && item.ReleaseDate.HasValue)
                .OrderBy(item => item.ReleaseDate);
        }

        protected override BaseItemDto CreateDto(LidarrCalendarDto calendarItem, PluginConfiguration config)
        {

            var releaseDate = calendarItem.ReleaseDate ?? DateTime.Now;
            var countdownText = CalculateCountdown(releaseDate, config);

            var albumImage = calendarItem.Images?.FirstOrDefault(img => 
                string.Equals(img.CoverType, "cover", StringComparison.OrdinalIgnoreCase));

            var providerIds = new Dictionary<string, string>()
            {
                { "LidarrAlbumId", calendarItem.Id.ToString() },
                { "FormattedDate", countdownText },
                { "LidarrPoster", albumImage?.RemoteUrl ?? GetFallbackCoverUrl(calendarItem) }
            };

            return new BaseItemDto
            {
                Id = Guid.NewGuid(),
                Name = calendarItem.Title ?? "Unknown Album",
                Overview = $"{calendarItem.Artist?.ArtistName ?? "Unknown Artist"} - {calendarItem.AlbumType}",
                PremiereDate = calendarItem.ReleaseDate,
                Type = BaseItemKind.MusicAlbum,
                UserData = new UserItemDataDto
                {
                    Key = $"upcoming-album-{calendarItem.Id}",
                    PlaybackPositionTicks = 0,
                    IsFavorite = false,
                }
            };
        }

        protected override string GetServiceName() => "Lidarr";
        protected override string GetSectionName() => "upcoming music";

        public override IHomeScreenSection CreateInstance(Guid? userId, IEnumerable<IHomeScreenSection>? otherInstances = null)
        {
            return new UpcomingMusicSection(UserManager, DtoService, ArrApiService, (ILogger<UpcomingMusicSection>)Logger)
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
                ViewMode = SectionViewMode.Square,
                AllowViewModeChange = false,
                ContainerClass = "upcoming-music-section"
            };
        }
    }
}