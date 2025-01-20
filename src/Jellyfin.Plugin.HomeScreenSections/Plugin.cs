using System.Reflection;
using Jellyfin.Plugin.HomeScreenSections.Library;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.HomeScreenSections;

public class Plugin : BasePlugin<BasePluginConfiguration>, IPlugin, IHasWebPages
{
    public override Guid Id => Guid.Parse("b8298e01-2697-407a-b44d-aa8dc795e850");

    public override string Name => "Home Screen Sections";
    
    public static Plugin Instance { get; private set; }
    
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        
        string pluginPagesConfig = Path.Combine(applicationPaths.PluginConfigurationsPath, "Jellyfin.Plugin.PluginPages", "config.json");
        
        JObject config = new JObject();
        if (!File.Exists(pluginPagesConfig))
        {
            FileInfo info = new FileInfo(pluginPagesConfig);
            info.Directory?.Create();
        }
        else
        {
            config = JObject.Parse(File.ReadAllText(pluginPagesConfig));
        }

        if (!config.ContainsKey("pages"))
        {
            config.Add("pages", new JArray());
        }

        if (!config.Value<JArray>("pages").Any(x => x.Value<string>("Id") == typeof(Plugin).Namespace))
        {
            config.Value<JArray>("pages")!.Add(new JObject
            {
                { "Id", typeof(Plugin).Namespace },
                { "Url", "/ModularHomeViews/settings" },
                { "DisplayText", "Modular Home" },
                { "Icon", "ballot" }
            });
        
            File.WriteAllText(pluginPagesConfig, config.ToString(Formatting.Indented));
        }
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return Enumerable.Empty<PluginPageInfo>();
    }

    /// <summary>
    /// Get the views that the plugin serves.
    /// </summary>
    /// <returns>Array of <see cref="PluginPageInfo"/>.</returns>
    public IEnumerable<PluginPageInfo> GetViews()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "settings",
                EmbeddedResourcePath = $"{GetType().Namespace}.Config.settings.html"
            },
            new PluginPageInfo
            {
                Name = "settings.js",
                EmbeddedResourcePath = $"{GetType().Namespace}.Config.settings.js"
            },
        };
    }
}