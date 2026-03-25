using System;
using System.Collections.Generic;
using UnityEngine;

namespace CinematicBoss.CameraEffects
{
	internal static class CameraClearController
	{
		class RendererFadeData
		{
			public Renderer Renderer;
			public Material[] OriginalShared = Array.Empty<Material>();
			public Material[] GhostMats = Array.Empty<Material>();
		}

		private static readonly List<Renderer> hiddenRenderers = new List<Renderer>();
		private static readonly HashSet<Renderer> hiddenRendererSet = new HashSet<Renderer>();
		private static readonly Dictionary<Renderer, bool> originalEnabled = new Dictionary<Renderer, bool>();
		private static readonly Dictionary<Renderer, bool> originalForceOff = new Dictionary<Renderer, bool>();
		private static readonly Dictionary<Renderer, RendererFadeData> fadeData = new Dictionary<Renderer, RendererFadeData>();

		private static int _geometryMask;
		
		private static int GeometryMask
		{
			get
			{
				if (_geometryMask != 0)
				{
					return _geometryMask;
				}
				_geometryMask = LayerMask.GetMask("Default", "static_solid", "piece", "piece_nonsolid");
				return _geometryMask;
			}
		}

		public static void OnAfterCameraUpdate(GameCamera cam)
		{
			if (cam == null || ModUtils.GetPrivateValue(cam, "m_camera") == null)
			{
				return;
			}
			if ((bool)ModUtils.GetPrivateValue(cam, "m_freeFly"))
			{
				RestoreVisuals();
				return;
			}
			Player localPlayer = Player.m_localPlayer;
			if (localPlayer == null)
			{
				RestoreVisuals();
				return;
			}
			RestoreVisuals();
			if (CinematicBoss.CullingEnabled)
			{
				ApplyCulling(cam, localPlayer);
			}
		}

		private static Vector3 GetCameraTargetPosition(Player player)
		{
			if (player.m_eye != null)
			{
				return player.m_eye.position;
			}
			var head = ModUtils.GetPrivateValue(player, "m_head");
			if (head != null)
			{
				return ((Transform)head).position;
			}
			return player.transform.position + Vector3.up * 1.6f;
		}

		private static void ApplyCulling(GameCamera cam, Player player)
		{
			var camera = ModUtils.GetPrivateValue(cam, "m_camera");
			if (camera == null)
			{
				return;
			}
			Vector3 position = ((Camera)camera).transform.position;
			Vector3 cameraTargetPosition = GetCameraTargetPosition(player);
			Vector3 val = cameraTargetPosition - position;
			float magnitude = val.magnitude;
			if (magnitude < 0.1f)
			{
				return;
			}
			float num = Mathf.Min(magnitude, ConfigurationFile.CullMaxDistanceCfg.Value);
			Vector3 val2 = val / magnitude;
			float num2 = Mathf.Max(0.05f, ConfigurationFile.CullRadiusCfg.Value);
			RaycastHit[] array = Physics.SphereCastAll(position, num2, val2, num, GeometryMask, (QueryTriggerInteraction)2);
			if (array.Length == 0)
			{
				return;
			}
			RaycastHit[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				RaycastHit val3 = array2[i];
				Collider collider = val3.collider;
				if (collider != null && ShouldCullCollider((Camera)camera, cameraTargetPosition, collider, out Component root))
				{
					CullUnderRoot(root);
				}
			}
		}

		private static bool IsObstructingPlayer(Camera cam, Vector3 playerEyePos, Collider col)
		{
			Vector3 position = cam.transform.position;
			Vector3 val = playerEyePos - position;
			float magnitude = val.magnitude;
			if (magnitude < 0.1f)
			{
				return false;
			}
			Vector3 val2 = val / magnitude;
			Vector3 val3 = SafeClosestPoint(col, playerEyePos);
			float num = Vector3.Dot(val3 - position, val2);
			float num2 = magnitude;
			if (num <= 0f || num >= num2 + 0.05f)
			{
				return false;
			}
			Vector3 val4 = cam.WorldToViewportPoint(playerEyePos);
			Vector3 val5 = cam.WorldToViewportPoint(val3);
			if (val5.z <= 0f)
			{
				return false;
			}
			Vector2 val6 = new Vector2(val4.x, val4.y);
			Vector2 val7 = new Vector2(val5.x, val5.y);
			Vector2 val8 = val6 - val7;
			float sqrMagnitude = val8.sqrMagnitude;
			float value = ConfigurationFile.CullRadiusCfg.Value;
			return sqrMagnitude <= value * value;
		}

		private static Vector3 SafeClosestPoint(Collider col, Vector3 targetWorldPos)
		{
			if (col == null)
			{
				return targetWorldPos;
			}
			if (!(col is BoxCollider) && !(col is SphereCollider) && !(col is CapsuleCollider))
			{
				MeshCollider val = col is MeshCollider ? (MeshCollider)col : null;
				if (val != null && val.convex)
				{
					return val.ClosestPoint(targetWorldPos);
				}
				Bounds bounds = col.bounds;
				return bounds.ClosestPoint(targetWorldPos);
			}
			return col.ClosestPoint(targetWorldPos);
		}

		private static bool ShouldCullCollider(Camera cam, Vector3 playerEyePos, Collider col, out Component root)
		{
			root = null;
			GameObject val = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
			if (val == null)
			{
				return false;
			}
			if (val.GetComponentInParent<Player>() != null)
			{
				return false;
			}
			if (val.GetComponentInParent<Character>() != null)
			{
				return false;
			}
			if (val.GetComponentInParent<ItemDrop>() != null)
			{
				return false;
			}
			if (!IsObstructingPlayer(cam, playerEyePos, col))
			{
				return false;
			}
			Piece componentInParent = val.GetComponentInParent<Piece>();
			if (componentInParent != null)
			{
				root = componentInParent;
				return true;
			}
			root = val.transform;
			return true;
		}

		private static void CullUnderRoot(Component root)
		{
			if (root != null)
			{
				GameObject gameObject = root.gameObject;
				Logger.Log("CULL UNDER ROOT [HoverFix] Hit " + gameObject.name + ", " + $"occluder={gameObject.GetComponentInParent<ClearSightOccluderTag>() != null}, " + $"hasHover={gameObject.GetComponentInParent<Hoverable>() != null}, " + $"hasInteract={gameObject.GetComponentInParent<Interactable>() != null}");
				if (gameObject != null && gameObject.GetComponent<ClearSightOccluderTag>() == null)
				{
					gameObject.AddComponent<ClearSightOccluderTag>();
				}
				if (ConfigurationFile.UseTransparency && ClearSightTransparentTemplateProvider.Template != null)
				{
					FadeRenderersUnder(root, ConfigurationFile.FadeAlpha);
				}
				else
				{
					HideRenderersUnder(root);
				}
			}
		}

		private static void HideRenderersUnder(Component root)
		{
			Renderer[] componentsInChildren = root.GetComponentsInChildren<Renderer>(true);
			foreach (Renderer val in componentsInChildren)
			{
				if (val != null && !hiddenRendererSet.Contains(val) && (val.enabled || originalEnabled.ContainsKey(val)))
				{
					if (!originalEnabled.ContainsKey(val))
					{
						originalEnabled[val] = val.enabled;
						originalForceOff[val] = val.forceRenderingOff;
					}
					val.forceRenderingOff = true;
					hiddenRendererSet.Add(val);
					hiddenRenderers.Add(val);
				}
			}
		}

		private static void FadeRenderersUnder(Component root, float targetAlpha)
		{
			Renderer[] componentsInChildren = root.GetComponentsInChildren<Renderer>(true);
			targetAlpha = Mathf.Clamp01(targetAlpha);
			Renderer[] array = componentsInChildren;
			foreach (Renderer val in array)
			{
				if (val == null)
				{
					continue;
				}
				if (!fadeData.TryGetValue(val, out RendererFadeData value))
				{
					value = new RendererFadeData
					{
						Renderer = val,
						OriginalShared = val.sharedMaterials,
						GhostMats = BuildGhostMaterials(val.sharedMaterials)
					};
					fadeData.Add(val, value);
				}
				if (value.GhostMats == null || value.GhostMats.Length == 0)
				{
					continue;
				}
				val.sharedMaterials = value.GhostMats;
				Material[] ghostMats = value.GhostMats;
				foreach (Material val2 in ghostMats)
				{
					if (val2 != null && val2.HasProperty("_Color"))
					{
						Color color = val2.color;
						color.a = targetAlpha;
						val2.color = color;
					}
				}
			}
		}

		private static Material[] BuildGhostMaterials(Material[] originals)
		{
			if (originals == null || originals.Length == 0)
			{
				return Array.Empty<Material>();
			}
			Material template = ClearSightTransparentTemplateProvider.Template;
			if (template == null)
			{
				return Array.Empty<Material>();
			}
			Material[] array = new Material[originals.Length];
			for (int i = 0; i < originals.Length; i++)
			{
				Material val = originals[i];
				if (val != null)
				{
					Material val2 = new Material(template)
					{
						name = val.name + " (ClearSightGhost)",
						mainTexture = val.mainTexture,
						mainTextureScale = val.mainTextureScale,
						mainTextureOffset = val.mainTextureOffset
					};
					if (val.HasProperty("_Color") && val2.HasProperty("_Color"))
					{
						Color color = val.color;
						color.a = ConfigurationFile.FadeAlpha;
						val2.color = color;
					}
					else if (val2.HasProperty("_Color"))
					{
						Color color2 = val2.color;
						color2.a = ConfigurationFile.FadeAlpha;
						val2.color = color2;
					}
					array[i] = val2;
				}
			}
			return array;
		}

		private static void RestoreVisuals()
		{
			if (fadeData.Count > 0)
			{
				foreach (KeyValuePair<Renderer, RendererFadeData> fadeDatum in fadeData)
				{
					RendererFadeData value = fadeDatum.Value;
					if (value.Renderer != null && value.OriginalShared != null && value.OriginalShared.Length != 0)
					{
						value.Renderer.sharedMaterials = value.OriginalShared;
					}
				}
			}
			if (hiddenRenderers.Count <= 0)
			{
				return;
			}
			for (int i = 0; i < hiddenRenderers.Count; i++)
			{
				Renderer val = hiddenRenderers[i];
				if (val != null)
				{
					if (originalEnabled.TryGetValue(val, out var value2))
					{
						val.enabled = value2;
					}
					if (originalForceOff.TryGetValue(val, out var value3))
					{
						val.forceRenderingOff = value3;
					}
					else
					{
						val.forceRenderingOff = false;
					}
				}
			}
			hiddenRenderers.Clear();
			hiddenRendererSet.Clear();
			originalEnabled.Clear();
			originalForceOff.Clear();
		}
	}
}