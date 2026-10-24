// Better Storm's plugin: start-up, configuration, lifecycle, compatibility with other mods, shared game accessors and the
// debug storm controls.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

[assembly: InternalsVisibleTo("BetterStorm.Tests")]

namespace BetterStorm
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(ChaoticWindGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ClimateGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(BorderExpanderGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(MarinersHudGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(WindTotemGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(RealisticSkiesGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(KemyNavigationGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class BetterStormPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "DogEggz.BetterStorm";
        public const string PluginName = "Better Storm";
        public const string PluginVersion = "1.2.0";

        public const string ChaoticWindGuid = "com.pete.sailwind.windconfigurator";
        public const string ClimateGuid = "com.raddude.climate";
        public const string BorderExpanderGuid = "com.nandbrew.borderexpander";
        public const string MarinersHudGuid = "com.dixiecapt.sailwind.marinershud";
        public const string WindTotemGuid = "DogEggz.WindTotem";
        public const string RealisticSkiesGuid = "com.kingcam16.realisticskies";
        public const string KemyNavigationGuid = "com.larsonlogistics.sailwind.navsuite";

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
            MarinersHudCompatibility.Install(harmony);
            KemyNavigationCompatibility.Install(harmony);
            ApplyRuntimeSettings();
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            WindOverrideState.Tick();
            DoldrumsWind.Tick();
            MagneticField.Tick();
            MagneticSky.Tick();
            MistFogDome.Tick();
            MagneticMistRain.Tick();
            SandstormVisuals.Tick();
            GentleSnowVisuals.Tick();
            SandstormDirt.Tick();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            WindOverrideState.RestoreImmediately();
            DoldrumsWind.RestoreImmediately();
            // ShutdownTransientEffects also restores the Magnetic Mist's sky (MagneticSky.Restore).
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
                    "summon or push the priority-1 vanilla storm or a custom storm " +
                    "to its outer weather edge.",
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
            StormRainVisuals.Shutdown();
            RainEffects.RestoreRainSettings();
            SandstormVisuals.Shutdown();
            GentleSnowVisuals.Shutdown();
            CustomPrecipitationParticles.DestroyShiftingSpace();
            IndoorPrecipitationExclusion.ResetCaches();
            SandstormDirt.Reset();
            MagneticSky.Restore();
            MistFogDome.Shutdown();
            MagneticMistRain.Reset();
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

    // BepInEx Configuration Manager discovers this optional metadata shim by
    // its exact type name. Better Storm does not take a hard dependency on it.
    internal sealed class ConfigurationManagerAttributes
    {
        public bool? ShowRangeAsPercent;
        public Action<ConfigEntryBase> CustomDrawer;
        public bool? HideDefaultButton;
        public bool? HideSettingName;
        public string DispName = null;
        public int? Order;
    }

    internal sealed class SteppedAcceptableValueRange : AcceptableValueRange<float>
    {
        private readonly float step;

        internal SteppedAcceptableValueRange(float minimum, float maximum, float step)
            : base(minimum, maximum)
        {
            this.step = step;
        }

        public override object Clamp(object value)
        {
            float clamped = (float)base.Clamp(value);
            float snapped = MinValue +
                Mathf.Floor((clamped - MinValue) / step + 0.5f) * step;
            return Mathf.Clamp(snapped, MinValue, MaxValue);
        }

        public override bool IsValid(object value)
        {
            if (!(value is float floatValue))
            {
                return false;
            }

            return Mathf.Approximately(floatValue, (float)Clamp(floatValue));
        }
    }

    [Flags]
    internal enum ClimateSeasonMask
    {
        None = 0,
        Winter = 1,
        Spring = 2,
        Summer = 4,
        Autumn = 8,
        All = Winter | Spring | Summer | Autumn
    }

    internal static class ClimateSeasonCompatibility
    {
        private const int DefaultYearLength = 92;

        internal static bool Allows(StormDefinition definition)
        {
            if (definition == null ||
                definition.AllowedClimateSeasons == ClimateSeasonMask.All)
            {
                return true;
            }

            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            if (plugin == null ||
                plugin.DisableSeasonLimits.Value ||
                !TryGetActiveClimateYear(out int yearLength))
            {
                return true;
            }

            ClimateSeasonMask season = GetSeason(GameState.day, yearLength);
            return (definition.AllowedClimateSeasons & season) != 0;
        }

        internal static ClimateSeasonMask GetSeason(int day, int yearLength)
        {
            int validYearLength = Math.Max(4, yearLength);
            int dayInYear = day % validYearLength;
            if (dayInYear < 0)
            {
                dayInYear += validYearLength;
            }

            int daysPerSeason = validYearLength / 4;
            if (dayInYear < daysPerSeason)
            {
                return ClimateSeasonMask.Winter;
            }
            if (dayInYear < daysPerSeason * 2)
            {
                return ClimateSeasonMask.Spring;
            }
            if (dayInYear < daysPerSeason * 3)
            {
                return ClimateSeasonMask.Summer;
            }
            return ClimateSeasonMask.Autumn;
        }

        private static bool TryGetActiveClimateYear(out int yearLength)
        {
            yearLength = DefaultYearLength;
            if (!Chainloader.PluginInfos.TryGetValue(
                    BetterStormPlugin.ClimateGuid,
                    out PluginInfo climate) ||
                climate == null ||
                climate.Instance == null ||
                !climate.Instance.Config.TryGetEntry(
                    "Settings",
                    "Enable Custom Winds",
                    out ConfigEntry<bool> customWinds) ||
                !customWinds.Value)
            {
                return false;
            }

            if (climate.Instance.Config.TryGetEntry(
                    "Settings",
                    "Days In A Year",
                    out ConfigEntry<int> configuredYearLength))
            {
                yearLength = configuredYearLength.Value;
            }

            return yearLength >= 4;
        }
    }

    internal static class StormAccess
    {
        internal static readonly AccessTools.FieldRef<WeatherStorms, WanderingStorm[]> Storms =
            AccessTools.FieldRefAccess<WeatherStorms, WanderingStorm[]>("storms");
        internal static readonly AccessTools.FieldRef<WeatherStorms, float> CurrentStormRange =
            AccessTools.FieldRefAccess<WeatherStorms, float>("currentStormRange");
        internal static readonly AccessTools.FieldRef<WeatherStorms, WanderingStorm> CurrentStorm =
            AccessTools.FieldRefAccess<WeatherStorms, WanderingStorm>("currentStorm");
        internal static readonly AccessTools.FieldRef<WeatherStorms, float> RainBorder =
            AccessTools.FieldRefAccess<WeatherStorms, float>("rainBorder");
        internal static readonly AccessTools.FieldRef<WeatherStorms, float> CloudyBorder =
            AccessTools.FieldRefAccess<WeatherStorms, float>("cloudyBorder");
        internal static readonly AccessTools.FieldRef<Weather, WeatherParticlesSettings> FinalParticles =
            AccessTools.FieldRefAccess<Weather, WeatherParticlesSettings>("finalParticles");

        internal static readonly AccessTools.FieldRef<Wind, float> WindTimer =
            AccessTools.FieldRefAccess<Wind, float>("timer");
        internal static readonly AccessTools.FieldRef<Wind, float> GustTimer =
            AccessTools.FieldRefAccess<Wind, float>("gustTimer");

        internal static readonly AccessTools.FieldRef<WanderingStorm, int> Priority =
            AccessTools.FieldRefAccess<WanderingStorm, int>("stormPriority");
        internal static readonly AccessTools.FieldRef<WanderingStorm, float> ParticleDistance =
            AccessTools.FieldRefAccess<WanderingStorm, float>("particlesDistance");
        internal static readonly AccessTools.FieldRef<WanderingStorm, float> Radius =
            AccessTools.FieldRefAccess<WanderingStorm, float>("stormRadius");
        internal static readonly AccessTools.FieldRef<WanderingStorm, ParticleSystem> TopParticles =
            AccessTools.FieldRefAccess<WanderingStorm, ParticleSystem>("topParticles");
        internal static readonly AccessTools.FieldRef<WanderingStorm, ParticleSystem> BottomParticles =
            AccessTools.FieldRefAccess<WanderingStorm, ParticleSystem>("bottomParticles");

        internal static readonly AccessTools.FieldRef<WanderingStormLightning, float> LightningInterval =
            AccessTools.FieldRefAccess<WanderingStormLightning, float>("lightningInterval");
        internal static readonly AccessTools.FieldRef<WanderingStormLightning, float> LightningIntensity =
            AccessTools.FieldRefAccess<WanderingStormLightning, float>("lightIntensity");
        internal static readonly AccessTools.FieldRef<WanderingStormLightning, Light> LightningLight =
            AccessTools.FieldRefAccess<WanderingStormLightning, Light>("light");
        internal static readonly AccessTools.FieldRef<WanderingStormLightning, ParticleSystem> LightningParticles =
            AccessTools.FieldRefAccess<WanderingStormLightning, ParticleSystem>("particles");
        internal static readonly AccessTools.FieldRef<WanderingStormLightning, AudioSource> LightningAudio1 =
            AccessTools.FieldRefAccess<WanderingStormLightning, AudioSource>("audio1");
        internal static readonly AccessTools.FieldRef<WanderingStormLightning, AudioSource> LightningAudio2 =
            AccessTools.FieldRefAccess<WanderingStormLightning, AudioSource>("audio2");
        internal static readonly AccessTools.FieldRef<WanderingStormLightning, float> LightningCooldown =
            AccessTools.FieldRefAccess<WanderingStormLightning, float>("lightningCooldown");
        internal static readonly AccessTools.FieldRef<WanderingStormLightning, float> LightningLightTimer =
            AccessTools.FieldRefAccess<WanderingStormLightning, float>("lightTimer");

        internal static readonly AccessTools.FieldRef<CleanableObject, Texture2D> DirtCoat =
            AccessTools.FieldRefAccess<CleanableObject, Texture2D>("dirtCoat");

        internal static readonly FieldInfo RegionBlenderTargetRegion =
            AccessTools.Field(typeof(RegionBlender), "currentTargetRegion");
    }

    internal static class GameplayRegionResolver
    {
        internal static Region GetCurrentRegion()
        {
            if (RegionBlender.instance != null && StormAccess.RegionBlenderTargetRegion != null)
            {
                Region target = StormAccess.RegionBlenderTargetRegion.GetValue(
                    RegionBlender.instance) as Region;
                if (target != null)
                {
                    return target;
                }
            }

            return Weather.instance != null ? Weather.instance.currentRegion : null;
        }
    }

    internal static class DebugStormControls
    {
        private const float DrawerWidth = 230f;
        private static bool expanded;

        internal static void DrawButtons(ConfigEntryBase unused)
        {
            string foldoutLabel = expanded
                ? "[-] Storm Debug Controls"
                : "[+] Storm Debug Controls";

            if (GUILayout.Button(foldoutLabel, GUILayout.Width(DrawerWidth)))
            {
                expanded = !expanded;
            }

            if (!expanded)
            {
                return;
            }

            GUILayout.BeginVertical(GUILayout.Width(DrawerWidth));
            DrawVanillaControls();
            for (int i = 0; i < StormCatalog.Custom.Length; i++)
            {
                StormDefinition definition = StormCatalog.Custom[i];
                DrawControls(definition.Name, definition.Id);
            }
            GUILayout.EndVertical();
        }

        private static void DrawVanillaControls()
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && TryGetVanillaContext(
                out WanderingStorm unusedStorm, out WeatherStorms unusedWeather,
                out Transform unusedPlayer);
            GUILayout.Space(4f);
            GUILayout.Label("Vanilla Storm (priority 1)", GUILayout.Width(DrawerWidth));
            if (GUILayout.Button("Summon", GUILayout.Width(DrawerWidth)))
                MoveVanillaStorm(true);
            if (GUILayout.Button("Push to outer edge", GUILayout.Width(DrawerWidth)))
                MoveVanillaStorm(false);
            GUI.enabled = previousEnabled;
        }

        internal static bool MoveVanillaStorm(bool summon)
        {
            if (!TryGetVanillaContext(out WanderingStorm storm,
                    out WeatherStorms weatherStorms, out Transform player))
                return false;
            float outerEdge = storm.GetRadius() +
                StormInfluenceService.GetWeatherRange(weatherStorms, null);
            Vector3 away = storm.transform.position - player.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                away = player.forward;
                away.y = 0f;
            }
            away = away.sqrMagnitude < 0.0001f ? Vector3.forward : away.normalized;
            Vector3 target = summon ? player.position : player.position + away * outerEdge;
            target.y = storm.transform.position.y;
            storm.transform.position = target;
            storm.active = true;
            ParticleSystem bottom = StormAccess.BottomParticles(storm);
            if (bottom != null)
            {
                ParticleSystem.EmissionModule emission = bottom.emission;
                emission.enabled = Vector3.Distance(target, player.position) <=
                    StormAccess.ParticleDistance(storm);
            }
            if (summon)
            {
                WindOverrideState.NotifyDebugSummon(StormKind.Vanilla);
                StormAccess.CurrentStorm(weatherStorms) = storm;
                WeatherStorms.currentStormDistance = Vector3.Distance(target, player.position);
                WanderingStormLightning lightning = storm.GetComponent<WanderingStormLightning>();
                if (lightning != null) StormAccess.LightningCooldown(lightning) = 0f;
            }
            Refresh(weatherStorms);
            BetterStormPlugin.Instance.LogFeatureInfo(summon
                ? "Debug summoned vanilla priority-1 storm."
                : "Debug pushed vanilla priority-1 storm to outer edge " + outerEdge.ToString("0.0") + ".");
            return true;
        }

        private static bool TryGetVanillaContext(out WanderingStorm storm,
            out WeatherStorms weatherStorms, out Transform player)
        {
            storm = null;
            weatherStorms = WeatherStorms.instance;
            Camera camera = Camera.main;
            player = camera != null ? camera.transform : null;
            Region region = GameplayRegionResolver.GetCurrentRegion();
            if (BetterStormPlugin.Instance == null || !GameState.playing ||
                weatherStorms == null || player == null || region == null || region.stormCount < 1)
                return false;
            WanderingStorm[] storms = StormAccess.Storms(weatherStorms);
            if (storms == null) return false;
            for (int i = 0; i < storms.Length; i++)
            {
                WanderingStorm candidate = storms[i];
                if (candidate != null && candidate.isActiveAndEnabled &&
                    StormAccess.Priority(candidate) == 1 &&
                    candidate.GetComponent<ModStormController>() == null)
                {
                    storm = candidate;
                    return true;
                }
            }
            return false;
        }

        internal static bool Summon(CustomStormId id)
        {
            if (!TryGetContext(
                    id,
                    out ModStormController controller,
                    out WeatherStorms weatherStorms,
                    out Transform player))
            {
                return false;
            }

            ThunderPoolRegistry.Reset(controller);

            Vector3 target = player.position;
            controller.SetDebugPosition(target);
            if (id == CustomStormId.MagneticMist)
            {
                MagneticMistFollow.DebugSummoned();
            }
            controller.PrepareDebugMove(true);
            WindOverrideState.NotifyDebugSummon(controller.Definition.Kind);

            // Debug Summon is an explicit override. Make the requested storm
            // current before selection runs so the normal 66% retention rule
            // cannot keep a previously selected storm instead.
            StormAccess.CurrentStorm(weatherStorms) = controller.Storm;
            WeatherStorms.currentStormDistance = 0f;

            Refresh(weatherStorms);
            BetterStormPlugin.Instance.LogFeatureInfo(
                "Debug summoned " + controller.Definition.Name + ".");
            return true;
        }

        internal static bool PushToOuterEdge(CustomStormId id)
        {
            if (!TryGetContext(
                    id,
                    out ModStormController controller,
                    out WeatherStorms weatherStorms,
                    out Transform player))
            {
                return false;
            }

            float localRange = StormInfluenceService.GetWeatherRange(
                weatherStorms,
                controller);
            float outerEdge = controller.Definition.Radius + localRange;

            Vector3 away = controller.transform.position - player.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                away = player.forward;
                away.y = 0f;
            }
            if (away.sqrMagnitude < 0.0001f)
            {
                away = Vector3.forward;
            }
            else
            {
                away.Normalize();
            }

            ThunderPoolRegistry.Reset(controller);

            Vector3 target = player.position + away * outerEdge;
            controller.SetDebugPosition(target);
            if (id == CustomStormId.MagneticMist)
            {
                MagneticMistFollow.DebugPushedToEdge();
            }
            controller.PrepareDebugMove(false);

            Refresh(weatherStorms);
            BetterStormPlugin.Instance.LogFeatureInfo(
                "Debug pushed " + controller.Definition.Name +
                " to outer edge " + outerEdge.ToString("0.0") + ".");
            return true;
        }

        private static void DrawControls(string label, CustomStormId id)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && CanOperate(id);

            GUILayout.Space(4f);
            GUILayout.Label(label, GUILayout.Width(DrawerWidth));
            if (GUILayout.Button("Summon", GUILayout.Width(DrawerWidth)))
            {
                Summon(id);
            }
            if (GUILayout.Button(
                    "Push to outer edge",
                    GUILayout.Width(DrawerWidth)))
            {
                PushToOuterEdge(id);
            }

            if (id == CustomStormId.MagneticMist)
            {
                GUILayout.Label(MagneticMistFollow.DescribeState(), GUILayout.Width(DrawerWidth));
                if (GUILayout.Button("End follow now", GUILayout.Width(DrawerWidth)))
                {
                    MagneticMistFollow.DebugEndFollow();   // starts the 120 h cooldown
                }
                if (GUILayout.Button("Clear cooldown", GUILayout.Width(DrawerWidth)))
                {
                    MagneticMistFollow.DebugClearCooldown();
                }
            }

            GUI.enabled = previousEnabled;
        }

        private static bool CanOperate(CustomStormId id)
        {
            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            ModStormController controller = ModStormFactory.Find(id);
            return plugin != null &&
                plugin.IsEnabled(id) &&
                GameState.playing &&
                controller != null &&
                // the Magnetic Mist's buttons also work near land, where it stays off
                controller.ShouldBeActive(id == CustomStormId.MagneticMist) &&
                WeatherStorms.instance != null &&
                Camera.main != null;
        }

        private static bool TryGetContext(
            CustomStormId id,
            out ModStormController controller,
            out WeatherStorms weatherStorms,
            out Transform player)
        {
            controller = ModStormFactory.Find(id);
            weatherStorms = WeatherStorms.instance;
            player = Camera.main != null ? Camera.main.transform : null;

            if (!CanOperate(id) ||
                controller == null ||
                weatherStorms == null ||
                player == null)
            {
                BetterStormPlugin.Instance?.LogFeatureWarning(
                    "Debug storm action is unavailable for " + id + ".");
                return false;
            }

            return true;
        }

        private static void Refresh(WeatherStorms weatherStorms)
        {
            weatherStorms.FindClosestStorm();
            if (weatherStorms.GetCurrentStorm() != null)
            {
                weatherStorms.ApplyStorm();
            }
            WindOverrideState.Tick();
        }
    }
}
