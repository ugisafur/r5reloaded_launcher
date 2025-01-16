using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ValveKeyValue;

namespace launcher
{
    public class PlaylistFile
    {
        public static PlaylistsFilee ParseFile(string filePath)
        {
            var stream = File.OpenRead(filePath); // or any other Stream

            var options = new KVSerializerOptions
            {
                HasEscapeSequences = false,
            };
            options.Conditions.Clear();

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            PlaylistsFilee data = kv.Deserialize<PlaylistsFilee>(stream, options);

            return data;
        }

        public static List<string> GetMaps(PlaylistsFilee data)
        {
            List<string> maps = [];
            foreach (var playlists in data.Playlists)
            {
                foreach (var gamemodes in playlists.Value.Gamemodes)
                {
                    foreach (var map in gamemodes.Value.Maps)
                    {
                        if (!maps.Contains(map.Key))
                            maps.Add(map.Key);
                    }
                }
            }
            return maps;
        }

        public static List<string> GetPlaylists(PlaylistsFilee data)
        {
            List<string> playlistnames = [];
            foreach (var playlists in data.Playlists)
            {
                if (!playlistnames.Contains(playlists.Key))
                    playlistnames.Add(playlists.Key);
            }
            return playlistnames;
        }
    }

    public class PlaylistsFilee
    {
        // The top-level name is "playlists", so these properties capture its children
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("versionNum")]
        public int VersionNum { get; set; }

        // 'Gamemodes' is a nested object with multiple named keys.
        [JsonProperty("Gamemodes")]
        public Gamemodes Gamemodes { get; set; }

        // 'Playlists' is another nested object with multiple named keys (fs_aimtrainer, fs_dm, etc.)
        [JsonProperty("Playlists")]
        public Dictionary<string, PlaylistDefinition> Playlists { get; set; }

        // "LocalizedStrings" is another named object
        [JsonProperty("LocalizedStrings")]
        public LocalizedStrings LocalizedStrings { get; set; }

        // "KVFileOverrides" is empty in your file, but we’ll keep a placeholder
        [JsonProperty("KVFileOverrides")]
        public Dictionary<string, object> KVFileOverrides { get; set; }
            = new Dictionary<string, object>();
    }

    public class Gamemodes
    {
        // Each key is the gamemode name ("defaults", "survival", "menufall", "fs_dm", etc.),
        // and the value is its definition (vars/maps/inherit).
        [JsonExtensionData]
        public Dictionary<string, GamemodeDefinition> Items { get; set; }
            = new Dictionary<string, GamemodeDefinition>();
    }

    public class GamemodeDefinition
    {
        // If "inherit" is present, store it here
        [JsonProperty("inherit")]
        public string Inherit { get; set; }

        // Many gamemodes have a "vars" block with large amounts of arbitrary key-value pairs
        [JsonProperty("vars")]
        public Dictionary<string, string> Vars { get; set; }

        // Many gamemodes have a "maps" block with arbitrary key-value pairs
        [JsonProperty("maps")]
        public Dictionary<string, string> Maps { get; set; }
    }

    public class PlaylistDefinition
    {
        [JsonProperty("inherit")]
        public string Inherit { get; set; }

        // 'vars' is another block with many key-value pairs
        [JsonProperty("vars")]
        public Dictionary<string, string> Vars { get; set; }

        // 'gamemodes' block is yet another nested dictionary
        [JsonProperty("gamemodes")]
        public Dictionary<string, PlaylistGamemodeDefinition> Gamemodes { get; set; }
    }

    public class PlaylistGamemodeDefinition
    {
        [JsonProperty("maps")]
        public Dictionary<string, string> Maps { get; set; }
    }

    public class LocalizedStrings
    {
        [JsonProperty("lang")]
        public Lang Lang { get; set; }
    }

    public class Lang
    {
        [JsonProperty("Language")]
        public string Language { get; set; }

        [JsonProperty("Tokens")]
        public Dictionary<string, string> Tokens { get; set; }
    }
}