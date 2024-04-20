using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MistrootTamer;

[HarmonyPatch(typeof(ParticleMist), nameof(ParticleMist.Awake))]
static class ParticleMistAwakePatch
{
    [UsedImplicitly]
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
            m_groupSizeMax = 3,
            m_groupRadius = 6f,
            m_inForest = false,
            m_forestTresholdMin = 0,
            m_forestTresholdMax = 1f,
            m_foldout = false
        };

        __instance.m_vegetation.Add(vegetation);
    }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
static class ZoneSystemStartPatch
{
    [UsedImplicitly]
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
            m_groupSizeMax = 3,
            m_groupRadius = 6f,
            m_inForest = false,
            m_forestTresholdMin = 0,
            m_forestTresholdMax = 1f,
            m_foldout = false
        };

        __instance.m_vegetation.Add(vegetation);
    }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ZNetSceneAwakePatch
{
    public static EffectList HitEffect = null!;
    public static EffectList DestroyEffect = null!;

    static void Postfix(ZNetScene __instance)
    {
        GameObject? fab = __instance.GetPrefab("Mistroot");
        fab.AddComponent<AzuMist>();
        HitEffect = __instance.GetPrefab("Pickable_Flax_Wild").GetComponent<Destructible>().m_hitEffect;
        DestroyEffect = __instance.GetPrefab("Pickable_Flax_Wild").GetComponent<Destructible>().m_destroyedEffect;
    }
}

public class AzuMist : MonoBehaviour, IDestructible
{
    public static float m_ttBloom = 60f; // Time to rebloom after being hit
    public static float MaxHealth = 100f;


    public static List<ParticleMist> BloomingMists = [];

    private ZNetView _znv = null!;
    private List<ParticleMist> _mists = null!;
    private static readonly int StartBloom = Animator.StringToHash("Start Bloom");
    private static readonly int StartDebloom = Animator.StringToHash("Start Debloom");

    private bool IsBlooming
    {
        get => _znv.m_zdo.GetBool("IsBlooming_Azu", false);
        set => _znv.m_zdo.Set("IsBlooming_Azu", value);
    }

    private float BloomTime
    {
        get => _znv.m_zdo.GetFloat("BloomTime_Azu", 0f);
        set => _znv.m_zdo.Set("BloomTime_Azu", value);
    }

    private float Health
    {
        get => _znv.m_zdo.GetFloat(ZDOVars.s_health, MaxHealth);
        set => _znv.m_zdo.Set(ZDOVars.s_health, value);
    }

    private void Awake()
    {
        _znv = GetComponent<ZNetView>();
        _mists = this.GetComponentsInChildren<ParticleMist>(true).ToList();
        if (!_znv || _znv.GetZDO() == null)
            return;
        if (!_znv.IsValid()) return;
        foreach (var mist in _mists) BloomingMists.Add(mist);
        DoAnimation(0, IsBlooming);
        _znv.Register<bool>("DoBloomAnimation", DoAnimation);
        _znv.Register<HitData>("Damage", OnDamage);
    }

    private void DoAnimation(long sender, bool isBloom)
    {
        Animator animator = GetComponent<Animator>();
        if (!isBloom)
        {
            animator.SetTrigger(StartBloom);
        }
        else
        {
            animator.SetTrigger(StartDebloom);
        }
    }

    private void FixedUpdate()
    {
        if (!_znv.IsValid() || !_znv.IsOwner() || !IsBlooming) return;
        BloomTime += Time.fixedDeltaTime;
        if (BloomTime < m_ttBloom) return;
        BloomTime = 0f;
        IsBlooming = false;
        Health = MaxHealth;
        _znv.InvokeRPC(ZNetView.Everybody, "DoBloomAnimation", IsBlooming);
    }

    private void OnDamage(long sender, HitData hit)
    {
        if (!_znv.IsOwner()) return;

        ZNetSceneAwakePatch.HitEffect.Create(transform.position, Quaternion.identity);

        if (IsBlooming) return;

        float damage = hit.GetTotalDamage();
        Health -= damage;
        DamageText.instance.ShowText(HitData.DamageModifier.Normal, hit.m_point, damage, false);

        if (Health > 0f) return;
        IsBlooming = true;
        _znv.InvokeRPC(ZNetView.Everybody, "DoBloomAnimation", IsBlooming);
        ZNetSceneAwakePatch.DestroyEffect.Create(transform.position, Quaternion.identity);
    }

    public void Damage(HitData hit)
    {
        if (!_znv.IsValid() || IsBlooming) return;
        _znv.InvokeRPC("Damage", hit);
    }

    public DestructibleType GetDestructibleType()
    {
        return DestructibleType.Tree;
    }

    private void OnDestroy()
    {
        foreach (var mist in _mists) BloomingMists.Remove(mist);
    }
}