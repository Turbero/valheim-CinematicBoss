using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CinematicBoss
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class CinematicBoss : BaseUnityPlugin
    {
        public const string GUID = "Turbero.CinematicBoss";
        public const string NAME = "Cinematic Boss";
        public const string VERSION = "1.1.3";

        private readonly Harmony harmony = new Harmony(GUID);

        internal static bool CullingEnabled;

        internal static bool NoClipEnabled;
        
        void Awake()
        {
            ConfigurationFile.LoadConfig(this);

            harmony.PatchAll();
            
            CullingEnabled = ConfigurationFile.CullingEnabledCfg.Value;
            NoClipEnabled = ConfigurationFile.NoClipEnabledCfg.Value;
        }
        
        private void Start()
        {
            StartCoroutine(WaitForNetworking());
        }
        
        
 
        private void Update()
        {
         	KeyboardShortcut value = ConfigurationFile.ToggleCullKey.Value;
         	if ((int)value.MainKey != 0 && ConfigurationFile.ToggleCullKey.Value.IsDown())
         	{
                CullingEnabled = !CullingEnabled;
         		ConfigurationFile.CullingEnabledCfg.Value = !ConfigurationFile.CullingEnabledCfg.Value;
         		Debug.Log("Camera culling: " + (ConfigurationFile.CullingEnabledCfg.Value ? "ON" : "OFF"));
         	}
         	value = ConfigurationFile.ToggleNoClipKey.Value;
         	if ((int)value.MainKey != 0 && ConfigurationFile.ToggleNoClipKey.Value.IsDown())
         	{
         		NoClipEnabled = !NoClipEnabled;
                ConfigurationFile.NoClipEnabledCfg.Value = !ConfigurationFile.NoClipEnabledCfg.Value;
                Debug.Log("Camera no-clip: " + (NoClipEnabled ? "ON" : "OFF"));
         	}
        }

        private System.Collections.IEnumerator WaitForNetworking()
        {
            // Wait until full networking initialization
            while (ZRoutedRpc.instance == null || ZNet.instance == null)
                yield return new WaitForSeconds(1f);
            
            // Commands registration
            Commands.RegisterConsoleCommand();
        }

        void onDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
    
    class Commands
    {
        public static void RegisterConsoleCommand()
        {
            new Terminal.ConsoleCommand("remove_cutscene", "Rollback cutscene and move camera back to player", args =>
            {
                if (Player.m_localPlayer && IsAdmin(Player.m_localPlayer))
                    Cutscene.EndCinematic();
                else 
                    Logger.LogWarning("Not enabled or not an admin to use this command!");
            });
        }
        
        private static bool IsAdmin(Player player)
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
    
    [HarmonyPatch(typeof(Game), "Start")]
    public class GameStartPatch {
        private static void Prefix() {
            ZRoutedRpc.instance.Register("RPC_CinematicPlayerNearby", new Action<long, ZPackage>(OfferingBowlPatch.RPC_CinematicPlayerNearby));
        }
    }
}
