using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace CinematicBoss
{
    public class EnemiesCounts
    {
        public static int GetCountMonstersAroundPlayer()
        {
            List<Character> charactersNearby = new List<Character>();
            Vector3 playerPosition = Player.m_localPlayer.transform.position;
            Character.GetCharactersInRange(playerPosition, ConfigurationFile.acceptOfferingWithMonstersAroundRange.Value, charactersNearby);

            return charactersNearby.FindAll(c => c.IsMonsterFaction(Time.time)).Count;
        }
        
        public static int GetCountBossesAroundPlayer()
        {
            List<Character> charactersNearby = new List<Character>();
            Vector3 playerPosition = Player.m_localPlayer.transform.position;
            Character.GetCharactersInRange(playerPosition, ConfigurationFile.acceptOfferingWithMonstersAroundRange.Value, charactersNearby);

            return charactersNearby.FindAll(c => c.IsBoss()).Count;
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

            int countMonsters = EnemiesCounts.GetCountMonstersAroundPlayer();
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
            int countBosses = EnemiesCounts.GetCountBossesAroundPlayer();
            if (countBosses > 0)
            {
                Logger.Log("Same boss is already out. Skipping cutscene.");
                return;
            }
            
            //Patch.StartCinematic(__instance.transform.position);
            Cutscene.StartCinematic(spawnPoint, __instance.m_bossPrefab.name);

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
                        package.Write(__instance.m_bossPrefab.name);
                        ZRoutedRpc.instance.InvokeRoutedRPC(playerInfo.GetZDOID().UserID, "RPC_CinematicPlayerNearby", package);
                    }
                }
            }
        }

        public static void RPC_CinematicPlayerNearby(long sender, ZPackage package)
        {
            Logger.Log($"RPC_CinematicPlayerNearby | sender {sender} sending cutscene to player {Player.m_localPlayer.GetPlayerID()}");
            Cutscene.StartCinematic(package.ReadVector3(), package.ReadString());
        }
    }

    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    public static class CameraPatch
    {
        static bool Prefix(GameCamera __instance)
        {
            // Always animate bars even when cinematic has been completed
            Cutscene.AnimateLetterbox();

            if (Cutscene.State == Cutscene.CinematicState.Inactive)
                return true;

            Transform cam = __instance.transform;

            Cutscene.Timer += Time.deltaTime;

            switch (Cutscene.State)
            {
                case Cutscene.CinematicState.MovingToBossSpawnPos:

                    float moveDuration = Mathf.Max(0.01f, ConfigurationFile.cameraGoesToBossDuration.Value);
                    float t = Cutscene.Timer / moveDuration;

                    cam.position = Vector3.Lerp(Cutscene.StartPos, Cutscene.TargetPos, t);
                    cam.rotation = Quaternion.Slerp(Cutscene.StartRot, Cutscene.TargetRot, t);

                    if (t >= 1f)
                    {
                        Cutscene.Timer = 0f;
                        Cutscene.State = Cutscene.CinematicState.WaitingForBoss;
                    }

                    return false;

                case Cutscene.CinematicState.WaitingForBoss:

                    cam.position = Cutscene.TargetPos;
                    cam.rotation = Cutscene.TargetRot;

                    if (!Cutscene.BossInstance)
                    {
                        Cutscene.BossInstance =
                            Object.FindObjectsByType<Character>(FindObjectsSortMode.None)
                                .Where(c => c.m_boss)
                                .OrderBy(c => Vector3.Distance(c.transform.position, Cutscene.SpawnPoint))
                                .FirstOrDefault();
                    }

                    if (Cutscene.BossInstance)
                    {
                        Cutscene.Timer = 0f;
                        Cutscene.State = Cutscene.CinematicState.LookingAtBoss;
                    }

                    return false;

                case Cutscene.CinematicState.LookingAtBoss:

                    cam.position = Cutscene.TargetPos;
                    if (Cutscene.BossInstance)
                    {
                        Vector3 lookDir = Cutscene.BossInstance.transform.position - cam.position;
                        cam.rotation = Quaternion.LookRotation(lookDir);
                    }

                    float waitingTime = ConfigurationFile.waitAtBossCameraPosition.Value ? GetWaitTimeByBossName(Cutscene.BossName) : 1f;
                    if (Cutscene.Timer >= waitingTime)
                    {
                        Cutscene.Timer = 0f;
                        Cutscene.State = Cutscene.CinematicState.Returning;
                    }

                    return false;

                case Cutscene.CinematicState.Returning:

                    float r = Cutscene.Timer / 1.5f;

                    cam.position = Vector3.Lerp(Cutscene.TargetPos, Cutscene.StartPos, r);
                    cam.rotation = Quaternion.Slerp(Cutscene.TargetRot, Cutscene.StartRot, r);

                    if (r >= 1f)
                        Cutscene.EndCinematic();

                    return false;
            }

            return true;
        }

        private static float GetWaitTimeByBossName(string bossPrefabName)
        {
            Logger.Log("bossPrefabName: " + bossPrefabName);
            if (bossPrefabName.Equals("Eikthyr"))
                return 2f;
            if (bossPrefabName.Equals("gd_king"))
                return 5f;
            if (bossPrefabName.Equals("Bonemass"))
                return 3f;
            if (bossPrefabName.Equals("Dragon"))
                return 2f;
            if (bossPrefabName.Equals("GoblinKing"))
                return 10f;
            if (bossPrefabName.Equals("SeekerQueen"))
                return 2f;
            if (bossPrefabName.Equals("Fader"))
                return 2f;
            return 1f;
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
            if (ConfigurationFile.lockPlayerDuringCutscene.Value && Cutscene.State != Cutscene.CinematicState.Inactive)
            {
                __result = true;
            }
        }
    }
}
