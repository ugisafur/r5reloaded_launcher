using Newtonsoft.Json;

namespace launcher.Services.Models
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