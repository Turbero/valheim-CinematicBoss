using System.Collections.Generic;
using System.Reflection;

namespace CinematicBoss
{
    public static class ModUtils
    {
        public static object GetPrivateValue(object obj, string name, BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic)
        {
            return obj.GetType().GetField(name, bindingAttr)?.GetValue(obj);
        }
        public static void SetPrivateValue(object obj, string name, object value, BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic)
        {
            obj.GetType().GetField(name, bindingAttr)?.SetValue(obj, value);
        }
        
        public static bool IsAdmin(Player player)
        {
            var playerName = player.GetPlayerName();
            List<ZNet.PlayerInfo> result = ZNet.instance.GetPlayerList().FindAll(p => p.m_name == playerName);
            if (result.Count == 0) return false;
            
            string steamID = result[0].m_userInfo.m_id.m_userID;
            Logger.Log($"[IsAdmin] Matching steamID {steamID} in adminList...");
            bool serverAdmin = 
                ZNet.instance != null &&
                ZNet.instance.GetAdminList() != null &&
                ZNet.instance.GetAdminList().Contains(steamID);
            return serverAdmin;
        }
    }
}
