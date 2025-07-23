using Newtonsoft.Json;
using System.Collections.Generic;

namespace launcher.GameManagement.PlaylistModels
{
    /// <summary>
    /// Represents the root of the playlist data structure.
    /// </summary>
    public class PlaylistRoot
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("versionNum")]
        public int VersionNum { get; set; }

        [JsonProperty(nameof(Gamemodes))]
        public Gamemodes Gamemodes { get; set; }

        [JsonProperty(nameof(Playlists))]
        public Dictionary<string, PlaylistDefinition> Playlists { get; set; }

        [JsonProperty(nameof(LocalizedStrings))]
        public LocalizedStrings LocalizedStrings { get; set; }

        [JsonProperty(nameof(KVFileOverrides))]
        public Dictionary<string, object> KVFileOverrides { get; set; } = new Dictionary<string, object>();
    }
} 