using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BetterStorm
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(ChaoticWindGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ClimateGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(BorderExpanderGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class BetterStormPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "DogEggz.BetterStorm";
        public const string PluginName = "Better Storm";
        public const string PluginVersion = "1.1.1";

        public const string ChaoticWindGuid = "com.pete.sailwind.windconfigurator";
        public const string ClimateGuid = "com.raddude.climate";
        public const string BorderExpanderGuid = "com.nandbrew.borderexpander";

        private Harmony harmony;
        private readonly Dictionary<CustomStormId, ConfigEntry<bool>>
            stormEnabledEntries =
                new Dictionary<CustomStormId, ConfigEntry<bool>>();

        internal static BetterStormPlugin Instance { get; private set; }

        internal ConfigEntry<bool> DisableSeasonLimits { get; private set; }
        internal ConfigEntry<float> FallbackBonusCap { get; private set; }
        internal ConfigEntry<int> RelocationDistance { get; private set; }
        private void Awake()
        {
            Instance = this;
            if (!StormCatalog.TryValidate(out string catalogError))
            {
                Logger.LogError("Better Storm cannot start: " + catalogError);
                Instance = null;
                enabled = false;
                return;
            }

            BindConfiguration();

            if (ChaoticWindCompatibility.HasLegacyStormOwner())
            {
                Logger.LogError(
                    "Better Storm cannot start while the legacy storm-owning Chaotic Wind " +
                    "1.3.4 build is loaded. Install the reduced Chaotic Wind companion " +
                    "separately, or remove Chaotic Wind.");
                Instance = null;
                enabled = false;
                return;
            }

            Subscribe();
            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ClimateCompatibility.Install(harmony);
            ApplyRuntimeSettings();
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            WindOverrideState.Tick();
            SandstormVisuals.Tick();
            GentleSnowVisuals.Tick();
            SandstormDirt.Tick();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            WindOverrideState.RestoreImmediately();
            RuntimeEffectLifecycle.ShutdownTransientEffects();
            GlobalLightningSettings.RestoreSnapshots();
            ModStormFactory.Shutdown();
            StormPositionPersistence.ResetAll();

            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void BindConfiguration()
        {
            for (int i = 0; i < StormCatalog.Custom.Length; i++)
            {
                StormDefinition definition = StormCatalog.Custom[i];
                stormEnabledEntries.Add(
                    definition.Id,
                    Config.Bind(
                        "Custom Storms",
                        "Enable " + definition.Name,
                        true,
                        definition.EnableDescription));
            }

            DisableSeasonLimits = Config.Bind(
                "Climate Compatibility", "Disable Seasonal Storm Limits", false,
                "Allow Hurricane, Sandstorm, Dry Thunderstorm, and Gentle " +
                "Snow year-round, even while Climate Custom Winds is enabled.");

            FallbackBonusCap = BindSteppedSlider(
                "Wind", "Storm + Ocean Bonus Cap",
                40f, 0f, 100f, 1f,
                "Used when Chaotic Wind is not installed. Chaotic Wind's " +
                "setting has priority when both mods are installed.", 10);

            RelocationDistance = Config.Bind(
                "Storms", "Storm Relocation Distance",
                44000,
                new ConfigDescription(
                    "Distance at which a custom storm is recycled across the player. " +
                    "Increasing this reduces the chance of encountering a storm.",
                    new AcceptableValueRange<int>(18000, 90000),
                    new ConfigurationManagerAttributes
                    {
                        ShowRangeAsPercent = false,
                        Order = 10
                    }));

            Config.Bind(
                "Debug", "Custom Storm Controls", string.Empty,
                new ConfigDescription(
                    "Always-available storm controls. Expand the compact panel to " +
                    "summon or push a custom storm to its outer weather edge.",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        CustomDrawer = DebugStormControls.DrawButtons,
                        HideDefaultButton = true,
                        HideSettingName = true,
                        Order = -10
                    }));
        }

        private ConfigEntry<float> BindSteppedSlider(
            string section,
            string key,
            float defaultValue,
            float minimum,
            float maximum,
            float step,
            string description,
            int order)
        {
            return Config.Bind(
                section,
                key,
                defaultValue,
                new ConfigDescription(
                    description,
                    new SteppedAcceptableValueRange(minimum, maximum, step),
                    new ConfigurationManagerAttributes
                    {
                        ShowRangeAsPercent = false,
                        Order = order
                    }));
        }

        private void Subscribe()
        {
            foreach (ConfigEntry<bool> entry in stormEnabledEntries.Values)
            {
                entry.SettingChanged += OnStormToggleChanged;
            }
            DisableSeasonLimits.SettingChanged += OnStormToggleChanged;
        }

        private void Unsubscribe()
        {
            foreach (ConfigEntry<bool> entry in stormEnabledEntries.Values)
            {
                entry.SettingChanged -= OnStormToggleChanged;
            }
            if (DisableSeasonLimits != null) DisableSeasonLimits.SettingChanged -= OnStormToggleChanged;
        }

        private void OnStormToggleChanged(object sender, EventArgs e)
        {
            ModStormFactory.ApplyEnabledState();

            if (!IsEnabled(CustomStormId.Sandstorm))
            {
                SandstormVisuals.DeactivateImmediately();
                SandstormDirt.Reset();
            }

            if (!IsEnabled(CustomStormId.GentleSnow))
            {
                GentleSnowVisuals.DeactivateImmediately();
            }

            RefreshCurrentStorm();
            WindOverrideState.Tick();
        }

        private void ApplyRuntimeSettings()
        {
            ModStormFactory.ApplyEnabledState();
            ModStormFactory.RefreshSandstormCloudVisuals();
            GlobalLightningSettings.ApplyToLiveStorms();
        }

        internal bool IsEnabled(CustomStormId id)
        {
            return stormEnabledEntries.TryGetValue(
                       id,
                       out ConfigEntry<bool> entry) &&
                   entry.Value;
        }

        internal void LogFeatureInfo(string message)
        {
            Logger.LogInfo(message);
        }

        internal void LogFeatureWarning(string message)
        {
            Logger.LogWarning(message);
        }

        internal void LogFeatureError(string message)
        {
            Logger.LogError(message);
        }

        internal static void RefreshCurrentStorm()
        {
            WeatherStorms weatherStorms = WeatherStorms.instance;
            if (!GameState.playing || weatherStorms == null)
            {
                return;
            }

            weatherStorms.FindClosestStorm();
            if (weatherStorms.GetCurrentStorm() != null)
            {
                weatherStorms.ApplyStorm();
            }
        }

    }

    internal static class RuntimeEffectLifecycle
    {
        internal static void ShutdownTransientEffects()
        {
            ThunderPoolRegistry.Shutdown();
            SandstormVisuals.Shutdown();
            GentleSnowVisuals.Shutdown();
            SandstormDirt.Reset();
        }
    }

    internal static class ChaoticWindCompatibility
    {
        internal static bool IsLoaded
        {
            get { return Chainloader.PluginInfos.ContainsKey(BetterStormPlugin.ChaoticWindGuid); }
        }

        internal static bool HasLegacyStormOwner()
        {
            if (!Chainloader.PluginInfos.TryGetValue(
                    BetterStormPlugin.ChaoticWindGuid,
                    out PluginInfo info) ||
                info == null ||
                info.Instance == null)
            {
                return false;
            }

            return info.Instance.GetType().Assembly.GetType(
                "ChaoticWind.StormInfluenceService",
                false) != null;
        }

        internal static bool TryGetFloat(string section, string key, out float value)
        {
            value = 0f;
            if (!TryGetPlugin(out PluginInfo info))
            {
                return false;
            }

            if (!info.Instance.Config.TryGetEntry(
                    section,
                    key,
                    out ConfigEntry<float> entry))
            {
                return false;
            }

            value = entry.Value;
            return true;
        }

        internal static bool TryGetBool(string section, string key, out bool value)
        {
            value = false;
            if (!TryGetPlugin(out PluginInfo info))
            {
                return false;
            }

            if (!info.Instance.Config.TryGetEntry(
                    section,
                    key,
                    out ConfigEntry<bool> entry))
            {
                return false;
            }

            value = entry.Value;
            return true;
        }

        private static bool TryGetPlugin(out PluginInfo info)
        {
            return Chainloader.PluginInfos.TryGetValue(
                       BetterStormPlugin.ChaoticWindGuid,
                       out info) &&
                   info != null &&
                   info.Instance != null;
        }
    }
}
