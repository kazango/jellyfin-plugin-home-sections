using System.ComponentModel.DataAnnotations;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Jellyfin.Plugins.HomeScreenSections.Library;
using Jellyfin.Plugins.HomeScreenSections.Model.Dto;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugins.HomeScreenSections.Controllers
{
    /// <summary>
    /// API controller for the Modular Home Screen.
    /// </summary>
    public class HomeScreenController : ControllerBase
    {
        private readonly IHomeScreenManager m_homeScreenManager;
        private readonly IDisplayPreferencesManager m_displayPreferencesManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvShowsController"/> class.
        /// </summary>
        /// <param name="homeScreenManager">Instance of the <see cref="IHomeScreenManager"/> interface.</param>
        /// <param name="displayPreferencesManager">Instance of the <see cref="IDisplayPreferencesManager" /> interface.</param>
        public HomeScreenController(
            IHomeScreenManager homeScreenManager,
            IDisplayPreferencesManager displayPreferencesManager)
        {
            m_homeScreenManager = homeScreenManager;
            m_displayPreferencesManager = displayPreferencesManager;
        }

        /// <summary>
        /// Get what home screen sections the user has enabled.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns></returns>
        [HttpGet("Sections")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<HomeScreenSectionInfo>> GetHomeScreenSections(
            [FromQuery] Guid? userId)
        {
            string displayPreferencesId = "usersettings";
            Guid itemId = displayPreferencesId.GetMD5();

            DisplayPreferences displayPreferences = m_displayPreferencesManager.GetDisplayPreferences(userId ?? Guid.Empty, itemId, "emby");
            ModularHomeUserSettings? settings = m_homeScreenManager.GetUserSettings(userId ?? Guid.Empty);

            List<IHomeScreenSection> sectionTypes = m_homeScreenManager.GetSectionTypes().Where(x => settings?.EnabledSections.Contains(x.Section ?? string.Empty) ?? false).ToList();

            List<IHomeScreenSection> sectionInstances = new List<IHomeScreenSection>();

            List<string> homeSectionOrderTypes = new List<string>();
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

            foreach (string type in homeSectionOrderTypes)
            {
                IHomeScreenSection? sectionType = sectionTypes.FirstOrDefault(x => x.Section == type);

                if (sectionType != null)
                {
                    if (sectionType.Limit > 1)
                    {
                        Random rnd = new Random();
                        int instanceCount = rnd.Next(0, sectionType.Limit ?? 1) + 1;

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

            List<IHomeScreenSection> pluginSections = new List<IHomeScreenSection>(); // we want these randomly distributed among each other.

            foreach (IHomeScreenSection sectionType in sectionTypes)
            {
                if (sectionType.Limit > 1)
                {
                    Random rnd = new Random();
                    int instanceCount = rnd.Next(0, sectionType.Limit ?? 1) + 1;

                    for (int i = 0; i < instanceCount; ++i)
                    {
                        pluginSections.Add(sectionType.CreateInstance(userId, sectionInstances.Where(x => x.GetType() == sectionType.GetType())));
                    }
                }
                else if (sectionType.Limit == 1)
                {
                    pluginSections.Add(sectionType.CreateInstance(userId));
                }
            }

            pluginSections.Shuffle();

            sectionInstances.AddRange(pluginSections);

            List<HomeScreenSectionInfo> sections = sectionInstances.Where(x => x != null).Select(x => x.AsInfo()).ToList();

            return new QueryResult<HomeScreenSectionInfo>(
                0,
                sections.Count,
                sections);
        }

        /// <summary>
        /// Get the content for the home screen section based on <paramref name="sectionType"/>.
        /// </summary>
        /// <param name="sectionType">The section type.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="additionalData">Any additional data this section is showing.</param>
        /// <returns></returns>
        [HttpGet("Section/{sectionType}")]
        public QueryResult<BaseItemDto> GetSectionContent(
            [FromRoute] string sectionType,
            [FromQuery, Required] Guid userId,
            [FromQuery] string? additionalData)
        {
            HomeScreenSectionPayload payload = new HomeScreenSectionPayload
            {
                UserId = userId,
                AdditionalData = additionalData
            };

            return m_homeScreenManager.InvokeResultsDelegate(sectionType, payload);
        }
    }
}
