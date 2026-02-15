using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using UnityEngine.UI;
using UnityEngine;

namespace CinematicBoss
{
    public static class Patch
    {
        public enum CinematicState
        {
            Inactive,
            MovingToBossSpawnPos,
            WaitingForBoss,
            LookingAtBoss,
            Returning
        }

        public static CinematicState State = CinematicState.Inactive;

        public static Vector3 SpawnPoint;
        public static Vector3 StartPos;
        public static Quaternion StartRot;
        public static Vector3 TargetPos;
        public static Quaternion TargetRot;

        public static float Timer;
        public static Character BossInstance;

        private static bool hudWasVisible = true;

        // LETTERBOX
        private static GameObject letterboxCanvas;
        private static RectTransform topBar;
        private static RectTransform bottomBar;

        private static float currentHeight = 0f;
        private static bool letterboxActive = false;

        // --------------------------------------------------

        public static void StartCinematic(Vector3 spawnPoint)
        {
            if (!Player.m_localPlayer || !GameCamera.instance)
                return;
            
            Logger.Log("Cinematic started for player "+Player.m_localPlayer.GetPlayerName());

            SpawnPoint = spawnPoint;

            Transform cam = GameCamera.instance.transform;
            StartPos = cam.position;
            StartRot = cam.rotation;

            TargetPos = spawnPoint + new Vector3(0f, 8f, -15f);
            TargetRot = Quaternion.LookRotation(spawnPoint - TargetPos);

            Timer = 0f;
            State = CinematicState.MovingToBossSpawnPos;

            HideHud(true);
            EnableLetterbox(true);
        }

        public static void EndCinematic()
        {
            HideHud(false);
            EnableLetterbox(false);

            State = CinematicState.Inactive;
            BossInstance = null;
            
            Logger.Log("Cinematic ended for player "+Player.m_localPlayer.GetPlayerName());
        }

        public static void HideHud(bool hide)
        {
            if (!Hud.instance)
                return;

            if (hide)
            {
                hudWasVisible = Hud.instance.gameObject.activeSelf;
                Hud.instance.gameObject.SetActive(false);
            }
            else
            {
                Hud.instance.gameObject.SetActive(hudWasVisible);
            }
        }

        // --------------------------------------------------
        // LETTERBOX
        // --------------------------------------------------

        public static void EnableLetterbox(bool enable)
        {
            if (enable)
            {
                if (letterboxCanvas == null)
                    CreateLetterbox();

                letterboxCanvas.SetActive(true);
                letterboxActive = true;
            }
            else
            {
                letterboxActive = false;
            }
        }

        private static void CreateLetterbox()
        {
            letterboxCanvas = new GameObject("CinematicLetterbox");
            Object.DontDestroyOnLoad(letterboxCanvas);

            Canvas canvas = letterboxCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            letterboxCanvas.AddComponent<CanvasScaler>();
            letterboxCanvas.AddComponent<GraphicRaycaster>();

            topBar = CreateBar("TopBar", true);
            bottomBar = CreateBar("BottomBar", false);
        }

        private static RectTransform CreateBar(string name, bool top)
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(letterboxCanvas.transform);

            RectTransform rt = bar.AddComponent<RectTransform>();

            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, top ? 1f : 0f);

            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            Image img = bar.AddComponent<Image>();
            img.color = Color.black;

            return rt;
        }

        public static void AnimateLetterbox()
        {
            if (letterboxCanvas == null)
                return;

            float targetHeight = letterboxActive
                ? Screen.height * (ConfigurationFile.letterBoxHeightPercent.Value / 100f)
                : 0f;

            // Stateless soft interpolation
            float lerpSpeed = Time.deltaTime / ConfigurationFile.letterBoxDuration.Value;

            currentHeight = Mathf.Lerp(currentHeight, targetHeight, lerpSpeed);

            if (topBar != null)
                topBar.sizeDelta = new Vector2(0f, currentHeight);

            if (bottomBar != null)
                bottomBar.sizeDelta = new Vector2(0f, currentHeight);

            // Safe turn off when almost 0
            if (!letterboxActive && currentHeight < 0.5f)
            {
                currentHeight = 0f;
                topBar.sizeDelta = Vector2.zero;
                bottomBar.sizeDelta = Vector2.zero;
                letterboxCanvas.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(OfferingBowl), "InitiateSpawnBoss")]
    public static class InitiateSpawnBossPatch
    {
        static bool Prefix(OfferingBowl __instance, Vector3 point, bool removeItemsFromInventory)
        {
            if (ConfigurationFile.acceptOfferingWithMonstersAround.Value)
                return true;
            
            //Detect monsters around
            Logger.Log("Detecting monsters around "+ConfigurationFile.acceptOfferingWithMonstersAroundRange.Value + " meters...");

            List<Character> charactersNearby = new List<Character>();
            Vector3 playerPosition = Player.m_localPlayer.transform.position;
            Character.GetCharactersInRange(playerPosition, ConfigurationFile.acceptOfferingWithMonstersAroundRange.Value, charactersNearby);

            int countMonsters = charactersNearby.FindAll(c => c.IsMonsterFaction(Time.time)).Count;
            if (countMonsters > 0)
            {
                Logger.Log(countMonsters + " monsters detected. Cancelling...");
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_bedenemiesnearby");
                return false;
            }
            
            Logger.Log("No monsters detected.");
            return true;
        }
    }

    [HarmonyPatch(typeof(OfferingBowl), "SpawnBoss")]
    public static class OfferingBowlPatch
    {
        static void Postfix(OfferingBowl __instance, Vector3 spawnPoint)
        {
            //Patch.StartCinematic(__instance.transform.position);
            Patch.StartCinematic(spawnPoint);

            if (ConfigurationFile.playersNearbyCutscene.Value)
            {
                List<Player> playersInRange = new List<Player>();
                Player.GetPlayersInRange(__instance.transform.position, ConfigurationFile.playersNearbyRange.Value, playersInRange);

                foreach (var playerInfo in playersInRange)
                {
                    if (playerInfo.GetPlayerID() != Player.m_localPlayer.GetPlayerID())
                    {
                        ZPackage package = new ZPackage();
                        package.Write(spawnPoint);
                        ZRoutedRpc.instance.InvokeRoutedRPC(playerInfo.GetZDOID().UserID, "RPC_CinematicPlayerNearby", package);
                    }
                }
            }
        }

        public static void RPC_CinematicPlayerNearby(long sender, ZPackage package)
        {
            Logger.Log($"RPC_CinematicPlayerNearby | sender {sender} sending cutscene to player {Player.m_localPlayer.GetPlayerID()}");
            Patch.StartCinematic(package.ReadVector3());
        }
    }

    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    public static class CameraPatch
    {
        static bool Prefix(GameCamera __instance)
        {
            // Always animate bars even when cinematic has been completed
            Patch.AnimateLetterbox();

            if (Patch.State == Patch.CinematicState.Inactive)
                return true;

            Transform cam = __instance.transform;

            Patch.Timer += Time.deltaTime;

            switch (Patch.State)
            {
                case Patch.CinematicState.MovingToBossSpawnPos:

                    float moveDuration = Mathf.Max(0.01f, ConfigurationFile.cameraGoesToBossDuration.Value);
                    float t = Patch.Timer / moveDuration;

                    cam.position = Vector3.Lerp(Patch.StartPos, Patch.TargetPos, t);
                    cam.rotation = Quaternion.Slerp(Patch.StartRot, Patch.TargetRot, t);

                    if (t >= 1f)
                    {
                        Patch.Timer = 0f;
                        Patch.State = Patch.CinematicState.WaitingForBoss;
                    }

                    return false;

                case Patch.CinematicState.WaitingForBoss:

                    cam.position = Patch.TargetPos;
                    cam.rotation = Patch.TargetRot;

                    if (!Patch.BossInstance)
                    {
                        Patch.BossInstance =
                            Object.FindObjectsByType<Character>(FindObjectsSortMode.None)
                                .Where(c => c.m_boss)
                                .OrderBy(c => Vector3.Distance(c.transform.position, Patch.SpawnPoint))
                                .FirstOrDefault();
                    }

                    if (Patch.BossInstance)
                    {
                        Patch.Timer = 0f;
                        Patch.State = Patch.CinematicState.LookingAtBoss;
                    }

                    return false;

                case Patch.CinematicState.LookingAtBoss:

                    cam.position = Patch.TargetPos;
                    if (Patch.BossInstance)
                    {
                        Vector3 lookDir = Patch.BossInstance.transform.position - cam.position;
                        cam.rotation = Quaternion.LookRotation(lookDir);
                    }

                    if (Patch.Timer >= ConfigurationFile.waitAtBossCameraPosition.Value)
                    {
                        Patch.Timer = 0f;
                        Patch.State = Patch.CinematicState.Returning;
                    }

                    return false;

                case Patch.CinematicState.Returning:

                    float r = Patch.Timer / 1.5f;

                    cam.position = Vector3.Lerp(Patch.TargetPos, Patch.StartPos, r);
                    cam.rotation = Quaternion.Slerp(Patch.TargetRot, Patch.StartRot, r);

                    if (r >= 1f)
                        Patch.EndCinematic();

                    return false;
            }

            return true;
        }
    }

    // --------------------------------------------------
    // PLAYER LOCK
    // --------------------------------------------------
    [HarmonyPatch(typeof(Player), nameof(Player.InCutscene))]
    public static class Player_InCutscene_Patch
    {
        static void Postfix(ref bool __result)
        {
            if (ConfigurationFile.lockPlayerDuringCutscene.Value && Patch.State != Patch.CinematicState.Inactive)
            {
                __result = true;
            }
        }
    }
}
