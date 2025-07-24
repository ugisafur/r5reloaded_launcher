using Newtonsoft.Json;
using System.Collections.Generic;

namespace launcher.Services.Models
{
    /// <summary>
    /// Represents the definition of a game mode.
    /// </summary>
    public class GamemodeDefinition
    {
        [JsonProperty("inherit")]
        public string Inherit { get; set; }

        [JsonProperty("vars")]
        public Dictionary<string, string> Vars { get; set; }

        [JsonProperty("maps")]
        public Dictionary<string, string> Maps { get; set; }
    }
} 