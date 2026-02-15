using UnityEngine;
using UnityEngine.UI;

namespace CinematicBoss
{
    public static class Cutscene
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

    
}