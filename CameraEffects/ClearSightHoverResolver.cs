using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CinematicBoss.CameraEffects
{
    public static class ClearSightHoverResolver
	{
		internal static bool TryResolveHoverBehindOccluder(Player player, out GameObject bestHover, out Character bestChar)
		{
			bestHover = null;
			bestChar = null;
			GameCamera instance = GameCamera.instance;
			if (instance == null || ModUtils.GetPrivateValue(instance, "m_camera") == null)
			{
				return false;
			}
			Camera camera = (Camera)ModUtils.GetPrivateValue(instance, "m_camera");
			Vector3 position = camera.transform.position;
			Vector3 forward = camera.transform.forward;
			int interactMask = (int)ModUtils.GetPrivateValue(player, "m_interactMask");
			RaycastHit[] array = Physics.RaycastAll(position, forward, 50f, interactMask, (QueryTriggerInteraction)2);
			if (array == null || array.Length == 0)
			{
				return false;
			}
			Array.Sort(array, (a, b) => a.distance.CompareTo(b.distance));
			RaycastHit[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				RaycastHit val = array2[i];
				Collider collider = val.collider;
				if (collider == null)
				{
					continue;
				}
				GameObject val2 = collider.attachedRigidbody ? collider.attachedRigidbody.gameObject : collider.gameObject;
				if (val2 == null)
				{
					continue;
				}
				bool flag = val2.GetComponentInParent<ClearSightOccluderTag>() != null;
				Hoverable componentInParent = val2.GetComponentInParent<Hoverable>();
				Interactable componentInParent2 = val2.GetComponentInParent<Interactable>();
				Character componentInParent3 = val2.GetComponentInParent<Character>();
				Logger.Log("TryResolveHoverBehindOccluder[HoverFix] Hit " + val2.name + ", " + $"occluder={flag}, hasHover={componentInParent != null}, hasInteract={componentInParent2 != null}");
				if (val2.GetComponentInParent<Player>() == player || (flag && componentInParent3 == null && componentInParent == null && componentInParent2 == null))
				{
					continue;
				}
				if (componentInParent3 != null && componentInParent3 != player)
				{
					bestHover = componentInParent3.gameObject;
					bestChar = componentInParent3;
					break;
				}
				if (componentInParent != null || componentInParent2 != null)
				{
					var val3 = (Component)componentInParent;
					if (val3 == null)
					{
						val3 = (Component)componentInParent2;
					}
					Component val4 = val3;
					if (val4 != null)
					{
						bestChar = (bestHover = val4.gameObject).GetComponentInParent<Character>();
						break;
					}
				}
				Piece componentInParent4 = val2.GetComponentInParent<Piece>();
				if (componentInParent4 != null)
				{
					bestHover = componentInParent4.gameObject;
					bestChar = componentInParent4.GetComponentInParent<Character>();
					break;
				}
			}
			return bestHover != null;
		}
	}
	
    public sealed class ClearSightOccluderTag : MonoBehaviour
	{
	}
	internal static class ClearSightTransparentTemplateProvider
	{
		private static bool _initialized;

		private static Material _template;

		internal static Material Template
		{
			get
			{
				if (!_initialized)
				{
					InitializeLazy();
				}
				return _template;
			}
		}

		internal static void BootstrapFromFejdStartup(FejdStartup fejd)
		{
			if (_initialized || fejd == null || fejd.m_objectDBPrefab == null)
			{
				return;
			}
			try
			{
				ZNetScene component = fejd.m_objectDBPrefab.GetComponent<ZNetScene>();
				if (component != null && TryInitFromArmorLeather(component))
				{
					_initialized = true;
				}
			}
			catch (Exception arg)
			{
				Logger.Log($"FejdStartup bootstrap failed: {arg}");
			}
		}

		private static void InitializeLazy()
		{
			if (!_initialized)
			{
				_initialized = true;
				if (_template == null && !TryInitFromAnyStandardMaterial() && !TryInitFromAnyTransparentMaterial())
				{
					Logger.LogWarning("Could not find any suitable transparent template. Transparent culling will fall back to Hidden mode.");
				}
			}
		}

		private static bool TryInitFromArmorLeather(ZNetScene scene)
		{
			if (scene == null)
			{
				return false;
			}
			string[] array = { "ArmorLeather", "ArmorLeatherChest", "ArmorLeatherLegs", "ArmorLeatherHelmet" };
			foreach (string prefabName in array)
			{
				GameObject val = scene.m_prefabs.FirstOrDefault(x => x.name == prefabName);
				if (val != null && TryBuildTemplateFromRenderers(val.GetComponentsInChildren<Renderer>(true)))
				{
					return true;
				}
			}
			return false;
		}

		private static bool TryInitFromAnyStandardMaterial()
		{
			try
			{
				Material[] array = Resources.FindObjectsOfTypeAll<Material>();
				foreach (Material val in array)
				{
					if (val != null && val.shader != null && val.shader.name.Contains("Standard") && TryBuildTemplateFromMaterial(val, "Standard material '" + val.name + "'"))
					{
						return true;
					}
				}
			}
			catch (Exception arg)
			{
				Logger.Log($"Standard material scan failed: {arg}");
			}
			return false;
		}

		private static bool TryInitFromAnyTransparentMaterial()
		{
			try
			{
				foreach (Material item in from m in Resources.FindObjectsOfTypeAll<Material>()
					orderby m.renderQueue
					select m)
				{
					if (item != null && item.HasProperty("_Color") && item.HasProperty("_MainTex") && item.renderQueue >= 3000 && TryBuildTemplateFromMaterial(item, "transparent material '" + item.name + "'"))
					{
						return true;
					}
				}
			}
			catch (Exception arg)
			{
				Logger.Log($"Transparent material scan failed: {arg}");
			}
			return false;
		}

		private static bool TryBuildTemplateFromRenderers(IEnumerable<Renderer> renderers)
		{
			foreach (Renderer renderer in renderers)
			{
				if (renderer == null)
				{
					continue;
				}
				Material[] sharedMaterials = renderer.sharedMaterials;
				if (sharedMaterials == null)
				{
					continue;
				}
				Material[] array = sharedMaterials;
				foreach (Material val in array)
				{
					if (val != null && val.shader != null && val.shader.name.Contains("Standard") && TryBuildTemplateFromMaterial(val, "ArmorLeather material '" + (val.name + "'")))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool TryBuildTemplateFromMaterial(Material source, string reason)
		{
			if (source == null || source.shader == null)
			{
				return false;
			}
			Material val = new Material(source)
			{
				name = "ClearSight_TransparentTemplate"
			};
			if (val.HasProperty("_Mode") && val.HasProperty("_SrcBlend") && val.HasProperty("_DstBlend") && val.HasProperty("_ZWrite"))
			{
				val.SetFloat("_Mode", 2f);
				val.SetInt("_SrcBlend", 5);
				val.SetInt("_DstBlend", 10);
				val.SetInt("_ZWrite", 0);
				val.DisableKeyword("_ALPHATEST_ON");
				val.EnableKeyword("_ALPHABLEND_ON");
				val.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			}
			val.renderQueue = 3000;
			_template = val;
			Logger.LogInfo("Using " + reason + " (shader '" + source.shader.name + "') as transparent template.");
			return true;
		}
	}
	
}