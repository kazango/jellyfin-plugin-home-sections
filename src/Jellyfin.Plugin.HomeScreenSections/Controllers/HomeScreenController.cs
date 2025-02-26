using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Jellyfin.Plugin.HomeScreenSections.HomeScreen.Sections;
using Jellyfin.Plugin.HomeScreenSections.Library;
using Jellyfin.Plugin.HomeScreenSections.Model;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.HomeScreenSections.Controllers
{
    /// <summary>
    /// API controller for the Modular Home Screen.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HomeScreenController : ControllerBase
    {
        private readonly IHomeScreenManager m_homeScreenManager;
        private readonly IDisplayPreferencesManager m_displayPreferencesManager;
        private readonly IServerApplicationHost m_serverApplicationHost;
        private readonly IApplicationPaths m_applicationPaths;

        public HomeScreenController(
            IHomeScreenManager homeScreenManager,
            IDisplayPreferencesManager displayPreferencesManager,
            IServerApplicationHost serverApplicationHost, 
            IApplicationPaths applicationPaths)
        {
            m_homeScreenManager = homeScreenManager;
            m_displayPreferencesManager = displayPreferencesManager;
            m_serverApplicationHost = serverApplicationHost;
            m_applicationPaths = applicationPaths;
        }

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

        [HttpPost("RegisterSection")]
        public ActionResult RegisterSection([FromBody] SectionRegisterPayload payload)
        {
            m_homeScreenManager.RegisterResultsDelegate(new PluginDefinedSection(payload.Id, payload.DisplayText!, payload.Route, payload.AdditionalData)
            {
                OnGetResults = sectionPayload =>
                {
                    JObject jsonPayload = JObject.FromObject(sectionPayload);

                    string? publishedServerUrl = m_serverApplicationHost.GetType()
                        .GetProperty("PublishedServerUrl", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(m_serverApplicationHost) as string;
                
                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(publishedServerUrl ?? $"http://localhost:{m_serverApplicationHost.HttpPort}");
                    
                    HttpResponseMessage responseMessage = client.PostAsync(payload.ResultsEndpoint, 
                        new StringContent(jsonPayload.ToString(Formatting.None), MediaTypeHeaderValue.Parse("application/json"))).GetAwaiter().GetResult();

                    return JsonConvert.DeserializeObject<QueryResult<BaseItemDto>>(responseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult()) ?? new QueryResult<BaseItemDto>();
                }
            });
            
            return Ok();
        }

        [HttpPost("Patch/LoadSections")]
        public ActionResult ApplyLoadSectionsPatch([FromBody] PatchRequestPayload content)
        {
            // replace `",loadSections:` with itself followed by our function followed by `",originalLoadSections:`
            Stream replacementStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{GetType().Namespace}.loadSections.js")!;
            using TextReader replacementTextReader = new StreamReader(replacementStream);
        
            string[] parts = content.Contents!.Split(",loadSections:", StringSplitOptions.RemoveEmptyEntries);
            Regex variableFind = new Regex(@"var\s+([a-zA-Z][^=]*)=");
            string thisVariableName = variableFind.Matches(parts[0]).Last().Groups[1].Value;
            string replacementText = replacementTextReader.ReadToEnd()
                .Replace("{{this_hook}}", thisVariableName)
                .Replace("{{layoutmanager_hook}}", "n") // TODO: lookup the first "assigned" variable after `var`
                .Replace("{{cardbuilder_hook}}", "h"); // TODO: lookup the last "assigned" variable in block that includes "SmallLibraryTiles" 

            string regex = content.Contents.Replace(",loadSections:", $",loadSections:{replacementText},originalLoadSections:");
        
            return Content(regex, "application/javascript");
        }
    }
}
