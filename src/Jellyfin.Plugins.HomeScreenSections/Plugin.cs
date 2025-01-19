using System.Reflection;
using Jellyfin.Plugins.HomeScreenSections.Library;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugins.HomeScreenSections;

public class Plugin : BasePlugin<BasePluginConfiguration>
{
    public override Guid Id => Guid.Parse("b8298e01-2697-407a-b44d-aa8dc795e850");

    public override string Name => "Home Screen Sections";
    
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
    {
    }
}