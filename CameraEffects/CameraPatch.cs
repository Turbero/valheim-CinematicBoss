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
			if (Player.m_localPlayer == null || __instance != Player.m_localPlayer || !CinematicBoss.CullingEnabled || ModUtils.GetPrivateValue(__instance, "m_hovering") == null)
			{
				return;
			}
			GameObject hovering = (GameObject)ModUtils.GetPrivateValue(__instance, "m_hovering");
			if (hovering.GetComponentInParent<ClearSightOccluderTag>() != null && hovering.GetComponentInParent<Hoverable>() == null && hovering.GetComponentInParent<Interactable>() == null)
			{
				if (!ClearSightHoverResolver.TryResolveHoverBehindOccluder(__instance, out GameObject bestHover, out Character bestChar))
				{
					ModUtils.SetPrivateValue(__instance, "m_hovering", null);
					ModUtils.SetPrivateValue(__instance, "m_hoveringCreature", null);
					//__instance.m_hovering = null;
					//__instance.m_hoveringCreature = null;
				}
				else
				{
					ModUtils.SetPrivateValue(__instance, "m_hovering", bestHover);
					ModUtils.SetPrivateValue(__instance, "m_hoveringCreature", bestChar);
					//__instance.m_hovering = bestHover;
					//__instance.m_hoveringCreature = bestChar;
				}
			}
		}
	}
	[HarmonyPatch(typeof(Player), "RemovePiece")]
	internal static class Player_RemovePiece_ClearSightPatch
	{
		private static void Prefix(Player __instance)
		{
			if (Player.m_localPlayer != null && !__instance == Player.m_localPlayer && CinematicBoss.CullingEnabled && 
			    ModUtils.GetPrivateValue(__instance, "m_hovering") != null &&
			    ((GameObject)ModUtils.GetPrivateValue(__instance, "m_hovering")).GetComponentInParent<ClearSightOccluderTag>() != null)
			{
				if (!ClearSightHoverResolver.TryResolveHoverBehindOccluder(__instance, out GameObject bestHover, out Character bestChar))
				{
					ModUtils.SetPrivateValue(__instance, "m_hovering", null);
					ModUtils.SetPrivateValue(__instance, "m_hoveringCreature", null);
					//__instance.m_hovering = null;
					//__instance.m_hoveringCreature = null;
				}
				else
				{
					ModUtils.SetPrivateValue(__instance, "m_hovering", bestHover);
					ModUtils.SetPrivateValue(__instance, "m_hoveringCreature", bestChar);
					//__instance.m_hovering = bestHover;
					//__instance.m_hoveringCreature = bestChar;
				}
			}
		}
	}
	[HarmonyPatch(typeof(Player), "Interact")]
	internal static class Player_Interact_ClearSightPatch
	{
		private static void Prefix(Player __instance, ref GameObject go, bool hold, bool alt)
		{
			if (Player.m_localPlayer != null && __instance == Player.m_localPlayer && CinematicBoss.CullingEnabled && go != null && go.GetComponentInParent<ClearSightOccluderTag>() != null && go.GetComponentInParent<Hoverable>() == null && go.GetComponentInParent<Interactable>() == null && ClearSightHoverResolver.TryResolveHoverBehindOccluder(__instance, out GameObject bestHover, out Character bestChar))
			{
				Logger.Log("Player.Interact redirect: " + go.name + " (occluder) → " + bestHover.name);
				go = bestHover;
				ModUtils.SetPrivateValue(__instance, "m_hovering", bestHover);
				ModUtils.SetPrivateValue(__instance, "m_hoveringCreature", bestChar);
				//__instance.m_hovering = bestHover;
				//__instance.m_hoveringCreature = bestChar;
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
			try
			{
				Type type = typeof(Piece);
				FieldInfo info = type.GetField("s_allPieces", BindingFlags.NonPublic | BindingFlags.Static);
				var s_allPieces = info?.GetValue(null);
				if (s_allPieces == null)
				{
					return;
				}

				List<Piece> allPieces = (List<Piece>)s_allPieces;
				for (int num = allPieces.Count - 1; num >= 0; num--)
				{
					if (allPieces[num] == null)
					{
						allPieces.RemoveAt(num);
					}
				}
			}
			catch (Exception arg)
			{
				Logger.LogError($"Failed to sanitize Piece.m_allPieces: {arg}");
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
	[HarmonyPatch(typeof(GameCamera), "CollideRay2")]
	internal static class GameCamera_CollideRay2_ClearSightPatch
	{
		private static bool Prefix(GameCamera __instance, Vector3 eyePos, Vector3 offsetedEyePos, ref Vector3 end)
		{
			if (!CinematicBoss.NoClipEnabled)
			{
				return true;
			}
			int mask = LayerMask.GetMask("terrain");
			float num = Vector3.Distance(eyePos, end);
			if (num <= 0.01f)
			{
				return false;
			}
			Vector3 val = end - offsetedEyePos;
			Vector3 normalized = val.normalized;
			if (normalized.sqrMagnitude < 1E-06f)
			{
				return false;
			}
			float num2 = num;
			bool flag = false;
			float raycastWidth = __instance.m_raycastWidth;
			if (Physics.SphereCast(offsetedEyePos, raycastWidth, normalized, out RaycastHit val2, num, mask))
			{
				num2 = val2.distance;
				flag = true;
			}
			if (Physics.SphereCast(eyePos, raycastWidth, normalized, out val2, num, mask))
			{
				if (val2.distance < num2)
				{
					num2 = val2.distance;
				}
				flag = true;
			}
			if (flag)
			{
				float num3 = Utils.LerpStep(0.5f, 2f, num2);
				val = end - eyePos;
				Vector3 val3 = eyePos + val.normalized * num2;
				val = end - offsetedEyePos;
				Vector3 val4 = offsetedEyePos + val.normalized * num2;
				end = Vector3.Lerp(val3, val4, num3);
			}
			return false;
		}
	}
	[HarmonyPatch(typeof(GameCamera), "Awake")]
	internal static class GameCamera_Awake_ClearSightPatch
	{
		private static void Postfix(GameCamera __instance)
		{
			float value = ConfigurationFile.MaxDistanceCfg.Value;
			if (!(value <= 0f))
			{
				if (value > __instance.m_maxDistance)
				{
					__instance.m_maxDistance = value;
				}
				if (value > __instance.m_maxDistanceBoat)
				{
					__instance.m_maxDistanceBoat = value;
				}
			}
		}
	}
}