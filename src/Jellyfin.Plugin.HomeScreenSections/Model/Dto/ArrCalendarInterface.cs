using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.HomeScreenSections.Model.Dto
{
    // Generic image DTO used by both Sonarr and Radarr
    public class ArrImageDto
    {
        [JsonPropertyName("coverType")]
        public string? CoverType { get; set; }

        [JsonPropertyName("remoteUrl")]
        public string? RemoteUrl { get; set; }
    }

    // Generic interface for calendar items
    public interface ICalendarItem
    {
        int Id { get; }
        string? Title { get; }
        DateTime? AirDate { get; }
        bool HasFile { get; }
        bool Monitored { get; }
        string? GetImageUrl();
        string GetDisplayInfo();
    }

    // Wrapper classes to implement the interface
    public class SonarrCalendarItem : ICalendarItem
    {
        private readonly SonarrCalendarDto _dto;

        public SonarrCalendarItem(SonarrCalendarDto dto)
        {
            _dto = dto;
        }

        public int Id => _dto.Id;
        public string? Title => _dto.Series?.Title;
        public DateTime? AirDate => _dto.AirDateUtc;
        public bool HasFile => _dto.HasFile;
        public bool Monitored => _dto.Monitored;

        public string? GetImageUrl()
        {
            return _dto.Series?.Images?.FirstOrDefault(img => 
                string.Equals(img.CoverType, "poster", StringComparison.OrdinalIgnoreCase))?.RemoteUrl;
        }

        public string GetDisplayInfo()
        {
            return $"S{_dto.SeasonNumber:D2}E{_dto.EpisodeNumber:D2} - {_dto.Title}";
        }

        public SonarrCalendarDto OriginalDto => _dto;
    }

    public class RadarrCalendarItem : ICalendarItem
    {
        private readonly RadarrCalendarDto _dto;

        public RadarrCalendarItem(RadarrCalendarDto dto)
        {
            _dto = dto;
        }

        public int Id => _dto.Id;
        public string? Title => _dto.Title;
        public DateTime? AirDate => _dto.DigitalRelease;
        public bool HasFile => _dto.HasFile;
        public bool Monitored => _dto.Monitored;

        public string? GetImageUrl()
        {
            return _dto.Images?.FirstOrDefault(img => 
                string.Equals(img.CoverType, "poster", StringComparison.OrdinalIgnoreCase))?.RemoteUrl;
        }

        public string GetDisplayInfo()
        {
            return $"{_dto.Title} ({_dto.Year})";
        }

        public RadarrCalendarDto OriginalDto => _dto;
    }
}