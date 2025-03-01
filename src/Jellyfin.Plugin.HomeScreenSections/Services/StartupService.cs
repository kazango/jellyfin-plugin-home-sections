using System.Diagnostics;
using System.IO.Pipes;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
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
                    payload.Add("transformationEndpoint", "/HomeScreen/Patch/LoadSections");

                    string fileTransformationPipeName = "Jellyfin.Plugin.FileTransformation.NamedPipe";
                    bool serverSupportsPipeCommunication = Directory.GetFiles(@"\\.\pipe\").Contains($@"\\.\pipe\{fileTransformationPipeName}");

                    if (serverSupportsPipeCommunication)
                    {
                        NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", fileTransformationPipeName, PipeDirection.InOut);
                        await pipeClient.ConnectAsync();
                        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload.ToString(Formatting.None));
                        
                        await pipeClient.WriteAsync(BitConverter.GetBytes((long)payloadBytes.Length));
                        await pipeClient.WriteAsync(payloadBytes, 0, payloadBytes.Length);
                        
                        pipeClient.ReadByte();
                        
                        await pipeClient.DisposeAsync();
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                        string? publishedServerUrl = m_serverApplicationHost.GetType()
                            .GetProperty("PublishedServerUrl", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(m_serverApplicationHost) as string;
                
                        HttpClient client = new HttpClient();
                        client.BaseAddress = new Uri(publishedServerUrl ?? $"http://localhost:{m_serverApplicationHost.HttpPort}");

                        try
                        {
                            await client.PostAsync("/FileTransformation/RegisterTransformation",
                                new StringContent(payload.ToString(Formatting.None),
                                    MediaTypeHeaderValue.Parse("application/json")));
                        }
                        catch (Exception ex)
                        {
                            m_logger.LogError(ex, $"Caught exception when attempting to register file transformation. Ensure you have `File Transformation` plugin installed on your server.");
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