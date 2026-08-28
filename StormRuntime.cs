using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterStorm
{
    internal sealed class ModStormController : MonoBehaviour
    {
        private Transform player;
        private ParticleSystem topParticles;
        private ParticleSystem bottomParticles;
        private float oneSecondTimer;

        internal WanderingStorm Storm { get; private set; }
        internal StormDefinition Definition { get; private set; }

        internal void Configure(StormDefinition definition, Transform playerTransform)
        {
            Definition = definition;
            Storm = GetComponent<WanderingStorm>();
            player = playerTransform;

            if (Storm == null)
            {
                throw new InvalidOperationException(
                    "Custom storm clone has no WanderingStorm component.");
            }

            StormAccess.Priority(Storm) = definition.Priority;
            StormAccess.Radius(Storm) = definition.Radius;
            StormAccess.ParticleDistance(Storm) = definition.ParticleDistance;
            topParticles = StormAccess.TopParticles(Storm);
            bottomParticles = StormAccess.BottomParticles(Storm);

            if (definition.SupportsLightning)
            {
                ConfigureLightning(definition.Lightning);
            }
            else
            {
                DisableInheritedLightning();
            }

            if (definition.Kind == StormKind.Sandstorm)
            {
                SandstormVisuals.ApplyStormCloudColors(topParticles, bottomParticles);
            }

            oneSecondTimer = 0f;
        }

        internal bool ShouldBeActive()
        {
            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            Region region = GameplayRegionResolver.GetCurrentRegion();
            return plugin != null &&
                plugin.IsEnabled(Definition.Id) &&
                region != null &&
                Definition.AllowsRegion(region.gameObject.name) &&
                ClimateSeasonCompatibility.Allows(Definition) &&
                region.stormCount >= Definition.Priority;
        }

        internal void CustomUpdate()
        {
            if (!GameState.playing || Storm == null)
            {
                return;
            }

            if (player == null && Camera.main != null)
            {
                player = Camera.main.transform;
            }

            if (player == null)
            {
                return;
            }

            transform.Translate(
                Wind.currentWind.normalized * Time.deltaTime * Definition.MoveSpeed,
                Space.World);

            Vector3 awayFromPlayer = transform.position - player.position;
            awayFromPlayer.y = 0f;
            if (awayFromPlayer.sqrMagnitude > 0.0001f)
            {
                transform.Translate(
                    awayFromPlayer.normalized * Time.deltaTime *
                    WeatherStorms.totemAttraction * Storm.totemMult,
                    Space.World);
            }

            oneSecondTimer -= Time.deltaTime;
            if (oneSecondTimer > 0f)
            {
                return;
            }

            oneSecondTimer = 1f;
            Storm.active = ShouldBeActive();

            Vector3 horizontal = transform.position - player.position;
            horizontal.y = 0f;
            float distance = horizontal.magnitude;

            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            if (Storm.active &&
                plugin != null &&
                distance > plugin.RelocationDistance.Value)
            {
                transform.Translate(-horizontal * 1.75f, Space.World);
                horizontal = transform.position - player.position;
                horizontal.y = 0f;
                distance = horizontal.magnitude;
            }

            SetParticleEmission(
                Storm.active && distance <= Definition.ParticleDistance);
        }

        internal void SetModEnabled(bool enabled)
        {
            SetParticleEmission(false);
            if (!enabled)
            {
                ThunderPoolRegistry.Reset(this);
                Storm.active = false;
                gameObject.SetActive(false);
                oneSecondTimer = 0f;
                return;
            }

            gameObject.SetActive(true);
            Storm.active = ShouldBeActive();
            oneSecondTimer = 0f;
        }

        internal void RefreshSandstormCloudVisuals()
        {
            if (Definition.Kind == StormKind.Sandstorm)
            {
                SandstormVisuals.ApplyStormCloudColors(topParticles, bottomParticles);
            }
        }

        internal void PrepareDebugMove(bool resetLightningCooldown)
        {
            gameObject.SetActive(true);
            Storm.active = ShouldBeActive();
            oneSecondTimer = 0f;

            if (resetLightningCooldown && Definition.SupportsLightning)
            {
                WanderingStormLightning[] lightning =
                    GetComponentsInChildren<WanderingStormLightning>(true);
                for (int i = 0; i < lightning.Length; i++)
                {
                    StormAccess.LightningCooldown(lightning[i]) = 0f;
                }
            }

            if (player != null)
            {
                Vector3 horizontal = transform.position - player.position;
                horizontal.y = 0f;
                SetParticleEmission(
                    Storm.active &&
                    horizontal.magnitude <= Definition.ParticleDistance);
            }
        }

        private void ConfigureLightning(LightningProfile profile)
        {
            WanderingStormLightning[] lightning =
                GetComponentsInChildren<WanderingStormLightning>(true);
            for (int i = 0; i < lightning.Length; i++)
            {
                WanderingStormLightning strike = lightning[i];
                StormAccess.LightningInterval(strike) = profile.Interval;
                StormAccess.LightningCooldown(strike) = profile.Interval;
                StormAccess.LightningIntensity(strike) = profile.LightIntensity;

                Light light = strike.GetComponent<Light>();
                if (light != null)
                {
                    light.range = profile.LightRange;
                }

                AudioSource[] sources = strike.GetComponents<AudioSource>();
                for (int j = 0; j < sources.Length; j++)
                {
                    sources[j].minDistance = StormCatalog.AudioMinimumDistance;
                    sources[j].maxDistance = profile.AudioSourceMaxDistance;
                }
            }
        }

        private void DisableInheritedLightning()
        {
            WanderingStormLightning[] lightning =
                GetComponentsInChildren<WanderingStormLightning>(true);
            for (int i = 0; i < lightning.Length; i++)
            {
                WanderingStormLightning strike = lightning[i];
                strike.enabled = false;

                Light light = strike.GetComponent<Light>();
                if (light != null)
                {
                    light.enabled = false;
                }

                ParticleSystem particles = strike.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    particles.Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                AudioSource[] sources = strike.GetComponents<AudioSource>();
                for (int j = 0; j < sources.Length; j++)
                {
                    sources[j].Stop();
                    sources[j].enabled = false;
                }
            }
        }

        private void SetParticleEmission(bool enabled)
        {
            SetEmission(topParticles, enabled);
            SetEmission(bottomParticles, enabled);
        }

        private static void SetEmission(ParticleSystem particles, bool enabled)
        {
            if (particles == null)
            {
                return;
            }

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = enabled;
        }
    }

    internal static class ModStormFactory
    {
        private static readonly List<ModStormController> CustomStorms =
            new List<ModStormController>();

        private static WeatherStorms owner;
        private static WanderingStorm[] originalStorms;
        private static bool initialized;

        internal static ModStormController Hurricane { get; private set; }
        internal static ModStormController Sandstorm { get; private set; }

        internal static bool AnyMediStormEnabled
        {
            get
            {
                BetterStormPlugin plugin = BetterStormPlugin.Instance;
                if (plugin == null)
                {
                    return false;
                }

                for (int i = 0; i < StormCatalog.Custom.Length; i++)
                {
                    StormDefinition definition = StormCatalog.Custom[i];
                    if (definition.AllowsRegion("Region Medi East") &&
                        plugin.IsEnabled(definition.Id))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal static void Initialize(WeatherStorms weatherStorms)
        {
            if (weatherStorms == null)
            {
                return;
            }

            if (initialized && owner == weatherStorms)
            {
                return;
            }

            if (initialized)
            {
                Shutdown();
            }

            WanderingStorm[] vanillaStorms = StormAccess.Storms(weatherStorms);
            WanderingStorm template = FindTemplate(vanillaStorms);
            Camera camera = Camera.main;
            if (vanillaStorms == null || template == null || camera == null)
            {
                BetterStormPlugin.Instance?.LogFeatureError(
                    "Custom storms could not initialize because the vanilla storm " +
                    "template or player camera was unavailable.");
                return;
            }

            owner = weatherStorms;
            originalStorms = vanillaStorms;
            bool templateWasActive = template.gameObject.activeSelf;
            template.gameObject.SetActive(false);

            try
            {
                for (int i = 0; i < StormCatalog.Custom.Length; i++)
                {
                    StormDefinition definition = StormCatalog.Custom[i];
                    GameObject clone = UnityEngine.Object.Instantiate(
                        template.gameObject,
                        camera.transform.position + definition.InitialOffset,
                        template.transform.rotation,
                        template.transform.parent);
                    clone.name = "Better Storm " + definition.Name;

                    SaveableObject[] duplicateSaveables =
                        clone.GetComponentsInChildren<SaveableObject>(true);
                    for (int j = 0; j < duplicateSaveables.Length; j++)
                    {
                        UnityEngine.Object.DestroyImmediate(duplicateSaveables[j]);
                    }

                    ModStormController controller =
                        clone.AddComponent<ModStormController>();
                    controller.Configure(definition, camera.transform);
                    CustomStorms.Add(controller);
                    StormPositionPersistence.ApplyLoadedPosition(
                        definition.Id,
                        controller);

                    if (definition.Id == CustomStormId.Hurricane)
                    {
                        Hurricane = controller;
                    }
                    else if (definition.Id == CustomStormId.Sandstorm)
                    {
                        Sandstorm = controller;
                    }
                }

                WanderingStorm[] combined = new WanderingStorm[
                    vanillaStorms.Length + CustomStorms.Count];
                Array.Copy(vanillaStorms, combined, vanillaStorms.Length);
                for (int i = 0; i < CustomStorms.Count; i++)
                {
                    combined[vanillaStorms.Length + i] = CustomStorms[i].Storm;
                }

                StormAccess.Storms(weatherStorms) = combined;
                initialized = true;
                ApplyEnabledState();
                MediEastStormSetting.Apply(AnyMediStormEnabled);
                RefreshSandstormCloudVisuals();
                GlobalLightningSettings.ApplyToLiveStorms();
                BetterStormPlugin.Instance?.LogFeatureInfo(
                    "Initialized three Squalls, one Hurricane, one Dry " +
                    "Thunderstorm, and one Sandstorm.");
            }
            catch (Exception exception)
            {
                BetterStormPlugin.Instance?.LogFeatureError(
                    "Custom storm initialization failed: " + exception);
                Shutdown();
            }
            finally
            {
                if (template != null)
                {
                    template.gameObject.SetActive(templateWasActive);
                }
            }
        }

        internal static void ApplyEnabledState()
        {
            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            if (plugin == null)
            {
                return;
            }

            for (int i = 0; i < CustomStorms.Count; i++)
            {
                ModStormController storm = CustomStorms[i];
                if (storm != null)
                {
                    storm.SetModEnabled(plugin.IsEnabled(storm.Definition.Id));
                }
            }
        }

        internal static ModStormController Find(CustomStormId id)
        {
            for (int i = 0; i < CustomStorms.Count; i++)
            {
                ModStormController storm = CustomStorms[i];
                if (storm != null && storm.Definition.Id == id)
                {
                    return storm;
                }
            }

            return null;
        }

        internal static void RefreshSandstormCloudVisuals()
        {
            Sandstorm?.RefreshSandstormCloudVisuals();
        }

        internal static void Shutdown()
        {
            if (owner != null && originalStorms != null)
            {
                StormAccess.Storms(owner) = originalStorms;
            }

            for (int i = 0; i < CustomStorms.Count; i++)
            {
                ModStormController storm = CustomStorms[i];
                if (storm != null)
                {
                    UnityEngine.Object.Destroy(storm.gameObject);
                }
            }

            CustomStorms.Clear();
            Hurricane = null;
            Sandstorm = null;
            originalStorms = null;
            owner = null;
            initialized = false;
        }

        private static WanderingStorm FindTemplate(WanderingStorm[] storms)
        {
            if (storms == null)
            {
                return null;
            }

            for (int i = 0; i < storms.Length; i++)
            {
                if (storms[i] != null &&
                    storms[i].GetComponent<ModStormController>() == null)
                {
                    return storms[i];
                }
            }

            return null;
        }
    }

    internal static class MediEastStormSetting
    {
        private static Region mediEast;
        private static int originalStormCount;

        internal static void Apply(bool enabled)
        {
            if (mediEast == null)
            {
                Region[] regions = UnityEngine.Object.FindObjectsOfType<Region>();
                for (int i = 0; i < regions.Length; i++)
                {
                    if (regions[i].gameObject.name == "Region Medi East")
                    {
                        mediEast = regions[i];
                        originalStormCount = regions[i].stormCount;
                        break;
                    }
                }
            }

            if (mediEast != null)
            {
                mediEast.stormCount = enabled ? 3 : originalStormCount;
            }
        }
    }

    [HarmonyPatch(typeof(WeatherStorms), "Start")]
    internal static class WeatherStormsStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix(WeatherStorms __instance)
        {
            ThunderPoolRegistry.Shutdown();
            SandstormVisuals.Shutdown();
            SandstormDirt.Reset();
            GlobalLightningSettings.RestoreSnapshots();
            ModStormFactory.Initialize(__instance);
        }
    }

    [HarmonyPatch(typeof(WeatherStorms), "FindClosestStorm")]
    internal static class StrongestStormPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            WeatherStorms __instance,
            WanderingStorm[] ___storms,
            Transform ___player,
            ref WanderingStorm ___currentStorm)
        {
            if (___storms == null || ___player == null)
            {
                return true;
            }

            WanderingStorm best = null;
            float bestNormalized = float.MaxValue;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < ___storms.Length; i++)
            {
                WanderingStorm storm = ___storms[i];
                if (storm == null || !storm.active)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    ___player.position,
                    storm.transform.position);
                float normalized = StormInfluenceService.NormalizeForSelection(
                    __instance,
                    storm,
                    distance);

                if (normalized < bestNormalized ||
                    (Mathf.Approximately(normalized, bestNormalized) &&
                     distance < bestDistance))
                {
                    best = storm;
                    bestNormalized = normalized;
                    bestDistance = distance;
                }
            }

            ___currentStorm = best;
            WeatherStorms.currentStormDistance = best != null
                ? bestDistance
                : 100000000f;
            return false;
        }
    }

    [HarmonyPatch(typeof(WeatherStorms), "GetNormalizedDistance")]
    internal static class CustomStormRangePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(WanderingStorm ___currentStorm, ref float __result)
        {
            if (___currentStorm == null)
            {
                return true;
            }

            ModStormController controller =
                ___currentStorm.GetComponent<ModStormController>();
            if (controller == null ||
                !controller.Definition.UsesFixedWeatherRange)
            {
                return true;
            }

            __result = Mathf.Clamp01(
                (WeatherStorms.currentStormDistance -
                 controller.Definition.Radius) /
                Mathf.Max(0.0001f, controller.Definition.FixedWeatherRange));
            return false;
        }
    }

    [HarmonyPatch(typeof(WanderingStorm), "Update")]
    internal static class CustomStormMovementPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(WanderingStorm __instance)
        {
            ModStormController controller =
                __instance.GetComponent<ModStormController>();
            if (controller == null)
            {
                return true;
            }

            controller.CustomUpdate();
            return false;
        }
    }

    [HarmonyPatch(typeof(RegionBlender), "Start")]
    internal static class RegionBlenderStartPatch
    {
        [HarmonyPostfix]
        [HarmonyAfter(new[] { BetterStormPlugin.BorderExpanderGuid })]
        private static void Postfix()
        {
            MediEastStormSetting.Apply(ModStormFactory.AnyMediStormEnabled);
            ModStormFactory.ApplyEnabledState();
        }
    }
}
