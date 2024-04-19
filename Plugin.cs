using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CreatureManager;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using LocalizationManager;
using LocationManager;
using PieceManager;
using ServerSync;
using SkillManager;
using StatusEffectManager;
using UnityEngine;
using PrefabManager = ItemManager.PrefabManager;
using Range = LocationManager.Range;

namespace MistrootTamer
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class MistrootTamerPlugin : BaseUnityPlugin
    {
        internal const string ModName = "MistrootTamer";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource MistrootTamerLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        // Location Manager variables
        public Texture2D tex = null!;

        // Use only if you need them
        //private Sprite mySprite = null!;
        //private SpriteRenderer sr = null!;

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            //Localizer.Load();
            bool saveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet = false;

            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);


            // General settings for the mist effect
            Biome = config("1 - General", "Biome", Heightmap.Biome.Mistlands, "Defines the biome where the mist effect is active.");
            LocalRange = config("1 - General", "Local Range", 15f, "Sets the radius around the player within which mist particles are generated.");
            LocalEmissionRate = config("1 - General", "Local Emission Rate", 50, "Controls the base rate of mist particle generation around the player.");
            LocalEmissionPerUnit = config("1 - General", "Local Emission Per Unit", 30, "Determines the number of mist particles generated per unit of movement or time.");
            MaxMistAltitude = config("1 - General", "Max Mist Altitude", 10f, "Specifies the maximum height above the ground at which mist can appear.");

            // Settings for distant mist generation
            DistantMaxRange = config("2 - Misters", "Distant Max Range", 25f, "Defines the maximum distance from the player where mist can form.");
            DistantMinSize = config("2 - Misters", "Distant Min Size", 15f, "Sets the minimum size of distant mist particles.");
            DistantMaxSize = config("2 - Misters", "Distant Max Size", 20f, "Sets the maximum size of distant mist particles.");
            DistantEmissionMax = config("2 - Misters", "Distant Emission Max", 0.008f, "Controls the maximum emission rate for distant mist.");
            DistantEmissionMaxVelocity = config("2 - Misters", "Distant Emission Max Velocity", 2f, "Determines the maximum velocity at which distant mist particles move.");
            DistantThickness = config("2 - Misters", "Distant Thickness", 4f, "Specifies the thickness of the mist layer at a distance.");

            // Settings for mist dissipation
            MinDistance = config("3 - Demisters", "Min Distance", 5f, "Defines the minimum distance from a source where mist begins to dissipate.");
            MaxDistance = config("3 - Demisters", "Max Distance", 40f, "Defines the maximum distance from a source at which mist is fully dissipated.");
            EmissionMax = config("3 - Demisters", "Emission Max", 0.05f, "Sets the maximum rate of mist dissipation.");
            EmissionPerUnit = config("3 - Demisters", "Emission Per Unit", 20f, "Determines the rate of mist dissipation per unit of movement or time.");
            MinSize = config("3 - Demisters", "Min Size", 5f, "Sets the minimum size of mist particles near demisters.");
            MaxSize = config("3 - Demisters", "Max Size", 15f, "Sets the maximum size of mist particles near demisters.");

            DestructibleHealth = config("4 - Destructible Component", "Destructible Health", 500, "Sets the health of plant.");
            DestructibleBluntModifier = config("4 - Destructible Component", "Destructible Blunt Modifier", HitData.DamageModifier.Normal, "Sets the blunt damage modifier of plant.");
            DestructibleSlashModifier = config("4 - Destructible Component", "Destructible Slash Modifier", HitData.DamageModifier.Normal, "Sets the slash damage modifier of plant.");
            DestructiblePierceModifier = config("4 - Destructible Component", "Destructible Pierce Modifier", HitData.DamageModifier.Normal, "Sets the pierce damage modifier of plant.");
            DestructibleChopModifier = config("4 - Destructible Component", "Destructible Chop Modifier", HitData.DamageModifier.Normal, "Sets the chop damage modifier of plant.");
            DestructiblePickaxeModifier = config("4 - Destructible Component", "Destructible Pickaxe Modifier", HitData.DamageModifier.Normal, "Sets the pickaxe damage modifier of plant.");
            DestructibleFireModifier = config("4 - Destructible Component", "Destructible Fire Modifier", HitData.DamageModifier.Normal, "Sets the fire damage modifier of plant.");
            DestructibleFrostModifier = config("4 - Destructible Component", "Destructible Frost Modifier", HitData.DamageModifier.Normal, "Sets the frost damage modifier of plant.");
            DestructibleLightningModifier = config("4 - Destructible Component", "Destructible Lightning Modifier", HitData.DamageModifier.Normal, "Sets the lightning damage modifier of plant.");
            DestructibleSpiritModifier = config("4 - Destructible Component", "Destructible Spirit Modifier", HitData.DamageModifier.Normal, "Sets the spirit damage modifier of plant.");
            DestructiblePoisonModifier = config("4 - Destructible Component", "Destructible Poison Modifier", HitData.DamageModifier.Normal, "Sets the poison damage modifier of plant.");
            DestructibleMinimumDamageThreshold = config("4 - Destructible Component", "Destructible Minimum Damage Threshold", 0.0f, "Sets the minimum damage threshold of plant.");
            DestructibleMinimumToolTier = config("4 - Destructible Component", "Destructible Minimum Tool Tier", 0, "Sets the minimum tool tier of plant. Meaning the minimum strength (tool tier) your weapon must have to begin doing damage.");
            DestructibleHitNoise = config("4 - Destructible Component", "Destructible Hit Noise", 0.0f, "Sets the distance the noise made when plant is hit plays.");
            DestructibleDestroyNoise = config("4 - Destructible Component", "Destructible Destroy Noise", 0.0f, "Sets the distance the noise made when plant is destroyed plays.");
            DestructibleTriggerPrivateArea = config("4 - Destructible Component", "Destructible Trigger Private Area", Toggle.Off, "Sets whether the plant triggers the private area (vanilla wards) when destroyed.");
            DestructibleTTL = config("4 - Destructible Component", "Destructible TTL", 0.0f, "Sets the time to live of plant. It will auto destroy after this time. (in seconds)");
            DestructibleSpawnWhenDestroyed = config("4 - Destructible Component", "Destructible Spawn When Destroyed", "", "Sets the prefab to spawn when plant is destroyed. Limited to one prefab. Uses prefab name.");


            GameObject fab = PieceManager.PiecePrefabManager.RegisterPrefab("mistroottamer", "Mistroot");
            var mistToUpdate = Utils.FindChild(fab.transform, "ThickMist").GetComponent<ParticleMist>();
          //MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(fab.transform, "FollowPlayer").gameObject);
          //MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(fab.transform, "ThickMist_").gameObject);
          //MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(fab.transform, "LocalMist").gameObject);
            var destructibleToUpdate = fab.GetComponent<Destructible>();
            UpdateMistValues(mistToUpdate);
            UpdateDestructValues(destructibleToUpdate);
            Biome.SettingChanged += (sender, args) => UpdateMistrootComponents();
            LocalRange.SettingChanged += (sender, args) => UpdateMistrootComponents();
            LocalEmissionRate.SettingChanged += (sender, args) => UpdateMistrootComponents();
            LocalEmissionPerUnit.SettingChanged += (sender, args) => UpdateMistrootComponents();
            MaxMistAltitude.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DistantMaxRange.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DistantMinSize.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DistantMaxSize.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DistantEmissionMax.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DistantEmissionMaxVelocity.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DistantThickness.SettingChanged += (sender, args) => UpdateMistrootComponents();
            MinDistance.SettingChanged += (sender, args) => UpdateMistrootComponents();
            MaxDistance.SettingChanged += (sender, args) => UpdateMistrootComponents();
            EmissionMax.SettingChanged += (sender, args) => UpdateMistrootComponents();
            EmissionPerUnit.SettingChanged += (sender, args) => UpdateMistrootComponents();
            MinSize.SettingChanged += (sender, args) => UpdateMistrootComponents();
            MaxSize.SettingChanged += (sender, args) => UpdateMistrootComponents();

            DestructibleHealth.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleBluntModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleSlashModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructiblePierceModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleChopModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructiblePickaxeModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleFireModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleFrostModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleLightningModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleSpiritModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructiblePoisonModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleMinimumDamageThreshold.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleMinimumToolTier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleHitNoise.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleDestroyNoise.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleTriggerPrivateArea.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleTTL.SettingChanged += (sender, args) => UpdateMistrootComponents();
            DestructibleSpawnWhenDestroyed.SettingChanged += (sender, args) => UpdateMistrootComponents();


            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();

            if (saveOnSet)
            {
                Config.SaveOnConfigSet = saveOnSet;
                Config.Save();
            }
        }

        private void UpdateMistrootComponents()
        {
            foreach (ParticleMist particleMist in Resources.FindObjectsOfTypeAll<ParticleMist>().Where(x => x.transform.root.gameObject.name.Replace("(Clone)", "") == "Mistroot"))
            {
                UpdateMistValues(particleMist);
            }

            if (ZNetScene.instance == null) return;
            // Update all mistroot prefabs
            GameObject? fab = ZNetScene.instance.GetPrefab("Mistroot");
            if (fab == null) return;
            Destructible? d = fab.GetComponent<Destructible>();
            ParticleMist? thickMist = Utils.FindChild(fab.transform, "ThickMist").GetComponent<ParticleMist>();
            UpdateMistValues(thickMist);
            UpdateDestructValues(d);
        }

        private void UpdateMistValues(ParticleMist mist)
        {
            mist.m_biome = Biome.Value;
            mist.m_localRange = LocalRange.Value;
            mist.m_localEmission = LocalEmissionRate.Value;
            mist.m_localEmissionPerUnit = LocalEmissionPerUnit.Value;
            mist.m_maxMistAltitude = MaxMistAltitude.Value;
            // Misters
            mist.m_distantMaxRange = DistantMaxRange.Value;
            mist.m_distantMinSize = DistantMinSize.Value;
            mist.m_distantMaxSize = DistantMaxSize.Value;
            mist.m_distantEmissionMax = DistantEmissionMax.Value;
            mist.m_distantEmissionMaxVel = DistantEmissionMaxVelocity.Value;
            mist.m_distantThickness = DistantThickness.Value;
            // Demisters
            mist.m_minDistance = MinDistance.Value;
            mist.m_maxDistance = MaxDistance.Value;
            mist.m_emissionMax = EmissionMax.Value;
            mist.m_emissionPerUnit = EmissionPerUnit.Value;
            mist.m_minSize = MinSize.Value;
            mist.m_maxSize = MaxSize.Value;
        }

        private void UpdateDestructValues(Destructible d)
        {
            d.m_health = DestructibleHealth.Value;
            d.m_damages.m_blunt = DestructibleBluntModifier.Value;
            d.m_damages.m_slash = DestructibleSlashModifier.Value;
            d.m_damages.m_pierce = DestructiblePierceModifier.Value;
            d.m_damages.m_chop = DestructibleChopModifier.Value;
            d.m_damages.m_pickaxe = DestructiblePickaxeModifier.Value;
            d.m_damages.m_fire = DestructibleFireModifier.Value;
            d.m_damages.m_frost = DestructibleFrostModifier.Value;
            d.m_damages.m_lightning = DestructibleLightningModifier.Value;
            d.m_damages.m_spirit = DestructibleSpiritModifier.Value;
            d.m_damages.m_poison = DestructiblePoisonModifier.Value;
            d.m_minDamageTreshold = DestructibleMinimumDamageThreshold.Value;
            d.m_minToolTier = DestructibleMinimumToolTier.Value;
            d.m_hitNoise = DestructibleHitNoise.Value;
            d.m_destroyNoise = DestructibleDestroyNoise.Value;
            d.m_triggerPrivateArea = DestructibleTriggerPrivateArea.Value == Toggle.Off;
            d.m_ttl = DestructibleTTL.Value;
            if (ZNetScene.instance != null)
            {
                GameObject fab = ZNetScene.instance.GetPrefab(DestructibleSpawnWhenDestroyed.Value);
                if (fab != null)
                    d.m_spawnWhenDestroyed = fab;
                else
                {
                    MistrootTamerLogger.LogWarning($"Prefab {DestructibleSpawnWhenDestroyed.Value} not found in ZNetScene.");
                }
            }
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                MistrootTamerLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                MistrootTamerLogger.LogError($"There was an issue loading your {ConfigFileName}");
                MistrootTamerLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        private static ConfigEntry<Heightmap.Biome> Biome = null!;
        private static ConfigEntry<float> LocalRange = null!;
        private static ConfigEntry<int> LocalEmissionRate = null!;
        private static ConfigEntry<int> LocalEmissionPerUnit = null!;

        private static ConfigEntry<float> MaxMistAltitude = null!;

        // Misters
        private static ConfigEntry<float> DistantMaxRange = null!;
        private static ConfigEntry<float> DistantMinSize = null!;
        private static ConfigEntry<float> DistantMaxSize = null!;
        private static ConfigEntry<float> DistantEmissionMax = null!;
        private static ConfigEntry<float> DistantEmissionMaxVelocity = null!;

        private static ConfigEntry<float> DistantThickness = null!;

        // Demisters
        private static ConfigEntry<float> MinDistance = null!;
        private static ConfigEntry<float> MaxDistance = null!;
        private static ConfigEntry<float> EmissionMax = null!;
        private static ConfigEntry<float> EmissionPerUnit = null!;
        private static ConfigEntry<float> MinSize = null!;
        private static ConfigEntry<float> MaxSize = null!;

        // Destructible
        private static ConfigEntry<int> DestructibleHealth = null!;
        private static ConfigEntry<HitData.DamageModifier> DestructibleBluntModifier = null!;
        private static ConfigEntry<HitData.DamageModifier> DestructibleSlashModifier = null!;
        private static ConfigEntry<HitData.DamageModifier> DestructiblePierceModifier = null!;
        private static ConfigEntry<HitData.DamageModifier> DestructibleChopModifier = null!;
        private static ConfigEntry<HitData.DamageModifier> DestructiblePickaxeModifier = null!;
        private static ConfigEntry<HitData.DamageModifier> DestructibleFireModifier = null!;
        private static ConfigEntry<HitData.DamageModifier> DestructibleFrostModifier = null!;
        private static ConfigEntry<HitData.DamageModifier> DestructibleLightningModifier = null!;
        private static ConfigEntry<HitData.DamageModifier> DestructibleSpiritModifier = null!;
        private static ConfigEntry<HitData.DamageModifier> DestructiblePoisonModifier = null!;
        private static ConfigEntry<float> DestructibleMinimumDamageThreshold = null!;
        private static ConfigEntry<int> DestructibleMinimumToolTier = null!;
        private static ConfigEntry<float> DestructibleHitNoise = null!;
        private static ConfigEntry<float> DestructibleDestroyNoise = null!;
        private static ConfigEntry<Toggle> DestructibleTriggerPrivateArea = null!;
        private static ConfigEntry<float> DestructibleTTL = null!;
        private static ConfigEntry<string> DestructibleSpawnWhenDestroyed = null!;


        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string? Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }

        #endregion
    }

    public static class KeyboardExtensions
    {
        public static bool IsKeyDown(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }

        public static bool IsKeyHeld(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }
    }
}