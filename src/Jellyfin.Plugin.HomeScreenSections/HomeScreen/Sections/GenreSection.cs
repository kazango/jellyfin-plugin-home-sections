using Jellyfin.Plugin.HomeScreenSections.Configuration;
using Jellyfin.Plugin.HomeScreenSections.JellyfinVersionSpecific;
using Jellyfin.Plugin.HomeScreenSections.Library;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Jellyfin.Plugin.HomeScreenSections.HomeScreen.Sections;

public class GenreSection : IHomeScreenSection
{
    public string? Section => "Genre";
    public string? DisplayText { get; set; } = "Genre";
    public int? Limit => 5;
    public string? Route => null;
    public string? AdditionalData { get; set; }
    public object? OriginalPayload => null;

    private readonly IUserManager m_userManager;
    private readonly ILibraryManager m_libraryManager;
    private readonly CollectionManagerProxy m_collectionManagerProxy;
    private readonly IUserDataManager m_userDataManager;
    private readonly IDtoService m_dtoService;
    
    private Dictionary<Guid, (string Genre, int Score)[]> m_userGenreCache = new Dictionary<Guid, (string Genre, int Score)[]>();
    
    public GenreSection(IUserManager userManager, ILibraryManager libraryManager, CollectionManagerProxy collectionManagerProxy,
        IUserDataManager userDataManager, IDtoService dtoService)
    {
        m_userManager = userManager;
        m_libraryManager = libraryManager;
        m_collectionManagerProxy = collectionManagerProxy;
        m_userDataManager = userDataManager;
        m_dtoService = dtoService;
    }
    
    public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload, IQueryCollection queryCollection)
    {
        if (payload.AdditionalData == null)
        {
            return new QueryResult<BaseItemDto>();
        }

        User? user = m_userManager.GetUserById(payload.UserId);

        Genre genre = m_libraryManager.GetGenre(payload.AdditionalData);
        
        DtoOptions? dtoOptions = new DtoOptions 
        { 
            Fields = new[] 
            { 
                ItemFields.PrimaryImageAspectRatio, 
                ItemFields.MediaSourceCount
            }
        };
        
        InternalItemsQuery? genreMovies = new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[]
            {
                BaseItemKind.Movie
            },
            OrderBy = new[] { (ItemSortBy.Random, SortOrder.Descending) },
            Limit = 16,
            ParentId = Guid.Empty,
            Recursive = true,
            DtoOptions = dtoOptions,
            Genres = new List<string> { genre.Name }
        };

        return new QueryResult<BaseItemDto>(m_dtoService.GetBaseItemDtos(m_libraryManager.GetItemList(genreMovies), dtoOptions, user));
    }

    public IHomeScreenSection CreateInstance(Guid? userId, IEnumerable<IHomeScreenSection>? otherInstances = null)
    {
        User? user = userId is null || userId.Value.Equals(default)
            ? null
            : m_userManager.GetUserById(userId.Value);

        if (user == null)
        {
            throw new Exception();
        }
        
        if ((otherInstances?.Count() ?? 0) == 0)
        {
            // If this is the "first" for this request, lets do the calculation for all of the genres and cache and ordered list to retrieve from
            m_userGenreCache.Remove(userId!.Value);
            
            m_userGenreCache.Add(userId!.Value, GetGenresForUser(user));
        }

        var userGenreScores = m_userGenreCache[userId!.Value];
        int totalScore = userGenreScores.Sum(x => x.Score);
        Random rnd = new Random();

        string? selectedGenre = null;
        bool foundNew = false;
        do
        {
            int randomScore = rnd.Next(0, totalScore);

            foreach (var userGenre in userGenreScores)
            {
                randomScore -= userGenre.Score;

                if (randomScore < 0)
                {
                    selectedGenre = userGenre.Genre;
                    break;
                }
            }

            if (!otherInstances.Any(x => x.AdditionalData == selectedGenre))
            {
                foundNew = true;
            }
        } while (!foundNew);

        GenreSection section = new GenreSection(m_userManager, m_libraryManager, m_collectionManagerProxy, m_userDataManager, m_dtoService)
        {
            AdditionalData = selectedGenre,
            DisplayText = $"{selectedGenre} Movies"
        };
        
        return section;
    }

    private (string Genre, int Score)[] GetGenresForUser(User user)
    {
        int likedScore = 100;
        int favouriteScore = 150;
        int recentlyWatchedScore = 50;
        int scorePerPlay = 1;
        
        DtoOptions? dtoOptions = new DtoOptions 
        { 
            Fields = new[] 
            { 
                ItemFields.PrimaryImageAspectRatio, 
                ItemFields.MediaSourceCount
            }
        };

        InternalItemsQuery? favoriteOrLikedQuery = new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[]
            {
                BaseItemKind.Movie
            },
            Limit = null,
            ParentId = Guid.Empty,
            Recursive = true,
            IsFavoriteOrLiked = true,
            DtoOptions = dtoOptions,
        };

        IEnumerable<BaseItem>? likedOrFavoritedMovies = m_libraryManager.GetItemList(favoriteOrLikedQuery);

        var scoredGenres = likedOrFavoritedMovies.OfType<Movie>().SelectMany(x =>
        {
            int score = 0;
            var userData = m_userDataManager.GetUserData(user, x);

            score += userData.IsFavorite ? favouriteScore : 0;
            score += (userData.Likes ?? false) ? likedScore : 0;
            score += userData.PlayCount * scorePerPlay;

            return x.Genres.Select(genre => new
            {
                Genre = genre,
                Score = score
            });
        }).GroupBy(x => x.Genre).Select(x => new
        {
            Genre = x.Key,
            Score = x.Sum(y => y.Score)
        });
        
        InternalItemsQuery? recentlyWatchedQuery = new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[]
            {
                BaseItemKind.Movie
            },
            OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending), (ItemSortBy.Random, SortOrder.Descending) },
            Limit = 7,
            ParentId = Guid.Empty,
            Recursive = true,
            IsPlayed = true,
            IsFavoriteOrLiked = false, // Ignore all the favourited and liked movies as we've already catered for them and their watches.
            DtoOptions = dtoOptions
        };

        var recentlyPlayedMovies = m_libraryManager.GetItemList(recentlyWatchedQuery)
            .SelectMany(x =>
            {
                int score = 0;
                var userData = m_userDataManager.GetUserData(user, x);

                score += recentlyWatchedScore;
                score += userData.PlayCount * scorePerPlay;

                return x.Genres.Select(genre => new
                {
                    Genre = genre,
                    Score = score
                });
            }).GroupBy(x => x.Genre).Select(x => new
            {
                Genre = x.Key,
                Score = x.Sum(y => y.Score)
            });

        scoredGenres = scoredGenres.Concat(recentlyPlayedMovies);

        return scoredGenres.Select(x => (x.Genre, x.Score)).ToArray();
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