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
    public class PluginServiceRegistrator : Services.PluginServiceRegistrator
    {
        public override void ConfigureServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
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