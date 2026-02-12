using BepInEx;
using HarmonyLib;

namespace CinematicBoss
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class CinematicBoss : BaseUnityPlugin
    {
        public const string GUID = "Turbero.CinematicBoss";
        public const string NAME = "Cinematic Boss";
        public const string VERSION = "1.0.0";

        private readonly Harmony harmony = new Harmony(GUID);

        void Awake()
        {
            ConfigurationFile.LoadConfig(this);

            harmony.PatchAll();
        }

        void onDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
