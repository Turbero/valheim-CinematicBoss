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

        public static ConfigEntry<bool> transparencyWhenInvokingBoss;
        public static ConfigEntry<string> transparencyWhenInvokingBossList;
        
        public static ConfigEntry<float> transparencyRadiusAreaEffect;
        public static ConfigEntry<float> transparencyMaxDistance;
        public static ConfigEntry<float> transparencyFadeAlpha;

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
                transparencyWhenInvokingBoss = config("2 - Cinematic Camera", "Transparency in objects around boss altar when invoking", true, "Add a smooth transparency effect to surrounding objects when invoking a boss when they are an obstacle to see the boss appearing.");
                
                transparencyWhenInvokingBossList = config("2.1 - Transparency Effect", "Bosses to apply transparency", "Eikthyr,gd_king", "Comma-separated boss prefabId list to apply transparency effect during cutscene");
                transparencyFadeAlpha = config("2.1 - Transparency Effect", "Transparency Fade Alpha", 0.25f, new ConfigDescription("Alpha value (0–1) used to apply transparency on objects during cutscene", new AcceptableValueRange<float>(0f, 1f)));
                transparencyRadiusAreaEffect = config("2.1 - Transparency Effect", "Transparency Area Effect", 10f, "Radius of the sphere delimited between camera and player to find occluding pieces and apply transparency effect on them during cutscene");
                transparencyMaxDistance = config("2.1 - Transparency Effect", "Transparency Max Distance", 15f, "Maximum distance from the camera to look for items to apply transparency effect on them during cutscene");
                
                letterBoxDuration = config("3 - Cinematic Letter Box", "Letter Box Duration (seconds)", 2f, "Duration of the black area up and down on the screen when an offering is accepted and the boss shows up on the ground");
                letterBoxHeightPercent = config("3 - Cinematic Letter Box", "Letter Box Relative Size (percentage)", 6f, new ConfigDescription("Size in percentage of the screen that the black area of the letter box will cover", new AcceptableValueRange<float>(0f, 100f)));
                
                acceptOfferingWithMonstersAround = config("4 - Spawn Boss Conditions", "Player can invoke boss with monsters around (true/false)", false, "Players can use offering bawls even if some monsters are around (default = true)");
                acceptOfferingWithMonstersAroundRange = config("4 - Spawn Boss Conditions", "Player can invoke boss with monsters around (range)", 30f, "Range to detect monsters around when a player is going to call a boss (default = 10f)");

                playersNearbyRange = config("5 - Multiplayer", "Players Nearby Receive Cutscene (range)", 30f, new ConfigDescription("Set up the range in which players will receive the boss cutscene (default = 30)", new AcceptableValueRange<float>(10f, 200f)));
                playersNearbyCutscene = config("5 - Multiplayer", "Players Nearby Receive Cutscene (true/false)", true, "Players nearby will receive the boss cutscene (default = true)");
                
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
