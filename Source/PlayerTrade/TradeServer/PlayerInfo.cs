using System;
using System.IO;
using Newtonsoft.Json;

namespace TradeServer
{
    /// <summary>
    /// Server side player data.
    /// </summary>
    public class PlayerInfo
    {
        public static string PlayerInfoDir = "players";

        public string Guid;
        public string Secret;
        public PermissionLevel Permission = PermissionLevel.Player;
        public DateTime LastOnline;

        public DateTime? BannedUntil;
        public string BanReason;

        [JsonIgnore]
        public bool IsBanned
        {
            get
            {
                if (BannedUntil == null)
                    return false;
                return BannedUntil > DateTime.Now;
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(PlayerInfoDir);
            File.WriteAllText(GetPath(Guid), JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static PlayerInfo Load(string guid, bool createIfNotExist)
        {
            if (Directory.Exists(PlayerInfoDir) && File.Exists(GetPath(guid))) // Load player info
                return JsonConvert.DeserializeObject<PlayerInfo>(File.ReadAllText(GetPath(guid)));

            if (createIfNotExist) // Create new player info
                return new PlayerInfo { Guid = guid };

            // Not found
            return null;
        }

        private static string GetPath(string guid)
        {
            return PlayerInfoDir + Path.DirectorySeparatorChar + guid.SanitizeFileName().ToLower() + ".json";
        }
    }
}
