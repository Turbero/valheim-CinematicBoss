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
            MovingToAltar,
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

        private static float targetHeightPercent = ConfigurationFile.targetHeightPercent.Value / 100f;
        private static float animationDuration = ConfigurationFile.letterBoxDuration.Value;
        private static float currentHeight = 0f;
        private static bool letterboxActive = false;

        // --------------------------------------------------

        public static void StartCinematic(Vector3 spawnPoint)
        {
            if (!Player.m_localPlayer || !GameCamera.instance)
                return;

            SpawnPoint = spawnPoint;

            Transform cam = GameCamera.instance.transform;
            StartPos = cam.position;
            StartRot = cam.rotation;

            TargetPos = spawnPoint + new Vector3(0f, 8f, -15f);
            TargetRot = Quaternion.LookRotation(spawnPoint - TargetPos);

            Timer = 0f;
            State = CinematicState.MovingToAltar;

            ApplyPlayerLock(true);
            HideHud(true);
            EnableLetterbox(true);
        }

        public static void EndCinematic()
        {
            ApplyPlayerLock(false);
            HideHud(false);
            EnableLetterbox(false);

            State = CinematicState.Inactive;
            BossInstance = null;
        }

        // --------------------------------------------------
        // PLAYER LOCK
        // --------------------------------------------------

        public static void ApplyPlayerLock(bool locked)
        {
            /*if (ZInput.instance != null)
                ZInput.instance.BlockInput(locked);*/

            if (!Player.m_localPlayer)
                return;

            if (locked)
            {
                Player.m_localPlayer.SetControls(
                    Vector3.zero,
                    false, false, false, false,
                    false, false, false, false,
                    false, false);
            }
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
                ? Screen.height * targetHeightPercent
                : 0f;

            // Interpolación suave independiente del estado
            float lerpSpeed = Time.deltaTime / animationDuration;

            currentHeight = Mathf.Lerp(currentHeight, targetHeight, lerpSpeed);

            if (topBar != null)
                topBar.sizeDelta = new Vector2(0f, currentHeight);

            if (bottomBar != null)
                bottomBar.sizeDelta = new Vector2(0f, currentHeight);

            // Apagado seguro cuando casi llega a 0
            if (!letterboxActive && currentHeight < 0.5f)
            {
                currentHeight = 0f;
                topBar.sizeDelta = Vector2.zero;
                bottomBar.sizeDelta = Vector2.zero;
                letterboxCanvas.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(OfferingBowl), "SpawnBoss")]
    public static class OfferingBowlPatch
    {
        static void Postfix(OfferingBowl __instance, Vector3 spawnPoint)
        {
            //Patch.StartCinematic(__instance.transform.position);
            Patch.StartCinematic(spawnPoint);
        }
    }

    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    public static class CameraPatch
    {
        static bool Prefix(GameCamera __instance)
        {
            // Siempre animamos barras aunque la cinemática haya terminado
            Patch.AnimateLetterbox();

            if (Patch.State == Patch.CinematicState.Inactive)
                return true;

            Transform cam = __instance.transform;

            Patch.Timer += Time.deltaTime;
            float duration = ConfigurationFile.cameraGoesBackToPlayerDuration.Value;

            switch (Patch.State)
            {
                case Patch.CinematicState.MovingToAltar:

                    float t = Patch.Timer / 2f;

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
                            .FirstOrDefault(c => c.m_boss);
                    }

                    if (Patch.BossInstance)
                    {
                        Patch.Timer = 0f;
                        Patch.State = Patch.CinematicState.LookingAtBoss;
                    }

                    return false;

                case Patch.CinematicState.LookingAtBoss:

                    cam.position = Patch.TargetPos;

                    if (Patch.Timer >= duration)
                    {
                        Patch.Timer = 0f;
                        Patch.State = Patch.CinematicState.Returning;
                    }

                    return false;

                case Patch.CinematicState.Returning:

                    float r = Patch.Timer / 1.5f;

                    cam.position = Vector3.Lerp(Patch.TargetPos, Patch.StartPos, r);

                    if (r >= 1f)
                        Patch.EndCinematic();

                    return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.InCutscene))]
    public static class Player_InCutscene_Patch
    {
        static void Postfix(ref bool __result)
        {
            if (Patch.State != Patch.CinematicState.Inactive)
            {
                __result = true;
            }
        }
    }
}
