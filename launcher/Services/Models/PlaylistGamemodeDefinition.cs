using Newtonsoft.Json;
using System.Collections.Generic;

namespace launcher.Services.Models
{
    /// <summary>
    /// Represents the definition of a game mode within a playlist.
    /// </summary>
    public class PlaylistGamemodeDefinition
    {
        [JsonProperty("maps")]
        public Dictionary<string, string> Maps { get; set; }
    }
} 