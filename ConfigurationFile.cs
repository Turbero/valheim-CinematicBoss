using BepInEx.Configuration;
using BepInEx;
using ServerSync;
using System;
using System.IO;
using UnityEngine;

namespace CinematicBoss
{
    public enum CullingVisualMode
    {
        Hidden,
        Transparent
    }
    
    internal class ConfigurationFile
    {
        private static ConfigEntry<bool> _serverConfigLocked = null;

        public static ConfigEntry<bool> debug;

        public static ConfigEntry<bool> waitAtBossCameraPosition;
        public static ConfigEntry<float> cameraGoesToBossDuration;
        public static ConfigEntry<bool> lockPlayerDuringCutscene;

        public static ConfigEntry<bool> acceptOfferingWithMonstersAround;
        public static ConfigEntry<float> acceptOfferingWithMonstersAroundRange;
        public static ConfigEntry<float> letterBoxDuration;
        public static ConfigEntry<float> letterBoxHeightPercent;
        public static ConfigEntry<float> playersNearbyRange;
        public static ConfigEntry<bool> playersNearbyCutscene;

        public static ConfigEntry<bool> transparencyWhenInvokingBossToggle;
        public static ConfigEntry<string> transparencyWhenInvokingBossList;
        
        public static ConfigEntry<KeyboardShortcut> ToggleCullKey;
        public static ConfigEntry<KeyboardShortcut> ToggleNoClipKey;
        public static ConfigEntry<bool> CullingEnabledCfg;
        public static ConfigEntry<bool> NoClipEnabledCfg;
        public static ConfigEntry<float> MaxDistanceCfg;
        public static ConfigEntry<float> GroundPaddingCfg;
        public static ConfigEntry<float> CullRadiusCfg;
        public static ConfigEntry<float> CullMaxDistanceCfg;
        public static ConfigEntry<CullingVisualMode> CullingVisualModeCfg;
        public static ConfigEntry<float> CullingFadeAlphaCfg;
        
        internal static bool UseTransparency
        {
            get
            {
                ConfigEntry<CullingVisualMode> cullingVisualModeCfg = CullingVisualModeCfg;
                if (cullingVisualModeCfg != null)
                {
                    return cullingVisualModeCfg.Value == CullingVisualMode.Transparent;
                }
                return false;
            }
        }
         
        internal static float FadeAlpha => Mathf.Clamp01(CullingFadeAlphaCfg?.Value ?? 0.25f);

        private static ConfigFile configFile;
        private static readonly string ConfigFileName = CinematicBoss.GUID + ".cfg";
        private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        private static readonly ConfigSync ConfigSync = new ConfigSync(CinematicBoss.GUID)
        {
            DisplayName = CinematicBoss.NAME,
            CurrentVersion = CinematicBoss.VERSION,
            MinimumRequiredVersion = CinematicBoss.VERSION
        };

        internal static void LoadConfig(BaseUnityPlugin plugin)
        {
            {
                configFile = plugin.Config;

                _serverConfigLocked = config("1 - General", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.");
                _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
                debug = config("1 - General", "DebugMode", false, "Enabling/Disabling the debugging in the console (default = false)", false);
                
                cameraGoesToBossDuration = config("2 - Cinematic Camera", "Camera goes from player to boss (seconds)", 10f, "Duration of the camera movement going to the boss position where it will appear in the world after the offering is done");
                waitAtBossCameraPosition = config("2 - Cinematic Camera", "Camera waits at boss until he is fully out (true/false)", true, "Camera waits the necessary time at boss after he spawns before returning to the player (if false, just wait for one second after spawning)");
                lockPlayerDuringCutscene = config("2 - Cinematic Camera", "Player is locked during cutscene (true/false)", true, "Players cannot move during cutscene if true, otherwise they can move around but the camera will not be focused at them (default = true)");
                
                letterBoxDuration = config("3 - Cinematic Letter Box", "Letter Box Duration (seconds)", 2f, "Duration of the black area up and down on the screen when an offering is accepted and the boss shows up on the ground");
                letterBoxHeightPercent = config("3 - Cinematic Letter Box", "Letter Box Relative Size (percentage)", 6f, new ConfigDescription("Size in percentage of the screen that the black area of the letter box will cover", new AcceptableValueRange<float>(0f, 100f)));
                
                acceptOfferingWithMonstersAround = config("4 - Spawn Boss Conditions", "Player can invoke boss with monsters around (true/false)", false, "Players can use offering bawls even if some monsters are around (default = true)");
                acceptOfferingWithMonstersAroundRange = config("4 - Spawn Boss Conditions", "Player can invoke boss with monsters around (range)", 30f, "Range to detect monsters around when a player is going to call a boss (default = 10f)");

                playersNearbyRange = config("5 - Multiplayer", "Players Nearby Receive Cutscene (range)", 30f, new ConfigDescription("Set up the range in which players will receive the boss cutscene (default = 30)", new AcceptableValueRange<float>(10f, 200f)));
                playersNearbyCutscene = config("5 - Multiplayer", "Players Nearby Receive Cutscene (true/false)", true, "Players nearby will receive the boss cutscene (default = true)");
                
                transparencyWhenInvokingBossToggle = config("6 - Transparency", "Transparency when invoking boss", false, "Add transparency to surrounding objects when invoking a boss.");
                transparencyWhenInvokingBossList = config("6 - Transparency", "Bosses to apply transparency", "Eikthyr", "Comma-separated boss prefabId list to apply transparency effect");

                ToggleCullKey = config("6.1 - Hotkeys", "Toggle Culling Key", new KeyboardShortcut((KeyCode)288), "Toggle camera occluder culling (roof/wall hiding) on/off.");
                ToggleNoClipKey = config("6.1 - Hotkeys", "Toggle No-Clip Key", new KeyboardShortcut((KeyCode)289), "Toggle camera no-clip (ignore walls, respect ground) on/off.");
                CullingEnabledCfg = config("6.2 - Culling", "Culling Enabled", true, "If true, the mod will attempt to hide occluding pieces between camera and player.");
                CullingVisualModeCfg = config("6.2 - Culling", "Culling Visual Mode", CullingVisualMode.Transparent, "How to visually treat occluding pieces between camera and player.\nHidden = fully disable renderers.\nTransparent = keep them visible but fade to the configured alpha.");
                CullingFadeAlphaCfg = config("6.2 - Culling", "Culling Fade Alpha", 0.25f, "Alpha value (0–1) used when Culling Visual Mode is Transparent.");
                CullRadiusCfg = config("6.2 - Culling", "Cull Sphere Radius", 1f, "Radius of the spherecast between camera and player used to find occluding pieces.");
                CullMaxDistanceCfg = config("6.2 - Culling", "Cull Max Distance", 15f, "Maximum distance from the camera along the ray to look for occluders.");
                NoClipEnabledCfg = config("6.3 - NoClip", "NoClip Enabled", true, "If true, the mod will override camera collision and allow the camera to stay zoomed out, still respecting terrain.");
                MaxDistanceCfg = config("6.3 - NoClip", "Forced Max Distance", 12f, "Target camera distance when no-clip is active. The vanilla max distance is around ~6 in most cases.");
                GroundPaddingCfg = config("6.3 - NoClip", "Ground Padding", 0.5f, "Minimum distance above terrain to keep the camera when no-clip is active.");
                
                SetupWatcher();
            }
        }

        private static void SetupWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private static void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                Logger.Log("Attempting to reload configuration...");
                configFile.Reload();
                SettingsChanged(null, null);
                Logger.Log("Configuration reload complete.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"There was an issue loading {ConfigFileName}: " + ex);
            }
        }

        private static void SettingsChanged(object sender, EventArgs e)
        {
            CinematicBoss.CullingEnabled = CullingEnabledCfg.Value;
            CinematicBoss.NoClipEnabled = NoClipEnabledCfg.Value;
        }

        private static ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new ConfigDescription(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = configFile.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
    }
}
