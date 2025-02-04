using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.HomeScreenSections.Library;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.HomeScreenSections.HomeScreen.Sections
{
    public class LatestShowsSection : IHomeScreenSection
    {
        public string? Section => "LatestShows";
        
        public string? DisplayText { get; set; } = "Latest Shows";
        
        public int? Limit => 1;
        
        public string? Route { get; }
        
        public string? AdditionalData { get; set; }
        
        public object? OriginalPayload { get; }
        
        private readonly IUserViewManager m_userViewManager;
        private readonly IUserManager m_userManager;
        private readonly ILibraryManager m_libraryManager;
        private readonly ITVSeriesManager m_tvSeriesManager;
        private readonly IDtoService m_dtoService;

        public LatestShowsSection(IUserViewManager userViewManager,
            IUserManager userManager,
            ILibraryManager libraryManager,
            ITVSeriesManager tvSeriesManager,
            IDtoService dtoService)
        {
            m_userViewManager = userViewManager;
            m_userManager = userManager;
            m_libraryManager = libraryManager;
            m_tvSeriesManager = tvSeriesManager;
            m_dtoService = dtoService;
        }
        
        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload)
        {
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
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.Thumb
            };
            
            User? user = m_userManager.GetUserById(payload.UserId);

            List<BaseItem> episodes = m_libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Episode },
                OrderBy = new[] { (ItemSortBy.PremiereDate, SortOrder.Descending) },
                DtoOptions = new DtoOptions
                    { Fields = new[] { ItemFields.SeriesPresentationUniqueKey }, EnableImages = false }
            });
            
            List<BaseItem> series = episodes.Select(x => (x.FindParent<Series>(), x)).GroupBy(x => x.Item1).Select(x => x.Key as BaseItem).ToList();
            
            return new QueryResult<BaseItemDto>(Array.ConvertAll(series.ToArray(),
                i => m_dtoService.GetBaseItemDto(i, dtoOptions, user)));
        }

        public IHomeScreenSection CreateInstance(Guid? userId, IEnumerable<IHomeScreenSection>? otherInstances = null)
        {
            return this;
        }
    }
}