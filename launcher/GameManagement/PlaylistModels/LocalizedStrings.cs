using Newtonsoft.Json;

namespace launcher.GameManagement.PlaylistModels
{
    /// <summary>
    /// Represents localized strings.
    /// </summary>
    public class LocalizedStrings
    {
        [JsonProperty("lang")]
        public Lang Lang { get; set; }
    }
} 