using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.HomeScreenSections.Library
{
    public interface IHomeScreenManager
    {
        void RegisterResultsDelegate<T>() where T : IHomeScreenSection;

        void RegisterResultsDelegate<T>(T handler) where T : IHomeScreenSection;
        
        IEnumerable<IHomeScreenSection> GetSectionTypes();

        QueryResult<BaseItemDto> InvokeResultsDelegate(string key, HomeScreenSectionPayload payload);

        bool GetUserFeatureEnabled(Guid userId);

        void SetUserFeatureEnabled(Guid userId, bool enabled);

        ModularHomeUserSettings? GetUserSettings(Guid userId);

        bool UpdateUserSettings(Guid userId, ModularHomeUserSettings userSettings);
    }

    public interface IHomeScreenSection
    {
        public string? Section { get; }

        public string? DisplayText { get; set; }

        public int? Limit { get; }

        public string? Route { get; }

        public string? AdditionalData { get; set; }

        public object? OriginalPayload { get; }
        
        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload);

        public IHomeScreenSection CreateInstance(Guid? userId, IEnumerable<IHomeScreenSection>? otherInstances = null);
    }

    public class HomeScreenSectionInfo
    {
        public string? Section { get; set; }

        public string? DisplayText { get; set; }

        public int Limit { get; set; } = 1;

        public string? Route { get; set; }

        public string? AdditionalData { get; set; }

        public object? OriginalPayload { get; set; }
    }

    public class ModularHomeUserSettings
    {
        public Guid UserId { get; set; }

        public List<string> EnabledSections { get; set; } = new List<string>();
    }

    public static class HomeScreenSectionExtensions
    {
        public static HomeScreenSectionInfo AsInfo(this IHomeScreenSection section)
        {
            return new HomeScreenSectionInfo
            {
                Section = section.Section,
                DisplayText = section.DisplayText,
                AdditionalData = section.AdditionalData,
                Route = section.Route,
                Limit = section.Limit ?? 1,
                OriginalPayload = section.OriginalPayload
            };
        }
    }
}
