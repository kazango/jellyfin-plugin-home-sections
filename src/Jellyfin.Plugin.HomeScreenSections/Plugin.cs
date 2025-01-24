using System.Reflection;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.FileTransformation.Controller;
using Jellyfin.Plugin.HomeScreenSections.Library;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.HomeScreenSections;

public class Plugin : BasePlugin<BasePluginConfiguration>, IPlugin, IHasWebPages
{
    public override Guid Id => Guid.Parse("b8298e01-2697-407a-b44d-aa8dc795e850");

    public override string Name => "Home Screen Sections";
    
    public static Plugin Instance { get; private set; }
    
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IServiceProvider serviceProvider) : base(applicationPaths, xmlSerializer)
    {
        IWebFileTransformationWriteService webFileTransformationWriteService = serviceProvider.GetRequiredService<IWebFileTransformationWriteService>();
        
        webFileTransformationWriteService.AddTransformation("hometab\\.[a-zA-z0-9]+\\.chunk\\.js", Transformation);
        
        // Look through the web path and find the file that contains `",loadSections:`
        string[] allJsChunks = Directory.GetFiles(applicationPaths.WebPath, "*.chunk.js", SearchOption.AllDirectories);
        foreach (string jsChunk in allJsChunks)
        {
            if (File.ReadAllText(jsChunk).Contains(",loadSections:"))
            {
                webFileTransformationWriteService.AddTransformation(Path.GetFileName(jsChunk), Transformation2);
            }
        }
        
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

    private void Transformation2(string path, Stream contents)
    {
        // replace `",loadSections:` with itself followed by our function followed by `",originalLoadSections:`
        Stream replacementStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{GetType().Namespace}.Controllers.loadSections.js");
        using TextReader replacementTextReader = new StreamReader(replacementStream);
        
        using var textReader = new StreamReader(contents, null, true, -1, true);
        var text = textReader.ReadToEnd();

        string[] parts = text.Split(",loadSections:", StringSplitOptions.RemoveEmptyEntries);
        Regex variableFind = new Regex(@"var\s+([a-zA-Z][^=]*)=");
        string thisVariableName = variableFind.Matches(parts[0]).Last().Groups[1].Value;
        string replacementText = replacementTextReader.ReadToEnd()
            .Replace("{{this_hook}}", thisVariableName)
            .Replace("{{layoutmanager_hook}}", "n") // TODO: lookup the first "assigned" variable after `var`
            .Replace("{{cardbuilder_hook}}", "h"); // TODO: lookup the last "assigned" variable in block that includes "SmallLibraryTiles" 

        var regex = text.Replace(",loadSections:", $",loadSections:{replacementText},originalLoadSections:");
        
        contents.Seek(0, SeekOrigin.Begin);

        using var textWriter = new StreamWriter(contents, null, -1, true);
        textWriter.Write(regex);
        
        
    }

    private void Transformation(string path, Stream contents)
    {
        
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