using System.Reflection;
using System.Runtime.Loader;
using Jellyfin.Plugin.HomeScreenSections.HomeScreen;
using Jellyfin.Plugin.HomeScreenSections.Library;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.HomeScreenSections
{
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.StartsWith("Jellyfin.Plugin.FileTransformation"))
                {
                    Assembly? assembly = AssemblyLoadContext.All.Where(x => !x.IsCollectible).SelectMany(x => x.Assemblies)
                        .FirstOrDefault(x => x.FullName == args.Name);
                    // Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
                    //     .FirstOrDefault(x => x.FullName == args.Name);
                    if (assembly != null)
                    {
                        return assembly;
                    }
                    
                    string pluginsDir = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                    string fileTransformationDir = Directory.GetDirectories(pluginsDir, "File Transformation_*", SearchOption.TopDirectoryOnly).OrderBy(
                        x =>
                        {
                            return Version.Parse(x.Split('_').LastOrDefault());
                        }).Last();
                    
                    string dll = Path.Combine(fileTransformationDir, "Jellyfin.Plugin.FileTransformation.dll");
                    
                    return Assembly.LoadFile(dll);
                }
                
                return null;
            };
            
            serviceCollection.AddSingleton<CollectionManagerProxy>();
            serviceCollection.AddSingleton<IHomeScreenManager, HomeScreenManager>(services =>
            {
                IApplicationPaths appPaths = services.GetRequiredService<IApplicationPaths>();
                
                HomeScreenManager homeScreenManager = ActivatorUtilities.CreateInstance<HomeScreenManager>(services);
                
                string pluginLocation = Path.Combine(appPaths.PluginConfigurationsPath, typeof(Plugin).Namespace!);

                string[] extraDlls = Directory.GetFiles(pluginLocation, "*.dll", SearchOption.AllDirectories).ToArray();

                foreach (string extraDll in extraDlls)
                {
                    Assembly extraPluginAssembly = Assembly.LoadFile(extraDll);

                    Type[] homeScreenSectionTypes = extraPluginAssembly.GetTypes().Where(x => x.IsAssignableTo(typeof(IHomeScreenSection))).ToArray();

                    foreach (Type homeScreenSectionType in homeScreenSectionTypes)
                    {
                        homeScreenManager.RegisterResultsDelegate(homeScreenSectionType);
                    }
                }

                return homeScreenManager;
            });
        }
    }
}