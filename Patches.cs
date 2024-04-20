/*using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MistrootTamer;
/* Animation Triggers
 ********************
 * Start Debloom
 * Start Bloom
 * InBloomTransition
 * Bloomed
 #1#

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ZNetSceneAwakePatch
{
    static void Postfix(ZNetScene __instance)
    {
        GameObject? fab = __instance.GetPrefab("Mistroot");
        fab.AddComponent<AzuMist>();
        Destructible? destructible = fab.GetComponent<Destructible>();
        destructible.m_hitEffect = __instance.GetPrefab("Pickable_Flax_Wild").GetComponent<Destructible>().m_hitEffect;
        destructible.m_destroyedEffect = __instance.GetPrefab("Pickable_Flax_Wild").GetComponent<Destructible>().m_destroyedEffect;
    }
}

/*[HarmonyPatch(typeof(Destructible), nameof(Destructible.Awake))]
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
}#1#
/*[HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy), typeof(Vector3), typeof(Vector3))]
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
}#1#

/*[HarmonyPatch(typeof(Destructible), nameof(Destructible.Awake))]
static class DestructibleAwakePatch
{
    static void Postfix(Destructible __instance)
    {
        if (__instance.gameObject.name.Replace("(Clone)", "") == "Mistroot")
        {
            // Initialize ZDO properties
            if (__instance.m_nview.IsValid() && __instance.m_nview.GetZDO() != null)
            {
                __instance.m_nview.GetZDO().Set("IsDeblooming", false);
                __instance.m_nview.GetZDO().Set("IsBlooming", true);
            }

            __instance.StartCoroutine(TimeToBloom(__instance));
        }
    }#1#

    /*static void Postfix(Destructible __instance)
    {
        if (__instance.gameObject.name.Replace("(Clone)", "") == "Mistroot")
        {
            if (__instance.m_nview.IsValid() && __instance.m_nview.GetZDO() != null)
            {
                long currentServerTime = ZNet.instance.GetTime().Ticks;
                long storedTransitionTime = __instance.m_nview.GetZDO().GetLong("NextTransitionTime", currentServerTime);

                if (storedTransitionTime > currentServerTime)
                {
                    // Wait until the stored transition time to make changes
                    __instance.StartCoroutine(TimeToTransition(__instance, storedTransitionTime - currentServerTime));
                }
                else
                {
                    // If time has passed, trigger appropriate state change
                    UpdateState(__instance);
                }
            }
        }
    }#1#

    /*internal static IEnumerator TimeToTransition(Destructible d, long delayTicks)
    {
        yield return new WaitForSeconds((float)new TimeSpan(delayTicks).TotalSeconds);
        UpdateState(d);
    }#1#

    /*
    private static void UpdateState(Destructible d)
    {
        if (d.m_nview.IsValid())
        {
            bool isDeblooming = d.m_nview.GetZDO().GetBool("IsDeblooming", false);
            if (isDeblooming)
            {
                // Transition from deblooming to blooming
                d.m_nview.GetZDO().Set("IsBlooming", true);
                d.m_nview.GetZDO().Set("IsDeblooming", false);
                Animator? animator = d.gameObject.GetComponent<Animator>();
                animator.SetTrigger("Start Bloom");
            }
            else
            {
                // Currently blooming, set for deblooming
            }
        }
    }#1#

    /*
    internal static IEnumerator TimeToBloom(Destructible d)
    {
        yield return new WaitForSeconds(DestructibleDestroyPatch.m_ttBloom);
        if (d.m_nview.IsValid() && d.m_nview.IsOwner() && d.m_nview.GetZDO().GetBool("IsDeblooming"))
        {
            d.m_nview.GetZDO().Set("IsBlooming", true);
            d.m_nview.GetZDO().Set("IsDeblooming", false);
            d.m_nview.GetZDO().Set(ZDOVars.s_health, d.m_health); // Reset health
            // Trigger bloom animation
            Animator? animator = d.gameObject.GetComponent<Animator>();
            animator.SetTrigger("Start Bloom");
        }
    }
}/*

[HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy), typeof(Vector3), typeof(Vector3))]
static class DestructibleDestroyPatch
{
    /*public static float m_ttBloom = 60f; // Time to rebloom after being hit

    static bool Prefix(Destructible __instance)
    {
        if (__instance.gameObject.name.Replace("(Clone)", "") == "Mistroot")
        {
            // Check and set flags to control bloom behavior
            if (__instance.m_nview.IsValid() && __instance.m_nview.IsOwner())
            {
                if (!__instance.m_nview.GetZDO().GetBool("IsDeblooming") && __instance.m_nview.GetZDO().GetBool("IsBlooming"))
                {
                    __instance.m_nview.GetZDO().Set("IsDeblooming", true);
                    __instance.m_nview.GetZDO().Set("IsBlooming", false);
                    // Trigger debloom animation
                    Animator? animator = __instance.gameObject.GetComponent<Animator>();
                    animator.SetTrigger("Start Debloom");
                }

                __instance.StartCoroutine(DestructibleAwakePatch.TimeToBloom(__instance));
            }

            return false; // Prevent the default destroy behavior
        }

        return true; // Continue with normal behavior for other objects
    }#1#

    /*static bool Prefix(Destructible __instance)
    {
        if (__instance.gameObject.name.Replace("(Clone)", "") == "Mistroot")
        {
            if (__instance.m_nview.IsValid() && __instance.m_nview.IsOwner())
            {
                ZDO zdo = __instance.m_nview.GetZDO();
                if (!zdo.GetBool("IsDeblooming") && zdo.GetBool("IsBlooming"))
                {
                    long nextTransitionTime = ZNet.instance.GetTime().Ticks + TimeSpan.FromSeconds(m_ttBloom).Ticks;
                    zdo.Set("NextTransitionTime", nextTransitionTime);
                    zdo.Set("IsDeblooming", true);
                    zdo.Set("IsBlooming", false);

                    Animator? animator = __instance.gameObject.GetComponent<Animator>();
                    animator.SetTrigger("Start Debloom");

                    __instance.StartCoroutine(DestructibleAwakePatch.TimeToTransition(__instance, TimeSpan.FromSeconds(m_ttBloom).Ticks));
                }

                return false; // Prevent the default destroy behavior
            }
        }

        return true; // Continue with normal behavior for other objects
    }
}/*

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
}*/