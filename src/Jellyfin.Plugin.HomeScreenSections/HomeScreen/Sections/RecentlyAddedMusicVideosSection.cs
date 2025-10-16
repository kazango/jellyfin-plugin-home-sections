using Jellyfin.Plugin.HomeScreenSections.Configuration;
using Jellyfin.Plugin.HomeScreenSections.Library;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.HomeScreenSections.HomeScreen.Sections
{
    /// <summary>
    /// Latest Music Videos Section.
    /// </summary>
    public class RecentlyAddedMusicVideosSection : IHomeScreenSection
    {
        /// <inheritdoc/>
        public string? Section => "RecentlyAddedMusicVideos";

        /// <inheritdoc/>
        public string? DisplayText { get; set; } = "Recently Added MusicVideos";

        /// <inheritdoc/>
        public int? Limit => 1;

        /// <inheritdoc/>
        public string? Route => "musicvideos";

        /// <inheritdoc/>
        public string? AdditionalData { get; set; } = "musicvideos";

        public object? OriginalPayload { get; set; } = null;
        
        private readonly IUserViewManager m_userViewManager;
        private readonly IUserManager m_userManager;
        private readonly ILibraryManager m_libraryManager;
        private readonly IDtoService m_dtoService;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="userViewManager">Instance of <see href="IUserViewManager" /> interface.</param>
        /// <param name="userManager">Instance of <see href="IUserManager" /> interface.</param>
        /// <param name="dtoService">Instance of <see href="IDtoService" /> interface.</param>
        public RecentlyAddedMusicVideosSection(IUserViewManager userViewManager,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IDtoService dtoService)
        {
            m_userViewManager = userViewManager;
            m_userManager = userManager;
            m_libraryManager = libraryManager;
            m_dtoService = dtoService;
        }

        /// <inheritdoc/>
        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload, IQueryCollection queryCollection)
        {
            User? user = m_userManager.GetUserById(payload.UserId);

            DtoOptions? dtoOptions = new DtoOptions
            {
                Fields = new List<ItemFields>
                {
                    ItemFields.PrimaryImageAspectRatio,
                    ItemFields.Path
                }
            };

            dtoOptions.ImageTypeLimit = 1;
            dtoOptions.ImageTypes = new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Backdrop,
                ImageType.Primary,
            };

            var config = HomeScreenSectionsPlugin.Instance?.Configuration;
            var sectionSettings = config?.SectionSettings.FirstOrDefault(x => x.SectionId == Section);
            // If HideWatchedItems is enabled for this section, set isPlayed to false to hide watched items; otherwise, include all.
            bool? isPlayed = sectionSettings?.HideWatchedItems == true ? false : null;

            IReadOnlyList<BaseItem> recentlyAddedMusicVideos = m_libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[]
                {
                    BaseItemKind.MusicVideo
                },
                Limit = 16,
                OrderBy = new[]
                {
                    (ItemSortBy.DateCreated, SortOrder.Descending)
                },
                DtoOptions = dtoOptions,
                IsPlayed = isPlayed
            });

            return new QueryResult<BaseItemDto>(Array.ConvertAll(recentlyAddedMusicVideos.ToArray(),
                i => m_dtoService.GetBaseItemDto(i, dtoOptions, user)));
        }

        /// <inheritdoc/>
        public IHomeScreenSection CreateInstance(Guid? userId, IEnumerable<IHomeScreenSection>? otherInstances = null)
        {
            User? user = m_userManager.GetUserById(userId ?? Guid.Empty);

            BaseItemDto? originalPayload = null;
            
            // Get only music video collection folders that the user can access
            var musicvideoFolders = m_libraryManager.GetUserRootFolder()
                .GetChildren(user, true)
                .OfType<Folder>()
                .Where(x => (x as ICollectionFolder)?.CollectionType == CollectionType.musicvideos)
                .ToArray();
            
            // Check if there's a configured default library, otherwise use first available
            var config = HomeScreenSectionsPlugin.Instance?.Configuration;
            var folder = !string.IsNullOrEmpty(config?.DefaultMusicVideosLibraryId)
                ? musicvideoFolders.FirstOrDefault(x => x.Id.ToString() == config.DefaultMusicVideosLibraryId)
                : null;
            
            // Fall back to first music videos library if no configured library found
            folder ??= musicvideoFolders.FirstOrDefault();
            
            if (folder != null)
            {
                DtoOptions dtoOptions = new DtoOptions();
                dtoOptions.Fields =
                    [..dtoOptions.Fields, ItemFields.PrimaryImageAspectRatio, ItemFields.DisplayPreferencesId];

                originalPayload = Array.ConvertAll(new[] { folder }, i => m_dtoService.GetBaseItemDto(i, dtoOptions, user)).First();
            }

            return new RecentlyAddedMusicVideosSection(m_userViewManager, m_userManager, m_libraryManager, m_dtoService)
            {
                AdditionalData = AdditionalData,
                DisplayText = DisplayText,
                OriginalPayload = originalPayload
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
                AllowHideWatched = true
            };
        }
    }
}
