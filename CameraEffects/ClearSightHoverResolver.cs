using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CinematicBoss.CameraEffects
{
    public static class ClearSightHoverResolver
	{
		internal static bool TryResolveHoverBehindOccluder(Player player, out GameObject mostConvenientHover, out Character mostConvenientChar)
		{
			mostConvenientHover = null;
			mostConvenientChar = null;
			GameCamera instance = GameCamera.instance;
			if (instance == null || ModUtils.GetPrivateValue(instance, "m_camera") == null)
				return false;
			
			Camera camera = (Camera)ModUtils.GetPrivateValue(instance, "m_camera");
			Vector3 position = camera.transform.position;
			Vector3 forward = camera.transform.forward;
			int interactMask = (int)ModUtils.GetPrivateValue(player, "m_interactMask");
			RaycastHit[] raycastHitsOrigin = Physics.RaycastAll(position, forward, 50f, interactMask, (QueryTriggerInteraction)2);
			if (raycastHitsOrigin == null || raycastHitsOrigin.Length == 0)
				return false;
			
			Array.Sort(raycastHitsOrigin, (a, b) => a.distance.CompareTo(b.distance));
			RaycastHit[] raycastHits = raycastHitsOrigin;
			for (int i = 0; i < raycastHits.Length; i++)
			{
				RaycastHit raycastHit = raycastHits[i];
				Collider collider = raycastHit.collider;
				if (collider == null)
					continue;
				
				GameObject go = collider.attachedRigidbody ? collider.attachedRigidbody.gameObject : collider.gameObject;
				if (go == null)
					continue;
				
				bool flag = go.GetComponentInParent<ClearSightOccluderTag>() != null;
				Hoverable hoverable = go.GetComponentInParent<Hoverable>();
				Interactable interactable = go.GetComponentInParent<Interactable>();
				Character character = go.GetComponentInParent<Character>();
				if (go.GetComponentInParent<Player>() == player || (flag && character == null && hoverable == null && interactable == null))
					continue;
				
				if (character != null && character != player)
				{
					mostConvenientHover = character.gameObject;
					mostConvenientChar = character;
					break;
				}
				if (hoverable != null || interactable != null)
				{
					var componentToPickUp = (Component)hoverable;
					if (componentToPickUp == null)
						componentToPickUp = (Component)interactable;
					
					Component component = componentToPickUp;
					if (component != null)
					{
						mostConvenientChar = (mostConvenientHover = component.gameObject).GetComponentInParent<Character>();
						break;
					}
				}
				Piece piece = go.GetComponentInParent<Piece>();
				if (piece != null)
				{
					mostConvenientHover = piece.gameObject;
					mostConvenientChar = piece.GetComponentInParent<Character>();
					break;
				}
			}
			return mostConvenientHover != null;
		}
	}
	
    public sealed class ClearSightOccluderTag : MonoBehaviour
	{
	}
	internal static class ClearSightTransparentTemplateProvider
	{
		private static bool initialized;

		private static Material _template;

		internal static Material Template
		{
			get
			{
				if (!initialized)
				{
					InitializeLazy();
				}
				return _template;
			}
		}

		internal static void BootstrapFromFejdStartup(FejdStartup fejd)
		{
			if (initialized || fejd == null || fejd.m_objectDBPrefab == null)
				return;
			
			try
			{
				ZNetScene component = fejd.m_objectDBPrefab.GetComponent<ZNetScene>();
				if (component != null && FindTransparentInArmorLeather(component))
					initialized = true;
			}
			catch (Exception ex)
			{
				Logger.LogError($"Error at BootstrapFromFejdStartup. {ex}");
			}
		}

		private static void InitializeLazy()
		{
			if (!initialized)
			{
				initialized = true;
				if (_template == null && !FindTransparentInAnyStandardMaterial() && !FindTransparentInTransparentMaterial())
					Logger.LogWarning("Transparent template not found.");
			}
		}

		private static bool FindTransparentInArmorLeather(ZNetScene scene)
		{
			if (scene == null)
				return false;
			
			string[] array = { "ArmorLeather", "ArmorLeatherChest", "ArmorLeatherLegs", "ArmorLeatherHelmet" };
			foreach (string prefabName in array)
			{
				GameObject go = scene.m_prefabs.FirstOrDefault(x => x.name == prefabName);
				if (go != null && HasTemplateInRendersList(go.GetComponentsInChildren<Renderer>(true)))
					return true;
			}
			return false;
		}

		private static bool FindTransparentInAnyStandardMaterial()
		{
			try
			{
				Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
				foreach (Material material in materials)
				{
					if (material != null && material.shader != null && material.shader.name.Contains("Standard") && HasTemplateInMaterial(material, "Standard material '" + material.name + "'"))
						return true;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"Error in FindTransparentInAnyStandardMaterial. {ex}");
			}
			return false;
		}

		private static bool FindTransparentInTransparentMaterial()
		{
			try
			{
				foreach (Material material in from m in Resources.FindObjectsOfTypeAll<Material>()
					orderby m.renderQueue
					select m)
				{
					if (material != null && material.HasProperty("_Color") && material.HasProperty("_MainTex") && material.renderQueue >= 3000 && HasTemplateInMaterial(material, "transparent material '" + material.name + "'"))
						return true;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"Error at FindTransparentInTransparentMaterial. {ex}");
			}
			return false;
		}

		private static bool HasTemplateInRendersList(IEnumerable<Renderer> renderers)
		{
			foreach (Renderer renderer in renderers)
			{
				if (renderer == null)
					continue;
				
				Material[] sharedMaterials = renderer.sharedMaterials;
				if (sharedMaterials == null)
					continue;
				
				Material[] materials = sharedMaterials;
				foreach (Material material in materials)
				{
					if (material != null && material.shader != null && material.shader.name.Contains("Standard") && HasTemplateInMaterial(material, "ArmorLeather material '" + (material.name + "'")))
						return true;
				}
			}
			return false;
		}

		private static bool HasTemplateInMaterial(Material source, string reason)
		{
			if (source == null || source.shader == null)
				return false;
			
			Material material = new Material(source)
			{
				name = "ClearSight_TransparentTemplate"
			};
			if (material.HasProperty("_Mode") && material.HasProperty("_SrcBlend") && material.HasProperty("_DstBlend") && material.HasProperty("_ZWrite"))
			{
				material.SetFloat("_Mode", 2f);
				material.SetInt("_SrcBlend", 5);
				material.SetInt("_DstBlend", 10);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			}
			material.renderQueue = 3000;
			_template = material;
			return true;
		}
	}
	
}