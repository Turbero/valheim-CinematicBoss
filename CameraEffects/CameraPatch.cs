using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CinematicBoss.CameraEffects
{
    [HarmonyPatch(typeof(Player), "UpdateHover")]
	internal static class Player_UpdateHover_ClearSightPatch
	{
		private static void Postfix(Player __instance)
		{
			if (Cutscene.State == Cutscene.CinematicState.Inactive)
				return; 
			
			if (Player.m_localPlayer == null || __instance != Player.m_localPlayer ||
			    !ConfigurationFile.transparencyWhenInvokingBoss.Value ||
			    !ConfigurationFile.transparencyWhenInvokingBossList.Value.Contains(Cutscene.BossName) ||
			    ModUtils.GetPrivateValue(__instance, "m_hovering") == null)
				return;
			
			GameObject hovering = (GameObject)ModUtils.GetPrivateValue(__instance, "m_hovering");
			if (hovering.GetComponentInParent<ClearSightOccluderTag>() != null && hovering.GetComponentInParent<Hoverable>() == null && hovering.GetComponentInParent<Interactable>() == null)
			{
				if (!ClearSightHoverResolver.TryResolveHoverBehindOccluder(__instance, out GameObject mostConvenientHover, out Character mostConvenientChar))
				{
					ModUtils.SetPrivateValue(__instance, "m_hovering", null);
					ModUtils.SetPrivateValue(__instance, "m_hoveringCreature", null);
				}
				else
				{
					ModUtils.SetPrivateValue(__instance, "m_hovering", mostConvenientHover);
					ModUtils.SetPrivateValue(__instance, "m_hoveringCreature", mostConvenientChar);
				}
			}
		}
	}
	[HarmonyPatch(typeof(Player), "RemovePiece")]
	internal static class Player_RemovePiece_ClearSightPatch
	{
		private static void Prefix(Player __instance)
		{
			if (Cutscene.State == Cutscene.CinematicState.Inactive)
				return;
			
			if (Player.m_localPlayer != null && !__instance == Player.m_localPlayer && 
			    ConfigurationFile.transparencyWhenInvokingBoss.Value && 
			    ConfigurationFile.transparencyWhenInvokingBossList.Value.Contains(Cutscene.BossName) &&
			    ModUtils.GetPrivateValue(__instance, "m_hovering") != null &&
			    ((GameObject)ModUtils.GetPrivateValue(__instance, "m_hovering")).GetComponentInParent<ClearSightOccluderTag>() != null)
			{
				if (!ClearSightHoverResolver.TryResolveHoverBehindOccluder(__instance, out GameObject mostConvenientHover, out Character mostConvenientChar))
				{
					ModUtils.SetPrivateValue(__instance, "m_hovering", null);
					ModUtils.SetPrivateValue(__instance, "m_hoveringCreature", null);
				}
				else
				{
					ModUtils.SetPrivateValue(__instance, "m_hovering", mostConvenientHover);
					ModUtils.SetPrivateValue(__instance, "m_hoveringCreature", mostConvenientChar);
				}
			}
		}
	}
	[HarmonyPatch(typeof(Player), "Interact")]
	internal static class Player_Interact_ClearSightPatch
	{
		private static void Prefix(Player __instance, ref GameObject go, bool hold, bool alt)
		{
			if (Cutscene.State == Cutscene.CinematicState.Inactive) return;
			
			if (Player.m_localPlayer != null && __instance == Player.m_localPlayer &&
			    ConfigurationFile.transparencyWhenInvokingBoss.Value &&
			    ConfigurationFile.transparencyWhenInvokingBossList.Value.Contains(Cutscene.BossName) &&
			    go != null && go.GetComponentInParent<ClearSightOccluderTag>() != null &&
			    go.GetComponentInParent<Hoverable>() == null && 
			    go.GetComponentInParent<Interactable>() == null &&
			    ClearSightHoverResolver.TryResolveHoverBehindOccluder(__instance, out GameObject mostConvenientHover, out Character mostConvenientChar))
			{
				go = mostConvenientHover;
				ModUtils.SetPrivateValue(__instance, "m_hovering", mostConvenientHover);
				ModUtils.SetPrivateValue(__instance, "m_hoveringCreature", mostConvenientChar);
			}
		}
	}
	[HarmonyPatch(typeof(FejdStartup), "Awake")]
	internal static class FejdStartup_Awake_ClearSightBootstrap
	{
		private static void Postfix(FejdStartup __instance)
		{
			ClearSightTransparentTemplateProvider.BootstrapFromFejdStartup(__instance);
		}
	}
	[HarmonyPatch(typeof(Piece), "GetAllComfortPiecesInRadius")]
	internal static class Piece_GetAllComfortPiecesInRadius_SanitizePatch
	{
		private static void Prefix()
		{
			if (Cutscene.State == Cutscene.CinematicState.Inactive)
				return;
			
			try
			{
				Type type = typeof(Piece);
				FieldInfo info = type.GetField("s_allPieces", BindingFlags.NonPublic | BindingFlags.Static);
				var s_allPieces = info?.GetValue(null);
				if (s_allPieces == null)
					return;
				
				List<Piece> allPieces = (List<Piece>)s_allPieces;
				for (int num = allPieces.Count - 1; num >= 0; num--)
				{
					if (allPieces[num] == null)
						allPieces.RemoveAt(num);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"Error when cleaning Piece.m_allPieces. {ex}");
			}
		}
	}
	[HarmonyPatch(typeof(GameCamera), "LateUpdate")]
	internal static class GameCamera_LateUpdate_ClearSightPatch
	{
		private static void Postfix(GameCamera __instance)
		{
			CameraClearController.OnAfterCameraUpdate(__instance);
		}
	}
}