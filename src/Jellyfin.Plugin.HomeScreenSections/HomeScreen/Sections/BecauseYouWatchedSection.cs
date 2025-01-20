using Jellyfin.Data.Enums;
using Jellyfin.Plugin.HomeScreenSections.Library;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.HomeScreenSections.HomeScreen.Sections
{
	public class BecauseYouWatchedSection : IHomeScreenSection
	{
		public string? Section => "BecauseYouWatched";

		public string? DisplayText { get; set; } = "Because You Watched";

		public int? Limit => 5;

		public string? Route => null;

		public string? AdditionalData { get; set; }

		private IUserDataManager UserDataManager { get; set; }
		private IUserManager UserManager { get; set; }
		private ILibraryManager LibraryManager { get; set; }
		private IDtoService DtoService { get; set; }
		private ICollectionManager CollectionManager { get; set; }
		private CollectionManagerProxy CollectionManagerProxy { get; set; }

		public BecauseYouWatchedSection(IUserDataManager userDataManager, IUserManager userManager, ILibraryManager libraryManager, 
			IDtoService dtoService, ICollectionManager collectionManager, CollectionManagerProxy collectionProxy)
		{
			UserDataManager = userDataManager;
			UserManager = userManager;
			LibraryManager = libraryManager;
			DtoService = dtoService;
			CollectionManager = collectionManager;
			CollectionManagerProxy = collectionProxy;
		}

		public IHomeScreenSection CreateInstance(Guid? userId, IEnumerable<IHomeScreenSection>? otherInstances = null)
		{
			Jellyfin.Data.Entities.User? user = userId is null || userId.Value.Equals(default)
				? null
				: UserManager.GetUserById(userId.Value);

			BecauseYouWatchedSection section = new BecauseYouWatchedSection(UserDataManager, UserManager, LibraryManager, DtoService, CollectionManager, CollectionManagerProxy);

			var dtoOptions = new DtoOptions 
			{ 
				Fields = new[] 
				{ 
					ItemFields.PrimaryImageAspectRatio, 
					ItemFields.MediaSourceCount
				}
			};

			var query = new InternalItemsQuery(user)
			{
				IncludeItemTypes = new[]
				{
					BaseItemKind.Movie,
                    // nameof(Trailer),
                    // nameof(LiveTvProgram)
                },
				// IsMovie = true
				OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending), (ItemSortBy.Random, SortOrder.Descending) },
				Limit = 7,
				ParentId = Guid.Empty,
				Recursive = true,
				IsPlayed = true,
				DtoOptions = dtoOptions
			};

			List<BaseItem>? recentlyPlayedMovies = LibraryManager.GetItemList(query);

			recentlyPlayedMovies = recentlyPlayedMovies.Where(x => !otherInstances?.Select(y => y.AdditionalData).Contains(x.Id.ToString()) ?? true).Where(x =>
			{
				var collections = CollectionManagerProxy.GetCollections(user).Where(y => y.GetChildren(user, true).OfType<Movie>().Contains(x as Movie));

				foreach (BoxSet? collection in collections)
				{
					if (collection.GetChildren(user, true).OfType<Movie>().Any(y => otherInstances?.Select(z => z.AdditionalData).Contains(y.Id.ToString()) ?? true))
					{
						return false;
					}
				}

				return true;
			}).ToList();

			Random rnd = new Random();

			if (recentlyPlayedMovies.Count == 0)
			{
				return null;
			}

			BaseItem item = recentlyPlayedMovies.ElementAt(rnd.Next(0, recentlyPlayedMovies.Count));

			section.AdditionalData = item.Id.ToString();
			section.DisplayText = "Because You Watched " + item.Name;

			return section;
		}

		public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload)
		{
			var dtoOptions = new DtoOptions
			{
				Fields = new[]
				{
					ItemFields.PrimaryImageAspectRatio,
					ItemFields.MediaSourceCount
				},
				ImageTypes = new[]
				{
					ImageType.Primary,
					ImageType.Backdrop,
					ImageType.Banner,
					ImageType.Thumb
				},
				ImageTypeLimit = 1
			};

			BaseItem item = LibraryManager.GetItemById(Guid.Parse(payload.AdditionalData ?? Guid.Empty.ToString()));

			var similar = LibraryManager.GetItemList(new InternalItemsQuery(UserManager.GetUserById(payload.UserId))
			{
				Limit = 8,
				IncludeItemTypes = new[]
				{
					BaseItemKind.Movie
				},
				IsMovie = true,
				SimilarTo = item,
				EnableGroupByMetadataKey = true,
				DtoOptions = dtoOptions
			});

			return new QueryResult<BaseItemDto>(DtoService.GetBaseItemDtos(similar, dtoOptions));
		}
	}
}
