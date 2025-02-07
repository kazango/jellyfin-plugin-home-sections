using System.Reflection;
using Jellyfin.Plugin.HomeScreenSections.HomeScreen.Sections;
using Jellyfin.Plugin.HomeScreenSections.Library;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.HomeScreenSections.HomeScreen
{
    /// <summary>
    /// Manager for the Modular Home Screen.
    /// </summary>
    public class HomeScreenManager : IHomeScreenManager
    {
        private Dictionary<string, IHomeScreenSection> m_delegates = new Dictionary<string, IHomeScreenSection>();
        private Dictionary<Guid, bool> m_userFeatureEnabledStates = new Dictionary<Guid, bool>();

        private readonly IServiceProvider m_serviceProvider;
        private readonly IApplicationPaths m_applicationPaths;

        private const string c_settingsFile = "ModularHomeSettings.json";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="serviceProvider">Instance of the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        public HomeScreenManager(IServiceProvider serviceProvider, IApplicationPaths applicationPaths)
        {
            m_serviceProvider = serviceProvider;
            m_applicationPaths = applicationPaths;

            string userFeatureEnabledPath = Path.Combine(m_applicationPaths.PluginConfigurationsPath, typeof(Plugin).Namespace!, "userFeatureEnabled.json");
            if (File.Exists(userFeatureEnabledPath))
            {
                m_userFeatureEnabledStates = JsonConvert.DeserializeObject<Dictionary<Guid, bool>>(File.ReadAllText(userFeatureEnabledPath)) ?? new Dictionary<Guid, bool>();
            }

            RegisterResultsDelegate<MyMediaSection>();
            RegisterResultsDelegate<ContinueWatchingSection>();
            RegisterResultsDelegate<NextUpSection>();
            RegisterResultsDelegate<RecentlyAddedMoviesSection>();
            RegisterResultsDelegate<RecentlyAddedShowsSection>();
            RegisterResultsDelegate<LatestMoviesSection>();
            RegisterResultsDelegate<LatestShowsSection>();
            RegisterResultsDelegate<BecauseYouWatchedSection>();
            RegisterResultsDelegate<LiveTvSection>();
            RegisterResultsDelegate<MyListSection>();
            RegisterResultsDelegate<WatchAgainSection>();
        }

        /// <inheritdoc/>
        public IEnumerable<IHomeScreenSection> GetSectionTypes()
        {
            return m_delegates.Values;
        }

        /// <inheritdoc/>
        public QueryResult<BaseItemDto> InvokeResultsDelegate(string key, HomeScreenSectionPayload payload)
        {
            if (m_delegates.ContainsKey(key))
            {
                return m_delegates[key].GetResults(payload);
            }

            return new QueryResult<BaseItemDto>(Array.Empty<BaseItemDto>());
        }

        /// <inheritdoc/>
        public void RegisterResultsDelegate<T>() where T : IHomeScreenSection
        {
            T handler = ActivatorUtilities.CreateInstance<T>(m_serviceProvider);

            RegisterResultsDelegate(handler);
        }

        public void RegisterResultsDelegate<T>(T handler) where T : IHomeScreenSection
        {
            if (handler.Section != null)
            {
                if (!m_delegates.ContainsKey(handler.Section))
                {
                    m_delegates.Add(handler.Section, handler);
                }
                else
                {
                    throw new Exception($"Section type '{handler.Section}' has already been registered to type '{m_delegates[handler.Section].GetType().FullName}'.");
                }
            }
        }

        public void RegisterResultsDelegate(Type homeScreenSectionType)
        {
            IHomeScreenSection handler = (IHomeScreenSection)ActivatorUtilities.CreateInstance(m_serviceProvider, homeScreenSectionType);

            if (handler.Section != null)
            {
                if (!m_delegates.ContainsKey(handler.Section))
                {
                    m_delegates.Add(handler.Section, handler);
                }
                else
                {
                    throw new Exception($"Section type '{handler.Section}' has already been registered to type '{m_delegates[handler.Section].GetType().FullName}'.");
                }
            }
        }

        /// <inheritdoc/>
        public bool GetUserFeatureEnabled(Guid userId)
        {
            if (m_userFeatureEnabledStates.ContainsKey(userId))
            {
                return m_userFeatureEnabledStates[userId];
            }

            m_userFeatureEnabledStates.Add(userId, false);

            return false;
        }

        /// <inheritdoc/>
        public void SetUserFeatureEnabled(Guid userId, bool enabled)
        {
            if (!m_userFeatureEnabledStates.ContainsKey(userId))
            {
                m_userFeatureEnabledStates.Add(userId, enabled);
            }

            m_userFeatureEnabledStates[userId] = enabled;

            string userFeatureEnabledPath = Path.Combine(m_applicationPaths.PluginConfigurationsPath, typeof(Plugin).Namespace!, "userFeatureEnabled.json");
            new FileInfo(userFeatureEnabledPath).Directory?.Create();
            File.WriteAllText(userFeatureEnabledPath, JObject.FromObject(m_userFeatureEnabledStates).ToString(Formatting.Indented));
        }

        /// <inheritdoc/>
        public ModularHomeUserSettings? GetUserSettings(Guid userId)
        {
            string pluginSettings = Path.Combine(m_applicationPaths.PluginConfigurationsPath, typeof(Plugin).Namespace!, c_settingsFile);

            if (File.Exists(pluginSettings))
            {
                JArray settings = JArray.Parse(File.ReadAllText(pluginSettings));

                if (settings.Select(x => JsonConvert.DeserializeObject<ModularHomeUserSettings>(x.ToString())).Any(x => x != null && x.UserId.Equals(userId)))
                {
                    return settings.Select(x => JsonConvert.DeserializeObject<ModularHomeUserSettings>(x.ToString())).First(x => x != null && x.UserId.Equals(userId));
                }
            }

            return new ModularHomeUserSettings
            {
                UserId = userId
            };
        }

        /// <inheritdoc/>
        public bool UpdateUserSettings(Guid userId, ModularHomeUserSettings userSettings)
        {
            string pluginSettings = Path.Combine(m_applicationPaths.PluginConfigurationsPath, typeof(Plugin).Namespace!, c_settingsFile);
            FileInfo fInfo = new FileInfo(pluginSettings);
            fInfo.Directory?.Create();

            JArray settings = new JArray();
            List<ModularHomeUserSettings?> newSettings = new List<ModularHomeUserSettings?>();

            if (File.Exists(pluginSettings))
            {
                settings = JArray.Parse(File.ReadAllText(pluginSettings));
                newSettings = settings.Select(x => JsonConvert.DeserializeObject<ModularHomeUserSettings>(x.ToString())).ToList()!;
                newSettings.RemoveAll(x => x != null && x.UserId.Equals(userId));

                newSettings.Add(userSettings);

                settings.Clear();
            }

            foreach (ModularHomeUserSettings? userSetting in newSettings)
            {
                settings.Add(JObject.FromObject(userSetting ?? new ModularHomeUserSettings()));
            }

            File.WriteAllText(pluginSettings, settings.ToString(Formatting.Indented));

            return true;
        }
    }
}
