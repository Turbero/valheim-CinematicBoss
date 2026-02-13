using BepInEx.Configuration;
using BepInEx;
using ServerSync;
using System;
using System.IO;

namespace CinematicBoss
{
    internal class ConfigurationFile
    {
        private static ConfigEntry<bool> _serverConfigLocked = null;

        public static ConfigEntry<bool> debug;

        public static ConfigEntry<float> cameraGoesBackToPlayerDuration;
        public static ConfigEntry<float> cameraGoesToBossDuration;
        public static ConfigEntry<float> letterBoxDuration;
        public static ConfigEntry<float> letterBoxHeightPercent;

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
                cameraGoesBackToPlayerDuration = config("2 - Cinematic Config", "Camera goes back to player duration (seconds)", 1.5f, "Duration of the camera movement back to the player after the boss shows up on the ground");
                cameraGoesToBossDuration = config("2 - Cinematic Config", "Camera goes to boss (seconds)", 10f, "Duration of the camera movement going to the boss position where it will appear in the world after the offering is done");
                letterBoxDuration = config("2 - Cinematic Config", "Letter Box duration (seconds)", 2f, "Duration of the black area up and down on the screen when an offering is accepted and the boss shows up on the ground");
                letterBoxHeightPercent = config("2 - Cinematic Config", "Letter Box Relative Size (percentage)", 6f, new ConfigDescription("Size in percentage of the screen that the black area of the letter box will cover", new AcceptableValueRange<float>(0f, 100f)));
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
            }
            catch
            {
                Logger.LogError($"There was an issue loading {ConfigFileName}");
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
