using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugins.HomeScreenSections.Library;
using Jellyfin.Plugins.HomeScreenSections.Model.Dto;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugins.HomeScreenSections.HomeScreen.Sections
{
    /// <summary>
    /// Latest Movies Section.
    /// </summary>
    public class LatestMoviesSection : IHomeScreenSection
    {
        /// <inheritdoc/>
        public string? Section => "LatestMovies";

        /// <inheritdoc/>
        public string? DisplayText { get; set; } = "Latest Movies";

        /// <inheritdoc/>
        public int? Limit => 1;

        /// <inheritdoc/>
        public string? Route => "movies";

        /// <inheritdoc/>
        public string? AdditionalData { get; set; } = "movies";

        private readonly IUserViewManager m_userViewManager;
        private readonly IUserManager m_userManager;
        private readonly IDtoService m_dtoService;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="userViewManager">Instance of <see href="IUserViewManager" /> interface.</param>
        /// <param name="userManager">Instance of <see href="IUserManager" /> interface.</param>
        /// <param name="dtoService">Instance of <see href="IDtoService" /> interface.</param>
        public LatestMoviesSection(IUserViewManager userViewManager,
            IUserManager userManager,
            IDtoService dtoService)
        {
            m_userViewManager = userViewManager;
            m_userManager = userManager;
            m_dtoService = dtoService;
        }

        /// <inheritdoc/>
        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload)
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
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.Thumb
            };

            MyMediaSection myMedia = new MyMediaSection(m_userViewManager, m_userManager, m_dtoService);
            QueryResult<BaseItemDto> media = myMedia.GetResults(payload);

            Guid parentId = media.Items.FirstOrDefault(x => x.Name == payload.AdditionalData)?.Id ?? Guid.Empty;

            List<Tuple<BaseItem, List<BaseItem>>>? list = m_userViewManager.GetLatestItems(
                new LatestItemsQuery
                {
                    GroupItems = false,
                    Limit = 16,
                    ParentId = parentId,
                    User = user,
                    IncludeItemTypes = new BaseItemKind[]
                    {
                        BaseItemKind.Movie
                    }
                },
                dtoOptions);

            IEnumerable<BaseItemDto>? dtos = list.Select(i =>
            {
                BaseItem? item = i.Item2[0];
                int childCount = 0;

                if (i.Item1 != null && (i.Item2.Count > 1 || i.Item1 is MusicAlbum))
                {
                    item = i.Item1;
                    childCount = i.Item2.Count;
                }

                BaseItemDto? dto = m_dtoService.GetBaseItemDto(item, dtoOptions, user);

                dto.ChildCount = childCount;

                return dto;
            });

            return new QueryResult<BaseItemDto>(dtos.ToList());
        }

        /// <inheritdoc/>
        public IHomeScreenSection CreateInstance(Guid? userId, IEnumerable<IHomeScreenSection>? otherInstances = null)
        {
            return this;
        }
    }
}
