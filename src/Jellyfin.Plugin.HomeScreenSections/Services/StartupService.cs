using System.Diagnostics;
using System.IO.Pipes;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Jellyfin.Plugin.HomeScreenSections.Helpers;
using Jellyfin.Plugin.HomeScreenSections.Model;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.HomeScreenSections.Services
{
    public class StartupService : IScheduledTask
    {
        public string Name => "HomeScreenSections Startup";

        public string Key => "Jellyfin.Plugin.HomeScreenSections.Startup";
        
        public string Description => "Startup Service for HomeScreenSections";
        
        public string Category => "Startup Services";
        
        private readonly IServerApplicationHost m_serverApplicationHost;
        private readonly IApplicationPaths m_applicationPaths;
        private readonly ILogger<HomeScreenSectionsPlugin> m_logger;

        public StartupService(IServerApplicationHost serverApplicationHost, IApplicationPaths applicationPaths, ILogger<HomeScreenSectionsPlugin> logger)
        {
            m_serverApplicationHost = serverApplicationHost;
            m_applicationPaths = applicationPaths;
            m_logger = logger;
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Look through the web path and find the file that contains `",loadSections:`
            string[] allJsChunks = Directory.GetFiles(m_applicationPaths.WebPath, "*.chunk.js", SearchOption.AllDirectories);
            foreach (string jsChunk in allJsChunks)
            {
                if (File.ReadAllText(jsChunk).Contains(",loadSections:"))
                {
                    JObject payload = new JObject();
                    payload.Add("id", "ea4045f3-6604-4ba4-9581-f91f96bbd2ae");
                    payload.Add("fileNamePattern", Path.GetFileName(jsChunk));
                    payload.Add("callbackAssembly", GetType().Assembly.FullName);
                    payload.Add("callbackClass", typeof(TransformationPatches).FullName);
                    payload.Add("callbackMethod", nameof(TransformationPatches.LoadSections));
                    
                    Assembly? fileTransformationAssembly =
                        AssemblyLoadContext.All.SelectMany(x => x.Assemblies).FirstOrDefault(x =>
                            x.FullName?.Contains(".FileTransformation") ?? false);

                    if (fileTransformationAssembly != null)
                    {
                        Type? pluginInterfaceType = fileTransformationAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");

                        if (pluginInterfaceType != null)
                        {
                            pluginInterfaceType.GetMethod("RegisterTransformation")?.Invoke(null, new object?[] { payload });
                        }
                    }
                    break;
                }
            }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo()
            {
                Type = TaskTriggerInfo.TriggerStartup
            };
        }
    }
}