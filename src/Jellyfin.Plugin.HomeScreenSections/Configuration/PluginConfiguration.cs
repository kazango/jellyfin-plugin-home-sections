using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.HomeScreenSections.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool Enabled { get; set; } = false;

        public bool AllowUserOverride { get; set; } = true;

        public string? LibreTranslateUrl { get; set; } = "";

        public string? LibreTranslateApiKey { get; set; } = "";
        
        public string? JellyseerrUrl { get; set; } = "";

        public string? JellyseerrApiKey { get; set; } = "";
        
        public string? JellyseerrPreferredLanguages { get; set; } = "en";
        
        public string? DefaultMoviesLibraryId { get; set; } = "";
        
        public string? DefaultTVShowsLibraryId { get; set; } = "";
        
        public string? DefaultMusicLibraryId { get; set; } = "";
        
        public string? DefaultBooksLibraryId { get; set; } = "";

        public string? SonarrUrl { get; set; } = "";

        public string? SonarrApiKey { get; set; } = "";

        public string? RadarrUrl { get; set; } = "";

        public string? RadarrApiKey { get; set; } = "";

        public int UpcomingShowsTimeframeValue { get; set; } = 1;

        public TimeframeUnit UpcomingShowsTimeframeUnit { get; set; } = TimeframeUnit.Weeks;

        public int UpcomingMoviesTimeframeValue { get; set; } = 1;

        public TimeframeUnit UpcomingMoviesTimeframeUnit { get; set; } = TimeframeUnit.Months;

        public string DateFormat { get; set; } = "YYYY/MM/DD";

        public string DateDelimiter { get; set; } = "/";
        public bool DeveloperMode { get; set; } = false;

        public int CacheBustCounter { get; set; } = 0;

        public int CacheTimeoutSeconds { get; set; } = 86400;

        public SectionSettings[] SectionSettings { get; set; } = Array.Empty<SectionSettings>();
    }

    public enum SectionViewMode
    {
        Portrait,
        Landscape,
        Square
    }

    public enum TimeframeUnit
    {
        Days,
        Weeks,
        Months,
        Years
    }
    
    public class SectionSettings
    {
        public string SectionId { get; set; } = string.Empty;
        
        public bool Enabled { get; set; }
        
        public bool AllowUserOverride { get; set; }
        
        public int LowerLimit { get; set; }
        
        public int UpperLimit { get; set; }

        public int OrderIndex { get; set; }
        
        public SectionViewMode ViewMode { get; set; } = SectionViewMode.Landscape;
    }
}