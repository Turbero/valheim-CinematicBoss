using BepInEx.Logging;
using UnityEngine;

namespace CinematicBoss
{
    public static class Logger
    {
        public static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(CinematicBoss.NAME);
        internal static void Log(object s)
        {
            if (!ConfigurationFile.debug.Value)
            {
                return;
            }

            logger.LogInfo(s?.ToString());
        }

        internal static void LogInfo(object s)
        {
            logger.LogInfo(s?.ToString());
        }

        internal static void LogWarning(object s)
        {
            var toPrint = $"{CinematicBoss.NAME} {CinematicBoss.VERSION}: {(s != null ? s.ToString() : "null")}";

            Debug.LogWarning(toPrint);
        }

        internal static void LogError(object s)
        {
            var toPrint = $"{CinematicBoss.NAME} {CinematicBoss.VERSION}: {(s != null ? s.ToString() : "null")}";

            Debug.LogError(toPrint);
        }
    }
}
