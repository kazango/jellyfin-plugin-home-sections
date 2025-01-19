using System.Reflection;
using Jellyfin.Plugins.HomeScreenSections.HomeScreen;
using Jellyfin.Plugins.HomeScreenSections.Library;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugins.HomeScreenSections
{
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<IHomeScreenManager, HomeScreenManager>(services =>
            {
                HomeScreenManager homeScreenManager = ActivatorUtilities.CreateInstance<HomeScreenManager>(services);
                
                string pluginDll = Assembly.GetExecutingAssembly().Location;
                string pluginLocation = Path.GetDirectoryName(pluginDll)!;

                string[] extraDlls = Directory.GetFiles(pluginLocation, "*.dll", SearchOption.AllDirectories)
                    .Where(x => x != pluginDll).ToArray();

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