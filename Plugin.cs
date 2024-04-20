using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using JetBrains.Annotations;
using PieceManager;
using ServerSync;
using UnityEngine;

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

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
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

            AzuMistHealth = config("4 - Mistroot", "Mistroot Health", 100f, "Sets the health of the plant.");
            AzuMistTTB = config("4 - Mistroot", "Mistroot Time To Bloom", 600f, "Sets the time to bloom of the plant. Default 10 minutes");
            AzuMistBluntModifier = config("4 - Mistroot", "Mistroot Blunt Modifier", HitData.DamageModifier.Normal, "Sets the blunt damage modifier of the plant.");
            AzuMistSlashModifier = config("4 - Mistroot", "Mistroot Slash Modifier", HitData.DamageModifier.Normal, "Sets the slash damage modifier of the plant.");
            AzuMistPierceModifier = config("4 - Mistroot", "Mistroot Pierce Modifier", HitData.DamageModifier.Normal, "Sets the pierce damage modifier of the plant.");
            AzuMistChopModifier = config("4 - Mistroot", "Mistroot Chop Modifier", HitData.DamageModifier.Normal, "Sets the chop damage modifier of the plant.");
            AzuMistPickaxeModifier = config("4 - Mistroot", "Mistroot Pickaxe Modifier", HitData.DamageModifier.Normal, "Sets the pickaxe damage modifier of the plant.");
            AzuMistFireModifier = config("4 - Mistroot", "Mistroot Fire Modifier", HitData.DamageModifier.Normal, "Sets the fire damage modifier of the plant.");
            AzuMistFrostModifier = config("4 - Mistroot", "Mistroot Frost Modifier", HitData.DamageModifier.Normal, "Sets the frost damage modifier of the plant.");
            AzuMistLightningModifier = config("4 - Mistroot", "Mistroot Lightning Modifier", HitData.DamageModifier.Normal, "Sets the lightning damage modifier of the plant.");
            AzuMistSpiritModifier = config("4 - Mistroot", "Mistroot Spirit Modifier", HitData.DamageModifier.Normal, "Sets the spirit damage modifier of the plant.");
            AzuMistPoisonModifier = config("4 - Mistroot", "Mistroot Poison Modifier", HitData.DamageModifier.Normal, "Sets the poison damage modifier of the plant.");
            AzuMistTriggerPrivateArea = config("4 - Mistroot", "Mistroot Trigger Private Area", Toggle.Off, "Sets whether the plant triggers the private area (vanilla wards) when destroyed.");
            AzuMistSpawnWhenDestroyed = config("4 - Mistroot", "Mistroot Spawn When Destroyed", "", "Sets the prefab to spawn when plant is de-bloomed. Limited to one prefab. Uses prefab name.");


            GameObject fab = PieceManager.PiecePrefabManager.RegisterPrefab("mistroottamer", "Mistroot");
            var mistToUpdate = Utils.FindChild(fab.transform, "ThickMist_").GetComponent<ParticleMist>();
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(fab.transform, "FollowPlayer").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(fab.transform, "ThickMist_").gameObject);
            MaterialReplacer.RegisterGameObjectForMatSwap(Utils.FindChild(fab.transform, "LocalMist").gameObject);
            var azuMistToUpdate = fab.GetComponent<AzuMist>();
            UpdateMistValues(mistToUpdate);
            if (azuMistToUpdate != null)
            {
                UpdateAzuMistValues(azuMistToUpdate);
            }
            else
            {
                azuMistToUpdate = fab.AddComponent<AzuMist>();
                UpdateAzuMistValues(azuMistToUpdate);
            }

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

            AzuMistHealth.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistTTB.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistBluntModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistSlashModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistPierceModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistChopModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistPickaxeModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistFireModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistFrostModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistLightningModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistSpiritModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistPoisonModifier.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistTriggerPrivateArea.SettingChanged += (sender, args) => UpdateMistrootComponents();
            AzuMistSpawnWhenDestroyed.SettingChanged += (sender, args) => UpdateMistrootComponents();


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
            foreach (ParticleMist particleMist in AzuMist.BloomingMists)
            {
                UpdateMistValues(particleMist);
            }

            if (ZNetScene.instance == null) return;
            // Update all mistroot prefabs 
            GameObject? fab = ZNetScene.instance.GetPrefab("Mistroot");
            if (fab == null) return;
            AzuMist? d = fab.GetComponent<AzuMist>();
            ParticleMist? thickMist = Utils.FindChild(fab.transform, "ThickMist_").GetComponent<ParticleMist>();
            UpdateMistValues(thickMist);
            UpdateAzuMistValues(d);
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

        internal static void UpdateAzuMistValues(AzuMist d)
        {
            d.MaxHealth = AzuMistHealth.Value;
            d.m_ttBloom = AzuMistTTB.Value;
            d.m_damages.m_blunt = AzuMistBluntModifier.Value;
            d.m_damages.m_slash = AzuMistSlashModifier.Value;
            d.m_damages.m_pierce = AzuMistPierceModifier.Value;
            d.m_damages.m_chop = AzuMistChopModifier.Value;
            d.m_damages.m_pickaxe = AzuMistPickaxeModifier.Value;
            d.m_damages.m_fire = AzuMistFireModifier.Value;
            d.m_damages.m_frost = AzuMistFrostModifier.Value;
            d.m_damages.m_lightning = AzuMistLightningModifier.Value;
            d.m_damages.m_spirit = AzuMistSpiritModifier.Value;
            d.m_damages.m_poison = AzuMistPoisonModifier.Value;
            d.m_triggerPrivateArea = AzuMistTriggerPrivateArea.Value == Toggle.Off;
            if (ZNetScene.instance != null && !string.IsNullOrWhiteSpace(AzuMistSpawnWhenDestroyed.Value))
            {
                GameObject fab = ZNetScene.instance.GetPrefab(AzuMistSpawnWhenDestroyed.Value);
                if (fab != null)
                    d.m_spawnWhenDebloom = fab;
                else
                {
                    MistrootTamerLogger.LogWarning($"Prefab {AzuMistSpawnWhenDestroyed.Value} not found in ZNetScene.");
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

        // AzuMist
        public static ConfigEntry<float> AzuMistHealth = null!;
        public static ConfigEntry<float> AzuMistTTB = null!;
        public static ConfigEntry<HitData.DamageModifier> AzuMistBluntModifier = null!;
        public static ConfigEntry<HitData.DamageModifier> AzuMistSlashModifier = null!;
        public static ConfigEntry<HitData.DamageModifier> AzuMistPierceModifier = null!;
        public static ConfigEntry<HitData.DamageModifier> AzuMistChopModifier = null!;
        public static ConfigEntry<HitData.DamageModifier> AzuMistPickaxeModifier = null!;
        public static ConfigEntry<HitData.DamageModifier> AzuMistFireModifier = null!;
        public static ConfigEntry<HitData.DamageModifier> AzuMistFrostModifier = null!;
        public static ConfigEntry<HitData.DamageModifier> AzuMistLightningModifier = null!;
        public static ConfigEntry<HitData.DamageModifier> AzuMistSpiritModifier = null!;
        public static ConfigEntry<HitData.DamageModifier> AzuMistPoisonModifier = null!;
        public static ConfigEntry<Toggle> AzuMistTriggerPrivateArea = null!;
        public static ConfigEntry<string> AzuMistSpawnWhenDestroyed = null!;


        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
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

        #endregion
    }
}