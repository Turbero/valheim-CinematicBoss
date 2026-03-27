using System;
using System.Collections.Generic;
using UnityEngine;

namespace CinematicBoss.CameraEffects
{
	internal static class CameraClearController
	{
		class RendererFadeElements
		{
			public Renderer Renderer;
			public Material[] OriginalShared = Array.Empty<Material>();
			public Material[] GhostMats = Array.Empty<Material>();
		}

		private static readonly List<Renderer> hiddenRenderers = new List<Renderer>();
		private static readonly HashSet<Renderer> hiddenRendererSet = new HashSet<Renderer>();
		private static readonly Dictionary<Renderer, bool> originalEnabled = new Dictionary<Renderer, bool>();
		private static readonly Dictionary<Renderer, bool> originalForceOff = new Dictionary<Renderer, bool>();
		private static readonly Dictionary<Renderer, RendererFadeElements> fadeData = new Dictionary<Renderer, RendererFadeElements>();

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
			if (!ConfigurationFile.transparencyWhenInvokingBoss.Value) return;
			
			if (cam == null || ModUtils.GetPrivateValue(cam, "m_camera") == null)
				return;
			
			RestoreVisuals();
			if ((bool)ModUtils.GetPrivateValue(cam, "m_freeFly"))
				return;
			
			Player localPlayer = Player.m_localPlayer;
			if (localPlayer == null)
				return;
			
			if (Cutscene.State != Cutscene.CinematicState.Inactive && ConfigurationFile.transparencyWhenInvokingBossList.Value.Contains(Cutscene.BossName))
				ApplyTransparency(cam, localPlayer);
		}

		private static Vector3 GetCameraTargetPosition(Player player)
		{
			if (player.m_eye != null)
				return player.m_eye.position;
			
			var head = ModUtils.GetPrivateValue(player, "m_head");
			if (head != null)
				return ((Transform)head).position;
			
			return player.transform.position + Vector3.up * 1.6f;
		}

		private static void ApplyTransparency(GameCamera cam, Player player)
		{
			var camera = ModUtils.GetPrivateValue(cam, "m_camera");
			if (camera == null)
				return;
			
			Vector3 position = ((Camera)camera).transform.position;
			Vector3 cameraTargetPosition = GetCameraTargetPosition(player);
			Vector3 diff = cameraTargetPosition - position;
			float magnitude = diff.magnitude;
			if (magnitude < 0.1f)
				return;
			
			float maxDistance = Mathf.Min(magnitude, ConfigurationFile.transparencyMaxDistance.Value);
			Vector3 relation = diff / magnitude;
			float areaEffect = Mathf.Max(0.05f, ConfigurationFile.transparencyRadiusAreaEffect.Value);
			RaycastHit[] raycastHitsOrigin = Physics.SphereCastAll(position, areaEffect, relation, maxDistance, GeometryMask, (QueryTriggerInteraction)2);
			if (raycastHitsOrigin.Length == 0)
				return;
			
			RaycastHit[] raycastHits = raycastHitsOrigin;
			for (int i = 0; i < raycastHits.Length; i++)
			{
				RaycastHit raycastHit = raycastHits[i];
				Collider collider = raycastHit.collider;
				if (collider != null && HasToApplyTransparencyToCollider((Camera)camera, cameraTargetPosition, collider, out Component root))
					TransparencyOnComponent(root);
			}
		}

		private static bool IsBlockingPlayer(Camera camera, Vector3 playerEyePos, Collider collider)
		{
			Vector3 position = camera.transform.position;
			Vector3 diff = playerEyePos - position;
			float magnitude = diff.magnitude;
			if (magnitude < 0.1f)
				return false;
			
			Vector3 relation = diff / magnitude;
			Vector3 closestPoint = SafeClosestPoint(collider, playerEyePos);
			float dotValue = Vector3.Dot(closestPoint - position, relation);
			float magnitudeValue = magnitude;
			if (dotValue <= 0f || dotValue >= magnitudeValue + 0.05f)
				return false;
			
			Vector3 playerPointViewport = camera.WorldToViewportPoint(playerEyePos);
			Vector3 closestPointViewport = camera.WorldToViewportPoint(closestPoint);
			if (closestPointViewport.z <= 0f)
				return false;
			
			Vector2 playerViewport = new Vector2(playerPointViewport.x, playerPointViewport.y);
			Vector2 closestViewport = new Vector2(closestPointViewport.x, closestPointViewport.y);
			Vector2 diffViewport = playerViewport - closestViewport;
			float sqrMagnitude = diffViewport.sqrMagnitude;
			float radiusAreaEffect = ConfigurationFile.transparencyRadiusAreaEffect.Value;
			return sqrMagnitude <= radiusAreaEffect * radiusAreaEffect;
		}

		private static Vector3 SafeClosestPoint(Collider collider, Vector3 targetWorldPos)
		{
			if (collider == null)
				return targetWorldPos;
			
			if (!(collider is BoxCollider) && !(collider is SphereCollider) && !(collider is CapsuleCollider))
			{
				MeshCollider meshCollider = collider is MeshCollider ? (MeshCollider)collider : null;
				if (meshCollider != null && meshCollider.convex)
					return meshCollider.ClosestPoint(targetWorldPos);
				
				Bounds bounds = collider.bounds;
				return bounds.ClosestPoint(targetWorldPos);
			}
			return collider.ClosestPoint(targetWorldPos);
		}

		private static bool HasToApplyTransparencyToCollider(Camera camera, Vector3 playerEyePos, Collider collider, out Component root)
		{
			root = null;
			GameObject go = collider.attachedRigidbody != null ? collider.attachedRigidbody.gameObject : collider.gameObject;
			if (go == null)
				return false;
			
			if (go.GetComponentInParent<Player>() != null)
				return false;
			
			if (go.GetComponentInParent<Character>() != null)
				return false;
			
			if (go.GetComponentInParent<ItemDrop>() != null)
				return false;
			
			if (!IsBlockingPlayer(camera, playerEyePos, collider))
				return false;
			
			Piece parentPiece = go.GetComponentInParent<Piece>();
			if (parentPiece != null)
			{
				root = parentPiece;
				return true;
			}
			root = go.transform;
			return true;
		}

		private static void TransparencyOnComponent(Component root)
		{
			if (root != null)
			{
				GameObject gameObject = root.gameObject;
				if (gameObject != null && gameObject.GetComponent<ClearSightOccluderTag>() == null)
					gameObject.AddComponent<ClearSightOccluderTag>();
				
				if (ConfigurationFile.transparencyWhenInvokingBoss.Value && ClearSightTransparentTemplateProvider.Template != null)
					FadeRenderersUnderComponent(root, ConfigurationFile.transparencyFadeAlpha.Value);
				else
					HideRenderersUnderComponent(root);
			}
		}

		private static void HideRenderersUnderComponent(Component root)
		{
			Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
			foreach (Renderer renderer in renderers)
			{
				if (renderer != null && !hiddenRendererSet.Contains(renderer) && (renderer.enabled || originalEnabled.ContainsKey(renderer)))
				{
					if (!originalEnabled.ContainsKey(renderer))
					{
						originalEnabled[renderer] = renderer.enabled;
						originalForceOff[renderer] = renderer.forceRenderingOff;
					}
					renderer.forceRenderingOff = true;
					hiddenRendererSet.Add(renderer);
					hiddenRenderers.Add(renderer);
				}
			}
		}

		private static void FadeRenderersUnderComponent(Component root, float targetAlpha)
		{
			Renderer[] renderersInChildren = root.GetComponentsInChildren<Renderer>(true);
			targetAlpha = Mathf.Clamp01(targetAlpha);
			Renderer[] renderers = renderersInChildren;
			foreach (Renderer renderer in renderers)
			{
				if (renderer == null)
					continue;
				
				if (!fadeData.TryGetValue(renderer, out RendererFadeElements rendererFadeElements))
				{
					rendererFadeElements = new RendererFadeElements
					{
						Renderer = renderer,
						OriginalShared = renderer.sharedMaterials,
						GhostMats = BuildGhostMaterials(renderer.sharedMaterials)
					};
					fadeData.Add(renderer, rendererFadeElements);
				}
				if (rendererFadeElements.GhostMats == null || rendererFadeElements.GhostMats.Length == 0)
					continue;
				
				renderer.sharedMaterials = rendererFadeElements.GhostMats;
				Material[] materials = rendererFadeElements.GhostMats;
				foreach (Material material in materials)
				{
					if (material != null && material.HasProperty("_Color"))
					{
						Color color = material.color;
						color.a = targetAlpha;
						material.color = color;
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
			Material[] materials = new Material[originals.Length];
			for (int i = 0; i < originals.Length; i++)
			{
				Material material = originals[i];
				if (material != null)
				{
					Material materialTemplate = new Material(template)
					{
						name = material.name + " (ClearSightGhost)",
						mainTexture = material.mainTexture,
						mainTextureScale = material.mainTextureScale,
						mainTextureOffset = material.mainTextureOffset
					};
					if (material.HasProperty("_Color") && materialTemplate.HasProperty("_Color"))
					{
						Color color = material.color;
						color.a = ConfigurationFile.transparencyFadeAlpha.Value;
						materialTemplate.color = color;
					}
					else if (materialTemplate.HasProperty("_Color"))
					{
						Color color2 = materialTemplate.color;
						color2.a = ConfigurationFile.transparencyFadeAlpha.Value;
						materialTemplate.color = color2;
					}
					materials[i] = materialTemplate;
				}
			}
			return materials;
		}

		public static void RestoreVisuals()
		{
			if (fadeData.Count > 0)
			{
				foreach (KeyValuePair<Renderer, RendererFadeElements> pair in fadeData)
				{
					RendererFadeElements value = pair.Value;
					if (value.Renderer != null && value.OriginalShared != null && value.OriginalShared.Length != 0)
						value.Renderer.sharedMaterials = value.OriginalShared;
				}
			}
			if (hiddenRenderers.Count <= 0)
			{
				return;
			}
			for (int i = 0; i < hiddenRenderers.Count; i++)
			{
				Renderer renderer = hiddenRenderers[i];
				if (renderer != null)
				{
					if (originalEnabled.TryGetValue(renderer, out var value2))
						renderer.enabled = value2;
					
					if (originalForceOff.TryGetValue(renderer, out var value3))
						renderer.forceRenderingOff = value3;
					else
						renderer.forceRenderingOff = false;
				}
			}
			hiddenRenderers.Clear();
			hiddenRendererSet.Clear();
			originalEnabled.Clear();
			originalForceOff.Clear();
		}
	}
}