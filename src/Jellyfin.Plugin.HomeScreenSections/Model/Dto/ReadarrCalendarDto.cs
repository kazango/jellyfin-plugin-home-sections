using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.HomeScreenSections.Model.Dto
{
    public class ReadarrCalendarDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("seriesTitle")]
        public string? SeriesTitle { get; set; }

        [JsonPropertyName("releaseDate")]
        public DateTime? ReleaseDate { get; set; }

        [JsonPropertyName("monitored")]
        public bool Monitored { get; set; }

        [JsonPropertyName("author")]
        public ReadarrAuthorDto? Author { get; set; }

        [JsonPropertyName("statistics")]
        public ReadarrStatisticsDto? Statistics { get; set; }

        [JsonPropertyName("images")]
        public ArrImageDto[]? Images { get; set; }

        // Helper property to check if book has files (using sizeOnDisk like Lidarr)
        public bool HasFile => Statistics?.SizeOnDisk > 0;
    }

    public class ReadarrAuthorDto
    {
        [JsonPropertyName("authorName")]
        public string? AuthorName { get; set; }
    }

    public class ReadarrStatisticsDto
    {
        [JsonPropertyName("sizeOnDisk")]
        public long SizeOnDisk { get; set; }
    }
}