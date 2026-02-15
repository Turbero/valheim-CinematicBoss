using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace CinematicBoss
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class CinematicBoss : BaseUnityPlugin
    {
        public const string GUID = "Turbero.CinematicBoss";
        public const string NAME = "Cinematic Boss";
        public const string VERSION = "1.1.0";

        private readonly Harmony harmony = new Harmony(GUID);

        void Awake()
        {
            ConfigurationFile.LoadConfig(this);

            harmony.PatchAll();
        }
        
        private void Start()
        {
            StartCoroutine(WaitForNetworking());
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
                Cutscene.EndCinematic();
            });
        }
    }
    
    [HarmonyPatch(typeof(Game), "Start")]
    public class GameStartPatch {
        private static void Prefix() {
            ZRoutedRpc.instance.Register("RPC_CinematicPlayerNearby", new Action<long, ZPackage>(OfferingBowlPatch.RPC_CinematicPlayerNearby));
        }
    }
}
