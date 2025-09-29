using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.HomeScreenSections.Model.Dto
{
    public class RadarrCalendarDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("digitalRelease")]
        public DateTime? DigitalRelease { get; set; }

        [JsonPropertyName("hasFile")]
        public bool HasFile { get; set; }

        [JsonPropertyName("monitored")]
        public bool Monitored { get; set; }

        [JsonPropertyName("images")]
        public ArrImageDto[]? Images { get; set; }
    }


}