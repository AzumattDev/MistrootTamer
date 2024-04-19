using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MistrootTamer;

// Start Bloom
// InBloomTransition
// Start Debloom
// Bloomed

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ZNetSceneAwakePatch
{
    static void Postfix(ZNetScene __instance)
    {
        GameObject? fab = __instance.GetPrefab("Mistroot");
        Destructible? destructible = fab.GetComponent<Destructible>();
        destructible.m_hitEffect = __instance.GetPrefab("Pickable_Flax_Wild").GetComponent<Destructible>().m_hitEffect;
        destructible.m_destroyedEffect = __instance.GetPrefab("Pickable_Flax_Wild").GetComponent<Destructible>().m_destroyedEffect;
    }
}

[HarmonyPatch(typeof(Destructible), nameof(Destructible.Awake))]
static class DestructibleAwakePatch
{
    // Coroutine to bloom the mistroot after a certain time
    public static Coroutine m_coroutine;

    static void Postfix(Destructible __instance)
    {
        if (__instance.gameObject.name.Replace("(Clone)", "") == "Mistroot")
        {
            // __instance.m_onDamaged = () => SetAnimationToDebloom(__instance);
            __instance.m_hitEffect = ZNetScene.instance.GetPrefab("Pickable_Flax_Wild").GetComponent<Destructible>().m_hitEffect;
            __instance.m_destroyedEffect = ZNetScene.instance.GetPrefab("Pickable_Flax_Wild").GetComponent<Destructible>().m_destroyedEffect;
            if (!__instance.m_nview || __instance.m_nview.GetZDO() == null)
                return;
            __instance.StartCoroutine(DestructibleDestroyPatch.TimeToBloom(__instance));
        }
    }

    public static void SetAnimationToDebloom(Destructible d)
    {
        if (!d.m_nview.IsValid() || !d.m_nview.IsOwner() || d.m_destroyed)
            return;
        if (d.m_nview.GetZDO().GetFloat(ZDOVars.s_health) < (d.m_health * 0.5f))
        {
            Animator? animator = d.gameObject.GetComponent<Animator>();
            animator.SetTrigger("Start Debloom");
        }
    }

    public static void SetAnimationToBloom(Destructible d)
    {
        if (!d.m_nview.IsValid() || !d.m_nview.IsOwner() || d.m_destroyed)
            return;
        if (d.m_nview.GetZDO().GetFloat(ZDOVars.s_health) >= (d.m_health * 0.5f))
        {
            Animator? animator = d.gameObject.GetComponent<Animator>();
            animator.SetTrigger("Start Bloom");
        }
    }
}

[HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy), typeof(Vector3), typeof(Vector3))]
static class DestructibleDestroyPatch
{
    public static float m_ttBloom = 10f; // Time to rebloom after being hit

    static bool Prefix(Destructible __instance)
    {
        if (__instance.gameObject.name.Replace("(Clone)", "") == "Mistroot")
        {
            // Trigger debloom animations/effects
            DestructibleAwakePatch.SetAnimationToDebloom(__instance);
            MistrootTamerPlugin.MistrootTamerLogger.LogInfo("Mistroot is deblooming");

            if (__instance.m_nview.IsValid() && __instance.m_nview.IsOwner())
            {
                __instance.m_nview.GetZDO().Set("ShouldBloom", true);
                __instance.StartCoroutine(TimeToBloom(__instance));
            }

            return false; // Prevent the default destroy behavior
        }

        return true; // Continue with normal behavior for other objects
    }

    internal static IEnumerator TimeToBloom(Destructible d)
    {
        yield return new WaitForSeconds(m_ttBloom);

        if (d.m_nview.IsValid() && d.m_nview.IsOwner())
        {
            if (d.m_nview.GetZDO().GetBool("ShouldBloom"))
            {
                d.m_nview.GetZDO().Set("ShouldBloom", false);
                d.m_nview.GetZDO().Set(ZDOVars.s_health, d.m_health); // Reset health

                DestructibleAwakePatch.SetAnimationToBloom(d);

                MistrootTamerPlugin.MistrootTamerLogger.LogInfo($"Mistroot has rebloomed with health: {d.m_health}");
            }
        }
    }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
static class ZoneSystemStartPatch
{
    static void Prefix(ZoneSystem __instance)
    {
        GameObject? Mistroot = ZNetScene.instance.GetPrefab("Mistroot");
        ZoneSystem.ZoneVegetation vegetation = new()
        {
            m_name = Mistroot.name,
            m_prefab = Mistroot,
            m_enable = true,
            m_max = 2,
            m_forcePlacement = true,
            m_scaleMin = 1f,
            m_scaleMax = 1.75f,
            m_chanceToUseGroundTilt = 0,
            m_biome = Heightmap.Biome.Mistlands,
            m_biomeArea = Heightmap.BiomeArea.Everything,
            m_blockCheck = true,
            m_minAltitude = 0.01f,
            m_maxAltitude = 1000f,
            m_groupSizeMin = 1,
            m_groupSizeMax = 6,
            m_groupRadius = 6f,
            m_inForest = false,
            m_forestTresholdMin = 0,
            m_forestTresholdMax = 1f,
            m_foldout = false
        };

        __instance.m_vegetation.Add(vegetation);
    }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.ValidateVegetation))]
static class ZoneSystemValidatePatch
{
    static void Prefix(ZoneSystem __instance)
    {
        GameObject? Mistroot = ZNetScene.instance.GetPrefab("Mistroot");
        ZoneSystem.ZoneVegetation vegetation = new()
        {
            m_name = Mistroot.name,
            m_prefab = Mistroot,
            m_enable = true,
            m_max = 2,
            m_forcePlacement = true,
            m_scaleMin = 1f,
            m_scaleMax = 1.75f,
            m_chanceToUseGroundTilt = 0,
            m_biome = Heightmap.Biome.Mistlands,
            m_biomeArea = Heightmap.BiomeArea.Everything,
            m_blockCheck = true,
            m_minAltitude = 0.01f,
            m_maxAltitude = 1000f,
            m_groupSizeMin = 1,
            m_groupSizeMax = 6,
            m_groupRadius = 6f,
            m_inForest = false,
            m_forestTresholdMin = 0,
            m_forestTresholdMax = 1f,
            m_foldout = false
        };

        __instance.m_vegetation.Add(vegetation);
    }
}

[HarmonyPatch(typeof(MistEmitter), nameof(MistEmitter.SetEmit))]
static class MistEmitterAwakePatch
{
    static void Prefix(MistEmitter __instance, ref bool emit)
    {
        try
        {
            if (__instance.gameObject.transform.root.gameObject.name.Replace("(Clone)", "") != "Mistroot")
            {
                Object.DestroyImmediate(__instance.gameObject);
            }
        }
        catch (Exception e)
        {
            MistrootTamerPlugin.MistrootTamerLogger.LogError(e);
        }
    }
}

[HarmonyPatch(typeof(ParticleMist), nameof(ParticleMist.Awake))]
static class ParticleMistAwakePatch
{
    static void Postfix(ParticleMist __instance)
    {
        try
        {
            if (__instance != null && __instance.m_ps != null && __instance.gameObject.transform.root.gameObject.name.Replace("(Clone)", "") != "Mistroot")
                Object.DestroyImmediate(__instance.gameObject);
        }
        catch (Exception e)
        {
            MistrootTamerPlugin.MistrootTamerLogger.LogError(e);
        }
    }
}