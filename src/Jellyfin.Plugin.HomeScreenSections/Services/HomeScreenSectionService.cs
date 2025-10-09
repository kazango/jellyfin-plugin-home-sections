using Jellyfin.Extensions;
using Jellyfin.Plugin.HomeScreenSections.Configuration;
using Jellyfin.Plugin.HomeScreenSections.Helpers;
using Jellyfin.Plugin.HomeScreenSections.Library;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.HomeScreenSections.Services;

public class HomeScreenSectionService
{
    private readonly IDisplayPreferencesManager m_displayPreferencesManager;
    private readonly IHomeScreenManager m_homeScreenManager;

    public HomeScreenSectionService(IDisplayPreferencesManager displayPreferencesManager,
        IHomeScreenManager homeScreenManager)
    {
        m_displayPreferencesManager = displayPreferencesManager;
        m_homeScreenManager = homeScreenManager;
    }
    
    public List<HomeScreenSectionInfo> GetSectionsFromRequest(Guid? userId, string? language)
    {
        string displayPreferencesId = "usersettings";
        Guid itemId = displayPreferencesId.GetMD5();

        DisplayPreferences displayPreferences = m_displayPreferencesManager.GetDisplayPreferences(userId ?? Guid.Empty, itemId, "emby");
        ModularHomeUserSettings? settings = m_homeScreenManager.GetUserSettings(userId ?? Guid.Empty);

        List<IHomeScreenSection> sectionTypes = m_homeScreenManager.GetSectionTypes().Where(x => settings?.EnabledSections.Contains(x.Section ?? string.Empty) ?? false).ToList();

        List<IHomeScreenSection> sectionInstances = new List<IHomeScreenSection>();

        List<string> homeSectionOrderTypes = new List<string>();
        if (HomeScreenSectionsPlugin.Instance.Configuration.AllowUserOverride)
        {
            foreach (HomeSection section in displayPreferences.HomeSections.OrderBy(x => x.Order))
            {
                switch (section.Type)
                {
                    case HomeSectionType.SmallLibraryTiles:
                        homeSectionOrderTypes.Add("MyMedia");
                        break;
                    case HomeSectionType.Resume:
                        homeSectionOrderTypes.Add("ContinueWatching");
                        break;
                    case HomeSectionType.LatestMedia:
                        homeSectionOrderTypes.Add("LatestMovies");
                        homeSectionOrderTypes.Add("LatestShows");
                        break;
                    case HomeSectionType.NextUp:
                        homeSectionOrderTypes.Add("NextUp");
                        break;
                }
            }
        }

        foreach (string type in homeSectionOrderTypes)
        {
            IHomeScreenSection? sectionType = sectionTypes.FirstOrDefault(x => x.Section == type);

            if (sectionType != null)
            {
                if (sectionType.Limit > 1)
                {
                    SectionSettings? sectionSettings = HomeScreenSectionsPlugin.Instance.Configuration.SectionSettings.FirstOrDefault(x =>
                        x.SectionId == sectionType.Section);

                    Random rnd = new Random();
                    int instanceCount = rnd.Next(sectionSettings?.LowerLimit ?? 0, sectionSettings?.UpperLimit ?? sectionType.Limit ?? 1);

                    for (int i = 0; i < instanceCount; ++i)
                    {
                        sectionInstances.Add(sectionType.CreateInstance(userId, sectionInstances.Where(x => x.GetType() == sectionType.GetType())));
                    }
                }
                else if (sectionType.Limit == 1)
                {
                    sectionInstances.Add(sectionType.CreateInstance(userId));
                }
            }
        }

        sectionTypes.RemoveAll(x => homeSectionOrderTypes.Contains(x.Section ?? string.Empty));

        IEnumerable<IGrouping<int, SectionSettings>> groupedOrderedSections = HomeScreenSectionsPlugin.Instance.Configuration.SectionSettings
            .OrderBy(x => x.OrderIndex)
            .GroupBy(x => x.OrderIndex);

        foreach (IGrouping<int, SectionSettings> orderedSections in groupedOrderedSections)
        {
            List<IHomeScreenSection> tmpPluginSections = new List<IHomeScreenSection>(); // we want these randomly distributed among each other.
            
            foreach (SectionSettings sectionSettings in orderedSections)
            {
                IHomeScreenSection? sectionType = sectionTypes.FirstOrDefault(x => x.Section == sectionSettings.SectionId);

                if (sectionType != null)
                {
                    if (sectionType.Limit > 1)
                    {
                        Random rnd = new Random();
                        int instanceCount = rnd.Next(sectionSettings?.LowerLimit ?? 0, sectionSettings?.UpperLimit ?? sectionType.Limit ?? 1);
                        
                        for (int i = 0; i < instanceCount; ++i)
                        {
                            IHomeScreenSection[] tmpSectionInstances = tmpPluginSections.Where(x => x?.GetType() == sectionType.GetType())
                                .Concat(sectionInstances.Where(x => x.GetType() == sectionType.GetType())).ToArray();
                        
                            tmpPluginSections.Add(sectionType.CreateInstance(userId, tmpSectionInstances));
                        }
                    }
                    else if (sectionType.Limit == 1)
                    {
                        tmpPluginSections.Add(sectionType.CreateInstance(userId));
                    }
                }
            }
            
            tmpPluginSections.Shuffle();
            
            sectionInstances.AddRange(tmpPluginSections);
        }
        
        List<HomeScreenSectionInfo> sections = sectionInstances.Where(x => x != null).Select(x =>
        {
            HomeScreenSectionInfo info = x.AsInfo();

            info.ViewMode = HomeScreenSectionsPlugin.Instance.Configuration.SectionSettings.FirstOrDefault(x => x.SectionId == info.Section)?.ViewMode ?? info.ViewMode ?? SectionViewMode.Landscape;
            
            if (language != "en" && !string.IsNullOrEmpty(language?.Trim()) &&
                info.DisplayText != null)
            {
                string? translatedResult = TranslationHelper.TranslateAsync(info.DisplayText, "en", language.Trim())
                    .GetAwaiter().GetResult();

                info.DisplayText = translatedResult;
            }
            
            return info;
        }).ToList();

        return sections;
    }

    public QueryResult<BaseItemDto> GetSectionResults(Guid userId, string? additionalData, string sectionType, HttpRequest request)
    {
        HomeScreenSectionPayload payload = new HomeScreenSectionPayload
        {
            UserId = userId,
            AdditionalData = additionalData
        };

        return m_homeScreenManager.InvokeResultsDelegate(sectionType, payload, request.Query);
    }
}