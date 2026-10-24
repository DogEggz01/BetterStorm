// Better Storm's custom storms: the catalog, the storm controllers and their movement, saving their places, the Sandstorm,
// Gentle Snow and the Dry Thunderstorm's own effects, the Doldrums and the Magnetic Mist.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BetterStorm
{
    internal enum CustomStormId
    {
        Squall1,
        Squall2,
        Squall3,
        Hurricane,
        DryThunderstorm,
        Sandstorm,
        GentleSnow,
        Doldrums,
        MagneticMist
    }

    internal enum StormKind
    {
        Vanilla,
        Squall,
        Hurricane,
        DryThunderstorm,
        Sandstorm,
        GentleSnow,
        Doldrums,
        MagneticMist
    }

    internal sealed class StormDefinition
    {
        internal string Name;
        internal string EnableDescription;
        internal string SaveKey;
        internal CustomStormId Id;
        internal StormKind Kind;
        internal Vector3 InitialOffset;
        internal int Priority;
        internal float Radius;
        internal float ParticleDistance;
        internal float MoveSpeed;
        internal bool UsesFixedWeatherRange;
        internal float FixedWeatherRange;
        internal float WindCoefficient;
        internal float? RainTarget;
        internal bool SuppressRainParticles;
        internal bool SuppressFog;
        internal bool SupportsLightning;
        internal HashSet<string> AllowedRegions;
        internal ClimateSeasonMask AllowedClimateSeasons = ClimateSeasonMask.All;
        internal float? MinimumLatitude;
        internal float? MaximumLatitude;
        internal LightningProfile Lightning;

        internal bool AllowsRegion(string regionName)
        {
            return AllowedRegions == null || AllowedRegions.Contains(regionName);
        }
    }

    internal static class StormCatalog
    {
        internal const float CloseThunderDistance = 1600f;
        internal const float AudioMinimumDistance = 250f;
        internal const float VisualPadding = 500f;

        internal static readonly LightningProfile VanillaLightning =
            new LightningProfile(
                interval: 16f,
                spawnRadius: 1500f,
                schedulingDistance: 5500f,
                lightRange: 4980f,
                strikeHeight: 500f,
                lightIntensity: 3f);

        internal static readonly StormDefinition[] Custom =
        {
            Squall(
                "Squall 1",
                CustomStormId.Squall1,
                "DogEggz.BetterStorm.squall-1-position.v2",
                new Vector3(18000f, 0f, 0f),
                1,
                13f),
            Squall(
                "Squall 2",
                CustomStormId.Squall2,
                "DogEggz.BetterStorm.squall-2-position.v2",
                new Vector3(-18000f, 0f, 0f),
                2,
                26f),
            Squall(
                "Squall 3",
                CustomStormId.Squall3,
                "DogEggz.BetterStorm.squall-3-position.v2",
                new Vector3(0f, 0f, 18000f),
                3,
                19f),
            new StormDefinition
            {
                Name = "Hurricane",
                EnableDescription =
                    "Enable the priority-2 Hurricane in Medi East, Emerald, " +
                    "and Emerald Lagoon only.",
                SaveKey = "DogEggz.BetterStorm.hurricane-position.v2",
                Id = CustomStormId.Hurricane,
                Kind = StormKind.Hurricane,
                InitialOffset = new Vector3(0f, 0f, -24000f),
                Priority = 2,
                Radius = 6000f,
                ParticleDistance = 18000f,
                MoveSpeed = 10f,
                UsesFixedWeatherRange = false,
                WindCoefficient = 34f,
                RainTarget = 13f,
                AllowedRegions = new HashSet<string>(StringComparer.Ordinal)
                {
                    "Region Medi East",
                    "Region Emerald (new smaller)",
                    "Region Emerald Lagoon"
                },
                AllowedClimateSeasons =
                    ClimateSeasonMask.Summer | ClimateSeasonMask.Autumn,
                SupportsLightning = true,
                Lightning = new LightningProfile(
                    interval: 12f,
                    spawnRadius: 3000f,
                    schedulingDistance: 18000f,
                    audioSourceMaxDistance: 18000f,
                    lightRange: 9960f,
                    strikeHeight: 500f,
                    lightIntensity: 4f)
            },
            new StormDefinition
            {
                Name = "Dry Thunderstorm",
                EnableDescription =
                    "Enable the priority-2 Dry Thunderstorm in Medi and " +
                    "Medi East only.",
                SaveKey =
                    "DogEggz.BetterStorm.dry-thunderstorm-position.v2",
                Id = CustomStormId.DryThunderstorm,
                Kind = StormKind.DryThunderstorm,
                InitialOffset = new Vector3(-24000f, 0f, 24000f),
                Priority = 2,
                Radius = 1800f,
                ParticleDistance = 2700f,
                MoveSpeed = 9f,
                UsesFixedWeatherRange = true,
                FixedWeatherRange = 900f,
                WindCoefficient = 30f,
                RainTarget = null,
                SuppressRainParticles = true,
                SuppressFog = true,
                AllowedClimateSeasons =
                    ClimateSeasonMask.Autumn | ClimateSeasonMask.Winter,
                SupportsLightning = true,
                AllowedRegions = new HashSet<string>(StringComparer.Ordinal)
                {
                    "Region Medi",
                    "Region Medi East"
                },
                Lightning = new LightningProfile(
                    interval: 1f,
                    spawnRadius: 1800f,
                    schedulingDistance: 3200f,
                    audioSourceMaxDistance: 6400f,
                    lightRange: 2394f,
                    strikeHeight: 500f,
                    lightIntensity: 4.5f)
            },
            new StormDefinition
            {
                Name = "Sandstorm",
                EnableDescription =
                    "Enable the priority-1 dry Sandstorm in Al'Ankh only.",
                SaveKey = "DogEggz.BetterStorm.sandstorm-position.v2",
                Id = CustomStormId.Sandstorm,
                Kind = StormKind.Sandstorm,
                InitialOffset = new Vector3(24000f, 0f, 24000f),
                Priority = 1,
                Radius = 2250f,
                ParticleDistance = 3600f,
                MoveSpeed = 14f,
                UsesFixedWeatherRange = true,
                FixedWeatherRange = 1350f,
                WindCoefficient = 26f,
                RainTarget = null,
                SuppressRainParticles = true,
                AllowedClimateSeasons =
                    ClimateSeasonMask.Spring | ClimateSeasonMask.Summer,
                SupportsLightning = false,
                AllowedRegions = new HashSet<string>(StringComparer.Ordinal)
                {
                    "Region Al'ankh"
                }
            },
            new StormDefinition
            {
                Name = "Gentle Snow",
                EnableDescription =
                    "Enable the priority-1 Gentle Snow above latitude 38.",
                SaveKey = "DogEggz.BetterStorm.gentle-snow-position.v2",
                Id = CustomStormId.GentleSnow,
                Kind = StormKind.GentleSnow,
                InitialOffset = new Vector3(12000f, 0f, 22000f),
                Priority = 1,
                Radius = 2500f,
                ParticleDistance = 0f,
                MoveSpeed = 7f,
                UsesFixedWeatherRange = true,
                FixedWeatherRange = 2000f,
                WindCoefficient = 0f,
                RainTarget = null,
                SuppressRainParticles = true,
                SuppressFog = true,
                AllowedClimateSeasons =
                    ClimateSeasonMask.Winter | ClimateSeasonMask.Spring,
                SupportsLightning = false,
                MinimumLatitude = GentleSnowRules.MinimumLatitude
            },
            new StormDefinition
            {
                // the user, after 1.1.7: shown as High Pressure; the code names and save key stay Doldrums
                Name = "High Pressure",
                EnableDescription =
                    "Enable the priority-1 High Pressure below latitude 37: calm, " +
                    "clear weather with no rain, cloud or fog near its centre.",
                SaveKey = "DogEggz.BetterStorm.doldrums-position.v2",
                Id = CustomStormId.Doldrums,
                Kind = StormKind.Doldrums,
                InitialOffset = new Vector3(-26000f, 0f, -26000f),
                Priority = 1,
                Radius = DoldrumsRules.CoreRadius,
                ParticleDistance = 0f,
                MoveSpeed = 24f,
                UsesFixedWeatherRange = true,
                FixedWeatherRange = DoldrumsRules.OuterEdge - DoldrumsRules.CoreRadius,
                WindCoefficient = 0f,
                RainTarget = null,
                SuppressRainParticles = true,
                SuppressFog = false,
                SupportsLightning = false,
                MaximumLatitude = DoldrumsRules.MaximumLatitude
            },
            new StormDefinition
            {
                Name = "Magnetic Mist",
                EnableDescription =
                    "Enable the priority-1 Magnetic Mist: fog, thick cloud and " +
                    "light rain that disable navigation instruments; it can " +
                    "follow the player.",
                SaveKey = "DogEggz.BetterStorm.magnetic-mist-position.v2",
                Id = CustomStormId.MagneticMist,
                Kind = StormKind.MagneticMist,
                InitialOffset = new Vector3(26000f, 0f, -26000f),
                Priority = 1,
                Radius = MagneticMistRules.CoreRadius,
                ParticleDistance = MagneticMistRules.OuterEdge,
                MoveSpeed = MagneticMistRules.WanderSpeed,
                UsesFixedWeatherRange = true,
                FixedWeatherRange = MagneticMistRules.OuterEdge - MagneticMistRules.CoreRadius,
                WindCoefficient = 0f,
                RainTarget = MagneticMistRules.RainPeak,
                SupportsLightning = false
            }
        };

        private static StormDefinition Squall(
            string name,
            CustomStormId id,
            string saveKey,
            Vector3 initialOffset,
            int priority,
            float windCoefficient)
        {
            return new StormDefinition
            {
                Name = name,
                EnableDescription =
                    "Enable the priority-" + priority + " Squall.",
                SaveKey = saveKey,
                Id = id,
                Kind = StormKind.Squall,
                InitialOffset = initialOffset,
                Priority = priority,
                Radius = 1350f,
                ParticleDistance = 1350f,
                MoveSpeed = 24f,
                UsesFixedWeatherRange = true,
                FixedWeatherRange = 450f,
                WindCoefficient = windCoefficient,
                RainTarget = 25f,
                SupportsLightning = true,
                Lightning = new LightningProfile(
                    interval: 8f,
                    spawnRadius: 1350f,
                    schedulingDistance: 1800f,
                    audioSourceMaxDistance: 3600f,
                    lightRange: 1800f,
                    strikeHeight: 500f,
                    lightIntensity: 3f)
            };
        }

        internal static StormDefinition Find(CustomStormId id)
        {
            for (int i = 0; i < Custom.Length; i++)
            {
                if (Custom[i].Id == id)
                {
                    return Custom[i];
                }
            }

            return null;
        }

        internal static bool TryValidate(out string error)
        {
            error = null;
            if (Custom.Length != Enum.GetValues(typeof(CustomStormId)).Length)
            {
                error = "The custom-storm catalog does not cover every storm ID.";
                return false;
            }

            HashSet<CustomStormId> ids = new HashSet<CustomStormId>();
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> saveKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Custom.Length; i++)
            {
                StormDefinition definition = Custom[i];
                if (definition == null ||
                    string.IsNullOrEmpty(definition.Name) ||
                    string.IsNullOrEmpty(definition.EnableDescription) ||
                    string.IsNullOrEmpty(definition.SaveKey) ||
                    !ids.Add(definition.Id) ||
                    !names.Add(definition.Name) ||
                    !saveKeys.Add(definition.SaveKey))
                {
                    error = "The custom-storm catalog contains missing or " +
                        "duplicate identity metadata at index " + i + ".";
                    return false;
                }

                if (definition.SupportsLightning &&
                    (definition.Lightning.SchedulingDistance <= 0f ||
                     definition.Lightning.AudioSourceMaxDistance <
                        definition.Lightning.SchedulingDistance))
                {
                    error = definition.Name +
                        " has invalid lightning audio distances.";
                    return false;
                }

                if (definition.MinimumLatitude.HasValue &&
                    (definition.MinimumLatitude.Value < -90f ||
                     definition.MinimumLatitude.Value > 90f))
                {
                    error = definition.Name +
                        " has an invalid minimum latitude.";
                    return false;
                }

                if (definition.MaximumLatitude.HasValue &&
                    (definition.MaximumLatitude.Value < -90f ||
                     definition.MaximumLatitude.Value > 90f))
                {
                    error = definition.Name +
                        " has an invalid maximum latitude.";
                    return false;
                }

                // The latitude helpers in ModStormController support one limit per storm.
                if (definition.MinimumLatitude.HasValue &&
                    definition.MaximumLatitude.HasValue)
                {
                    error = definition.Name +
                        " sets both a minimum and a maximum latitude.";
                    return false;
                }
            }

            return true;
        }
    }

    internal static class GameHours
    {
        // Absolute in-game hours. The sun clock runs 0.008 h per real second (x9 in moored or
        // tavern sleep, and Time.timeScale 16 while asleep).
        internal static bool TryNow(out double hours)
        {
            hours = 0.0;
            if (Sun.sun == null)
            {
                return false;
            }

            hours = GameState.day * 24.0 + Sun.sun.globalTime;
            return true;
        }
    }

    internal static class GentleSnowRules
    {
        internal const float MinimumLatitude = 38f;
        internal const float ParkingLatitudeBuffer = 0.05f;
        internal const float WorldUnitsPerLatitude = 9000f;
        internal const float MaximumWindTarget = 18f;
        internal const float FinalLerpSpeed = 0.25f;
        internal const float FullSnowNormalizedDistance = 0.33f;

        internal static bool IsLatitudeAllowed(
            float latitude,
            float minimumLatitude)
        {
            return latitude >= minimumLatitude;
        }

        internal static float GetParkingLatitude(float minimumLatitude)
        {
            return minimumLatitude + ParkingLatitudeBuffer;
        }

        internal static float GetMinimumDistanceToLatitude(
            float playerLatitude,
            float targetLatitude)
        {
            return Mathf.Max(0f, targetLatitude - playerLatitude) *
                WorldUnitsPerLatitude;
        }

        internal static bool ShouldPark(
            float playerLatitude,
            float minimumLatitude,
            float relocationDistance)
        {
            return GetMinimumDistanceToLatitude(
                       playerLatitude,
                       GetParkingLatitude(minimumLatitude)) >
                   relocationDistance;
        }

        internal static float GetSnowEmissionLerp(float normalizedDistance)
        {
            return Mathf.InverseLerp(
                1f,
                FullSnowNormalizedDistance,
                normalizedDistance);
        }
    }

    internal sealed class ModStormController : MonoBehaviour
    {
        private Transform player;
        private ParticleSystem topParticles;
        private ParticleSystem bottomParticles;
        private float oneSecondTimer;
        private bool latitudeParked;

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

            // the mist too: no storm cloud seen from outside (the user, 2026-10-25)
            if (definition.Kind == StormKind.GentleSnow ||
                definition.Kind == StormKind.Doldrums ||
                definition.Kind == StormKind.MagneticMist)
            {
                HideInheritedStormClouds();
            }

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
            return ShouldBeActive(false);
        }

        // ignoreOpenOcean: every condition but the Magnetic Mist's open-ocean rule, for
        // the debug panel, so its buttons also work near land.
        internal bool ShouldBeActive(bool ignoreOpenOcean)
        {
            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            Region region = GameplayRegionResolver.GetCurrentRegion();
            return plugin != null &&
                plugin.IsEnabled(Definition.Id) &&
                region != null &&
                Definition.AllowsRegion(region.gameObject.name) &&
                ClimateSeasonCompatibility.Allows(Definition) &&
                region.stormCount >= Definition.Priority &&
                StormLatitudePlacement.IsAllowed(transform, Definition) &&
                (Definition.Kind != StormKind.MagneticMist ||
                 ignoreOpenOcean ||
                 MagneticMistOpenOcean.Check());
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

            if (Definition.Kind == StormKind.MagneticMist)
            {
                MagneticMistFollow.UpdateMovement(this, player);
            }
            else if (!latitudeParked)
            {
                transform.Translate(
                    Wind.currentWind.normalized * Time.deltaTime *
                    Definition.MoveSpeed,
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

                if (HasLatitudeLimit)
                {
                    // A drift across the latitude line slides the storm along it.
                    transform.position = ClampToLatitudeLimit(transform.position);
                }
            }

            oneSecondTimer -= Time.deltaTime;
            if (oneSecondTimer > 0f)
            {
                return;
            }

            oneSecondTimer = 1f;
            Vector3 horizontal = transform.position - player.position;
            horizontal.y = 0f;
            float distance = horizontal.magnitude;

            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            float relocationDistance = plugin != null
                ? plugin.RelocationDistance.Value
                : 44000f;
            bool activeBeforeRelocation = ShouldBeActive();
            if (HasLatitudeLimit)
            {
                UpdateLatitudeRelocation(
                    activeBeforeRelocation,
                    distance,
                    relocationDistance);
            }
            else if (activeBeforeRelocation && distance > relocationDistance &&
                !(Definition.Kind == StormKind.MagneticMist &&
                  MagneticMistFollow.IsFollowing))
            {
                transform.Translate(-horizontal * 1.75f, Space.World);
            }

            Storm.active = ShouldBeActive();
            if (Definition.Kind == StormKind.MagneticMist &&
                MagneticMistFollow.IsFollowing &&
                MagneticMistOpenOcean.KnownNearLand)
            {
                // Reaching land is the escape: the normal release and its cooldown.
                MagneticMistFollow.ReleaseAtLand();
            }

            horizontal = transform.position - player.position;
            horizontal.y = 0f;
            distance = horizontal.magnitude;

            SetParticleEmission(
                Storm.active && distance <= Definition.ParticleDistance);
        }

        internal void EnsureValidInitialPlacement()
        {
            if (player == null || !HasLatitudeLimit)
            {
                return;
            }

            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            float relocationDistance = plugin != null
                ? plugin.RelocationDistance.Value
                : 44000f;
            if (ShouldParkForLatitude(relocationDistance))
            {
                ParkAtLatitudeLimit();
                return;
            }

            if (!StormLatitudePlacement.IsAllowed(transform, Definition))
            {
                RelocateAcrossPlayer(relocationDistance);
            }
        }

        internal void SetDebugPosition(Vector3 target)
        {
            target.y = transform.position.y;
            if (HasLatitudeLimit)
            {
                target = ClampToLatitudeLimit(target);
                BetterStormPlugin plugin = BetterStormPlugin.Instance;
                float relocationDistance = plugin != null
                    ? plugin.RelocationDistance.Value
                    : 44000f;
                latitudeParked = player != null &&
                    ShouldParkForLatitude(relocationDistance);
            }

            transform.position = target;
        }

        internal void SetModEnabled(bool enabled)
        {
            SetParticleEmission(false);
            if (!enabled)
            {
                if (Definition.Kind == StormKind.MagneticMist)
                {
                    MagneticMistFollow.StopFollowing();
                }

                ThunderPoolRegistry.Reset(this);
                Storm.active = false;
                gameObject.SetActive(false);
                oneSecondTimer = 0f;
                return;
            }

            if (Definition.Kind == StormKind.MagneticMist)
            {
                // Switched on: the player's position is taken as-is, as after a
                // load, so being within the edge now is not a crossing.
                MagneticMistFollow.SyncInside();
            }

            gameObject.SetActive(true);
            EnsureValidInitialPlacement();
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
            if (Definition.Kind == StormKind.GentleSnow ||
                Definition.Kind == StormKind.Doldrums ||
                Definition.Kind == StormKind.MagneticMist)
            {
                enabled = false;
            }

            SetEmission(topParticles, enabled);
            SetEmission(bottomParticles, enabled);
        }

        private bool HasLatitudeLimit
        {
            get
            {
                return Definition.MinimumLatitude.HasValue ||
                    Definition.MaximumLatitude.HasValue;
            }
        }

        private bool ShouldParkForLatitude(float relocationDistance)
        {
            float latitude = StormLatitudePlacement.GetLatitude(player);
            if (Definition.MinimumLatitude.HasValue)
            {
                return GentleSnowRules.ShouldPark(
                    latitude,
                    Definition.MinimumLatitude.Value,
                    relocationDistance);
            }

            return DoldrumsRules.ShouldPark(
                latitude,
                Definition.MaximumLatitude.Value,
                relocationDistance);
        }

        private Vector3 ClampToLatitudeLimit(Vector3 target)
        {
            if (Definition.MinimumLatitude.HasValue)
            {
                return StormLatitudePlacement.ClampTargetLatitude(
                    target,
                    transform,
                    GentleSnowRules.GetParkingLatitude(
                        Definition.MinimumLatitude.Value));
            }

            return StormLatitudePlacement.ClampTargetLatitudeMaximum(
                target,
                transform,
                DoldrumsRules.GetParkingLatitude(
                    Definition.MaximumLatitude.Value));
        }

        private void UpdateLatitudeRelocation(
            bool activeBeforeRelocation,
            float distance,
            float relocationDistance)
        {
            bool shouldPark = ShouldParkForLatitude(relocationDistance);

            if (latitudeParked)
            {
                if (shouldPark)
                {
                    return;
                }

                latitudeParked = false;
                RelocateAcrossPlayer(relocationDistance);
                return;
            }

            bool belowMinimum = !StormLatitudePlacement.IsAllowed(
                transform,
                Definition);
            if (!belowMinimum &&
                (!activeBeforeRelocation || distance <= relocationDistance))
            {
                return;
            }

            if (shouldPark)
            {
                ParkAtLatitudeLimit();
            }
            else
            {
                RelocateAcrossPlayer(relocationDistance);
            }
        }

        private void ParkAtLatitudeLimit()
        {
            Vector3 target = transform.position;
            target.x = player.position.x;
            target = ClampToLatitudeLimit(target);
            transform.position = target;
            latitudeParked = true;
        }

        private void RelocateAcrossPlayer(float relocationDistance)
        {
            Vector3 horizontal = transform.position - player.position;
            horizontal.y = 0f;
            Vector3 direction = horizontal.sqrMagnitude > 0.0001f
                ? -horizontal.normalized
                : Vector3.forward;
            Vector3 target = player.position +
                direction * relocationDistance * 0.75f;
            target.y = transform.position.y;
            target = ClampToLatitudeLimit(target);

            Vector3 constrainedOffset = target - player.position;
            constrainedOffset.y = 0f;
            if (constrainedOffset.magnitude > relocationDistance)
            {
                target.x = player.position.x;
            }

            transform.position = target;
            latitudeParked = false;
        }

        private void HideInheritedStormClouds()
        {
            HideParticleSystem(topParticles);
            HideParticleSystem(bottomParticles);
        }

        private static void HideParticleSystem(ParticleSystem particles)
        {
            if (particles == null)
            {
                return;
            }

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.MainModule main = particles.main;
            Color color = main.startColor.color;
            color.a = 0f;
            main.startColor = color;
            particles.Clear(true);

            ParticleSystemRenderer renderer =
                particles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
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

    internal static class StormLatitudePlacement
    {
        internal static float GetLatitude(Transform target)
        {
            FloatingOriginManager origin = FloatingOriginManager.instance;
            return origin != null
                ? origin.GetGlobeCoords(target).z
                : target.position.z / GentleSnowRules.WorldUnitsPerLatitude;
        }

        internal static bool IsAllowed(
            Transform target,
            StormDefinition definition)
        {
            if (definition.MinimumLatitude.HasValue)
            {
                return GentleSnowRules.IsLatitudeAllowed(
                    GetLatitude(target),
                    definition.MinimumLatitude.Value);
            }

            if (definition.MaximumLatitude.HasValue)
            {
                return DoldrumsRules.IsLatitudeAllowed(
                    GetLatitude(target),
                    definition.MaximumLatitude.Value);
            }

            return true;
        }

        // The mirror of ClampTargetLatitude, for a maximum latitude.
        internal static Vector3 ClampTargetLatitudeMaximum(
            Vector3 target,
            Transform reference,
            float maximumLatitude)
        {
            float targetLatitude = GetLatitude(reference) +
                (target.z - reference.position.z) /
                GentleSnowRules.WorldUnitsPerLatitude;
            if (targetLatitude <= maximumLatitude)
            {
                return target;
            }

            target.z -= (targetLatitude - maximumLatitude) *
                GentleSnowRules.WorldUnitsPerLatitude;
            return target;
        }

        internal static Vector3 ClampTargetLatitude(
            Vector3 target,
            Transform reference,
            float minimumLatitude)
        {
            float targetLatitude = GetLatitude(reference) +
                (target.z - reference.position.z) /
                GentleSnowRules.WorldUnitsPerLatitude;
            if (targetLatitude >= minimumLatitude)
            {
                return target;
            }

            target.z += (minimumLatitude - targetLatitude) *
                GentleSnowRules.WorldUnitsPerLatitude;
            return target;
        }
    }

    internal static class ModStormFactory
    {
        private static readonly List<ModStormController> CustomStorms =
            new List<ModStormController>();

        private static WeatherStorms owner;
        private static WanderingStorm[] originalStorms;
        private static bool initialized;

        internal static ModStormController Sandstorm { get; private set; }

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
                    controller.EnsureValidInitialPlacement();

                    if (definition.Id == CustomStormId.Sandstorm)
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
                RefreshSandstormCloudVisuals();
                GlobalLightningSettings.ApplyToLiveStorms();
                BetterStormPlugin.Instance?.LogFeatureInfo(
                    "Initialized three Squalls, one Hurricane, one Dry " +
                    "Thunderstorm, one Sandstorm, one Gentle Snow, one " +
                    "High Pressure, and one Magnetic Mist.");
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

    [HarmonyPatch(typeof(WeatherStorms), "Start")]
    internal static class WeatherStormsStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix(WeatherStorms __instance)
        {
            RuntimeEffectLifecycle.ShutdownTransientEffects();
            GlobalLightningSettings.RestoreSnapshots();
            ModStormFactory.Initialize(__instance);
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
            ModStormFactory.ApplyEnabledState();
        }
    }

    internal static class StormPositionCoordinates
    {
        internal static Vector3 ToSaveCoordinates(Vector3 runtimePosition)
        {
            FloatingOriginManager origin = FloatingOriginManager.instance;
            return origin != null
                ? origin.ShiftingPosToRealPos(runtimePosition)
                : runtimePosition;
        }

        internal static Vector3 ToRuntimeCoordinates(Vector3 savedPosition)
        {
            FloatingOriginManager origin = FloatingOriginManager.instance;
            return origin != null
                ? origin.RealPosToShiftingPos(savedPosition)
                : savedPosition;
        }
    }

    internal static class StormPositionPersistence
    {
        private static readonly PositionOwner[] Owners = BuildOwners();

        private static PositionOwner[] BuildOwners()
        {
            PositionOwner[] owners =
                new PositionOwner[StormCatalog.Custom.Length];
            for (int i = 0; i < StormCatalog.Custom.Length; i++)
            {
                StormDefinition definition = StormCatalog.Custom[i];
                owners[i] = new PositionOwner(
                    definition.Id,
                    definition.Name,
                    definition.SaveKey);
            }

            return owners;
        }

        internal static void BeginLoadAll()
        {
            for (int i = 0; i < Owners.Length; i++)
            {
                Owners[i].BeginLoad();
            }
        }

        internal static void LoadAll()
        {
            for (int i = 0; i < Owners.Length; i++)
            {
                Owners[i].Load();
            }
        }

        internal static void SaveAll()
        {
            for (int i = 0; i < Owners.Length; i++)
            {
                Owners[i].Save();
            }
        }

        internal static void ResetAll()
        {
            for (int i = 0; i < Owners.Length; i++)
            {
                Owners[i].Reset();
            }
        }

        internal static void ApplyLoadedPosition(
            CustomStormId id,
            ModStormController controller)
        {
            for (int i = 0; i < Owners.Length; i++)
            {
                if (Owners[i].StormId == id)
                {
                    Owners[i].ApplyLoadedPosition(controller);
                    return;
                }
            }
        }

        internal sealed class PositionOwner
        {
            private readonly CustomStormId stormId;
            private readonly string displayName;
            private readonly string betterStormV2Key;

            private bool loadCompleted;
            private bool hasLoadedPosition;
            private Vector3 loadedPosition;

            internal PositionOwner(
                CustomStormId stormId,
                string displayName,
                string betterStormV2Key)
            {
                this.stormId = stormId;
                this.displayName = displayName;
                this.betterStormV2Key = betterStormV2Key;
            }

            internal CustomStormId StormId
            {
                get { return stormId; }
            }

            internal void BeginLoad()
            {
                loadCompleted = false;
                hasLoadedPosition = false;
                loadedPosition = default(Vector3);
            }

            internal void Save()
            {
                ModStormController controller = ModStormFactory.Find(stormId);
                if (controller == null)
                {
                    return;
                }

                if (GameState.modData == null)
                {
                    GameState.modData = new Dictionary<string, string>();
                }

                Vector3 position = StormPositionCoordinates.ToSaveCoordinates(
                    controller.transform.position);
                SaveData data = new SaveData
                {
                    version = 2,
                    x = position.x,
                    y = position.y,
                    z = position.z
                };
                GameState.modData[betterStormV2Key] = JsonUtility.ToJson(data);
            }

            internal void Load()
            {
                loadCompleted = true;
                hasLoadedPosition = false;

                if (TryLoadKey(
                        betterStormV2Key,
                        out Vector3 position))
                {
                    loadedPosition = position;
                    hasLoadedPosition = true;
                }

                ApplyLoadedPosition(ModStormFactory.Find(stormId));
            }

            internal void ApplyLoadedPosition(ModStormController controller)
            {
                if (!loadCompleted || !hasLoadedPosition || controller == null)
                {
                    return;
                }

                controller.transform.position =
                    StormPositionCoordinates.ToRuntimeCoordinates(loadedPosition);
                controller.EnsureValidInitialPlacement();
            }

            internal void Reset()
            {
                loadCompleted = false;
                hasLoadedPosition = false;
                loadedPosition = default(Vector3);
            }

            private bool TryLoadKey(
                string key,
                out Vector3 position)
            {
                position = default(Vector3);
                if (GameState.modData == null ||
                    !GameState.modData.TryGetValue(key, out string json) ||
                    string.IsNullOrEmpty(json))
                {
                    return false;
                }

                try
                {
                    SaveData data = JsonUtility.FromJson<SaveData>(json);
                    if (data == null ||
                        data.version != 2 ||
                        !IsFinite(data.x) ||
                        !IsFinite(data.y) ||
                        !IsFinite(data.z))
                    {
                        BetterStormPlugin.Instance?.LogFeatureWarning(
                            "Ignoring invalid " + displayName +
                            " save data in key " + key + ".");
                        return false;
                    }

                    position = new Vector3(data.x, data.y, data.z);
                    return true;
                }
                catch (Exception exception)
                {
                    BetterStormPlugin.Instance?.LogFeatureWarning(
                        "Ignoring invalid " + displayName +
                        " save data in key " + key + ": " +
                        exception.Message);
                    return false;
                }
            }

            private static bool IsFinite(float value)
            {
                return !float.IsNaN(value) && !float.IsInfinity(value);
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public float x;
            public float y;
            public float z;
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), "LoadGame")]
    internal static class CustomStormBeginLoadPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            WindOverrideState.RestoreImmediately();
            RuntimeEffectLifecycle.ShutdownTransientEffects();
            StormPositionPersistence.BeginLoadAll();
            DoldrumsWind.Reset();
            MagneticMistFollow.BeginLoad();
            MagneticMistOpenOcean.Reset();
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), "SaveModData")]
    internal static class CustomStormSavePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            StormPositionPersistence.SaveAll();
            MagneticMistFollow.Save();
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), "LoadModData")]
    internal static class CustomStormLoadPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            StormPositionPersistence.LoadAll();
            MagneticMistFollow.Load();
        }
    }

    internal static class DryStormGameplay
    {
        internal static bool ShouldSuppressRain()
        {
            return StormInfluenceService.TryGetCurrent(
                       out StormInfluence influence) &&
                   influence.Definition != null &&
                   influence.Definition.SuppressRainParticles;
        }

        internal static bool ShouldSuppressFog()
        {
            return StormInfluenceService.TryGetCurrent(
                       out StormInfluence influence) &&
                   influence.Definition != null &&
                   influence.Definition.SuppressFog;
        }

        internal static void SuppressRainGameplay()
        {
            if (ShouldSuppressRain())
            {
                GameState.rainIntensity = 0f;
            }
        }
    }

    internal static class SandstormVisuals
    {
        private const float VerticalVelocity = -4f;
        private const float StartLifetime = 2f;
        private const float SandParticleIntensity = 50f;
        internal const float SandParticleMinimumSize = 0.01f;
        internal const float SandParticleMaximumSize = 0.02f;
        internal const float SandParticleSpawnRadius = 80f;
        private const float SandstormFogDensity = 0.03f;
        private const float NoiseStrength = 1.2f;
        private const float WindInfluence = 0.3f;
        private const float HorizontalVelocityMultiplier = 2f;
        internal const int MaximumParticles = 36000;
        internal const float EmissionRateMultiplier = 355.5556f;
        private const float DistantStormAlpha = 1f;
        private const float OverheadStormAlpha = 0.6f;

        private const string StormCloudTintShaderName =
            "Particles/Priority Alpha Blended";

        private static readonly Color SandParticleColor = new Color(
            0.4352941f,
            0.2588235f,
            0.1215686f,
            0.5f);
        private static readonly Color SandFogColor = new Color(
            195f / 255f,
            153f / 255f,
            72f / 255f,
            1f);
        private static readonly Color SandFogNightColor = new Color(
            56f / 255f,
            47f / 255f,
            36f / 255f,
            1f);

        private static ParticleSystem sourceRain;
        private static ParticleSystem sourceOuterRain;
        private static ParticleSystem sandRain;
        private static ParticleSystem distantStormClouds;
        private static ParticleSystem overheadStormClouds;
        private static SandstormCloudMaterialOwner distantStormCloudMaterial;
        private static SandstormCloudMaterialOwner overheadStormCloudMaterial;
        private static bool active;
        private static bool rainStageActive;

        internal static void ApplyWeather(
            StormInfluence influence,
            ParticleSystem rain,
            ParticleSystem outerRain,
            ParticleSystem rainSplash)
        {
            if (!influence.IsSandstorm)
            {
                Deactivate();
                return;
            }

            EnsureInitialized(rain, outerRain);
            if (!active)
            {
                CustomPrecipitationParticles.Clear(rain);
                CustomPrecipitationParticles.Clear(outerRain);
                CustomPrecipitationParticles.Clear(rainSplash);
            }
            active = true;
            SuppressVanillaRain(rain, outerRain, rainSplash);
            GameState.rainIntensity = 0f;

            if (!TryGetRainStageLerp(influence, out float rainStageLerp))
            {
                if (rainStageActive)
                {
                    CustomPrecipitationParticles.SetEmissionRate(sandRain, 0f);
                }
                rainStageActive = false;
                return;
            }

            rainStageActive = true;
            CustomPrecipitationParticles.SetEmissionRate(
                sandRain,
                SandParticleIntensity * rainStageLerp * EmissionRateMultiplier);
            RenderSettings.fogDensity = Mathf.Lerp(
                RenderSettings.fogDensity,
                SandstormFogDensity,
                rainStageLerp);
            RenderSettings.fogColor = Color.Lerp(
                RenderSettings.fogColor,
                GetCurrentSandstormColor(),
                rainStageLerp);
        }

        internal static void Tick()
        {
            RefreshStormCloudColors();
            if (!active ||
                !rainStageActive ||
                !TryGetActiveInfluence(out StormInfluence unused))
            {
                return;
            }

            UpdateParticleMotion(sandRain, Wind.currentWind * WindInfluence);
        }

        internal static void ApplyFogParticleColor(FogEffectController fog)
        {
            if (fog == null ||
                fog.fogParticles == null ||
                !TryGetActiveInfluence(out StormInfluence influence) ||
                !TryGetRainStageLerp(influence, out float rainStageLerp))
            {
                return;
            }

            ParticleSystem.MainModule main = fog.fogParticles.main;
            Color current = main.startColor.color;
            Color target = GetCurrentSandstormColor();
            target.a = current.a;
            main.startColor = Color.Lerp(current, target, rainStageLerp);
        }

        internal static void ApplyStormCloudColors(
            ParticleSystem topParticles,
            ParticleSystem bottomParticles)
        {
            distantStormClouds = topParticles;
            overheadStormClouds = bottomParticles;
            Color color = GetCurrentSandstormColor();
            distantStormCloudMaterial = ApplyStormCloudColor(
                distantStormClouds,
                color,
                DistantStormAlpha);
            overheadStormCloudMaterial = ApplyStormCloudColor(
                overheadStormClouds,
                color,
                OverheadStormAlpha);
        }

        internal static Color BlendSandstormColor(float nightLerp)
        {
            return Color.Lerp(
                SandFogColor,
                SandFogNightColor,
                Mathf.Clamp01(nightLerp));
        }

        internal static void Deactivate()
        {
            rainStageActive = false;
            CustomPrecipitationParticles.SetEmissionRate(sandRain, 0f);
            active = false;
        }

        internal static void DeactivateImmediately()
        {
            Deactivate();
            CustomPrecipitationParticles.Clear(sandRain);
        }

        internal static void Shutdown()
        {
            DeactivateImmediately();
            if (sandRain != null)
            {
                UnityEngine.Object.Destroy(sandRain.gameObject);
            }
            sourceRain = null;
            sourceOuterRain = null;
            sandRain = null;
        }

        private static bool TryGetActiveInfluence(out StormInfluence influence)
        {
            influence = default(StormInfluence);
            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            if (!StormInfluenceService.TryGetCurrent(out influence) ||
                !influence.IsSandstorm ||
                plugin == null ||
                !plugin.IsEnabled(CustomStormId.Sandstorm))
            {
                return false;
            }

            Region region = GameplayRegionResolver.GetCurrentRegion();
            return region != null &&
                region.gameObject.name == "Region Al'ankh";
        }

        private static bool TryGetRainStageLerp(
            StormInfluence influence,
            out float stageLerp)
        {
            WeatherStorms weatherStorms = WeatherStorms.instance;
            if (weatherStorms == null)
            {
                stageLerp = 0f;
                return false;
            }

            float rainBorder = StormAccess.RainBorder(weatherStorms);
            float normalizedDistance = 1f - influence.WeatherLerp;
            if (rainBorder <= 0f || normalizedDistance >= rainBorder)
            {
                stageLerp = 0f;
                return false;
            }

            stageLerp = Mathf.InverseLerp(
                rainBorder,
                0f,
                normalizedDistance);
            return stageLerp > 0f;
        }

        private static void EnsureInitialized(
            ParticleSystem rain,
            ParticleSystem outerRain)
        {
            if (rain == null || outerRain == null)
            {
                return;
            }

            if (sandRain != null &&
                sourceRain == rain &&
                sourceOuterRain == outerRain)
            {
                CustomPrecipitationParticles.FollowWorldShifts(sandRain);
                return;
            }

            Shutdown();
            sourceRain = rain;
            sourceOuterRain = outerRain;
            sandRain = CloneAndConfigure(
                outerRain,
                "Better Storm Sand Particles");
        }

        private static ParticleSystem CloneAndConfigure(
            ParticleSystem source,
            string name)
        {
            GameObject clone = UnityEngine.Object.Instantiate(
                source.gameObject,
                source.transform.position,
                source.transform.rotation,
                source.transform.parent);
            clone.name = name;
            clone.transform.localScale = source.transform.localScale;

            CustomPrecipitationParticles.DisableInheritedComponents(clone);

            ParticleSystem particles = clone.GetComponent<ParticleSystem>();
            if (particles == null)
            {
                UnityEngine.Object.Destroy(clone);
                return null;
            }

            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = StartLifetime;
            main.startSize3D = false;
            main.startSize = new ParticleSystem.MinMaxCurve(
                SandParticleMinimumSize,
                SandParticleMaximumSize);
            main.startSpeed = 0f;
            main.maxParticles = MaximumParticles;
            main.gravityModifier = 0f;
            main.startColor = SandParticleColor;
            // Not the outer rain's world space: the drops follow world shifts.
            CustomPrecipitationParticles.FollowWorldShifts(particles);

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = (ParticleSystemShapeType)8;
            shape.angle = 0f;
            shape.length = 29f;
            shape.radius = SandParticleSpawnRadius;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            shape.rotation = Vector3.zero;
            shape.position = Vector3.zero;

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.separateAxes = false;
            noise.strength = NoiseStrength;

            ParticleSystemRenderer renderer =
                clone.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            CustomPrecipitationParticles.SetEmissionRate(particles, 0f);
            IndoorPrecipitationExclusion.Attach(particles);
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            particles.Clear(true);
            particles.Play(true);
            return particles;
        }

        private static void UpdateParticleMotion(
            ParticleSystem particles,
            Vector3 windVelocity)
        {
            if (particles == null)
            {
                return;
            }

            ParticleSystem.VelocityOverLifetimeModule velocity =
                particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = windVelocity.x * HorizontalVelocityMultiplier;
            velocity.y = VerticalVelocity;
            velocity.z = windVelocity.z * HorizontalVelocityMultiplier;

            Camera camera = Camera.main;
            if (camera != null)
            {
                Vector3 worldPosition = camera.transform.position - windVelocity;
                worldPosition.y = 1f;
                particles.transform.position = worldPosition;
            }
        }

        private static SandstormCloudMaterialOwner ApplyStormCloudColor(
            ParticleSystem particles,
            Color baseColor,
            float alpha)
        {
            if (particles == null)
            {
                return null;
            }

            Color color = baseColor;
            color.a = Mathf.Clamp01(alpha);
            ParticleSystemRenderer renderer =
                particles.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                ParticleSystem.MainModule main = particles.main;
                main.startColor = color;
                return null;
            }

            Material sourceMaterial = renderer.sharedMaterial;
            Shader tintShader = Shader.Find(StormCloudTintShaderName);
            SandstormCloudMaterialOwner owner =
                particles.GetComponent<SandstormCloudMaterialOwner>();
            if (owner != null &&
                owner.OwnedMaterial != null &&
                owner.OwnedMaterial.shader == tintShader)
            {
                DisableParticleLighting(particles);
                ApplyTintAndAlpha(
                    particles,
                    owner.OwnedMaterial,
                    baseColor,
                    color.a);
                return owner;
            }

            if (sourceMaterial == null || tintShader == null)
            {
                ParticleSystem.MainModule main = particles.main;
                main.startColor = color;
                return null;
            }

            Texture mainTexture = sourceMaterial.HasProperty("_MainTex")
                ? sourceMaterial.GetTexture("_MainTex")
                : null;
            Vector2 textureScale = sourceMaterial.HasProperty("_MainTex")
                ? sourceMaterial.GetTextureScale("_MainTex")
                : Vector2.one;
            Vector2 textureOffset = sourceMaterial.HasProperty("_MainTex")
                ? sourceMaterial.GetTextureOffset("_MainTex")
                : Vector2.zero;
            float softParticleFactor = sourceMaterial.HasProperty("_InvFade")
                ? sourceMaterial.GetFloat("_InvFade")
                : 1f;

            Material tintMaterial = new Material(tintShader)
            {
                name = "Better Storm Sandstorm Cloud Tint",
                hideFlags = (HideFlags)52,
                renderQueue = sourceMaterial.renderQueue
            };
            tintMaterial.SetTexture("_MainTex", mainTexture);
            tintMaterial.SetTextureScale("_MainTex", textureScale);
            tintMaterial.SetTextureOffset("_MainTex", textureOffset);
            tintMaterial.SetFloat("_InvFade", softParticleFactor);

            DisableParticleLighting(particles);
            ApplyTintAndAlpha(
                particles,
                tintMaterial,
                baseColor,
                color.a);
            renderer.sharedMaterial = tintMaterial;
            if (owner == null)
            {
                owner = particles.gameObject.AddComponent<
                    SandstormCloudMaterialOwner>();
            }
            owner.Own(tintMaterial);
            return owner;
        }

        private static void ApplyTintAndAlpha(
            ParticleSystem particles,
            Material material,
            Color baseColor,
            float alpha)
        {
            float opacity = Mathf.Clamp01(alpha);
            ApplyMaterialTint(material, baseColor, opacity);

            Color particleColor = Color.white;
            particleColor.a = opacity;
            ParticleSystem.MainModule main = particles.main;
            main.startColor = particleColor;
            ApplyColorToExistingParticles(particles, particleColor);
        }

        private static void RefreshStormCloudColors()
        {
            Color color = GetCurrentSandstormColor();
            RefreshStormCloudColor(
                distantStormClouds,
                distantStormCloudMaterial,
                color,
                DistantStormAlpha);
            RefreshStormCloudColor(
                overheadStormClouds,
                overheadStormCloudMaterial,
                color,
                OverheadStormAlpha);
        }

        private static void RefreshStormCloudColor(
            ParticleSystem particles,
            SandstormCloudMaterialOwner materialOwner,
            Color baseColor,
            float alpha)
        {
            if (particles == null)
            {
                return;
            }

            if (materialOwner != null &&
                materialOwner.OwnedMaterial != null)
            {
                ApplyMaterialTint(
                    materialOwner.OwnedMaterial,
                    baseColor,
                    alpha);
                return;
            }

            Color particleColor = baseColor;
            particleColor.a = Mathf.Clamp01(alpha);
            ParticleSystem.MainModule main = particles.main;
            main.startColor = particleColor;
        }

        private static void ApplyMaterialTint(
            Material material,
            Color baseColor,
            float alpha)
        {
            Color materialTint = baseColor;
            materialTint.r *= 0.5f;
            materialTint.g *= 0.5f;
            materialTint.b *= 0.5f;
            materialTint.a = Mathf.Clamp01(alpha) * 0.5f;
            material.SetColor("_TintColor", materialTint);
        }

        private static Color GetCurrentSandstormColor()
        {
            float nightLerp = Sun.sun != null
                ? Sun.sun.GetNightLerp()
                : 0f;
            return BlendSandstormColor(nightLerp);
        }

        private static void ApplyColorToExistingParticles(
            ParticleSystem particles,
            Color color)
        {
            int particleCount = particles.particleCount;
            if (particleCount <= 0)
            {
                return;
            }

            ParticleSystem.Particle[] activeParticles =
                new ParticleSystem.Particle[particleCount];
            int activeCount = particles.GetParticles(activeParticles);
            Color32 particleColor = color;
            for (int i = 0; i < activeCount; i++)
            {
                activeParticles[i].startColor = particleColor;
            }
            particles.SetParticles(activeParticles, activeCount);
        }

        private static void DisableParticleLighting(ParticleSystem particles)
        {
            LitParticles[] lighting = particles.GetComponents<LitParticles>();
            for (int i = 0; i < lighting.Length; i++)
            {
                lighting[i].enabled = false;
            }
        }

        private static void SuppressVanillaRain(
            ParticleSystem rain,
            ParticleSystem outerRain,
            ParticleSystem rainSplash)
        {
            CustomPrecipitationParticles.SetEmissionRate(rain, 0f);
            CustomPrecipitationParticles.SetEmissionRate(outerRain, 0f);
            CustomPrecipitationParticles.SetEmissionRate(rainSplash, 0f);
        }
    }

    internal static class GentleSnowVisuals
    {
        internal const float FallSpeed = 1.25f;
        internal const float WindDriftMultiplier = 0.12f;
        internal const float ParticleLifetime = 30f;
        internal const float ParticleMinimumSize = 0.15f;
        internal const float ParticleMaximumSize = 0.3f;
        internal const int MaximumParticles = 20000;
        internal const float SpawnHeight = 50f;
        internal const float SpawnRadius = 150f;
        internal const float ParticleMinimumAlpha = 0.1f;
        internal const float ParticleMaximumAlpha = 0.25f;
        internal const float GravityModifier = 0.02f;
        private const float HorizontalNoiseStrength = 0.8f;
        private const float VerticalNoiseStrength = 0.15f;
        private const float NoiseFrequency = 0.22f;
        private const float NoiseScrollSpeed = 0.2f;

        private static readonly Color SnowColor = new Color(
            251f / 255f,
            253f / 255f,
            1f,
            1f);

        private static ParticleSystem snowParticles;
        private static bool active;

        internal static void ApplyWeather(
            StormInfluence influence,
            ParticleSystem rain,
            ParticleSystem outerRain,
            ParticleSystem rainSplash)
        {
            if (!influence.IsGentleSnow)
            {
                Deactivate();
                return;
            }

            EnsureInitialized(outerRain);
            if (!active)
            {
                CustomPrecipitationParticles.Clear(rain);
                CustomPrecipitationParticles.Clear(outerRain);
                CustomPrecipitationParticles.Clear(rainSplash);
            }

            active = true;
            CustomPrecipitationParticles.SetEmissionRate(rain, 0f);
            CustomPrecipitationParticles.SetEmissionRate(outerRain, 0f);
            CustomPrecipitationParticles.SetEmissionRate(rainSplash, 0f);
            GameState.rainIntensity = 0f;

            if (snowParticles == null)
            {
                return;
            }

            float emissionRate = MaximumParticles / ParticleLifetime;
            float emissionLerp = GentleSnowRules.GetSnowEmissionLerp(
                influence.NormalizedDistance);
            CustomPrecipitationParticles.SetEmissionRate(
                snowParticles,
                emissionRate * emissionLerp);
        }

        internal static void Tick()
        {
            if (!active ||
                !TryGetActiveInfluence(out StormInfluence unused) ||
                snowParticles == null)
            {
                return;
            }

            if (!UpdateEmitterPosition())
            {
                return;
            }

            ParticleSystem.VelocityOverLifetimeModule velocity =
                snowParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = Wind.currentWind.x * WindDriftMultiplier;
            velocity.y = -FallSpeed;
            velocity.z = Wind.currentWind.z * WindDriftMultiplier;

        }

        internal static void Deactivate()
        {
            CustomPrecipitationParticles.SetEmissionRate(snowParticles, 0f);
            active = false;
        }

        internal static void DeactivateImmediately()
        {
            Deactivate();
            CustomPrecipitationParticles.Clear(snowParticles);
        }

        internal static void Shutdown()
        {
            DeactivateImmediately();
            if (snowParticles != null)
            {
                UnityEngine.Object.Destroy(snowParticles.gameObject);
            }

            snowParticles = null;
        }

        private static bool TryGetActiveInfluence(
            out StormInfluence influence)
        {
            influence = default(StormInfluence);
            return StormInfluenceService.TryGetCurrent(out influence) &&
                influence.IsGentleSnow &&
                BetterStormPlugin.Instance != null &&
                BetterStormPlugin.Instance.IsEnabled(
                    CustomStormId.GentleSnow);
        }

        private static void EnsureInitialized(ParticleSystem outerRain)
        {
            if (outerRain == null)
            {
                return;
            }

            Transform owner = Refs.shiftingWorld != null
                ? Refs.shiftingWorld
                : outerRain.transform.parent;
            if (snowParticles != null)
            {
                if (snowParticles.transform.parent != owner)
                {
                    snowParticles.transform.SetParent(owner, true);
                }
                CustomPrecipitationParticles.FollowWorldShifts(snowParticles);
                return;
            }

            GameObject clone = UnityEngine.Object.Instantiate(
                outerRain.gameObject,
                outerRain.transform.position,
                outerRain.transform.rotation,
                owner);
            clone.name = "Better Storm Gentle Snow Particles";
            clone.transform.rotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            CustomPrecipitationParticles.DisableInheritedComponents(clone);

            snowParticles = clone.GetComponent<ParticleSystem>();
            if (snowParticles == null)
            {
                UnityEngine.Object.Destroy(clone);
                return;
            }

            ParticleSystem.MainModule main = snowParticles.main;
            main.startLifetime = ParticleLifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(
                ParticleMinimumSize,
                ParticleMaximumSize);
            main.startSpeed = 0f;
            main.maxParticles = MaximumParticles;
            main.gravityModifier = GravityModifier;
            main.startColor = GetSnowColorRange();
            CustomPrecipitationParticles.FollowWorldShifts(snowParticles);

            ParticleSystem.ShapeModule shape = snowParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = SpawnRadius;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            shape.scale = Vector3.one;
            shape.position = Vector3.zero;
            shape.rotation = new Vector3(90f, 0f, 0f);

            ParticleSystem.NoiseModule noise = snowParticles.noise;
            noise.enabled = true;
            noise.separateAxes = true;
            noise.strengthX = HorizontalNoiseStrength;
            noise.strengthY = VerticalNoiseStrength;
            noise.strengthZ = HorizontalNoiseStrength;
            noise.frequency = NoiseFrequency;
            noise.scrollSpeed = NoiseScrollSpeed;
            noise.damping = true;

            ParticleSystemRenderer renderer =
                clone.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                InstallSnowMaterial(clone, renderer);
            }

            ParticleSystem.EmissionModule emission = snowParticles.emission;
            emission.enabled = true;
            CustomPrecipitationParticles.SetEmissionRate(snowParticles, 0f);
            snowParticles.Clear(true);
            IndoorPrecipitationExclusion.Attach(snowParticles);
            UpdateEmitterPosition();
            snowParticles.Play(true);
        }

        private static bool UpdateEmitterPosition()
        {
            if (snowParticles == null)
            {
                return false;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            Vector3 position = camera.transform.position;
            position.y += SpawnHeight;
            snowParticles.transform.position = position;
            return true;
        }

        private static ParticleSystem.MinMaxGradient GetSnowColorRange()
        {
            Color minimum = SnowColor;
            minimum.a = ParticleMinimumAlpha;
            Color maximum = SnowColor;
            maximum.a = ParticleMaximumAlpha;
            return new ParticleSystem.MinMaxGradient(minimum, maximum);
        }

        private static void InstallSnowMaterial(
            GameObject owner,
            ParticleSystemRenderer renderer)
        {
            Material source = renderer.sharedMaterial;
            if (source == null)
            {
                return;
            }

            Material material = new Material(source)
            {
                name = "Better Storm Gentle Snow Material",
                hideFlags = (HideFlags)52
            };
            Texture2D texture = CreateSnowTexture();
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
            SetMaterialColorIfPresent(material, "_TintColor", Color.white);
            SetMaterialColorIfPresent(material, "_Color", Color.white);
            SetMaterialColorIfPresent(material, "_EmisColor", Color.white);
            SetMaterialColorIfPresent(material, "_EmissionColor", Color.white);
            renderer.sharedMaterial = material;

            SnowParticleResourceOwner resources =
                owner.AddComponent<SnowParticleResourceOwner>();
            resources.Own(material, texture);
        }

        private static void SetMaterialColorIfPresent(
            Material material,
            string propertyName,
            Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static Texture2D CreateSnowTexture()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false)
            {
                name = "Better Storm Gentle Snow Texture",
                hideFlags = (HideFlags)52,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 1f - Mathf.SmoothStep(0.35f, 1f, radius);
                    pixels[y * size + x] = new Color32(
                        255,
                        255,
                        255,
                        (byte)(Mathf.Clamp01(alpha) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

    }

    internal sealed class SnowParticleResourceOwner : MonoBehaviour
    {
        private Material material;
        private Texture2D texture;

        internal void Own(Material ownedMaterial, Texture2D ownedTexture)
        {
            material = ownedMaterial;
            texture = ownedTexture;
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                UnityEngine.Object.Destroy(material);
                material = null;
            }
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
        }
    }

    internal sealed class SandstormCloudMaterialOwner : MonoBehaviour
    {
        private Material ownedMaterial;

        internal Material OwnedMaterial
        {
            get { return ownedMaterial; }
        }

        internal void Own(Material material)
        {
            if (ownedMaterial != null && ownedMaterial != material)
            {
                UnityEngine.Object.Destroy(ownedMaterial);
            }
            ownedMaterial = material;
        }

        private void OnDestroy()
        {
            if (ownedMaterial != null)
            {
                UnityEngine.Object.Destroy(ownedMaterial);
                ownedMaterial = null;
            }
        }
    }

    internal static class SandstormDirt
    {
        private const float ApplicationInterval = 20f;
        private const float DirtStrength = 0.02f;

        private static Transform cachedBoat;
        private static CleanableObject cachedCleanable;
        private static float exposureTimer;

        internal static void Tick()
        {
            if (!TryGetExposedCleanable(out CleanableObject cleanable))
            {
                Reset();
                return;
            }

            exposureTimer += Time.deltaTime;
            if (exposureTimer < ApplicationInterval)
            {
                return;
            }

            if (ApplyVanillaDirt(cleanable))
            {
                exposureTimer -= ApplicationInterval;
            }
        }

        internal static void Reset()
        {
            cachedBoat = null;
            cachedCleanable = null;
            exposureTimer = 0f;
        }

        private static bool TryGetExposedCleanable(
            out CleanableObject cleanable)
        {
            cleanable = null;
            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            ModStormController sandstorm = ModStormFactory.Sandstorm;
            Transform boat = GameState.lastBoat;
            if (!GameState.playing ||
                plugin == null ||
                !plugin.IsEnabled(CustomStormId.Sandstorm) ||
                sandstorm == null ||
                sandstorm.Storm == null ||
                !sandstorm.Storm.active ||
                !sandstorm.gameObject.activeInHierarchy ||
                boat == null ||
                MasterPainter.instance == null)
            {
                return false;
            }

            Vector3 offset = boat.position - sandstorm.transform.position;
            offset.y = 0f;
            float outerEdge = sandstorm.Definition.Radius +
                sandstorm.Definition.FixedWeatherRange;
            if (offset.sqrMagnitude >= outerEdge * outerEdge)
            {
                return false;
            }

            if (cachedBoat != boat || cachedCleanable == null)
            {
                cachedBoat = boat;
                SaveableObject saveable = boat.GetComponent<SaveableObject>();
                cachedCleanable = saveable != null
                    ? saveable.GetCleanable()
                    : null;
                exposureTimer = 0f;
            }

            cleanable = cachedCleanable;
            return cleanable != null && cleanable.isActiveAndEnabled;
        }

        private static bool ApplyVanillaDirt(CleanableObject cleanable)
        {
            MasterPainter painter = MasterPainter.instance;
            if (painter == null || cleanable == null)
            {
                return false;
            }

            Texture2D dirtCoat = StormAccess.DirtCoat(cleanable);
            if (dirtCoat == null)
            {
                return false;
            }

            painter.ApplyCoat(cleanable, dirtCoat, DirtStrength);
            return true;
        }
    }

    [HarmonyPatch(typeof(WeatherStorms), "ApplyStorm")]
    internal static class GentleSnowClearWeatherPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix()
        {
            if (!StormInfluenceService.TryGetCurrent(
                    out StormInfluence influence) ||
                !(influence.IsGentleSnow || influence.IsDoldrums))
            {
                return true;
            }

            Weather weather = Weather.instance;
            if (weather == null ||
                weather.currentRegion == null ||
                weather.currentRegion.clearWeather == null)
            {
                return true;
            }

            weather.ChangeWeather(weather.currentRegion.clearWeather);
            return false;
        }
    }

    [HarmonyPatch(typeof(FogEffectController), "Update")]
    internal static class CustomStormFogPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(FogEffectController __instance)
        {
            if (DryStormGameplay.ShouldSuppressFog())
            {
                __instance.currentFogFromRegion = 0f;
                if (__instance.fogParticles != null)
                {
                    ParticleSystem.MainModule main =
                        __instance.fogParticles.main;
                    Color noFog = __instance.noFogColor;
                    noFog.a = 0f;
                    main.startColor = noFog;
                }
                return;
            }

            SandstormVisuals.ApplyFogParticleColor(__instance);
        }
    }

    [HarmonyPatch(typeof(Wind), "SetNewWindTarget")]
    internal static class GentleSnowWindTargetPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(new[]
        {
            BetterStormPlugin.ChaoticWindGuid,
            BetterStormPlugin.ClimateGuid
        })]
        private static void Postfix(ref Vector3 ___currentWindTarget)
        {
            if (!StormInfluenceService.TryGetCurrent(
                    out StormInfluence influence) ||
                !influence.IsGentleSnow)
            {
                return;
            }

            ___currentWindTarget = Vector3.ClampMagnitude(
                Wind.currentBaseWind,
                GentleSnowRules.MaximumWindTarget);
        }
    }

    // The Doldrums (HANDOFF_1.2.0.md §4): a calm, clear area south of 36° N.
    internal static class DoldrumsRules
    {
        internal const float CoreRadius = 2100f;
        internal const float OuterEdge = 6300f;
        internal const float FogClearDistance = 5300f;
        internal const float CoreBaseWind = 1.0f;
        internal const float GustMin = 0.67f;
        internal const float GustMaxAtEdge = 1.0f;
        internal const float FinalLerpSpeed = 0.25f;
        internal const float ChangeTimer = 30f;
        internal const float MaximumLatitude = 37f;               // the user, after 1.1.7 (was 36)

        // t: 0 at the outer edge, 1 at the core radius
        internal static float Depth(float distance)
        {
            return Mathf.InverseLerp(OuterEdge, CoreRadius, distance);
        }

        internal static float EffectiveBase(float normal, float t)
        {
            return Mathf.Lerp(normal, CoreBaseWind, t);
        }

        // The share of the rolled wind target kept at depth t; the base wind itself is never changed.
        internal static float TargetScale(float baseMagnitude, float t)
        {
            return baseMagnitude > 0.001f
                ? EffectiveBase(baseMagnitude, t) / baseMagnitude
                : 1f;
        }

        internal static float GustMax(float t)
        {
            return Mathf.Lerp(GustMaxAtEdge, GustMin, t);
        }

        // 1 at 6,300 -> 0 at 5,300
        internal static float FogFactor(float distance)
        {
            return Mathf.InverseLerp(FogClearDistance, OuterEdge, distance);
        }

        // 1 at 6,300 -> 0 at 2,100
        internal static float CloudFactor(float distance)
        {
            return Mathf.InverseLerp(CoreRadius, OuterEdge, distance);
        }

        // Latitude: the mirror of GentleSnowRules.
        internal static bool IsLatitudeAllowed(float latitude, float maximumLatitude)
        {
            return latitude <= maximumLatitude;
        }

        internal static float GetParkingLatitude(float maximumLatitude)
        {
            return maximumLatitude - GentleSnowRules.ParkingLatitudeBuffer;
        }

        internal static float GetMinimumDistanceToLatitude(
            float playerLatitude,
            float targetLatitude)
        {
            return Mathf.Max(0f, playerLatitude - targetLatitude) *
                GentleSnowRules.WorldUnitsPerLatitude;
        }

        internal static bool ShouldPark(
            float playerLatitude,
            float maximumLatitude,
            float relocationDistance)
        {
            return GetMinimumDistanceToLatitude(
                       playerLatitude,
                       GetParkingLatitude(maximumLatitude)) >
                   relocationDistance;
        }
    }

    // Only the wind target is scaled; the base wind (saved with the game) is never written.
    internal static class DoldrumsWind
    {
        private static readonly AccessTools.FieldRef<Wind, Vector3> WindTarget =
            AccessTools.FieldRefAccess<Wind, Vector3>("currentWindTarget");
        // private, no arguments; sets only currentGustTarget, never the gust timer
        private static readonly MethodInfo SetNewGustTarget =
            AccessTools.Method(typeof(Wind), "SetNewGustTarget");

        private static bool active;
        // the target as the game and every other mod rolled it, unscaled
        private static Vector3 normalTarget;

        internal static void Tick()
        {
            Wind wind = Wind.instance;
            if (wind == null)
            {
                Reset();
                return;
            }

            if (!StormInfluenceService.TryGetCurrent(out StormInfluence influence) ||
                !influence.IsDoldrums)
            {
                if (active)
                {
                    Exit(wind);
                }
                return;
            }

            if (!active)
            {
                active = true;
                normalTarget = WindTarget(wind);   // not scaled yet when the Doldrums takes control
            }

            Apply(wind, influence.WeatherLerp);
        }

        // SetNewWindTarget postfix, after every other mod
        internal static void AfterRoll(Wind wind)
        {
            if (!active)
            {
                return;
            }

            normalTarget = WindTarget(wind);
            if (StormInfluenceService.TryGetCurrent(out StormInfluence influence) &&
                influence.IsDoldrums)
            {
                Apply(wind, influence.WeatherLerp);
                // A postfix that ran before this one (Chaotic Wind, after a retarget it asked
                // for) may already have rolled a gust from the unscaled target: roll it again,
                // through StormGustPatch (the Doldrums gust, or vanilla while the orb commands).
                SetNewGustTarget?.Invoke(wind, null);
            }
        }

        internal static void Reset()
        {
            active = false;
        }

        internal static void RestoreImmediately()
        {
            Wind wind = Wind.instance;
            if (active && wind != null)
            {
                Exit(wind);
            }

            Reset();
        }

        private static void Apply(Wind wind, float t)
        {
            WindTarget(wind) = WindTotemCompatibility.OrbControlsWind
                ? normalTarget
                : normalTarget * DoldrumsRules.TargetScale(Wind.currentBaseWind.magnitude, t);
        }

        private static void Exit(Wind wind)
        {
            WindTarget(wind) = normalTarget;
            StormAccess.WindTimer(wind) = 0f;   // the game rolls a fresh target next frame
            Reset();
        }
    }

    [HarmonyPatch(typeof(Wind), "SetNewWindTarget")]
    internal static class DoldrumsWindTargetPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(new[]
        {
            BetterStormPlugin.ClimateGuid,
            BetterStormPlugin.ChaoticWindGuid,
            BetterStormPlugin.WindTotemGuid
        })]
        private static void Postfix(Wind __instance)
        {
            DoldrumsWind.AfterRoll(__instance);
        }
    }

    // Wind Totem (soft dependency, read by reflection): its portable orb overrides the calm.
    internal static class WindTotemCompatibility
    {
        private static bool resolved;
        private static FieldInfo ownerField;    // WindHold.owner: the orb being held, null when none
        private static Func<bool> holdActive;   // WindHold.Active: the 60-second hold after it is let go

        // true while a Wind Totem portable orb commands the wind: held, or in its 60-second hold
        internal static bool OrbControlsWind
        {
            get
            {
                if (!resolved)
                {
                    Resolve();
                }

                if (holdActive == null)
                {
                    return false;
                }

                UnityEngine.Object owner = ownerField.GetValue(null) as UnityEngine.Object;
                return owner != null || holdActive();
            }
        }

        private static void Resolve()
        {
            resolved = true;
            if (!Chainloader.PluginInfos.ContainsKey(BetterStormPlugin.WindTotemGuid))
            {
                return;
            }

            Type hold = AccessTools.TypeByName("WindTotem.WindHold");
            FieldInfo owner = hold != null ? AccessTools.Field(hold, "owner") : null;
            MethodInfo active = hold != null ? AccessTools.PropertyGetter(hold, "Active") : null;
            if (owner == null || active == null || !active.IsStatic ||
                active.ReturnType != typeof(bool))
            {
                BetterStormPlugin.Instance?.LogFeatureWarning(
                    "Wind Totem layout not recognised; its orb will not override the High Pressure.");
                return;
            }

            ownerField = owner;
            holdActive = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), active);
            BetterStormPlugin.Instance?.LogFeatureInfo(
                "Wind Totem orb overrides the High Pressure calm.");
        }
    }

    // The Magnetic Mist (HANDOFF_1.2.0.md §5): fog, cloud and light rain that disable navigation
    // and can follow the player, only 1° or more from land. Pure maths: no Unity native calls.
    internal static class MagneticMistRules
    {
        internal const float CoreRadius = 3500f;
        internal const float OuterEdge = 7000f;
        internal const float WanderSpeed = 16f;                // about 31 knots
        // 16 knots (the user, 2026-10-24) = 8.23 units/s; the game's Speedometer: knots = speed x 1.944
        internal const float FollowSpeed = 16f / 1.944f;
        internal const double FollowHours = 48.0;
        internal const double CooldownHours = 168.0;
        internal const float RainPeak = 3f;
        internal const float RainStartNormalized = 0.66f;
        internal const float RainFullNormalized = 0.33f;
        internal const float MaxWanderDegrees = 150f;
        internal const float SpinDegreesPerSecond = 120f;
        internal const float SpinBlendDistance = 200f;
        internal const float CoreCloudMultiplier = 2f;
        internal const float FogStart = OuterEdge;             // the fog starts at the outer edge
        internal const float MistFogPeak = 0.3f;               // the user, 2026-10-25 (0.35 first; 0.2 is the game's thickest)
        internal const double PushPauseHours = 6.0;            // after a sun-totem push it follows again 6 h later
        // a totem acts while |totemAttraction| >= 1: the game's decay stops just above 0, never at 0
        internal const float TotemActingThreshold = 1f;
        internal const float OpenOceanDistance = 9000f;        // works only 1° (9,000) or more from land
        internal const float OpenOceanOnMargin = 250f;         // on at 9,250, off below 9,000: no flicker

        // m: 0 at the outer edge, 1 at the core radius
        internal static float Strength(float distance)
        {
            return Mathf.InverseLerp(OuterEdge, CoreRadius, distance);
        }

        // 0 at 7,000 -> 1 at 3,500 (the same curve as m)
        internal static float FogFactor(float distance)
        {
            return Mathf.InverseLerp(FogStart, CoreRadius, distance);
        }

        // never lowers the fog already there
        internal static float MistFog(float current, float peak, float factor)
        {
            return Mathf.Max(current, Mathf.Lerp(current, peak, factor));
        }

        internal static bool IsPaused(double now, double pauseUntil)
        {
            return now < pauseUntil;
        }

        // §5.9, a released mist (away: from the player to the mist, wind: the wind now). It keeps leaving
        // while the player is inside its edge, or while it is upwind of the player and less than an edge
        // to the side, where the wind would carry it back over the player.
        internal static bool MustKeepLeaving(Vector3 away, Vector3 wind)
        {
            away.y = 0f;
            wind.y = 0f;
            if (away.magnitude < OuterEdge)
            {
                return true;
            }

            if (wind.sqrMagnitude < 1e-6f)
            {
                return false;
            }

            Vector3 w = wind.normalized;
            float along = Vector3.Dot(away, w);
            return along < 0f && (away - along * w).magnitude < OuterEdge;
        }

        // Away from the player but never upwind: an upwind part is dropped, so it leaves crosswind.
        internal static Vector3 LeaveDirection(Vector3 away, Vector3 wind)
        {
            away.y = 0f;
            wind.y = 0f;
            if (wind.sqrMagnitude < 1e-6f)
            {
                return away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward;
            }

            Vector3 w = wind.normalized;
            if (away.sqrMagnitude < 1e-4f)
            {
                return w;   // on the player: downwind
            }

            float along = Vector3.Dot(away, w);
            if (along >= 0f)
            {
                return away.normalized;
            }

            Vector3 side = away - along * w;
            return side.sqrMagnitude > 1e-4f ? side.normalized : new Vector3(-w.z, 0f, w.x);   // straight upwind: one side
        }

        internal static bool IsOpenOcean(bool wasOpen, float distanceToLand)
        {
            return distanceToLand >= OpenOceanDistance + (wasOpen ? 0f : OpenOceanOnMargin);
        }

        internal static float SpinBlend(float distance)
        {
            return Mathf.InverseLerp(CoreRadius + SpinBlendDistance, CoreRadius, distance);
        }

        internal static float WanderAmplitude(float m)
        {
            return m * m * MaxWanderDegrees;
        }

        internal static float NoiseFrequency(float m)
        {
            return 0.15f + 0.85f * m;
        }

        // 0 at normalised 0.66 (5,810) -> 3 at 0.33 (4,655) and inward
        internal static float RainTarget(float normalized)
        {
            return RainPeak * Mathf.InverseLerp(RainStartNormalized, RainFullNormalized, normalized);
        }

        // about the 1-second lag of the game's weather lerp (Weather.lerpRate 1)
        internal static float EaseRain(float current, float target, float dt)
        {
            return current + (target - current) * (1f - (float)Math.Exp(-dt));
        }

        internal static float CoreCloudDensity(float clear, float storm, float m)
        {
            return Mathf.Lerp(clear, storm * CoreCloudMultiplier, m);
        }

        internal static bool ShouldCapture(
            bool wasInside,
            bool insideNow,
            double now,
            double cooldownUntil,
            bool otherStorm)
        {
            return insideNow && !wasInside && now >= cooldownUntil && !otherStorm;
        }
    }

    // §5.11: the mist works only while the player is 1° (9,000) or more from land.
    internal static class MagneticMistOpenOcean
    {
        private static bool open;
        private static bool measured;
        // After a load, a measure counts only once a tracker cycle begun after the load has landed: the
        // cycle begun at the start menu runs on through the load and lands with islands measured from the
        // menu's place (1,666 for a player saved 10,035 out, BetterStorm_TEST_RESULTS.md).
        private static bool awaitingFreshCycle;
        private static bool freshCycleStarted;

        // true only when a real measure says the player is under 1° from land (not just after a load)
        internal static bool KnownNearLand
        {
            get { return measured && !open; }
        }

        internal static bool Check()
        {
            // to the nearest island's centre, measured about every 3 s; 999,999 just after a load
            float distance = GameState.distanceToLand;
            measured = !awaitingFreshCycle && distance < 999000f;
            open = measured && MagneticMistRules.IsOpenOcean(open, distance);
            return open;
        }

        internal static void Reset()
        {
            open = false;
            measured = false;
            awaitingFreshCycle = true;
            freshCycleStarted = false;
        }

        // Each IslandDistanceTracker.Update: started, a cycle began this frame; updating, one is running.
        internal static void TrackerTick(bool started, bool updating)
        {
            if (!awaitingFreshCycle)
            {
                return;
            }

            if (started)
            {
                freshCycleStarted = true;
            }
            else if (freshCycleStarted && !updating)
            {
                awaitingFreshCycle = false;
            }
        }
    }

    // Watches the game's land-distance tracker for MagneticMistOpenOcean: its Update starts a cycle when
    // its cooldown has run out (sets updating, then the coroutine clears it as it writes the distance;
    // CustomIslandAPI 1.0.2's replacement coroutine keeps both).
    [HarmonyPatch(typeof(IslandDistanceTracker), "Update")]
    internal static class LandMeasureCyclePatch
    {
        private static readonly AccessTools.FieldRef<IslandDistanceTracker, float> CooldownTimer =
            AccessTools.FieldRefAccess<IslandDistanceTracker, float>("cooldownTimer");
        private static readonly AccessTools.FieldRef<IslandDistanceTracker, bool> Updating =
            AccessTools.FieldRefAccess<IslandDistanceTracker, bool>("updating");

        [HarmonyPrefix]
        private static void Prefix(IslandDistanceTracker __instance, out bool __state)
        {
            __state = CooldownTimer(__instance) <= 0f;
        }

        [HarmonyPostfix]
        private static void Postfix(IslandDistanceTracker __instance, bool __state)
        {
            bool updating = Updating(__instance);
            MagneticMistOpenOcean.TrackerTick(__state && updating, updating);
        }
    }

    // §5.9: wander, capture, follow, release, cooldown; totems move it like every storm.
    internal static class MagneticMistFollow
    {
        private const string SaveKey = "DogEggz.BetterStorm.magnetic-mist-follow.v1";

        private static double followStart;
        private static double cooldownUntil;
        private static double pauseUntil;   // after a sun-totem push: the follow waits until then
        private static bool returning;      // pushed away: comes back at wander speed until the player is inside its edge
        private static bool leaving;        // released: moves away at wander speed until the player is outside its edge
        private static bool wasInside;
        private static bool syncInside = true;

        internal static bool IsFollowing { get; private set; }

        internal static void UpdateMovement(ModStormController controller, Transform player)
        {
            Transform mist = controller.transform;
            Vector3 toPlayer = player.position - mist.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            bool insideNow = controller.Storm.active && distance < MagneticMistRules.OuterEdge;
            if (syncInside)
            {
                // after a load or a switch-on: being inside then never captures
                wasInside = insideNow;
                syncInside = false;
            }

            bool haveClock = GameHours.TryNow(out double now);

            // Totems: the sun totem pushes it away, the rain totem pulls it in, wandering or
            // following, with the movement of every storm (WanderingStorm.Update).
            float attraction = WeatherStorms.totemAttraction;
            if (attraction != 0f && distance > 0.01f)
            {
                mist.Translate(
                    -toPlayer / distance * Time.deltaTime * attraction * controller.Storm.totemMult,
                    Space.World);
            }

            if (IsFollowing && haveClock)
            {
                if (attraction >= MagneticMistRules.TotemActingThreshold)
                {
                    pauseUntil = now + MagneticMistRules.PushPauseHours;   // back 6 h after the push ends
                    returning = true;
                }
                else if (attraction <= -MagneticMistRules.TotemActingThreshold)
                {
                    pauseUntil = 0.0;   // pulled in: follows again at once
                }
            }

            if (!IsFollowing)
            {
                if (leaving && !MagneticMistRules.MustKeepLeaving(-toPlayer, Wind.currentWind))
                {
                    leaving = false;
                }

                // Released: away from the player (never upwind) until the wind can't carry it back, then with the wind.
                Vector3 direction = leaving
                    ? MagneticMistRules.LeaveDirection(-toPlayer, Wind.currentWind)
                    : Wind.currentWind.normalized;
                mist.Translate(direction * Time.deltaTime * MagneticMistRules.WanderSpeed, Space.World);
                if (haveClock &&
                    MagneticMistRules.ShouldCapture(
                        wasInside,
                        insideNow,
                        now,
                        cooldownUntil,
                        AnyOtherStormAffectsPlayer(controller, player)))
                {
                    IsFollowing = true;
                    followStart = now;
                    leaving = false;
                    BetterStormPlugin.Instance?.LogFeatureInfo("Magnetic Mist captured the player.");
                }
            }
            else
            {
                // Pushed away: it stays where the push left it (no wind drift) until the pause ends.
                if (!(haveClock && MagneticMistRules.IsPaused(now, pauseUntil)))
                {
                    if (returning && distance < MagneticMistRules.OuterEdge)
                    {
                        returning = false;
                    }

                    // coming back from a push at wander speed, then the follow speed
                    float speed = returning ? MagneticMistRules.WanderSpeed : MagneticMistRules.FollowSpeed;
                    float step = Mathf.Min(speed * Time.deltaTime, distance);
                    if (step > 0f)
                    {
                        mist.Translate(toPlayer / distance * step, Space.World);
                    }
                }

                // the 48 hours keep running while paused
                if (haveClock && now - followStart >= MagneticMistRules.FollowHours)
                {
                    Release(now);
                }
            }

            wasInside = insideNow;
        }

        // Every active storm counts, the Doldrums and Gentle Snow included.
        private static bool AnyOtherStormAffectsPlayer(ModStormController self, Transform player)
        {
            WeatherStorms weatherStorms = WeatherStorms.instance;
            WanderingStorm[] storms = weatherStorms != null ? StormAccess.Storms(weatherStorms) : null;
            if (storms == null)
            {
                return false;
            }

            for (int i = 0; i < storms.Length; i++)
            {
                WanderingStorm storm = storms[i];
                if (storm == null || storm == self.Storm || !storm.active || !storm.isActiveAndEnabled)
                {
                    continue;
                }

                Vector3 offset = storm.transform.position - player.position;
                offset.y = 0f;
                float edge = storm.GetRadius() + StormInfluenceService.GetWeatherRange(
                    weatherStorms,
                    storm.GetComponent<ModStormController>());
                if (offset.magnitude < edge)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Release(double now)
        {
            IsFollowing = false;
            pauseUntil = 0.0;
            returning = false;
            leaving = true;
            cooldownUntil = now + MagneticMistRules.CooldownHours;
            BetterStormPlugin.Instance?.LogFeatureInfo(
                "Magnetic Mist released the player; cooldown " + MagneticMistRules.CooldownHours +
                " in-game hours.");
        }

        // the player reached land (under 1°) while followed: the escape, the same as a natural release
        internal static void ReleaseAtLand()
        {
            if (IsFollowing && GameHours.TryNow(out double now))
            {
                Release(now);
            }
        }

        internal static void StopFollowing()
        {
            IsFollowing = false;
            pauseUntil = 0.0;
            returning = false;
            leaving = false;
        }

        // debug "End follow now": the same as a natural release (starts the cooldown)
        internal static void DebugEndFollow()
        {
            if (IsFollowing && GameHours.TryNow(out double now))
            {
                Release(now);
            }
        }

        internal static void DebugClearCooldown()
        {
            cooldownUntil = 0.0;
        }

        // Debug "Push to outer edge": the follow ends, without a cooldown. The player counts as
        // inside, so the mist drifting back over its edge doesn't capture; a capture needs a real
        // crossing later (out, then in again) or a Summon.
        internal static void DebugPushedToEdge()
        {
            IsFollowing = false;
            pauseUntil = 0.0;
            returning = false;
            leaving = false;
            wasInside = true;
            syncInside = false;
        }

        // Debug "Summon": the next frame counts as entering, so it captures when allowed.
        internal static void DebugSummoned()
        {
            leaving = false;
            wasInside = false;
            syncInside = false;
        }

        // Switched on (SetModEnabled(true)): the player's position taken as-is, as after a load.
        internal static void SyncInside()
        {
            syncInside = true;
        }

        internal static string DescribeState()
        {
            if (!GameHours.TryNow(out double now))
            {
                return "Magnetic Mist: -";
            }

            string left = (MagneticMistRules.FollowHours - (now - followStart)).ToString("0.0") + " h left";
            if (IsFollowing && MagneticMistRules.IsPaused(now, pauseUntil))
            {
                return "Following: " + left + "; pushed away, back in " +
                    (pauseUntil - now).ToString("0.0") + " h";
            }

            if (IsFollowing)
            {
                return "Following: " + left + (returning ? "; coming back" : "");
            }

            if (now < cooldownUntil)
            {
                return "Cooldown: " + (cooldownUntil - now).ToString("0.0") + " h left" +
                    (leaving ? "; moving away" : "");
            }

            return "Wandering (can capture)";
        }

        internal static void Save()
        {
            if (GameState.modData == null)
            {
                GameState.modData = new Dictionary<string, string>();
            }

            GameState.modData[SaveKey] = JsonUtility.ToJson(new SaveData
            {
                version = 1,
                following = IsFollowing,
                followStart = followStart,
                cooldownUntil = cooldownUntil,
                pauseUntil = pauseUntil,
                returning = returning,
                leaving = leaving
            });
        }

        internal static void Load()
        {
            BeginLoad();
            if (GameState.modData == null ||
                !GameState.modData.TryGetValue(SaveKey, out string json) ||
                string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || data.version != 1)
                {
                    return;
                }

                IsFollowing = data.following;
                followStart = data.followStart;
                cooldownUntil = data.cooldownUntil;
                pauseUntil = data.pauseUntil;
                returning = data.returning;   // absent in 1.1.7 saves: false
                leaving = data.leaving;
            }
            catch (Exception exception)
            {
                BetterStormPlugin.Instance?.LogFeatureWarning(
                    "Ignoring invalid Magnetic Mist follow data: " + exception.Message);
            }
        }

        internal static void BeginLoad()
        {
            IsFollowing = false;
            followStart = 0.0;
            cooldownUntil = 0.0;
            pauseUntil = 0.0;
            returning = false;
            leaving = false;
            syncInside = true;
        }

        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public bool following;
            public double followStart;
            public double cooldownUntil;
            public double pauseUntil;
            public bool returning;
            public bool leaving;
        }
    }

    // §5.7: the mist's rain, eased with a 1-second lag like the game's weather lerp.
    internal static class MagneticMistRain
    {
        internal static float Current { get; private set; }

        internal static void Tick()
        {
            float target = 0f;
            if (StormInfluenceService.TryGetCurrent(out StormInfluence influence) &&
                influence.IsMagneticMist)
            {
                target = MagneticMistRules.RainTarget(influence.NormalizedDistance);
            }

            Current = MagneticMistRules.EaseRain(Current, target, Time.deltaTime);
            if (target <= 0f && Current < 0.001f)
            {
                Current = 0f;
            }
        }

        internal static void Reset()
        {
            Current = 0f;
        }
    }

    // §5.4: the field by distance from the player (whether or not the mist controls the weather).
    internal static class MagneticField
    {
        private static readonly AccessTools.FieldRef<Compass, Vector3> LockAxis =
            AccessTools.FieldRefAccess<Compass, Vector3>("lockAxis");
        private static float spinAngle;

        internal static float Strength { get; private set; }   // m, 0 when outside or switched off
        internal static float Spin { get; private set; }       // 0..1, the core blend

        internal static void Tick()
        {
            Strength = 0f;
            Spin = 0f;
            ModStormController mist = ModStormFactory.Find(CustomStormId.MagneticMist);
            Transform player = Camera.main != null ? Camera.main.transform : null;
            if (GameState.playing && mist != null && player != null &&
                mist.isActiveAndEnabled && mist.Storm != null && mist.Storm.active)
            {
                Vector3 offset = mist.transform.position - player.position;
                offset.y = 0f;
                Strength = MagneticMistRules.Strength(offset.magnitude);
                Spin = MagneticMistRules.SpinBlend(offset.magnitude);
            }

            float dt = Time.deltaTime;
            if (Spin > 0f)
            {
                spinAngle = Mathf.Repeat(spinAngle + MagneticMistRules.SpinDegreesPerSecond * Spin * dt, 360f);
            }
            else
            {
                // settle back instead of keeping a frozen offset after leaving the core
                spinAngle = Mathf.MoveTowardsAngle(spinAngle, 0f, 60f * dt);
            }
        }

        // yaw-only compasses (lock axis (0, 1, 0)); the level spheres are left alone
        internal static bool Affects(Compass compass)
        {
            return Strength > 0f && LockAxis(compass) == new Vector3(0f, 1f, 0f);
        }

        internal static float CompassOffsetDegrees(int seed)
        {
            float m = Strength;
            return MagneticMistRules.WanderAmplitude(m) * Noise(seed, MagneticMistRules.NoiseFrequency(m)) +
                Direction(seed) * spinAngle;
        }

        // The clocks follow the compass (the user, 2026-10-24): a dial's fastest hand is turned by a
        // compass-sized angle, with its own seed.
        internal static float DialOffsetDegrees(int seed)
        {
            return Strength > 0f ? CompassOffsetDegrees(seed + 7919) : 0f;
        }

        private static float Noise(int seed, float frequency)
        {
            return Mathf.PerlinNoise((seed % 997) * 0.37f, Time.time * frequency) * 2f - 1f;
        }

        private static float Direction(int seed)
        {
            return (seed & 1) == 0 ? 1f : -1f;
        }
    }

    // §5.4: compasses wander in the Magnetic Mist's band and spin in its core; clocks read wrong.
    [HarmonyPatch(typeof(Compass), "FixedUpdate")]
    internal static class MagneticCompassPatch
    {
        private static readonly AccessTools.FieldRef<Compass, Vector3> NorthAngles =
            AccessTools.FieldRefAccess<Compass, Vector3>("northAngles");
        private static readonly AccessTools.FieldRef<Compass, Vector3> LockAxis =
            AccessTools.FieldRefAccess<Compass, Vector3>("lockAxis");
        private static readonly AccessTools.FieldRef<Compass, float> Magnetism =
            AccessTools.FieldRefAccess<Compass, float>("magnetism");

        [HarmonyPrefix]
        private static bool Prefix(Compass __instance)
        {
            if (!MagneticField.Affects(__instance))
            {
                return true;   // outside, or a level sphere: vanilla
            }

            // vanilla FixedUpdate, with the magnetic offset turned into its north
            float offset = MagneticField.CompassOffsetDegrees(__instance.GetInstanceID());
            Quaternion north = Quaternion.AngleAxis(offset, Vector3.up) *
                Quaternion.Euler(NorthAngles(__instance));
            Transform transform = __instance.transform;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                north,
                Time.deltaTime * Magnetism(__instance));
            Vector3 euler = transform.localEulerAngles;
            Vector3 lockAxis = LockAxis(__instance);
            transform.localEulerAngles = new Vector3(
                euler.x * lockAxis.x,
                euler.y * lockAxis.y,
                euler.z * lockAxis.z);
            return false;
        }
    }

    [HarmonyPatch(typeof(ChronometerTime), "Update")]
    internal static class MagneticChronometerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ChronometerTime __instance)
        {
            float degrees = MagneticField.DialOffsetDegrees(__instance.GetInstanceID());
            if (degrees != 0f)
            {
                // the 24-hour dial (15° an hour) turned like a compass card
                __instance.transform.localRotation *= Quaternion.Euler(0f, 0f, degrees);
            }
        }
    }

    [HarmonyPatch(typeof(ShipItemClock), "ExtraLateUpdate")]
    internal static class MagneticClockPatch
    {
        private static readonly AccessTools.FieldRef<ShipItemClock, Transform> HourHand =
            AccessTools.FieldRefAccess<ShipItemClock, Transform>("hourHand");
        private static readonly AccessTools.FieldRef<ShipItemClock, Transform> MinuteHand =
            AccessTools.FieldRefAccess<ShipItemClock, Transform>("minuteHand");

        [HarmonyPostfix]
        private static void Postfix(ShipItemClock __instance)
        {
            if (MagneticField.Strength <= 0f || Sun.sun == null)
            {
                return;
            }

            // vanilla: hour hand globalTime x 30, minute hand globalTime x 360 (once a game minute);
            // the minute hand is turned by the compass-sized angle (360° an hour), the hour hand
            // follows it; after the player leaves, the minute hand corrects itself at its next tick
            float hours = Sun.sun.globalTime +
                MagneticField.DialOffsetDegrees(__instance.GetInstanceID()) / 360f;
            HourHand(__instance).localEulerAngles = new Vector3(hours * 30f + 90f, -90f, 90f);
            MinuteHand(__instance).localEulerAngles = new Vector3(hours * 360f + 90f, -90f, 90f);
        }
    }

    // §5.10: Mariners HUD (soft dependency, verified on 3.0.2). Its own clear-weather check compares
    // storm distance in world units with 0.66/0.33, so it always reports clear: the swap is needed.
    internal static class MarinersHudCompatibility
    {
        private const int HeadingSeed = 0x4D48;
        private const int TimeSeed = 0x4D49;

        private static FieldInfo headingField;
        private static FieldInfo timeTextField;
        private static FieldInfo timeValidField;
        private static FieldInfo latFreshField;
        private static FieldInfo lonFreshField;
        private static FieldInfo preferLocalField;
        private static MethodInfo formatTime;

        internal struct HudState
        {
            internal bool Swapped;
            internal float Heading;
            internal string TimeText;
        }

        internal static void Install(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.TryGetValue(
                    BetterStormPlugin.MarinersHudGuid,
                    out PluginInfo info) ||
                info == null ||
                info.Instance == null)
            {
                return;
            }

            Type type = info.Instance.GetType();   // MarinersHUD.MarinersHUDPlugin
            headingField = AccessTools.Field(type, "_snapHeadingDeg");          // float, set in Update()
            timeTextField = AccessTools.Field(type, "_snapTimeText");           // string, set in UpdateTimeReading()
            timeValidField = AccessTools.Field(type, "_snapTimeValid");         // bool
            latFreshField = AccessTools.Field(type, "_navLatFresh");            // bool, set in UpdateNavigation()
            lonFreshField = AccessTools.Field(type, "_navLonFresh");
            preferLocalField = AccessTools.Field(type, "_preferLocalTimeConfig");   // ConfigEntry<bool>
            formatTime = AccessTools.Method(type, "FormatGameTime", new[] { typeof(float) });   // private static string
            MethodInfo onGui = AccessTools.Method(type, "OnGUI");
            MethodInfo navigation = AccessTools.Method(type, "UpdateNavigation");
            if (headingField == null || headingField.FieldType != typeof(float) ||
                timeTextField == null || timeTextField.FieldType != typeof(string) ||
                timeValidField == null || timeValidField.FieldType != typeof(bool) ||
                latFreshField == null || latFreshField.FieldType != typeof(bool) ||
                lonFreshField == null || lonFreshField.FieldType != typeof(bool) ||
                preferLocalField == null || formatTime == null || !formatTime.IsStatic ||
                formatTime.ReturnType != typeof(string) || onGui == null || navigation == null)
            {
                BetterStormPlugin.Instance?.LogFeatureWarning(
                    "Mariners HUD layout not recognised; magnetic HUD effects disabled.");
                return;
            }

            try
            {
                harmony.Patch(
                    onGui,
                    new HarmonyMethod(typeof(MarinersHudCompatibility), nameof(OnGuiPrefix)),
                    new HarmonyMethod(typeof(MarinersHudCompatibility), nameof(OnGuiPostfix)));
                harmony.Patch(
                    navigation,
                    new HarmonyMethod(typeof(MarinersHudCompatibility), nameof(NavigationPrefix)));
                BetterStormPlugin.Instance?.LogFeatureInfo("Installed Mariners HUD magnetic compatibility.");
            }
            catch (Exception exception)
            {
                BetterStormPlugin.Instance?.LogFeatureError(
                    "Mariners HUD magnetic compatibility could not be installed: " + exception);
            }
        }

        // Scrambled values only while drawing, so the HUD's own state and fix times stay true.
        private static void OnGuiPrefix(object __instance, out HudState __state)
        {
            __state = default(HudState);
            if (MagneticField.Strength <= 0f)
            {
                return;
            }

            __state.Swapped = true;
            __state.Heading = (float)headingField.GetValue(__instance);
            __state.TimeText = (string)timeTextField.GetValue(__instance);
            headingField.SetValue(
                __instance,
                Mathf.Repeat(__state.Heading + MagneticField.CompassOffsetDegrees(HeadingSeed), 360f));
            if ((bool)timeValidField.GetValue(__instance) && Sun.sun != null)
            {
                ConfigEntry<bool> preferLocal = preferLocalField.GetValue(__instance) as ConfigEntry<bool>;
                float raw = preferLocal != null && preferLocal.Value
                    ? Sun.sun.localTime
                    : Sun.sun.globalTime;
                // off like the ship's clock: the compass-sized angle on the minute hand
                float scrambled = Mathf.Repeat(raw + MagneticField.DialOffsetDegrees(TimeSeed) / 360f, 24f);
                timeTextField.SetValue(__instance, (string)formatTime.Invoke(null, new object[] { scrambled }));
            }
        }

        private static void OnGuiPostfix(object __instance, HudState __state)
        {
            if (!__state.Swapped)
            {
                return;
            }

            headingField.SetValue(__instance, __state.Heading);
            timeTextField.SetValue(__instance, __state.TimeText);
        }

        // No new latitude/longitude fixes in the core; the last-known values stay as the HUD shows them.
        private static bool NavigationPrefix(object __instance)
        {
            if (MagneticField.Strength < 1f)
            {
                return true;
            }

            latFreshField.SetValue(__instance, false);
            lonFreshField.SetValue(__instance, false);
            return false;
        }
    }

    // §5.4: Kemy's Navigation Suite (soft dependency, verified on 1.0.0). Its hand compass and binnacle
    // compass Slerp their card toward true north in LateUpdate, a yaw about the parent's up. The card is
    // turned back by the magnetic offset before that Slerp and forward again after it, so the Slerp aims
    // at the offset north with the mod's own smoothing.
    internal static class KemyNavigationCompatibility
    {
        private static FieldInfo compassCardField;   // CompassPhysics.compassCardTransform
        private static FieldInfo binnacleFaceField;  // BinnaclePhysics.compassFace

        internal static void Install(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.TryGetValue(
                    BetterStormPlugin.KemyNavigationGuid,
                    out PluginInfo info) ||
                info == null ||
                info.Instance == null)
            {
                return;
            }

            Assembly assembly = info.Instance.GetType().Assembly;
            Type compass = assembly.GetType("KemyNavTools.CompassPhysics");
            Type binnacle = assembly.GetType("KemyNavTools.BinnaclePhysics");
            compassCardField = compass != null ? AccessTools.Field(compass, "compassCardTransform") : null;
            binnacleFaceField = binnacle != null ? AccessTools.Field(binnacle, "compassFace") : null;
            MethodInfo compassLate = compass != null ? AccessTools.Method(compass, "LateUpdate") : null;
            MethodInfo binnacleLate = binnacle != null ? AccessTools.Method(binnacle, "LateUpdate") : null;
            if (compassCardField == null || compassCardField.FieldType != typeof(Transform) ||
                binnacleFaceField == null || binnacleFaceField.FieldType != typeof(Transform) ||
                compassLate == null || binnacleLate == null)
            {
                BetterStormPlugin.Instance?.LogFeatureWarning(
                    "Kemy's Navigation Suite layout not recognised; its compasses stay true in the mist.");
                return;
            }

            try
            {
                harmony.Patch(
                    compassLate,
                    new HarmonyMethod(typeof(KemyNavigationCompatibility), nameof(CompassPrefix)),
                    new HarmonyMethod(typeof(KemyNavigationCompatibility), nameof(CompassPostfix)));
                harmony.Patch(
                    binnacleLate,
                    new HarmonyMethod(typeof(KemyNavigationCompatibility), nameof(BinnaclePrefix)),
                    new HarmonyMethod(typeof(KemyNavigationCompatibility), nameof(BinnaclePostfix)));
                BetterStormPlugin.Instance?.LogFeatureInfo("Installed Kemy's Navigation Suite magnetic compatibility.");
            }
            catch (Exception exception)
            {
                BetterStormPlugin.Instance?.LogFeatureError(
                    "Kemy's Navigation Suite magnetic compatibility could not be installed: " + exception);
            }
        }

        private static void CompassPrefix(object __instance, out float __state)
        {
            __state = TurnBack(compassCardField, __instance);
        }

        private static void CompassPostfix(object __instance, float __state)
        {
            TurnForward(compassCardField, __instance, __state);
        }

        private static void BinnaclePrefix(object __instance, out float __state)
        {
            __state = TurnBack(binnacleFaceField, __instance);
        }

        private static void BinnaclePostfix(object __instance, float __state)
        {
            TurnForward(binnacleFaceField, __instance, __state);
        }

        // Returns the offset turned back (0: nothing done).
        private static float TurnBack(FieldInfo field, object instance)
        {
            if (MagneticField.Strength <= 0f || !(field.GetValue(instance) is Transform card) || card == null)
            {
                return 0f;
            }

            float offset = MagneticField.CompassOffsetDegrees(((UnityEngine.Object)instance).GetInstanceID());
            card.localRotation = Quaternion.Euler(0f, -offset, 0f) * card.localRotation;
            return offset;
        }

        private static void TurnForward(FieldInfo field, object instance, float offset)
        {
            if (offset != 0f && field.GetValue(instance) is Transform card && card != null)
            {
                card.localRotation = Quaternion.Euler(0f, offset, 0f) * card.localRotation;
            }
        }
    }

    // §5.5: shadows, stars, moon and rainbow fade with (1 - m) while the Magnetic Mist controls the weather.
    internal static class MagneticSky
    {
        private static Light sun;
        private static Light moon;
        private static float sunOriginal = -1f;
        private static float moonOriginal = -1f;

        internal static float Depth { get; private set; }   // m while the mist is in control, else 0
        internal static bool MoonHidden { get; set; }

        internal static void Tick()
        {
            Depth = StormInfluenceService.TryGetCurrent(out StormInfluence influence) &&
                influence.IsMagneticMist
                ? influence.WeatherLerp
                : 0f;
            Track(ref sun, ref sunOriginal, Sun.sun != null ? Sun.sun.GetComponent<Light>() : null);
            Track(ref moon, ref moonOriginal, Moon.instance != null ? Moon.instance.moonlightWorld : null);
            if (sun != null)
            {
                sun.shadowStrength = sunOriginal * (1f - Depth);
            }
            if (moon != null)
            {
                moon.shadowStrength = moonOriginal * (1f - Depth);
            }
        }

        private static void Track(ref Light tracked, ref float original, Light current)
        {
            if (current == tracked)
            {
                return;
            }

            if (tracked != null && original >= 0f)
            {
                tracked.shadowStrength = original;
            }

            tracked = current;
            original = current != null ? current.shadowStrength : -1f;   // 0.9 sun, 0.91 moon
        }

        internal static void Restore()
        {
            Depth = 0f;
            if (sun != null && sunOriginal >= 0f)
            {
                sun.shadowStrength = sunOriginal;
            }
            if (moon != null && moonOriginal >= 0f)
            {
                moon.shadowStrength = moonOriginal;
            }

            sun = null;
            moon = null;
            sunOriginal = -1f;
            moonOriginal = -1f;
        }
    }

    // MIST_HORIZON_DRAFT.md: the sky, the upper clouds (LitCloudsNoFog) and far land take no fog, so a sea line stays in the
    // mist's core whatever its density. A fog-coloured dome around each camera as it renders, drawn after the clouds and before
    // the rain, with the mist's fog curve as its alpha (0 at 7,000, 1 at the core radius). Below the horizon its alpha ramps
    // from 0 where it meets the sea up to 1 at the horizon: the user's picture (2026-10-25) showed a line at that join, the sea
    // in front of the dome a few levels off its colour.
    internal static class MistFogDome
    {
        private const float Radius = 300f;               // inside the far clip (21,500); the mist's fog hides everything there
        private const int RenderQueue = 2999;            // the clouds' queue: centred on the view, the dome sorts after them
        // the legacy particle shader applies the scene's fog while it draws, so in the core the dome is the fog colour at draw time
        // (with all the user's mods a mod changed RenderSettings.fogColor after OnPreCull: the dome 14 levels darker than the sea,
        // run 20261025-022019); Sprites/Default (no fog) only if it is missing
        private const string ShaderName = "Legacy Shaders/Particles/Alpha Blended";
        private const string FallbackShaderName = "Sprites/Default";
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private const int Segments = 64;                 // around the horizon
        // ring elevations in degrees, dense just under the horizon where the ramp lies (a deck 1-20 above the sea: 0.2-3.8°)
        private static readonly float[] Rings =
        {
            -90f, -60f, -30f, -15f, -8f, -5f, -3.5f, -2.5f, -1.8f, -1.3f, -1f, -0.75f, -0.5f, -0.35f, -0.2f, -0.1f, 0f,
            0.5f, 1f, 2f, 4f, 8f, 15f, 30f, 60f, 90f
        };

        private static GameObject dome;
        private static Mesh mesh;
        private static Material material;
        private static MeshRenderer domeRenderer;
        private static Color[] colours;
        private static float rampDegrees = -1f;           // the join's depth below the horizon the colours were made for
        private static bool hooked;
        private static bool shaderMissingLogged;
        private static bool usesTint;
        private static float seaLevel;
        private static PropertyInfo crestInstance;
        private static PropertyInfo crestSeaLevel;
        private static bool crestLooked;

        internal static float Alpha { get; private set; }

        internal static void Tick()
        {
            // the same condition and curve as the mist's fog (WeatherSkyModifiers.Apply)
            float alpha = StormInfluenceService.TryGetCurrent(out StormInfluence influence) && influence.IsMagneticMist
                ? MagneticMistRules.FogFactor(influence.CenterDistance)
                : 0f;
            if (alpha <= 0f || (domeRenderer == null && !Create()))
            {
                Alpha = 0f;
                if (domeRenderer != null)
                {
                    domeRenderer.enabled = false;
                }
                return;
            }

            Alpha = alpha;
            seaLevel = CrestSeaLevel();
            domeRenderer.enabled = true;
        }

        // Crest's sea level (Simple Tides moves it); 0 when Crest is not found
        private static float CrestSeaLevel()
        {
            if (!crestLooked)
            {
                crestLooked = true;
                Type type = AccessTools.TypeByName("Crest.OceanRenderer");
                crestInstance = type != null ? AccessTools.Property(type, "Instance") : null;
                crestSeaLevel = type != null ? AccessTools.Property(type, "SeaLevel") : null;
            }

            object instance = crestInstance != null && crestSeaLevel != null ? crestInstance.GetValue(null, null) : null;
            return instance != null ? (float)crestSeaLevel.GetValue(instance, null) : 0f;
        }

        private static bool Create()
        {
            Shutdown();
            Shader shader = Shader.Find(ShaderName) ?? Shader.Find(FallbackShaderName);
            if (shader == null)
            {
                if (!shaderMissingLogged)
                {
                    shaderMissingLogged = true;
                    BetterStormPlugin.Instance?.LogFeatureWarning(
                        "Shaders " + ShaderName + " and " + FallbackShaderName + " not found; the horizon stays visible in the Magnetic Mist.");
                }
                return false;
            }

            mesh = BuildMesh();
            colours = new Color[mesh.vertexCount];
            rampDegrees = -1f;
            dome = new GameObject("Better Storm Mist Fog Dome");
            dome.AddComponent<MeshFilter>().sharedMesh = mesh;
            material = new Material(shader) { renderQueue = RenderQueue };   // Cull Off: seen from inside
            usesTint = material.HasProperty(TintColorId);
            domeRenderer = dome.AddComponent<MeshRenderer>();
            domeRenderer.sharedMaterial = material;
            domeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            domeRenderer.receiveShadows = false;
            domeRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            domeRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            domeRenderer.enabled = false;
            dome.transform.localScale = Vector3.one * Radius;

            Camera.onPreCull += OnPreCull;
            hooked = true;
            return true;
        }

        // a unit sphere in latitude rings (Rings) and Segments around
        private static Mesh BuildMesh()
        {
            int columns = Segments + 1;
            var vertices = new Vector3[Rings.Length * columns];
            for (int ring = 0; ring < Rings.Length; ring++)
            {
                float elevation = Rings[ring] * Mathf.Deg2Rad;
                for (int i = 0; i < columns; i++)
                {
                    float around = 2f * Mathf.PI * i / Segments;
                    vertices[ring * columns + i] = new Vector3(
                        Mathf.Cos(elevation) * Mathf.Cos(around),
                        Mathf.Sin(elevation),
                        Mathf.Cos(elevation) * Mathf.Sin(around));
                }
            }

            var triangles = new int[(Rings.Length - 1) * Segments * 6];
            int t = 0;
            for (int ring = 0; ring < Rings.Length - 1; ring++)
            {
                for (int i = 0; i < Segments; i++)
                {
                    int a = ring * columns + i, b = a + 1, c = a + columns, d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
            }

            var built = new Mesh { name = "Better Storm Mist Fog Dome", vertices = vertices, triangles = triangles };
            built.RecalculateBounds();
            return built;
        }

        // vertex alpha: 0 at and under the dome's join with the sea (`ramp` degrees below the horizon), up to 1 at the horizon
        private static void SetRamp(float ramp)
        {
            int columns = Segments + 1;
            for (int ring = 0; ring < Rings.Length; ring++)
            {
                float e = Rings[ring];
                float a = e >= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((e + ramp) / ramp));
                var colour = new Color(1f, 1f, 1f, a);
                for (int i = 0; i < columns; i++)
                {
                    colours[ring * columns + i] = colour;
                }
            }
            mesh.colors = colours;
            rampDegrees = ramp;
        }

        // every camera, as it renders (a free camera too; Camera.main is never cached)
        private static void OnPreCull(Camera camera)
        {
            if (domeRenderer == null || !domeRenderer.enabled || camera == null)
            {
                return;
            }

            Vector3 view = camera.transform.position;
            dome.transform.position = view;
            // where the dome meets the sea, seen from the view: asin(height / radius) under the horizon
            float height = Mathf.Clamp(view.y - seaLevel, 0.5f, Radius * 0.5f);
            float ramp = Mathf.Asin(height / Radius) * Mathf.Rad2Deg;
            if (Mathf.Abs(ramp - rampDegrees) > rampDegrees * 0.05f)
            {
                SetRamp(ramp);
            }

            Color fog = RenderSettings.fogColor;
            if (usesTint)
            {
                // the particle shader draws 2 x _TintColor x the vertex colour, then fogs it; the tint only shows where the fog
                // is thin (the mist's outer band)
                Color tint = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? (fog.linear * 0.5f).gamma
                    : fog * 0.5f;
                tint.a = Alpha * 0.5f;
                material.SetColor(TintColorId, tint);
            }
            else
            {
                material.color = new Color(fog.r, fog.g, fog.b, Alpha);
            }
        }

        internal static void Shutdown()
        {
            Alpha = 0f;
            if (hooked)
            {
                Camera.onPreCull -= OnPreCull;
                hooked = false;
            }
            if (dome != null)
            {
                UnityEngine.Object.Destroy(dome);
            }
            if (mesh != null)
            {
                UnityEngine.Object.Destroy(mesh);
            }
            if (material != null)
            {
                UnityEngine.Object.Destroy(material);
            }
            dome = null;
            mesh = null;
            material = null;
            domeRenderer = null;
            colours = null;
            rampDegrees = -1f;
            usesTint = false;
        }
    }

    [HarmonyPatch(typeof(Stars), "Update")]
    internal static class MagneticStarsPatch
    {
        private static readonly AccessTools.FieldRef<Stars, Renderer> StarsRenderer =
            AccessTools.FieldRefAccess<Stars, Renderer>("renderer");

        [HarmonyPostfix]
        private static void Postfix(Stars __instance)
        {
            float k = 1f - MagneticSky.Depth;
            Renderer renderer = StarsRenderer(__instance);
            if (k >= 1f || renderer == null)
            {
                return;
            }

            // the game sets alpha = nightLerp³ every frame, so this never compounds
            Color color = renderer.material.color;
            color.a *= k;
            renderer.material.color = color;
        }
    }

    [HarmonyPatch(typeof(Moon), "Update")]
    internal static class MagneticMoonPatch
    {
        private static readonly AccessTools.FieldRef<Moon, Renderer> MoonRenderer =
            AccessTools.FieldRefAccess<Moon, Renderer>("renderer");

        [HarmonyPostfix]
        private static void Postfix(Moon __instance)
        {
            Renderer renderer = MoonRenderer(__instance);
            if (renderer == null)
            {
                return;
            }

            float k = 1f - MagneticSky.Depth;
            bool hide = k <= 0.001f;
            if (hide && renderer.enabled)
            {
                renderer.enabled = false;
                MagneticSky.MoonHidden = true;
            }
            else if (!hide && MagneticSky.MoonHidden)
            {
                // only undo our own change
                renderer.enabled = true;
                MagneticSky.MoonHidden = false;
            }

            // the game sets the emission colour every frame
            if (!hide && k < 1f)
            {
                renderer.material.SetColor(
                    "_EmissionColor",
                    renderer.material.GetColor("_EmissionColor") * k);
            }
        }
    }

    // The game's rainbow would otherwise show inside the mist while the sun is hidden.
    [HarmonyPatch(typeof(Rainbow), "Update")]
    internal static class MagneticRainbowPatch
    {
        private static readonly AccessTools.FieldRef<Rainbow, Renderer> Main =
            AccessTools.FieldRefAccess<Rainbow, Renderer>("mainRainbow");
        private static readonly AccessTools.FieldRef<Rainbow, Renderer> Double =
            AccessTools.FieldRefAccess<Rainbow, Renderer>("doubleRainbow");
        private static readonly AccessTools.FieldRef<Rainbow, Renderer> Glow =
            AccessTools.FieldRefAccess<Rainbow, Renderer>("rainbowGlow");

        [HarmonyPostfix]
        private static void Postfix(Rainbow __instance)
        {
            // the game sets nothing while not playing, so multiplying then would compound
            if (!GameState.playing)
            {
                return;
            }

            float k = 1f - MagneticSky.Depth;
            if (k >= 1f)
            {
                return;
            }

            Fade(Main(__instance), k);
            Fade(Double(__instance), k);
            Fade(Glow(__instance), k);
        }

        private static void Fade(Renderer renderer, float k)
        {
            if (renderer == null)
            {
                return;
            }

            Color color = renderer.material.color;   // set fresh by the game this frame
            color.a *= k;
            renderer.material.color = color;
        }
    }

    // RealisticSkies resets its alphas on each Stars.Update call; this runs after it each time.
    [HarmonyPatch(typeof(Stars), "Update")]
    internal static class RealisticSkiesFadePatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(new[] { BetterStormPlugin.RealisticSkiesGuid })]
        private static void Postfix()
        {
            RealisticSkiesCompatibility.Fade(1f - MagneticSky.Depth);
        }
    }

    // RealisticSkies (soft dependency, read by reflection): its sky fades with the vanilla stars.
    internal static class RealisticSkiesCompatibility
    {
        private static bool resolved;
        private static bool usable;
        private static FieldInfo starSkybox;       // public static GameObject fields
        private static FieldInfo eclipticSkybox;
        private static FieldInfo zodiac;
        private static FieldInfo moonHalo;
        private static FieldInfo disableAll;       // static ConfigEntry<bool> fields of its Plugin
        private static FieldInfo zodiacOn;
        private static FieldInfo moonSizeOn;
        private static FieldInfo planetsOn;

        internal static void Fade(float k)
        {
            if (k >= 1f)
            {
                return;   // outside the mist: RealisticSkies is left alone
            }

            if (!resolved)
            {
                Resolve();
            }

            if (!usable || Flag(disableAll))
            {
                return;
            }

            // Only the parts whose setting is on: RealisticSkies resets only those.
            FadeObject(Get(starSkybox), k);
            if (Flag(zodiacOn))
            {
                FadeObject(Get(zodiac), k);
            }
            if (Flag(moonSizeOn))
            {
                FadeObject(Get(moonHalo), k);   // the moon itself: MagneticMoonPatch
            }
            if (Flag(planetsOn))
            {
                GameObject ecliptic = Get(eclipticSkybox);
                if (ecliptic == null)
                {
                    return;
                }

                foreach (Transform planet in ecliptic.transform)
                {
                    FadeObject(planet.gameObject, k);
                    if (planet.childCount > 0)
                    {
                        FadeObject(planet.GetChild(0).gameObject, k);   // glare
                    }
                }
            }
        }

        // only active objects: RealisticSkies resets the alpha of every active one on each call
        private static void FadeObject(GameObject obj, float k)
        {
            if (obj == null || !obj.activeInHierarchy)
            {
                return;
            }

            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            Color color = renderer.material.color;   // the same per-renderer instance RealisticSkies uses
            color.a *= k;
            renderer.material.color = color;
        }

        private static GameObject Get(FieldInfo field)
        {
            return field.GetValue(null) as GameObject;
        }

        private static bool Flag(FieldInfo field)
        {
            ConfigEntry<bool> entry = field.GetValue(null) as ConfigEntry<bool>;
            return entry != null && entry.Value;
        }

        private static void Resolve()
        {
            resolved = true;
            if (!Chainloader.PluginInfos.ContainsKey(BetterStormPlugin.RealisticSkiesGuid))
            {
                return;
            }

            Type sky = AccessTools.TypeByName("RealisticSkies.SkyboxManager");
            Type moon = AccessTools.TypeByName("RealisticSkies.MoonPatch");
            Type plugin = AccessTools.TypeByName("RealisticSkies.Plugin");
            starSkybox = sky != null ? AccessTools.Field(sky, "starSkybox") : null;
            eclipticSkybox = sky != null ? AccessTools.Field(sky, "eclipticSkybox") : null;
            zodiac = moon != null ? AccessTools.Field(moon, "Zodiac") : null;
            moonHalo = moon != null ? AccessTools.Field(moon, "moonHalo") : null;
            disableAll = plugin != null ? AccessTools.Field(plugin, "configDisable") : null;
            zodiacOn = plugin != null ? AccessTools.Field(plugin, "configZodiacToggle") : null;
            moonSizeOn = plugin != null ? AccessTools.Field(plugin, "configMoonSizeToggle") : null;
            planetsOn = plugin != null ? AccessTools.Field(plugin, "configPlanetsToggle") : null;
            usable = IsStatic(starSkybox) && IsStatic(eclipticSkybox) && IsStatic(zodiac) &&
                IsStatic(moonHalo) && IsStatic(disableAll) && IsStatic(zodiacOn) &&
                IsStatic(moonSizeOn) && IsStatic(planetsOn);
            if (usable)
            {
                BetterStormPlugin.Instance?.LogFeatureInfo("RealisticSkies sky fades in the Magnetic Mist.");
            }
            else
            {
                BetterStormPlugin.Instance?.LogFeatureWarning(
                    "RealisticSkies layout not recognised; its sky stays visible in the Magnetic Mist.");
            }
        }

        private static bool IsStatic(FieldInfo field)
        {
            return field != null && field.IsStatic;
        }
    }

    // §4.4 and §5.6: called from WeatherEffectsPatch after the game (palette, particles) and Climate
    // have set this frame's fog and clouds, so multiplying never compounds.
    internal static class WeatherSkyModifiers
    {
        // The user, after 1.1.7: clouds already in the sky when the Doldrums clears it fade out in about
        // 10 s instead of living out their life (63-97 s measured).
        private const float CloudFadeSeconds = 10f;
        private const float CloudFadeInterval = 0.5f;

        private static bool sunDiskOff;
        private static float nextCloudFade;
        private static ParticleSystem.Particle[] cloudBuffer;

        internal static void Apply(ParticleSystem lowerClouds, ParticleSystem upperClouds)
        {
            bool inStorm = StormInfluenceService.TryGetCurrent(out StormInfluence influence);
            Region region = Weather.instance != null ? Weather.instance.currentRegion : null;
            if (inStorm && influence.IsDoldrums)
            {
                // fog and clouds cleared in every region (the user, after 1.1.7; Emerald and the Lagoon kept theirs before)
                RenderSettings.fogDensity *= DoldrumsRules.FogFactor(influence.CenterDistance);
                float clouds = DoldrumsRules.CloudFactor(influence.CenterDistance);
                Scale(lowerClouds, clouds);
                Scale(upperClouds, clouds);
                if (clouds < 1f && Time.time >= nextCloudFade)
                {
                    nextCloudFade = Time.time + CloudFadeInterval;
                    FadeLiveClouds(lowerClouds, clouds);
                    FadeLiveClouds(upperClouds, clouds);
                }
            }

            if (inStorm && influence.IsMagneticMist)
            {
                // from the fog already there at 7,000 to the peak (0.3) at the core radius
                RenderSettings.fogDensity = MagneticMistRules.MistFog(
                    RenderSettings.fogDensity,
                    MagneticMistRules.MistFogPeak,
                    MagneticMistRules.FogFactor(influence.CenterDistance));
            }

            float m = MagneticSky.Depth;
            if (m > 0f && region != null && region.clearWeather != null && region.stormWeather != null)
            {
                // a floor, so Climate's higher values are kept
                float density = MagneticMistRules.CoreCloudDensity(
                    region.clearWeather.particles.cloudDensity,
                    region.stormWeather.particles.cloudDensity,
                    m);
                AtLeast(lowerClouds, density);
                AtLeast(upperClouds, density * 2f);
            }

            ApplySunDisk(m);
        }

        private static void ApplySunDisk(float m)
        {
            Material sky = RenderSettings.skybox;   // the game re-copies this material from the palette every frame
            if (sky == null)
            {
                return;
            }

            if (m > 0f && sky.HasProperty("_SunSize"))
            {
                sky.SetFloat("_SunSize", sky.GetFloat("_SunSize") * (1f - m));
            }

            if (m >= 0.999f)
            {
                sky.DisableKeyword("_SUNDISK_HIGH_QUALITY");
                sky.DisableKeyword("_SUNDISK_SIMPLE");
                sky.EnableKeyword("_SUNDISK_NONE");
                sunDiskOff = true;
            }
            else if (sunDiskOff)
            {
                sky.DisableKeyword("_SUNDISK_NONE");
                sky.EnableKeyword("_SUNDISK_HIGH_QUALITY");
                sunDiskOff = false;
            }
        }

        private static void Scale(ParticleSystem particles, float k)
        {
            if (particles == null)
            {
                return;
            }

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = emission.rateOverTime.constant * k;
        }

        // Each live cloud's remaining life capped at Lerp(10 s, the longest life, the cloud factor); its
        // start life shortened in step, so it keeps its place on the colour and size curves and fades
        // as it would, only sooner. Twice a second, at most 2,000 clouds a system.
        private static void FadeLiveClouds(ParticleSystem particles, float factor)
        {
            if (particles == null || particles.particleCount == 0)
            {
                return;
            }

            ParticleSystem.MainModule main = particles.main;
            float cap = Mathf.Lerp(
                CloudFadeSeconds,
                Mathf.Max(CloudFadeSeconds, main.startLifetime.constantMax),
                factor);
            if (cloudBuffer == null || cloudBuffer.Length < main.maxParticles)
            {
                cloudBuffer = new ParticleSystem.Particle[main.maxParticles];
            }

            int count = particles.GetParticles(cloudBuffer);
            bool changed = false;
            for (int i = 0; i < count; i++)
            {
                float remaining = cloudBuffer[i].remainingLifetime;
                if (remaining > cap)
                {
                    cloudBuffer[i].startLifetime *= cap / remaining;
                    cloudBuffer[i].remainingLifetime = cap;
                    changed = true;
                }
            }

            if (changed)
            {
                particles.SetParticles(cloudBuffer, count);
            }
        }

        private static void AtLeast(ParticleSystem particles, float rate)
        {
            if (particles == null)
            {
                return;
            }

            ParticleSystem.EmissionModule emission = particles.emission;
            if (emission.rateOverTime.constant < rate)
            {
                emission.rateOverTime = rate;
            }
        }
    }
}
