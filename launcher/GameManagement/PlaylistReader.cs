using Newtonsoft.Json;
using System.IO;
using ValveKeyValue;
using static launcher.Utils.Logger;

namespace launcher.GameManagement
{
    public class PlaylistReader
    {
        public static PlaylistRoot Parse(string filePath)
        {
            PlaylistRoot data = new();
            FileStream stream = File.OpenRead(filePath);
            try
            {
                KVSerializerOptions options = new()
                {
                    HasEscapeSequences = false,
                    EnableValveNullByteBugBehavior = true,
                };
                options.Conditions.Clear();

                KVSerializer kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
                data = kv.Deserialize<PlaylistRoot>(stream, options);
            }
            catch (Exception ex)
            {
                LogException($"Playlist Parsing Failed", LogSource.VDF, ex);
            }
            finally
            {
                stream.Close();
            }

            return data;
        }

        public static List<string> GetMaps(PlaylistRoot data)
        {
            List<string> maps = ["No Selection"];

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
                LogException($"Playlist Get Maps Failed", LogSource.VDF, ex);
            }

            return maps;
        }

        public static List<string> GetPlaylists(PlaylistRoot data)
        {
            List<string> playlistnames = ["No Selection"];

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
                LogException($"Playlist Get Gamemodes Failed", LogSource.VDF, ex);
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

        [JsonProperty(nameof(Gamemodes))]
        public Gamemodes Gamemodes { get; set; }

        [JsonProperty(nameof(Playlists))]
        public Dictionary<string, PlaylistDefinition> Playlists { get; set; }

        [JsonProperty(nameof(LocalizedStrings))]
        public LocalizedStrings LocalizedStrings { get; set; }

        [JsonProperty(nameof(KVFileOverrides))]
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
        [JsonProperty(nameof(Language))]
        public string Language { get; set; }

        [JsonProperty(nameof(Tokens))]
        public Dictionary<string, string> Tokens { get; set; }
    }
}