using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace LaddsTech.DigitalFoundry.Common
{
    public class ListingMetadata
    {
        public static string VariableKey = nameof(ListingMetadata);

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("short_title")]
        public string ShortTitle { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new List<string>();

        [JsonPropertyName("posts")]
        public List<SocialPost> SocialPosts { get; set; } = new List<SocialPost>();


        [JsonPropertyName("meta")]
        public GenerationMetadata? GenerationMetadata { get; set; }

        [JsonIgnore]
        public bool IsComplete => !string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Description) && Keywords.Any();
    }

    public class GenerationMetadata
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    public class SocialPost
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
