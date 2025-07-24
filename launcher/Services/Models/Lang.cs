using Newtonsoft.Json;
using System.Collections.Generic;

namespace launcher.Services.Models
{
    /// <summary>
    /// Represents a language and its associated tokens.
    /// </summary>
    public class Lang
    {
        [JsonProperty(nameof(Language))]
        public string Language { get; set; }

        [JsonProperty(nameof(Tokens))]
        public Dictionary<string, string> Tokens { get; set; }
    }
} 