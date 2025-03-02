using System.Diagnostics;
using System.IO.Pipes;
using System.Net.Http.Headers;
using System.Reflection;
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
        private readonly NamedPipeService m_namedPipeService;

        public StartupService(IServerApplicationHost serverApplicationHost, IApplicationPaths applicationPaths, ILogger<HomeScreenSectionsPlugin> logger,
            NamedPipeService namedPipeService)
        {
            m_serverApplicationHost = serverApplicationHost;
            m_applicationPaths = applicationPaths;
            m_logger = logger;
            m_namedPipeService = namedPipeService;
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
                    payload.Add("transformationPipe", "Jellyfin.Plugin.HomeScreenSections.Pipes.LoadSections");
                    m_namedPipeService.CreateNamedPipeHandler("Jellyfin.Plugin.HomeScreenSections.Pipes.LoadSections", async stream =>
                    {
                        byte[] lengthBuffer = new byte[8];
                        await stream.ReadExactlyAsync(lengthBuffer, 0, lengthBuffer.Length);
                        long length = BitConverter.ToInt64(lengthBuffer, 0);
                        
                        MemoryStream memoryStream = new MemoryStream();
                        while (length > 0)
                        {
                            byte[] buffer = new byte[Math.Min(length, 1024)];
                            int readBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                            length -= readBytes;
                            
                            memoryStream.Write(buffer, 0, readBytes);
                        }
                        
                        string rawJson = Encoding.UTF8.GetString(memoryStream.ToArray());
                        
                        string response = TransformationPatches.LoadSections(JsonConvert.DeserializeObject<PatchRequestPayload>(rawJson)!);
                        byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
                        byte[] responseLengthBuffer = BitConverter.GetBytes((long)responseBuffer.Length);
                        
                        await stream.WriteAsync(responseLengthBuffer, 0, responseLengthBuffer.Length);
                        await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    });

                    string fileTransformationPipeName = "Jellyfin.Plugin.FileTransformation.NamedPipe";
                    MethodInfo? getPipePathMethod = typeof(PipeStream).GetMethod("GetPipePath", BindingFlags.Static | BindingFlags.NonPublic);
                    string? pipePath = getPipePathMethod?.Invoke(null, new object[] { ".", fileTransformationPipeName }) as string;
                    string? pipeDirectory = Path.GetDirectoryName(pipePath);
            
                    if (Directory.Exists(pipeDirectory) && Directory.GetFiles(pipeDirectory).Contains(pipePath))
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