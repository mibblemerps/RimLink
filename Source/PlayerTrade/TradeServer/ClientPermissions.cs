using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayerTrade;

namespace TradeServer
{
    [Serializable]
    public class ClientPermissions
    {
        public static string PermissionsFilePath = "permissions.json";
        public static PermissionLevel DefaultLevel = PermissionLevel.Player;

        protected Dictionary<string, PermissionLevel> Permissions = new Dictionary<string, PermissionLevel>();

        public void SetPermission(string guid, PermissionLevel level)
        {
            Permissions[guid] = level;
            Save();
        }

        public PermissionLevel GetPermission(string guid)
        {
            if (!Permissions.ContainsKey(guid))
                return DefaultLevel;
            return Permissions[guid];
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(PermissionsFilePath))
                {
                    // File doesn't exist - create it (by saving)
                    Save();
                    return;
                }

                JsonConvert.PopulateObject(File.ReadAllText(PermissionsFilePath), this);
            }
            catch (Exception e)
            {
                Log.Error("Failed to read permissions data!", e);
            }
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(PermissionsFilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch (Exception e)
            {
                Log.Error("Failed to save permissions data!", e);
            }
        }

        public enum PermissionLevel
        {
            Banned,
            Player,
            Admin
        }
    }
}
