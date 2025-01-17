using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ValveKeyValue;
using static launcher.Logger;

namespace launcher
{
    public class PlaylistFile
    {
        public static PlaylistRoot Parse(string filePath)
        {
            PlaylistRoot data = new();

            try
            {
                FileStream stream = File.OpenRead(filePath); // or any other Stream

                KVSerializerOptions options = new KVSerializerOptions
                {
                    HasEscapeSequences = false,
                };
                options.Conditions.Clear();

                KVSerializer kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
                data = kv.Deserialize<PlaylistRoot>(stream, options);
            }
            catch (Exception ex)
            {
                LogError(Source.VDF, ex.Message);
            }

            return data;
        }

        public static List<string> GetMaps(PlaylistRoot data)
        {
            List<string> maps = [];

            if (data.Playlists == null)
                return maps;

            try
            {
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
            }
            catch (Exception ex)
            {
                LogError(Source.VDF, ex.Message);
            }

            return maps;
        }

        public static List<string> GetPlaylists(PlaylistRoot data)
        {
            List<string> playlistnames = [];

            if (data.Playlists == null)
                return playlistnames;

            try
            {
                foreach (var playlists in data.Playlists)
                {
                    if (!playlistnames.Contains(playlists.Key))
                        playlistnames.Add(playlists.Key);
                }
            }
            catch (Exception ex)
            {
                LogError(Source.VDF, ex.Message);
            }

            return playlistnames;
        }
    }

    public class PlaylistRoot
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("versionNum")]
        public int VersionNum { get; set; }

        [JsonProperty("Gamemodes")]
        public Gamemodes Gamemodes { get; set; }

        [JsonProperty("Playlists")]
        public Dictionary<string, PlaylistDefinition> Playlists { get; set; }

        [JsonProperty("LocalizedStrings")]
        public LocalizedStrings LocalizedStrings { get; set; }

        [JsonProperty("KVFileOverrides")]
        public Dictionary<string, object> KVFileOverrides { get; set; } = [];
    }

    public class Gamemodes
    {
        [JsonExtensionData]
        public Dictionary<string, GamemodeDefinition> Items { get; set; } = [];
    }

    public class GamemodeDefinition
    {
        [JsonProperty("inherit")]
        public string Inherit { get; set; }

        [JsonProperty("vars")]
        public Dictionary<string, string> Vars { get; set; }

        [JsonProperty("maps")]
        public Dictionary<string, string> Maps { get; set; }
    }

    public class PlaylistDefinition
    {
        [JsonProperty("inherit")]
        public string Inherit { get; set; }

        [JsonProperty("vars")]
        public Dictionary<string, string> Vars { get; set; }

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