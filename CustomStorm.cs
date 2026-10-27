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
                spawnRadius: 1500f,   // the game's (no eye since 1.2.1's in-game test, EYE_LAYER_DRAFT.md §5.9)
                schedulingDistance: StormEyeShape.GameStormCloudDistance,   // out to their outer edge
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
                Radius = 8000f,             // with its eye of 2,400 (StormEyeShape; the user: "make it 8000 radius")
                ParticleDistance = 20000f,  // its storm cloud shows out to its outer edge (Emerald: 20,000)
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
                    spawnRadius: 4250f,   // a ring from the wall's inner side (StormEyeShape.StrikeOuter)
                    schedulingDistance: 20000f,
                    audioSourceMaxDistance: 20000f,
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
                ParticleDistance = MagneticMistRules.CloudDistance,
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
        private ParticleSystem mistCloud;   // the Magnetic Mist's outer cloud ring

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

            if (definition.Kind == StormKind.GentleSnow ||
                definition.Kind == StormKind.Doldrums)
            {
                HideInheritedStormClouds();
            }

            // the mist: its storm cloud drawn without fog, wrapping the aurora on its core circle
            // (the user, 2026-10-25; the cloud was hidden in the last 1.2.0)
            if (definition.Kind == StormKind.MagneticMist)
            {
                mistCloud = MistAurora.BuildCloud(this, bottomParticles, out Material outerCloud, out Material innerCloud);
                MistAurora.Build(this, outerCloud, innerCloud);
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

            if (definition.Kind == StormKind.Hurricane)
            {
                StormEye.Build(Storm, StormEyeShape.Hurricane);
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
                ShowsCloud && distance <= Definition.ParticleDistance);
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
                    ShowsCloud &&
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
                Definition.Kind == StormKind.Doldrums)
            {
                enabled = false;
            }

            SetEmission(topParticles, enabled);
            SetEmission(bottomParticles, enabled);
            if (mistCloud != null)
            {
                SetEmission(mistCloud, enabled);
            }
        }

        // the mist's cloud shows whenever it is enabled, like its aurora, also while it is off near land
        private bool ShowsCloud
        {
            get { return Storm.active || Definition.Kind == StormKind.MagneticMist; }
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
                    StormEye.StripFromClone(clone);

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

                // the game's storms get their storm cloud only now, so the clones above are made from a template without one
                for (int i = 0; i < vanillaStorms.Length; i++)
                {
                    if (vanillaStorms[i] != null && vanillaStorms[i].GetComponent<ModStormController>() == null)
                    {
                        StormEye.BuildForGameStorm(vanillaStorms[i]);
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

            if (originalStorms != null)
            {
                for (int i = 0; i < originalStorms.Length; i++)
                {
                    StormEye.RemoveFromGameStorm(originalStorms[i]);
                }
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
                    out StormInfluence influence))
            {
                return true;
            }

            if (influence.EyeClearness > 0f)
            {
                return !StormEyeWeather.Apply(influence);
            }

            if (!(influence.IsGentleSnow || influence.IsDoldrums))
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
        internal static readonly AccessTools.FieldRef<Wind, Vector3> WindTarget =
            AccessTools.FieldRefAccess<Wind, Vector3>("currentWindTarget");
        // private, no arguments; sets only currentGustTarget, never the gust timer
        internal static readonly MethodInfo SetNewGustTarget =
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

    // The Magnetic Mist's aurora (the user, 2026-10-25): curtains closing a ring on its core circle (3,500), the capture
    // line, seen out to the far clip by day and night and also while the mist is off near land. Drawn without fog: a glow
    // layer (Particles/Priority Additive) by night, a colour layer (Sprites/Default) by day, crossfaded by the game's night
    // lerp. Three more curtains stand across the ring's middle, each an S seen from above. Its storm cloud wraps the
    // ring (the user: "the storm for mist should be bigger than aurora, so you can see aurora going in and out of the storm"): a
    // disc inside the ring and a ring outside it. Transparent things hide each other only
    // by their draw order, so seen from outside the core: the far curtains (2997), the inner cloud (2998), the fog dome
    // (2999), the near curtains (3000), the outer cloud (3000, sorted after them), the rain (3001); from inside the core both
    // clouds go under the dome (2998) and the aurora is hidden. Look checked in game: BetterStorm_TEST_RESULTS.md, the
    // -bsVisualProbe runs of 2026-10-25.
    internal sealed class MistAurora : MonoBehaviour
    {
        private const int Curtains = 12, Columns = 56, Rows = 5;
        // the aurora's light on the cloud (Skirt) under every SkirtStep-th column and the last: on every column it took a rebuild
        // from 521 to 632 us, 216 us a frame at 60 fps (-bsAurora, run 20261026-195501; the limit 200)
        private const int SkirtRows = 3, SkirtStep = 4;
        private const int SkirtColumns = (Columns - 2) / SkirtStep + 2;
        private const int NearQueue = 3000, FarQueue = 2997, UnderDomeQueue = 2998;
        private const float OuterCloudFudge = -2000f;   // sorts the outer cloud after the aurora (both sort from the storm's centre)
        private const float FoldLimit = 80f;          // the folds stay within this of the core circle
        private const float FadeSeconds = 5f;
        private const float CoreFadeWidth = 400f;     // faded inside the core (CoreClear), full from this far outside it
        // the curtains rebuilt 20 times a second, and not while off screen: a rebuild costs about 0.4 ms (-bsAurora,
        // run 20261025-132844) and the folds move slowly
        private const float RefreshSeconds = 0.05f;
        // the ragged edges' noise drifts slowly (0.03-0.1 a second): worked out 4 times a second, not at every rebuild (it took a
        // rebuild from 499 to 654 us, -bsAurora run 20261025-151707)
        private const float EdgeRefreshSeconds = 0.25f;
        // The day look (the user, 2026-10-27: "is there anyway to make aurora in daytime looks better?"; of the three ways offered,
        // "A, B, C", then from the pictures "A+B+C (Recommended)"; STORM_UPDATE_SPEC.md §3). The colour layer in a deep tint (0.6,
        // 0.75, 0.8) at full strength blended colours darker than the sky over it, so by day the aurora darkened the sky and never
        // lit it (12 km out at noon: 47 % of its area over 10 darker than the sky beside it, none over 6 brighter; BetterStorm_FAILED.md).
        // A: the colour layer lights the sky: its colour x DayGain at DayLightStrength (Sprites/Default draws colour x alpha over the
        //    sky x (1 - alpha), so over 1 it adds more light than it covers: the green over the noon sky, the purple about its
        //    brightness)
        private const float DayGain = 2f;
        private const float DayLightStrength = 0.6f;
        // B: softer by day: the colour layer's rays blurred along the curtain (a bell DayBlur texels wide, of the tile's 512: about
        //    70 m), and their reach with them, so no lone ray stands up purple over its neighbours
        private const float DayBlur = 6f;
        // C: brightest at dawn and dusk, paler by day: the day look x DayNoonShare by day, rising to x 1 with the game's dawn light
        //    (Sun.GetDawnLerp: 1 at the dawn border, 0 from the day border on, the same mornings and evenings the game's sky takes)
        private const float DayNoonShare = 0.55f;
        // Seen inside the core, faded by its fog (the user, 2026-10-27: "btw next build let aurora visible even in side the core
        // radius, i want to test how it would looks like", then on the trial's pictures "Keep, faded by fog"; hidden there from
        // 2026-10-25): each column x CoreClear ^ (its distance from the view / CoreRadius), the light left after its way through a fog
        // that lets CoreClear of it through over the core's radius (the ring's curtains from the centre), so near curtains show more
        // than far ones; full again over the first CoreFadeWidth outside the core. Drawn whole over the fog dome, the trial read as a
        // solid bright wall at noon (run 20261027-175658). In the core the aurora also takes its night look at every hour (the user,
        // 2026-10-27, on the fade's pictures: "i think with fog the aurora in day still too bright, not in line with night time aurora
        // at all, i want when in core, day time and night time aurora looks same"; by day the blended layer, x DayGain to light the
        // sky, stood bright over the dark fog, run 20261027-185701): the day layer off, the night layer at its full glow and the light
        // on the cloud under the curtains at its night strength (SkirtNight: by day x SkirtDay, 15 times as much, drew broad bright
        // sheets over the core's fog at noon, gone with the night layer's light off, not with the fog thinned, run 20261027-192105),
        // back to the hour's look over the same CoreFadeWidth. CoreClear the user's pick of 1, 0.5, 0.3 and 0.15 (2026-10-27, run
        // 20261027-192430: "0.3 (Recommended)")
        private const float CoreClear = 0.3f;
        private static readonly float[] RowHeight = { 0f, 0.06f, 0.3f, 0.65f, 1f };
        // a real curtain: a bright green lower part (oxygen, about 100 km up), purple and red-violet above it, at the lower border
        // as it fades in a pink fringe (the "type B" border of an active display); the upper part fainter than the green
        // (the purple row 0.6 until run 20261026-183757: by day the purple not seen)
        private static readonly float[] RowAlpha = { 0f, 1f, 0.85f, 0.8f, 0f };
        private static readonly Color[] RowColor =
        {
            new Color(0.85f, 0.35f, 0.75f), new Color(0.3f, 1f, 0.45f), new Color(0.35f, 0.95f, 0.5f),
            new Color(0.6f, 0.35f, 0.9f), new Color(0.8f, 0.25f, 0.8f)
        };
        // the rays (the user, 2026-10-26: "please check real life example how it looks like, currently it doesn't looks like aurora"):
        // a real curtain is a rayed band, thin bright vertical rays standing on its lower edge, each fading upward to its own height
        // (WMO International Cloud Atlas, polar aurora forms: "Rays", "vertical shafts of light ... like a searchlight beam
        // stretching upwards"; "Rayed band": long rays that kink and fold like a waving curtain). One white tile of rays along the
        // curtain (texture u) and up it (v, the rows' RowV), drifting slowly; the colour is the vertices'
        private const int RayColumns = 512, RayRows = 64, RayCount = 120;
        // over the rays, PlainRows of an even glow (the skirt's: no rays in it); the curtains take the texture's v up to CurtainV
        private const int PlainRows = 8;
        private const float CurtainV = (RayRows - 0.5f) / (RayRows + PlainRows);
        private const float RayTile = 6000f;        // metres of curtain a tile spans: rays 25-120 wide, about 3-14 px from 10 km
        private const float RayDrift = 0.003f;      // tiles a second along the ring (18 m a second)
        private static readonly float[] RowV = { 0f, 0.1f, 0.4f, 0.7f, 1f };
        // each ray (and the band between them) fades upward to 0.45 over its own reach (exp(-RayFade)): at 1.1 (a third) the purple
        // row kept under a third of the green's (run 20261026-183757)
        private const float RayFade = 0.8f;
        // by night the glow layer's brightness x this (it adds its light: the rays' texture, a mean of about 0.47, left it faint
        // against the full-brightness curtain before the rays, run 20261026-183757)
        private const float NightGain = 1.8f;
        // by day the purple (the upper rows) was lost against the bright sky (run 20261026-183757; the user: "while in day it should
        // still show the purple tint"): the day layer's texture keeps more of its alpha there (x 1 + DayUpperLift) and takes most of
        // the green out of it (x DayUpperTint), from the green rows (RowV[2]) to the purple one (RowV[3])
        private const float DayUpperLift = 0.8f;
        private static readonly Color DayUpperTint = new Color(1f, 0.55f, 1f);
        // the mist's storm cloud (the user, 2026-10-26: "change the storm cloud color of magnetic mist to gray with a little bit green,
        // like aurora reflect it's green on the storm cloud"; then, on a grey-green tint over the whole cloud: "i still want it to be
        // mainly vanilla color, but have some green, showing reflecting from aurora, currently you just make it all green"): grey, the
        // fog colour x CloudTone, so it is one tone with the hour (as the eye's wall, BetterStorm_FAILED.md "A straight, dark eye
        // wall"; swept at noon from 6 km, run 20261026-183757: 0.6 dark grey, 0.8 light grey, 1 near white), green only where the
        // aurora's light falls on it (Skirt). A tint can't vary over the cloud: its fog-free shader draws the material's emission
        // whatever each puff's colour (BetterStorm_FAILED.md). The game's own colour (the storm material's emission 161E22, as 1.2.2
        // drew it) read charcoal by day; the user chose the grey (2026-10-26: "i think gray looks better than vanilla one, we should
        // change to gray"; run 20261026-195215). At 0.8 it was the Hurricane's white from outside (StormEye.WhiteTone, the same fog
        // colour x 0.8: at noon the mist's puffs 152 against the Hurricane's deck 156, run 20261026-200212); the user asked for
        // a grey darker than the Hurricane's storm cloud (2026-10-26: "is this grey darker shade then cloud? from your pic it
        // doesn't look like it", "i mean darker shade than storm cloud from hurricane") and picked 0.5 of 0.8 / 0.65 / 0.5 (run
        // 20261026-204937, p16p-mist-grey-night.png: the lower puffs 125 against 146 at 0.8 and the Hurricane's 199 from outside;
        // the sun lights the puffs too, so the tone moves them less than its own change)
        private const float CloudTone = 0.5f;
        // the aurora's light on the cloud: under each column a soft band in the curtain's green, from SkirtY[0] over the sea up to
        // SkirtY[2], strongest (x Skirt x the column's strength) at SkirtY[1], in the cloud's upper part (its puffs' centres 200-1,100,
        // 400-1,000 across); drawn with the curtains (added by night, blended by day), so not seen from inside the core either
        // (the user's pick of 0, 0.15 and 0.3, 2026-10-26: "0.15"; p16o-mist-light.png, run 20261026-200212)
        private const float Skirt = 0.15f;
        // by day x this: the day layer blends over the light grey cloud, and at 0.15-0.3 its green didn't show (run 20261026-195215)
        private const float SkirtDay = 3f;
        // by night x this: the cloud is near black then (the fog colour, 0B0D13, x CloudTone), so at x 1 the light alone drew it, a
        // green cloud (the user, 2026-10-26: "also in night it looks like whole green mist again, i don't want that"); the user
        // picked x 0.3 (pictured at 1 / 0.4 / 0.2, and 0.4 on the cloud's top only, run 20261026-204937), then after testing 1.2.1 in
        // game (2026-10-26): "Reduce the night green color of storm cloud to x0.2"
        private const float SkirtNight = 0.2f;
        private static readonly float[] SkirtY = { 300f, 1000f, 1500f };
        // The arcs (the user, after testing 1.2.1 in game, 2026-10-26: "Show me a aurora without tapering, i think it will looks more
        // like real life aurora, if you have other suggestion to make it looks more like real life one, tell me. like maybe make it
        // have multiple section instead of only 1 long aurora"; on probe 17c's pictures: "Build arc."): the ring broken into four arcs
        // of different lengths, radii and heights by leaving three curtains out, each arc brightening and fading on its own over
        // minutes, as a real display's separate arcs (WMO International Cloud Atlas: arcs and bands, often several at once, each
        // changing on its own). No taper toward the ring's sides seen from outside (BetterStorm_FAILED.md).
        // by curtain: arcs of 3, 2, 3 and 1 curtains (sections 0-3), and between them the three curtains the arcs leave out, which
        // stand in the middle (sections 4-6)
        private static readonly int[] SectionByCurtain = { 0, 0, 0, 4, 1, 1, 5, 2, 2, 2, 6, 3 };
        private const int RingArcs = 4;                                               // the sections on the ring; the rest in the middle
        private static readonly float[] SectionOffset = { 0f, 160f, -120f, 200f };   // the arcs off the core circle, so they read as separate arcs
        private const float SectionReach = 200f;                                     // the most of them
        private static readonly float[] SectionHeight = { 1f, 0.8f, 1.1f, 0.7f, 0.9f, 0.75f, 0.8f };
        // The middle S-curtains (the user, 2026-10-26, on probe 17f's pictures: "for aurora,can you try adding multiple S shape aurora?
        // should add in the middle also, so it don't looks like it have empty middle." / "The veil should also be in the middle"; on
        // probe 17g's: "middle s curtain shouldn't shape like a circle, make it wind across the middle area"): three bands winding
        // across the middle from side to side, each MiddleWaves waves of a sine along a straight line, (u - 0.5) x MiddleLength along
        // it and MiddleBend x sin(2 pi MiddleWaves u) across, about MiddleCentre (x, z), the line turned MiddleTurn degrees from +z
        // toward +x; with the arcs' folds, curls, pulses, skirt and veil, each brightening and fading on its own. Seen from above 432
        // apart at the nearest, their bases within 2,820 of the centre. Set round the centre 1,200 out they read as a circle (run
        // 20261026-234449); the plan's first set crossed. Their columns are 83-104 apart against the arcs' 45, so their curls are as
        // much wider (curlWidth: on the arcs' 520-long wave a curl had 5 columns). Drawn with the far curtains, under the inner cloud
        // whatever the view (they stand in its middle). Kept for good, always drawn (the user, 2026-10-27: "Keep the curls, pulses,
        // veil and middle bands as they are; make them fixed and update the README.").
        private static readonly Vector2[] MiddleCentre = { new Vector2(0f, 0f), new Vector2(-700f, 1250f), new Vector2(750f, -1250f) };
        private static readonly float[] MiddleTurn = { 60f, 75f, 48f };
        private static readonly float[] MiddleLength = { 5000f, 4200f, 4200f };
        private static readonly float[] MiddleBend = { 420f, 400f, 420f };
        private static readonly float[] MiddleWaves = { 1.5f, 1f, 1f };
        // Claude's three ideas for a more real aurora, all on (the user, 2026-10-26: "also i want to try all these idea also, probally
        // all together"), kept for good with the middle (2026-10-27, above).
        // Curls: tighter S-shaped folds in places. Where a slow noise along the ring is high (CurlFrom-CurlFull), each column also
        // swings across the curtain and along it on a short wave (a curl, CurlReach x 2 pi / CurlWave over 1: the curtain folds back
        // on itself, an S seen from below), travelling along the curtain
        private const float CurlReach = 110f;
        private const float CurlWave = 520f;
        private const float CurlSpeed = 0.35f;         // radians a second
        private const float CurlFrom = 0.55f, CurlFull = 0.7f;
        // Pulses: now and then a patch that pulses. Where another slow noise is high (PulseFrom-PulseFull; it drifts over about 20 s),
        // the column's strength swings by up to PulseDepth on a period of PulseMin-PulseMax s, each curtain its own: a real pulsating
        // patch's seconds against the arcs' minutes, nearly out and back (at 0.7 the brightest patch only dimmed from 86 to 68, run
        // 20261026-225512)
        private const float PulseDepth = 1f;
        private const float PulseMin = 2.5f, PulseMax = 4.5f;
        private const float PulseFrom = 0.62f, PulseFull = 0.72f;
        // Veil: a faint wide glow on top of each curtain, red-violet as a real display's: the red oxygen glow lies on top of the green
        // and reaches above it, faint at the top where the air is thin (NASA's ISS aurora photographs; Windows to the Universe). Under
        // every SkirtStep-th column a smooth band (the texture's plain rows) on its arc's own smooth line, (3,000 + VeilWave x a sine
        // along the ring) x the arc's height, shared by the arc's curtains: from VeilAt[0] to VeilAt[2] x that line over the base,
        // strongest (VeilAlpha x the arc's strength) at VeilAt[1], on the curtains' upper part, so it glows out of them; its span
        // x 0.7 of the 0.5-1.4 (brightest 0.85) it had at first (the user, 2026-10-27: "i mean make the veil smaller", "i still want
        // the veil connect like how you make last turn btw", "Keep the veil at x 0.7."; "A, also make the curtain smaller to try out"
        // first read as the curtains', BetterStorm_FAILED.md); along the
        // curtain it fades as ends^0.75, wider than the curtain's ends^1.5 (and even where two curtains overlap); it neither flickers
        // nor pulses, and it leans on with the curtain (VeilSlope a metre up: the rays are straight). Each curtain's own veil over its
        // smooth top, on its own height wave, stood as a V over the tallest arc where two of its curtains met, floating over their
        // bright part (the user, 2026-10-27: "there is still that V shape high veil looks weird, or just increase the height of the
        // aurora beside it?"; then "A", this way, BetterStorm_FAILED.md); sized by the curtain's full height it reached 7,200 ("there
        // is 1 veil that is super high compare to others and it looks weirdly shape"); one band all round at one height looked weird
        // ("previous one looks better, only the tall high one is outlier"). At 0.22 a solid pink wall over the arcs by night (the glow
        // adds x 1.8)
        private const int VeilRows = 3;
        private const float VeilAlpha = 0.06f;
        private const float VeilWave = 400f;
        private const float VeilSlope = 120f / 3000f;                     // the curtains' lean: 120 at the top of a 3,000 curtain
        private static readonly Color VeilColour = new Color(0.7f, 0.2f, 0.5f);
        private static readonly float[] VeilAt = { 0.5f, 0.745f, 1.13f };
        private static readonly float[] VeilShape = { 0f, 1f, 0f };
        private static readonly float[] SkirtAlpha = { 0f, 1f, 0f };
        private static readonly int EmisColorId = Shader.PropertyToID("_EmisColor");

        private Mesh mesh;
        private Material night, day, nightFar, dayFar;
        private Material outerCloud, innerCloud;
        private Texture2D rays, dayRays, daySoftRays;
        private Vector3[] vertices;
        private Color32[] colors;
        private float lastRefresh = -1f;
        private float[] phase;
        private float shown;
        private float coreOpen = 1f;   // 0 inside the core, 1 from CoreFadeWidth outside it (Update)
        private float nightLook;       // 0 the day look, 1 the night look: the night's share, 1 in the core at every hour (Update)
        private MeshRenderer meshRenderer;
        private List<int>[] curtainTriangles;
        private float[] edgeLift, edgeFade, edgeDrop;   // per column: the start over the base, the fade-in, the top's share lowered
        private float[] curlAt, pulseAt;                // per column: its share of a curl and of a pulse
        // per column, fixed (BuildPaths): its point on the curtain's path, the way across it (the lean, the folds, a curl's swing) and
        // along it (a curl's swing), its metres along the curtain, and where the noise and the rays' texture are read along it
        private float[] pathX, pathZ, acrossX, acrossZ, alongX, alongZ, pathS, noiseAt;
        private float[] curlWidth;                      // per curtain: its curls' width and wave x this (1 on the arcs)
        private float lastEdges = -1f;
        private readonly List<int> nearTriangles = new List<int>(), farTriangles = new List<int>();
        private int farMask = -1;

        // The mist's own storm cloud layer becomes the disc inside the ring and a clone of it the ring outside, both drawn with
        // the game's fog-free cloud shader (LitCloudsNoFog). Returns the clone (switched with the storm's own layer), or null.
        internal static ParticleSystem BuildCloud(ModStormController storm, ParticleSystem layer, out Material outerMaterial, out Material innerMaterial)
        {
            outerMaterial = null;
            innerMaterial = null;
            ParticleSystemRenderer renderer = layer != null ? layer.GetComponent<ParticleSystemRenderer>() : null;
            Shader noFog = Shader.Find("LitCloudsNoFog");
            if (renderer == null || renderer.sharedMaterial == null || noFog == null)
            {
                BetterStormPlugin.Instance?.LogFeatureInfo("Magnetic Mist: its storm cloud keeps the fogged shader (no fog-free one found).");
                return null;
            }

            GameObject go = Instantiate(layer.gameObject, layer.transform.parent);
            go.name = "Better Storm Magnetic Mist outer cloud";
            ParticleSystem ring = go.GetComponent<ParticleSystem>();
            Material inner = new Material(renderer.sharedMaterial) { name = "Better Storm mist cloud (no fog)", shader = noFog, renderQueue = UnderDomeQueue };
            renderer.sharedMaterial = inner;
            innerMaterial = inner;
            layer.gameObject.AddComponent<OwnedMaterial>().Material = inner;
            outerMaterial = new Material(inner) { name = "Better Storm mist outer cloud (no fog)", renderQueue = NearQueue };
            ParticleSystemRenderer ringRenderer = go.GetComponent<ParticleSystemRenderer>();
            ringRenderer.sharedMaterial = outerMaterial;
            ringRenderer.sortingFudge = OuterCloudFudge;
            go.AddComponent<OwnedMaterial>().Material = outerMaterial;

            ShapeCloud(layer, storm.transform, 0f, MagneticMistRules.CloudInnerRadius, MagneticMistRules.CloudInnerPuffs);
            ShapeCloud(ring, storm.transform, MagneticMistRules.CloudRingInner, MagneticMistRules.CloudRingOuter, MagneticMistRules.CloudRingPuffs);
            ParticleSystem.EmissionModule emission = ring.emission;
            emission.enabled = false;   // switched with the storm's own layer (SetParticleEmission)
            return ring;
        }

        // A flat volume (a disc, or a ring from `inner`) from CloudBottom to CloudBottom + CloudDepth over the sea.
        private static void ShapeCloud(ParticleSystem ps, Transform storm, float inner, float outer, int puffs)
        {
            ps.transform.localPosition = new Vector3(0f, MagneticMistRules.CloudBottom - storm.position.y, 0f);
            ps.transform.localRotation = Quaternion.identity;
            ParticleSystem.MainModule main = ps.main;
            main.maxParticles = puffs;
            main.startSize = new ParticleSystem.MinMaxCurve(MagneticMistRules.CloudPuffMin, MagneticMistRules.CloudPuffMax);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.ConeVolume;
            shape.angle = 0f;
            shape.radius = outer;
            shape.radiusThickness = StormEyeShape.RingThickness(inner, outer);
            shape.length = MagneticMistRules.CloudDepth;
            shape.arc = 360f;
            shape.rotation = new Vector3(-90f, 0f, 0f);   // up from the bottom
            shape.position = Vector3.zero;
            shape.scale = Vector3.one;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = puffs / Mathf.Max(1f, main.startLifetime.constantMax);
        }

        internal static void Build(ModStormController storm, Material outerCloud, Material innerCloud)
        {
            Shader glow = Shader.Find("Particles/Priority Additive") ?? Shader.Find("Particles/Priority Additive (Soft)");
            Shader colour = Shader.Find("Sprites/Default");
            if (colour == null)
            {
                BetterStormPlugin.Instance?.LogFeatureInfo("Magnetic Mist: no aurora (Sprites/Default not found).");
                return;
            }

            GameObject go = new GameObject("Better Storm Magnetic Mist aurora");
            go.transform.SetParent(storm.transform, false);
            MistAurora aurora = go.AddComponent<MistAurora>();
            aurora.rays = BuildRays(false);
            // the day layer's rays blurred (the day look's B); the sharp copy only for a night layer without the glow shader, which
            // blends too: the day's texture, with its brightness in the alpha
            aurora.daySoftRays = BuildRays(true, DayBlur);
            aurora.dayRays = glow == null ? BuildRays(true) : null;
            aurora.night = new Material(glow ?? colour) { renderQueue = NearQueue, mainTexture = glow != null ? aurora.rays : aurora.dayRays };
            aurora.day = new Material(colour) { renderQueue = NearQueue, mainTexture = aurora.daySoftRays };
            aurora.nightFar = new Material(aurora.night) { renderQueue = FarQueue };
            aurora.dayFar = new Material(aurora.day) { renderQueue = FarQueue };
            aurora.outerCloud = outerCloud;
            aurora.innerCloud = innerCloud;
            aurora.BuildMesh();
            go.AddComponent<MeshFilter>().sharedMesh = aurora.mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { aurora.night, aurora.day, aurora.nightFar, aurora.dayFar };
            aurora.meshRenderer = renderer;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // One tile of rays: a glowing band (0.6) with RayCount rays at random places along it, 1-4 texels wide (a bell across), each
        // adding a random 0.15-0.5 and fading upward over its own share of the height (the band's 0.55). A curtain of isolated rays over
        // a faint glow (0.2, fading by 0.15) read as thin faint streaks, not a curtain (run 20261026-183242). The night layer's copy
        // grey and opaque (it adds: with the brightness in its alpha too it was squared, about a quarter of the curtain's light, run
        // -183757), the day layer's white with the brightness in its alpha (it blends: a grey copy darkened its colours), its upper
        // rows purpler and kept stronger (DayUpperTint, DayUpperLift); its soft copy (the day look's B) with the rays and their reach blurred
        // along the curtain by a bell `blur` texels wide.
        private static Texture2D BuildRays(bool forDay, float blur = 0f)
        {
            System.Random random = new System.Random(11);
            float[] bright = new float[RayColumns], reach = new float[RayColumns];
            for (int x = 0; x < RayColumns; x++)
            {
                bright[x] = 0.6f;
                reach[x] = 0.55f;
            }

            for (int r = 0; r < RayCount; r++)
            {
                float centre = (float)random.NextDouble() * RayColumns, width = 1f + 3f * (float)random.NextDouble();
                float strength = 0.15f + 0.35f * (float)random.NextDouble(), height = 0.55f + 0.45f * (float)random.NextDouble();
                int spread = Mathf.CeilToInt(width * 3f);
                for (int dx = -spread; dx <= spread; dx++)
                {
                    int x = ((Mathf.FloorToInt(centre) + dx) % RayColumns + RayColumns) % RayColumns;   // the tile repeats
                    float bell = Mathf.Exp(-0.5f * (dx / width) * (dx / width));
                    bright[x] += strength * bell;
                    reach[x] = Mathf.Max(reach[x], height * bell);
                }
            }

            if (blur > 0f)
            {
                bright = Blurred(bright, blur);
                reach = Blurred(reach, blur);
            }

            float most = 0f;
            for (int x = 0; x < RayColumns; x++)
            {
                most = Mathf.Max(most, bright[x]);
            }

            Color32[] pixels = new Color32[RayColumns * (RayRows + PlainRows)];
            for (int y = 0; y < RayRows; y++)
            {
                float v = y / (float)(RayRows - 1);
                float upper = forDay ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RowV[2], RowV[3], v)) : 0f;
                Color32 tint = Color.Lerp(Color.white, DayUpperTint, upper);
                for (int x = 0; x < RayColumns; x++)
                {
                    // full over the lower border, then down to 0.45 over the ray's own reach and on toward nothing
                    float fade = v <= RowV[1] ? 1f : Mathf.Exp(-RayFade * (v - RowV[1]) / Mathf.Max(0.1f, reach[x]));
                    byte level = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(bright[x] / most * fade * (1f + DayUpperLift * upper)));
                    pixels[y * RayColumns + x] = forDay ? new Color32(tint.r, tint.g, tint.b, level) : new Color32(level, level, level, 255);
                }
            }

            for (int y = RayRows; y < RayRows + PlainRows; y++)
            {
                for (int x = 0; x < RayColumns; x++)
                {
                    pixels[y * RayColumns + x] = new Color32(255, 255, 255, 255);
                }
            }

            Texture2D texture = new Texture2D(RayColumns, RayRows + PlainRows, TextureFormat.RGBA32, true)
            {
                name = forDay ? (blur > 0f ? "Better Storm aurora rays (day, soft)" : "Better Storm aurora rays (day)") : "Better Storm aurora rays",
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        // `values` (one per texel along the tile, which repeats) under a bell `width` texels wide (its standard deviation)
        private static float[] Blurred(float[] values, float width)
        {
            int n = values.Length, spread = Mathf.CeilToInt(width * 3f);
            float[] blurred = new float[n];
            for (int x = 0; x < n; x++)
            {
                float sum = 0f, weights = 0f;
                for (int dx = -spread; dx <= spread; dx++)
                {
                    float w = Mathf.Exp(-0.5f * (dx / width) * (dx / width));
                    sum += w * values[((x + dx) % n + n) % n];
                    weights += w;
                }

                blurred[x] = sum / weights;
            }

            return blurred;
        }

        private void BuildMesh()
        {
            int count = Curtains * (Columns * Rows + SkirtColumns * (SkirtRows + VeilRows));
            vertices = new Vector3[count];
            colors = new Color32[count];
            phase = new float[Curtains];
            edgeLift = new float[Curtains * Columns];
            edgeFade = new float[Curtains * Columns];
            edgeDrop = new float[Curtains * Columns];
            curlAt = new float[Curtains * Columns];
            pulseAt = new float[Curtains * Columns];
            System.Random random = new System.Random(7);
            for (int k = 0; k < Curtains; k++)
            {
                phase[k] = (float)random.NextDouble() * 100f;
            }

            curtainTriangles = new List<int>[Curtains];
            for (int k = 0; k < Curtains; k++)
            {
                List<int> triangles = curtainTriangles[k] = new List<int>();
                for (int c = 0; c < Columns - 1; c++)
                {
                    for (int r = 0; r < Rows - 1; r++)
                    {
                        int i = (k * Columns + c) * Rows + r, j = i + Rows;
                        triangles.Add(i); triangles.Add(j); triangles.Add(i + 1);
                        triangles.Add(i + 1); triangles.Add(j); triangles.Add(j + 1);
                    }
                }

                for (int n = 0; n < SkirtColumns - 1; n++)
                {
                    for (int s = 0; s < SkirtRows - 1; s++)
                    {
                        int i = SkirtIndex(k, n, s), j = i + SkirtRows;
                        triangles.Add(i); triangles.Add(j); triangles.Add(i + 1);
                        triangles.Add(i + 1); triangles.Add(j); triangles.Add(j + 1);
                    }

                    for (int g = 0; g < VeilRows - 1; g++)
                    {
                        int i = VeilIndex(k, n, g), j = i + VeilRows;
                        triangles.Add(i); triangles.Add(j); triangles.Add(i + 1);
                        triangles.Add(i + 1); triangles.Add(j); triangles.Add(j + 1);
                    }
                }
            }

            mesh = new Mesh { name = "Better Storm aurora" };
            mesh.MarkDynamic();
            BuildPaths();
            Animate(0f, Vector3.zero);
            // the rays' texture coordinates: along the ring or a middle curtain (each curtain a little offset, so the overlapping ones
            // don't repeat each other's rays) and up each column's rows; they stay as the curtains move
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int k = 0; k < Curtains; k++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    float along = noiseAt[k * Columns + c];
                    for (int r = 0; r < Rows; r++)
                    {
                        uvs[(k * Columns + c) * Rows + r] = new Vector2(along / RayTile + k * 0.37f, RowV[r] * CurtainV);
                    }

                    int slot = SkirtSlot(c);
                    for (int s = 0; slot >= 0 && s < SkirtRows; s++)
                    {
                        uvs[SkirtIndex(k, slot, s)] = new Vector2(along / RayTile + k * 0.37f, 1f);   // the plain rows
                    }

                    for (int g = 0; slot >= 0 && g < VeilRows; g++)
                    {
                        uvs[VeilIndex(k, slot, g)] = new Vector2(along / RayTile + k * 0.37f, 1f);
                    }
                }
            }

            mesh.uv = uvs;
            mesh.subMeshCount = 4;   // night and day near, night and day far
            SplitCurtains(0);
            // out to the arcs' reach, the folds, a curl and the veil's lean; up to the tallest arc's top (290 + 3,750 x 1.1: 4,415) and
            // its veil's (290 + 3,400 x 1.1 x 1.13: 4,516), with room
            float r0 = MagneticMistRules.CoreRadius + SectionReach + FoldLimit + CurlReach + VeilSlope * 6300f + 100f;
            mesh.bounds = new Bounds(new Vector3(0f, 3350f, 0f), new Vector3(r0 * 2f, 6700f, r0 * 2f));
        }

        // Each column's fixed place on its curtain's path. An arc's on the core circle (its section's offset off it): across the way
        // out from the centre, along the way round, the noise and the rays read by the angle round the ring (the overlapping curtains
        // share them). A middle curtain's on its S (MiddleCentre ...): across and along by the S's own way there, the noise and the rays
        // by the metres along it.
        private void BuildPaths()
        {
            int count = Curtains * Columns;
            pathX = new float[count]; pathZ = new float[count];
            acrossX = new float[count]; acrossZ = new float[count];
            alongX = new float[count]; alongZ = new float[count];
            pathS = new float[count]; noiseAt = new float[count];
            curlWidth = new float[Curtains];
            float radius = MagneticMistRules.CoreRadius, span = 360f / Curtains * 1.35f;   // overlapping: the ring is closed from every side
            float ringStep = span * Mathf.Deg2Rad * radius / (Columns - 1);                // metres between an arc's columns
            for (int k = 0; k < Curtains; k++)
            {
                int section = SectionByCurtain[k];
                curlWidth[k] = 1f;
                if (section < RingArcs)
                {
                    float start = k * 360f / Curtains - span * 0.15f, onRing = radius + SectionOffset[section];
                    for (int c = 0; c < Columns; c++)
                    {
                        int column = k * Columns + c;
                        float u = c / (float)(Columns - 1), angle = (start + u * span) * Mathf.Deg2Rad;
                        float sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);
                        pathX[column] = sin * onRing; pathZ[column] = cos * onRing;
                        acrossX[column] = sin; acrossZ[column] = cos;
                        alongX[column] = cos; alongZ[column] = -sin;
                        pathS[column] = u * span * Mathf.Deg2Rad * radius;
                        noiseAt[column] = angle * radius;
                    }
                    continue;
                }

                int m = section - RingArcs;
                float turn = MiddleTurn[m] * Mathf.Deg2Rad, dx = Mathf.Sin(turn), dz = Mathf.Cos(turn);   // along the line; across it (dz, -dx)
                float length = MiddleLength[m], bend = MiddleBend[m], waves = MiddleWaves[m], metres = 0f, lastX = 0f, lastZ = 0f;
                for (int c = 0; c < Columns; c++)
                {
                    int column = k * Columns + c;
                    float u = c / (float)(Columns - 1), wave = 2f * Mathf.PI * waves * u;
                    float a = (u - 0.5f) * length, b = bend * Mathf.Sin(wave), db = bend * 2f * Mathf.PI * waves * Mathf.Cos(wave);
                    float x = MiddleCentre[m].x + dx * a + dz * b, z = MiddleCentre[m].y + dz * a - dx * b;
                    float tx = dx * length + dz * db, tz = dz * length - dx * db, t = Mathf.Sqrt(tx * tx + tz * tz);   // the S's way there
                    tx /= t; tz /= t;
                    metres += c > 0 ? Mathf.Sqrt((x - lastX) * (x - lastX) + (z - lastZ) * (z - lastZ)) : 0f;
                    lastX = x; lastZ = z;
                    pathX[column] = x; pathZ[column] = z;
                    acrossX[column] = -tz; acrossZ[column] = tx;   // as the arcs' outward is from their way round
                    alongX[column] = tx; alongZ[column] = tz;
                    pathS[column] = metres;
                    noiseAt[column] = metres;
                }

                curlWidth[k] = Mathf.Max(1f, metres / (Columns - 1) / ringStep);
            }
        }

        // the vertex of curtain k, skirt column n, skirt row s (after the curtains' own)
        private static int SkirtIndex(int k, int n, int s)
        {
            return Curtains * Columns * Rows + (k * SkirtColumns + n) * SkirtRows + s;
        }

        // the veil's vertex over curtain k, skirt column n (its column the skirt's), veil row g (after the skirts')
        private static int VeilIndex(int k, int n, int g)
        {
            return Curtains * (Columns * Rows + SkirtColumns * SkirtRows) + (k * SkirtColumns + n) * VeilRows + g;
        }

        // the skirt column under curtain column c, or -1 (every SkirtStep-th column, and the last)
        private static int SkirtSlot(int c)
        {
            return c == Columns - 1 ? SkirtColumns - 1 : c % SkirtStep == 0 ? c / SkirtStep : -1;
        }

        // The curtains on the far side of the ring from the view and the middle ones (their bit set in `mask`) into the far
        // submeshes; rebuilt only when the split changes.
        private void SplitCurtains(int mask)
        {
            if (mask == farMask)
            {
                return;
            }

            farMask = mask;
            nearTriangles.Clear();
            farTriangles.Clear();
            for (int k = 0; k < Curtains; k++)
            {
                ((mask & (1 << k)) != 0 ? farTriangles : nearTriangles).AddRange(curtainTriangles[k]);
            }

            mesh.SetTriangles(nearTriangles, 0, false);
            mesh.SetTriangles(nearTriangles, 1, false);
            mesh.SetTriangles(farTriangles, 2, false);
            mesh.SetTriangles(farTriangles, 3, false);
        }

        private void Update()
        {
            // the base just over the sea whatever height the storm object has (the world shifts only sideways)
            Vector3 p = transform.parent.position;
            transform.position = new Vector3(p.x, 0f, p.z);
            transform.rotation = Quaternion.identity;
            shown = Mathf.MoveTowards(shown, 1f, Time.deltaTime / FadeSeconds);
            float nightLerp = Sun.sun != null ? Sun.sun.GetNightLerp() : 0f;
            Camera view = Camera.main;   // not cached: the user plays with a free camera
            Vector3 viewLocal = view != null ? transform.InverseTransformPoint(view.transform.position) : Vector3.zero;
            UpdateDrawOrder(viewLocal);
            // seen from inside the core too, each column faded by the fog on its way (CoreClear, Animate), and there in its night look at
            // every hour (its layers here, the light on the cloud in Animate)
            coreOpen = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(MagneticMistRules.CoreRadius,
                MagneticMistRules.CoreRadius + CoreFadeWidth, new Vector2(viewLocal.x, viewLocal.z).magnitude));
            nightLook = Mathf.Lerp(1f, nightLerp, coreOpen);
            if (lastRefresh < 0f || (Time.time - lastRefresh >= RefreshSeconds && meshRenderer.isVisible))
            {
                lastRefresh = Time.time;
                Animate(Time.time, viewLocal);
            }

            // the day look (A, B and C above; B is the day layer's texture)
            float twilight = Sun.sun != null ? Mathf.Lerp(DayNoonShare, 1f, Sun.sun.GetDawnLerp()) : 1f;
            float glow = shown * Mathf.Lerp(0.25f * twilight, 1f, nightLook), gain = Mathf.Lerp(1f, NightGain, nightLook);
            Color dayColour = new Color(DayGain, DayGain, DayGain, DayLightStrength * twilight * shown * (1f - nightLook));
            SetGlow(night, glow, gain);
            SetGlow(nightFar, glow, gain);
            day.color = dayColour;
            dayFar.color = dayColour;
            Vector2 drift = new Vector2(Mathf.Repeat(Time.time * RayDrift, 1f), 0f);
            night.mainTextureOffset = drift;
            nightFar.mainTextureOffset = drift;
            day.mainTextureOffset = drift;
            dayFar.mainTextureOffset = drift;
        }

        // the mist's storm cloud, grey (CloudTone; its green is the aurora's light under the curtains, Skirt): set whether or not the
        // aurora is seen (from inside the core the cloud is still drawn)
        private void LateUpdate()
        {
            Color emission = RenderSettings.fogColor * CloudTone;
            emission.a = 1f;

            SetEmission(innerCloud, emission);
            SetEmission(outerCloud, emission);
        }

        private static void SetEmission(Material material, Color emission)
        {
            if (material != null && material.HasProperty(EmisColorId))
            {
                material.SetColor(EmisColorId, emission);
            }
        }

        // From inside the core both clouds go under the fog dome and every curtain over it (faded by the fog there, CoreClear);
        // from outside, the far curtains and the middle ones go under the inner cloud and the outer cloud over the near ones.
        private void UpdateDrawOrder(Vector3 viewLocal)
        {
            Vector2 flat = new Vector2(viewLocal.x, viewLocal.z);
            bool inside = flat.magnitude < MagneticMistRules.CoreRadius;
            int mask = 0;
            if (!inside)
            {
                float span = 360f / Curtains * 1.35f;
                for (int k = 0; k < Curtains; k++)
                {
                    float middle = (k * 360f / Curtains + span * 0.35f) * Mathf.Deg2Rad;
                    if (SectionByCurtain[k] >= RingArcs || Mathf.Sin(middle) * flat.x + Mathf.Cos(middle) * flat.y < 0f)
                    {
                        mask |= 1 << k;
                    }
                }
            }

            SplitCurtains(mask);
            int cloudQueue = inside ? UnderDomeQueue : NearQueue;
            if (outerCloud != null && outerCloud.renderQueue != cloudQueue)
            {
                outerCloud.renderQueue = cloudQueue;
            }
        }

        // the glow layer's strength (its alpha) and brightness (`gain`: the additive shader draws 2 x its colour, so 0.5 is x 1)
        private static void SetGlow(Material material, float glow, float gain)
        {
            if (material.HasProperty("_TintColor"))
            {
                material.SetColor("_TintColor", new Color(0.5f * gain, 0.5f * gain, 0.5f * gain, 0.5f * glow));
            }
            else
            {
                material.color = new Color(1f, 1f, 1f, glow);
            }
        }

        private void OnDisable()
        {
            shown = 0f;   // fades in again when the mist is switched on
            lastRefresh = -1f;
        }

        // Folds as two travelling sines along each curtain (and a curl in places), a little lean across it at the top (outward on
        // the arcs), a flicker per column (and a pulse in places), the veil over the top; each column from its fixed
        // place on the path (BuildPaths). `viewLocal` (the camera in the aurora's space): from inside the core each column's fade
        // through the fog (CoreClear).
        private void Animate(float time, Vector3 viewLocal)
        {
            bool edges = lastEdges < 0f || time < lastEdges || time - lastEdges >= EdgeRefreshSeconds;
            if (edges)
            {
                lastEdges = time;
            }

            float skirt = Skirt * Mathf.Lerp(SkirtDay, SkirtNight, nightLook);
            // from inside the core each column faded by the fog on its way to the view (CoreClear), full again over the first
            // CoreFadeWidth outside the core (coreOpen, Update)
            float fogPerMetre = Mathf.Log(Mathf.Clamp(CoreClear, 0.001f, 1f)) / MagneticMistRules.CoreRadius;

            for (int k = 0; k < Curtains; k++)
            {
                // its section (an arc, or a middle curtain), at its own height, brightening and fading on its own over minutes
                int arc = SectionByCurtain[k];
                float ph = phase[k], width = curlWidth[k];
                float arcStrength = 0.55f + 0.45f * Mathf.PerlinNoise(time * 0.015f, 3.7f * arc + 1.3f);
                float arcHeight = SectionHeight[arc];
                float pulse = Mathf.Sin(time * 2f * Mathf.PI / Mathf.Lerp(PulseMin, PulseMax, Mathf.Repeat(ph, 1f)) + ph);
                for (int c = 0; c < Columns; c++)
                {
                    float u = c / (float)(Columns - 1);
                    int column = k * Columns + c;
                    float s = pathS[column], nx = acrossX[column], nz = acrossZ[column];
                    float fold = Mathf.Clamp(55f * Mathf.Sin(s / 420f + time * 0.25f + ph) + 22f * Mathf.Sin(s / 150f - time * 0.6f + ph * 2f),
                        -FoldLimit, FoldLimit);
                    float bottom = 220f + 70f * Mathf.Sin(s / 900f + ph);
                    // 1.5 times the first height (the user, 2026-10-25: "increase the overall size of aurora so it don't look too
                    // small from all the rag")
                    float height = (3000f + 750f * Mathf.Sin(s / 1300f + ph * 3f)) * arcHeight;
                    float ends = Mathf.Max(0f, Mathf.Sin(Mathf.PI * u));   // sin(pi) is a little under 0 in floats: its root would be NaN
                    if (edges)
                    {
                        float along = noiseAt[column];
                        curlAt[column] = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(CurlFrom, CurlFull,
                            Mathf.PerlinNoise(along / 1800f + k * 2.9f, time * 0.02f + 40f)));
                        pulseAt[column] = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(PulseFrom, PulseFull,
                            Mathf.PerlinNoise(along / 700f + k * 4.1f, time * 0.05f + 60f)));
                    }

                    float strength = ends * Mathf.Sqrt(ends) * (0.55f + 0.45f * Mathf.PerlinNoise(s / 260f + time * 0.3f, ph)) * arcStrength *
                        (1f + PulseDepth * pulseAt[column] * pulse);
                    // the curl: across the curtain (out from the centre on the arcs) and along it; the curtain's place there
                    float swing = CurlReach * width * curlAt[column], wave = s / (CurlWave * width) * 2f * Mathf.PI - time * CurlSpeed + ph;
                    float across = fold + swing * Mathf.Sin(wave), alongBy = swing * Mathf.Cos(wave);
                    float px = pathX[column] + nx * across + alongX[column] * alongBy, pz = pathZ[column] + nz * across + alongZ[column] * alongBy;
                    float clear = 1f;
                    if (coreOpen < 1f)
                    {
                        float dx = px - viewLocal.x, dz = pz - viewLocal.z;
                        clear = Mathf.Lerp(Mathf.Exp(fogPerMetre * Mathf.Sqrt(dx * dx + dz * dz)), 1f, coreOpen);
                        strength *= clear;
                    }
                    // a ragged lower edge (the user, 2026-10-25: "make the aurora bottom edge also have some irregular fade effect
                    // like real aurora, so it doesn't look cut off"; probe 8's ragged2, chosen by the user): each column starts
                    // 300-1,600 over its base by two octaves of noise along the ring and fades in over 100-500, mostly above the
                    // cloud's flat top (its puffs to 1,250), which drew a straight bottom edge over curtains starting at 150-290
                    if (edges)
                    {
                        float along = noiseAt[column];
                        edgeLift[column] = Mathf.Lerp(300f, 1600f, Mathf.Clamp01(0.6f * Mathf.PerlinNoise(along / 500f + k * 7.3f, time * 0.03f) +
                            0.4f * Mathf.PerlinNoise(along / 90f + k * 3.1f, time * 0.08f + 5f)));
                        edgeFade[column] = Mathf.Lerp(100f, 500f, Mathf.Clamp01(Mathf.PerlinNoise(along / 60f + k, 9f)));
                        edgeDrop[column] = 0.35f * Mathf.Clamp01(0.5f * Mathf.PerlinNoise(along / 280f + k * 5.7f, time * 0.04f + 20f) +
                            0.5f * Mathf.PerlinNoise(along / 60f + k * 2.3f, time * 0.1f + 30f));
                    }

                    float lift = edgeLift[column], fade = edgeFade[column];
                    // a ragged upper edge too (the user, 2026-10-25: "please make upper edge ragged also"): each column's top
                    // lowered by up to 35 % of its height, by two octaves of noise of their own
                    float top = bottom + height * (1f - edgeDrop[column]);
                    float full = Mathf.Min(bottom + lift + fade, top - 300f), rise = Mathf.Min(bottom + lift, full - 50f);
                    for (int r = 0; r < Rows; r++)
                    {
                        int i = (k * Columns + c) * Rows + r;
                        float lean = RowHeight[r] * 120f;
                        float y = r == 0 ? rise : r == 1 ? full : Mathf.Lerp(full, top, (r - 1) / (float)(Rows - 2));
                        vertices[i] = new Vector3(px + nx * lean, y, pz + nz * lean);
                        Color colour = RowColor[r];
                        colour.a = RowAlpha[r] * strength;
                        colors[i] = colour;   // 4 bytes a vertex to the GPU instead of 16
                    }

                    int slot = SkirtSlot(c);
                    for (int g = 0; slot >= 0 && g < SkirtRows; g++)
                    {
                        int i = SkirtIndex(k, slot, g);
                        vertices[i] = new Vector3(px, SkirtY[g], pz);
                        Color colour = RowColor[1];
                        colour.a = SkirtAlpha[g] * skirt * strength;
                        colors[i] = colour;
                    }

                    // the veil on its arc's own smooth line, from the curtains' upper part, leaning on with the curtain
                    float veil = slot >= 0 ? VeilAlpha * arcStrength * clear * Mathf.Pow(ends, 0.75f) : 0f;
                    float veilLine = slot >= 0 ? (3000f + VeilWave * Mathf.Sin(noiseAt[column] / 1600f + arc * 2.1f)) * arcHeight : 0f;
                    for (int g = 0; slot >= 0 && g < VeilRows; g++)
                    {
                        int i = VeilIndex(k, slot, g);
                        float up = veilLine * VeilAt[g], lean = fold + VeilSlope * up;
                        vertices[i] = new Vector3(pathX[column] + nx * lean, bottom + up, pathZ[column] + nz * lean);
                        Color colour = VeilColour;
                        colour.a = VeilShape[g] * veil;
                        colors[i] = colour;
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.colors32 = colors;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (night != null) Destroy(night);
            if (day != null) Destroy(day);
            if (nightFar != null) Destroy(nightFar);
            if (dayFar != null) Destroy(dayFar);
            if (rays != null) Destroy(rays);
            if (dayRays != null) Destroy(dayRays);
            if (daySoftRays != null) Destroy(daySoftRays);
        }
    }

    // A runtime material destroyed with the object that uses it.
    internal sealed class OwnedMaterial : MonoBehaviour
    {
        internal Material Material;

        private void OnDestroy()
        {
            if (Material != null)
            {
                Destroy(Material);
            }
        }
    }

    // A storm's eye (EYE_LAYER_DRAFT.md §5, the user, 2026-10-25): a clear sky inside a straight cloud wall, weak wind (3 at
    // the centre, 7 at the wall), the full storm wind and the lightning from the wall's inner side out. One shape for the
    // Hurricane, one for the game's three storms. Pure maths: no Unity native calls. d = the distance from the storm's centre.
    internal sealed class StormEyeShape
    {
        // the Hurricane: the eye 2,400 (the sky clear inside 2,350), the wall 2,650-3,050 up to 2,200 ("make it 8000 radius": core 8,000)
        internal static readonly StormEyeShape Hurricane = new StormEyeShape(
            "Hurricane", 2650f, 3050f, 2200f, 1600f, 1600f);
        // the game's storms: the storm cloud only, with no eye and the game's own radius (3,000), since 1.2.1's in-game test (the user,
        // 2026-10-26: "vanilla storm no longer have storm eye, so return it's radius value back to vanilla, keep it's looks from outside
        // so it still look like storm front cloud like hurricane"; their eye 1,600 and core 4,600 before, EYE_LAYER_DRAFT.md §3): the
        // layer a disc from the centre, the front up to 1,600 ("i want vanilla cloud wall high to be 1600") in three tiers (the user,
        // 2026-10-27: "also the vanilla storm looks very bad, please fix", then of probe 20's round 3 "3 tiers under 1,600": one tier
        // fitted over the first base, a flat lens of flat puffs; three from the lowered base with the rounder upper puffs, 950 / 1,629 /
        // 1,879, a puffy white top over a darker base, run 20261027-185238)
        internal static readonly StormEyeShape GameStorm = new StormEyeShape("game storm", 1600f, 3);

        // the game's radius (3,000) + the largest range (12,000): the cloud shows to the outer edge (the game's 5,500 ends inside it)
        internal const float GameStormCloudDistance = 15000f;
        internal const float CentreWind = 3f;                   // "hurrican eye wind speed also 3 at center, 7 at edge"
        internal const float EdgeWind = 7f;
        internal const float ExitMargin = 25f;                  // the wind leaves the eye only 25 past its edge: no flicker on the line
        // the storm cloud (the user after probe 15: "make puff bigger and reduce the quantity of puff", "more layed, like real
        // life storm front cloud", "there is no cloud under there"): flat puffs over twice the game's size (400-1,100) in thin
        // tiers, a darker tone on the lower ones
        // 1,000-2,400 at 0.4 left the sky showing between the tiers (run 20261025-225604): bigger, so the same counts cover the
        // sky about as the game's density did (puffs x their area per km²: the layer 15 x 2.96 km², the game's 80 x 0.55)
        internal const float PuffMin = 1500f;
        internal const float PuffMax = 3500f;
        internal const float WallPuffMin = 1800f;               // the wall's smallest a little bigger, so its tiers close up
        // a puff's height / its width: flatter than first built (0.45), so the tiers overlap less and each shows as its own band
        // (the shades of white, run 20261026-080018, EYE_LAYER_DRAFT.md §5.6)
        internal const float Flatness = 0.25f;
        // the lowest tier's bottom as first built: a puff's visible edge is about 0.35 of its size (probe 7), so the cloud's base
        // was about 1,450 - 0.35 x 0.25 x 3,500 = 1,144 up (at 0.45 it was 900; the game's about 0, probe 15's 415). The tiers'
        // count is still worked out from it (TierBottoms)
        internal const float CloudBase = 1450f;
        // the whole cloud this much lower, the tops kept (the user, 2026-10-27: "Make both the hurricane and vanilla storm cloud
        // lower, now the gap from sea level to cloud is too big"; of probe 20's steps they picked "1 Lower by 500"): the lowest tier
        // at 950, its visible base about 644 (thunderstorm bases mostly under about 600 m, NWS Key West; EYE_LAYER_DRAFT.md §5.11)
        internal const float BaseLower = 500f;
        internal const float LoweredBase = CloudBase - BaseLower;
        internal const float TierDepth = 150f;                  // each tier emitted in a band this deep
        // the front's tiers each emitted in a band this deep (the user picked "4 Deeper front band"): a tier's puffs scatter up
        // among the tiers over it and the front reads as one face, not as separate lens-shaped bands seen edge-on (their picture 3)
        internal const float FrontDepth = 600f;
        // the front's tiers over the lowest: a puff's height / its width (the user, 2026-10-27: "Hurricane cloud have this weird
        // looking cloud layer", the front's flat puffs seen edge-on as white-rimmed lenses; they picked "3 Rounder upper puffs"),
        // each lifted by the largest puff's extra visible half-height (about 429), so none hangs under the base (FlightGear's cloud
        // notes: puffs squashed upward "look squashed when seen from the side"); the base keeps Flatness
        internal const float UpperFlatness = 0.6f;
        internal const float UpperLift = 0.35f * (UpperFlatness - Flatness) * PuffMax;
        internal const float WallTierStep = 250f;               // the wall's tiers, at most this far apart, from the base to its top
        internal const float FrontTierStep = 350f;              // the front's tiers step up at most this far ...
        // ... and back this far from the full-cloud line: none, stacked over the line, so their shades of white show as bands on
        // the face (stepped back 300 each, the higher tiers sat behind the lowest and showed only at the top rim, run 20261026-074906)
        internal const float FrontSetBack = 0f;
        internal const float FrontWidth = 900f;                 // each front tier's band
        // puffs per km² of a tier's ring (no cap, the user; the game's layer: 80 smaller, round puffs)
        internal const float LayerPerKm2 = 15f;
        internal const float FrontPerKm2 = 30f;
        // the eye wall's, shared among its tiers: the Hurricane's four 30 each as before, the game storms' one (their wall 1,600
        // tall over a cloud base of 1,450) all 120, so their wall is as thick (the user, 2026-10-26, in a game storm's eye: "Cloud
        // wall is not fully closed, there is a gap you can see outside sky"; probe 16b, run 20261026-181835: 155 puffs, some 10° of
        // the ring empty, the blue sky through the wall all round)
        internal const float WallPerKm2 = 120f;
        // each tier's tone (the puffs' colour x this), from the lowest up: the bands of a layered cloud
        internal static readonly float[] Tones = { 0.84f, 1f, 0.9f, 1f, 0.94f, 1f };
        internal const float WallTurn = 0.006f;                 // radians a second, about the centre

        internal readonly string Label;
        internal readonly bool HasEye;   // false: the storm cloud only, no wall and clear nowhere (the game's storms)
        // the eye ends at the wall's inner side: the eye wind inside, the full storm wind and the lightning outside (the user,
        // asked where "the wall gap" is: "Wall's inner side")
        internal readonly float StormEdge;
        // the sky clears only in the eye (the user: "eye hole shouldn't be seen while in cloud wall transition gap also, only after
        // enter storm eye area then you will see it"): the full storm and the cover through the gap between the wall's
        // visible face (about 150 inside its band, probe 11) and the eye's edge (250 inside it: the Hurricane 2,400, the game
        // storms 1,600), clear inside ClearInner (the user: "crossing the wall will suddenly clear up"). Cleared across the gap
        // (from 100 inside the band) the sky showed grey and half open at 2,450 (run 20261026-093202)
        internal readonly float ClearOuter;
        internal readonly float ClearInner;
        internal readonly float WallEmitInner;   // the wall's band, straight up ("pull wall in 100": 100 nearer than first drafted)
        internal readonly float WallEmitOuter;   // the storm cloud layer from here out
        internal readonly float WallTop;         // the top tier's band ends here; the front rises to it too
        internal readonly float FullWindOuter;   // full storm wind from the storm edge out to here, easing to the outer edge
        internal readonly float StrikeOuter;     // lightning in a ring from the storm edge out to here
        // the wall fog-free this near the centre, where the storm's fog hides any fogged puff past about 150 anyway; farther out
        // the fogged original, so the wall never shows through the storm's haze
        internal readonly float WallClearSwitch;
        internal readonly int FrontTiers = 1;    // the front's least number of tiers (TierBottoms)

        private StormEyeShape(
            string label,
            float wallEmitInner,
            float wallEmitOuter,
            float wallTop,
            float fullWindBand,
            float strikeBand)
        {
            Label = label;
            HasEye = true;
            StormEdge = wallEmitInner;
            ClearOuter = wallEmitInner - 250f;
            ClearInner = wallEmitInner - 300f;
            WallEmitInner = wallEmitInner;
            WallEmitOuter = wallEmitOuter;
            WallTop = wallTop;
            FullWindOuter = StormEdge + fullWindBand;
            StrikeOuter = StormEdge + strikeBand;
            WallClearSwitch = wallEmitOuter + 250f;
        }

        // the storm cloud with no eye: no wall, the layer a disc from the centre, the front up to `cloudTop` in at least `frontTiers`
        private StormEyeShape(string label, float cloudTop, int frontTiers)
        {
            Label = label;
            WallTop = cloudTop;
            FrontTiers = frontTiers;
        }

        // 1 in the eye, 0 outside its edge: the sky, the rain, the clouds and their tone; 0 everywhere without an eye
        internal float Clearness(float d)
        {
            return HasEye ? Mathf.InverseLerp(ClearOuter, ClearInner, d) : 0f;
        }

        internal bool InEye(float d, bool wasInEye)
        {
            return HasEye && d < (wasInEye ? StormEdge + ExitMargin : StormEdge);
        }

        internal float EyeWind(float d)
        {
            return Mathf.Lerp(CentreWind, EdgeWind, Mathf.Clamp01(d / StormEdge));
        }

        // the bottoms of the tiers up to `top` (the last one's band ending there): as many as fit at most `step` apart over the
        // first base (CloudBase), at least `minimum`, spread evenly from the lowered base (LoweredBase), so the tops stay where
        // they were and the tiers under them reach down (the Hurricane's wall 950 / 1,317 / 1,683 / 2,050, its front 950 / 1,500 /
        // 2,050, as pictured in probe 20)
        internal static float[] TierBottoms(float top, float step, int minimum = 1)
        {
            float span = Mathf.Max(0f, top - TierDepth - CloudBase);
            int count = Mathf.Max(minimum, 1 + Mathf.CeilToInt(span / step - 0.001f));
            float lowered = Mathf.Max(0f, top - TierDepth - LoweredBase);
            float[] bottoms = new float[count];
            for (int i = 0; i < count; i++)
            {
                bottoms[i] = LoweredBase + (count > 1 ? lowered * i / (count - 1) : 0f);
            }

            return bottoms;
        }

        internal static float Tone(int tier)
        {
            return Tones[Mathf.Clamp(tier, 0, Tones.Length - 1)];
        }

        // puffs over a ring from inner to outer at `perKm2`
        internal static int Puffs(float inner, float outer, float perKm2)
        {
            return Mathf.Max(1, Mathf.RoundToInt(perKm2 * Mathf.PI * (outer * outer - inner * inner) / 1e6f));
        }

        // A cone volume's radiusThickness for a ring from inner to outer. Unity picks a puff's distance by area (between (1 -
        // thickness) and 1 of the radius squared), so the 1 - inner / outer used before put a ring's inner edge at
        // sqrt(inner x outer): the Hurricane's layer from about 6,040 instead of 3,050, its wall from 2,843 instead of 2,650, each
        // front tier only on its band's outer half, the mist's outer ring from 3,723 instead of 3,600 (probe 18, run
        // 20261027-173949: the layer's puffs 5,833-12,265 from the centre, a ring of bare sky between the eye wall and the deck)
        internal static float RingThickness(float inner, float outer)
        {
            float share = outer > 0f ? Mathf.Clamp01(inner / outer) : 0f;
            return 1f - share * share;
        }

        // evenly over the ring from inner to outer (u, v: 0-1)
        internal static Vector2 StrikeOffset(float u, float v, float inner, float outer)
        {
            float r = Mathf.Sqrt(Mathf.Lerp(inner * inner, outer * outer, u));
            float a = v * 2f * Mathf.PI;
            return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
    }

    // The eye's sky: the game's storm blend (WeatherStorms.ApplyStorm) with the depth raised to the eye's clearness, so
    // the eye is the region's clear weather and the wall runs back through cloudy and rain to storm. The game's
    // GetNormalizedDistance is not changed: other mods (Climate's pressure) still see the storm's full depth.
    internal static class StormEyeWeather
    {
        internal static bool Apply(StormInfluence influence)
        {
            WeatherStorms storms = WeatherStorms.instance;
            Weather weather = Weather.instance;
            Region region = weather != null ? weather.currentRegion : null;
            if (storms == null || region == null || region.clearWeather == null || region.cloudyWeather == null ||
                region.rainWeather == null || region.stormWeather == null)
            {
                return false;
            }

            float n = Mathf.Max(influence.NormalizedDistance, influence.EyeClearness);
            float cloudyBorder = StormAccess.CloudyBorder(storms);
            float rainBorder = StormAccess.RainBorder(storms);
            ref WeatherSet blended = ref StormAccess.BlendedSet(storms);
            if (n >= 1f)
            {
                WeatherSet.CopyFrom(ref blended, region.clearWeather, false);
            }
            else if (n > cloudyBorder)
            {
                WeatherSet.BlendSets(ref blended, region.clearWeather, region.cloudyWeather, Mathf.InverseLerp(1f, cloudyBorder, n));
            }
            else if (n > rainBorder)
            {
                WeatherSet.BlendSets(ref blended, region.cloudyWeather, region.rainWeather, Mathf.InverseLerp(cloudyBorder, rainBorder, n));
            }
            else
            {
                WeatherSet.BlendSets(ref blended, region.rainWeather, region.stormWeather, Mathf.InverseLerp(rainBorder, 0f, n));
            }

            weather.ChangeWeather(blended);
            return true;
        }
    }

    // Marks the objects a storm's eye adds (its tiers), so a clone of the storm can drop them.
    internal sealed class StormEyePart : MonoBehaviour
    {
    }

    // A storm's eye and its storm cloud, on the storm's own object (the Hurricane; each of the game's storms the storm cloud only, no
    // eye, since 1.2.1's in-game test, EYE_LAYER_DRAFT.md §5.9), all of flat puffs in thin tiers (StormEyeShape):
    // - the layer: the storm's own cloud layer turned into the lowest tier, a ring from the wall out to the game's full-cloud
    //   line (depth `cloudyBorder`, radius + 0.66 x range), its base high over the sea, pre-simulated when it turns on;
    // - the storm front: tiers stepping up and back from the full-cloud line to the wall's top, like a shelf cloud;
    // - the eye wall: straight tiers around the eye, from the cloud base to its top, turning slowly, drawn under the deck from
    //   outside its band and over it from inside (UpdateLook);
    // - (the Hurricane's; the game's storms none since 2026-10-27) a cover over the bare sky under the deck's base (Unity's fog
    //   never covers the sky), from wherever the viewer is: a fog
    //   dome reaching in each direction up to where the deck's base ends that way, and over the whole sky under the deck
    //   (UpdateCover). It also hides the eye's column from the storm, which a lid over the eye did until 1.2.2, and the gap under
    //   the deck's base from outside, which a rain curtain under the front did: the user saw the lid as the eye from inside the
    //   storm and the curtain as "a darker area under the storm" (2026-10-26; EYE_LAYER_DRAFT.md §5.8).
    // Every storm cloud is white from outside the full-cloud line and inside the eye (the user: "everything should look white
    // from outside of storm, and inside of eye"): drawn there with the game's fog-free cloud shader (LitCloudsNoFog) in a tone
    // from the fog colour x 0.85 (the storm's tone; the wall's x 0.49 near the eye) to x 0.8 (white) (a fogged cloud fades into the fog colour and read grey,
    // probe 14).
    // The tiers' emission follows the layer's, which the game (WanderingStorm.SetEmission) or the mod switches.
    internal sealed class StormEye : MonoBehaviour
    {
        private const float WhiteTone = 0.8f;      // the fog colour x this: white, at any hour (probes 8 and 12)
        // the dark end of every fog-free storm cloud: the fog colour x this reads as the storm (probe 10); the material's own
        // colour under the fog-free shader is near black (probe 15: black puffs at the wall, a dark grey storm from outside)
        private const float StormTone = 0.85f;
        // the wall's dark end (fog-free by the eye, within WallClearSwitch): at 0.85 its puffs read about 54 at 2,650 out (noon)
        // against the lid's 31 and the horizon fog's 27, a lit ring and a lit far wall that showed the eye from outside (the
        // user: "after cross the cloud wall, the storm eye hole should be cover right away"; run 20261026-000025: with the wall
        // hidden the view up read 28-34 all over); 0.85 x 31 / 54
        private const float WallStormTone = 0.49f;
        // the cloud's visible base over the sea: the lowest tier less a puff's visible half-height (0.35 x its height, probe 7)
        private const float DeckBase = StormEyeShape.LoweredBase - 0.35f * StormEyeShape.Flatness * StormEyeShape.PuffMax;
        // the cover (UpdateCover): full to CoverMargin degrees over the deck's edge in each direction, gone CoverFade higher;
        // fading out where it would reach from CoverHighFrom to CoverHighTo up (by the deck's edge, looking out), so it is gone as
        // the edge is crossed; past the deck's sides from outside full for CoverSideFull degrees, gone CoverSideFeather farther
        // (the rim's puffs reach past the full-cloud line)
        private const float CoverMargin = 1f;
        private const float CoverFade = 6f;
        private const float CoverHighFrom = 45f;
        private const float CoverHighTo = 80f;
        private const float CoverSideFull = 3f;
        private const float CoverSideFeather = 9f;
        private static readonly Color PuffColour = new Color(0.906f, 0.906f, 0.906f, 0.55f);   // the layer's white
        private static readonly int EmisColorId = Shader.PropertyToID("_EmisColor");
        private static readonly Dictionary<WanderingStorm, StormEye> eyes = new Dictionary<WanderingStorm, StormEye>();

        private WanderingStorm storm;
        private ModStormController controller;
        private ParticleSystem layer;
        private ParticleSystemRenderer layerRenderer;
        private readonly List<Tier> walls = new List<Tier>();
        private readonly List<Tier> fronts = new List<Tier>();
        private Material own;            // the layer's own material: fogged, its own colour
        private Material cloudClear;     // the layer's and the front's fog-free copy
        private Material wallClear;      // the wall's fog-free copy
        // the bare sky under the deck's base wherever the horizon fog leaves it, and the far sea under it (fogged, it read darker
        // than the deck: a thin dark line, run 20261025-234901): a fog dome covering each direction from where it meets the sea up
        // to the deck's edge that way, in the deck's tone (EYE_LAYER_DRAFT.md §5.5)
        private FogDome cover;
        private readonly float[] coverTop = new float[FogDome.Columns];
        private readonly float[] coverAlpha = new float[FogDome.Columns];
        // in the eye, also the band behind the wall in each direction, from its base to its top (FogDome.CoverBand)
        private readonly float[] coverFrom = new float[FogDome.Columns];
        private readonly float[] coverTo = new float[FogDome.Columns];
        private SavedLayer saved;
        private float ringOuter = -1f;
        private bool emitting;
        // the eye's clearness as shown (UpdateClearness, ClearnessOf): it follows the distance's (Shape.Clearness) by at most a full
        // change in EyeOpenSeconds, so a fast ship crossing the 50-wide opening in a moment still sees the eye open as a fast fade
        // (the user, 2026-10-27, sailing with a speed cheat: "make the eye's showing up a fast transition, instead a sudden pop out");
        // -1 until first set, then taken as it is (a game loaded in the eye)
        private const float EyeOpenSeconds = 1.5f;
        private float shownClearness = -1f;
        // the storm cloud drawn fog-free (its whiteness over 0): turned on from SwapOn, off only at 0, so the deck doesn't swap
        // back and forth on the full-cloud line as the ship bobs (run 20261026-095346: at depth 0.66 one shot fog-free over the
        // band's storm tone, the next fogged)
        // (swept at 0.5 and 0.8 in run 20261026-183242: while the deck stayed fogged past the line the cover kept the storm's tone,
        // a dark sky over the horizon)
        private const float SwapOn = 0.002f;
        // the deck's whiteness the moment it turns fog-free outside the full-cloud line (UpdateLook): at 0.15-0.3 its far side read
        // as a dark flat cloud, from 0.45 white (run 20261026-183242)
        private const float OutsideWhiteFrom = 0.5f;
        private bool deckClear;
        // the fog colour the white deck's emission was set from this frame (UpdateLook): the covers' white part takes the same, not
        // the colour read again as the camera renders (run 20261026-104719: the shaded face's base 161 in runs -103655 and -104719,
        // the cover at the same tone 162 and 177)
        private Color deckFog;
        // the storm cloud's draw order (UpdateOrder), fixed by the viewer's place. The wall, the layer and the front's tiers all sort
        // from the storm's centre, their bounds' centres metres apart and moving as puffs are born and die, so which drew on top was
        // arbitrary and changed from frame to frame: from outside a front tier's white band showed in one frame and was gone in the
        // next (the user, 2026-10-26, after testing 1.2.1 in game: "there is still flickering effect when seeing from outside, also
        // while you approaching"; their video at 60 frames a second, frames 176-177, 271-272, 301-302 and 368-369), and before it
        // the eye wall's ring ("when you turn the camera the white cloud will cover it up"). A lower fudge draws later, on top:
        // - outside the full-cloud line: the wall, the layer, then the front's tiers from the top down, the lowest last (seen from
        //   below, a shelf cloud's lowest lip is the nearest);
        // - inside it, under the deck: the layer over the front (overhead it is the nearest);
        // - inside the wall's band and in the eye: the wall over both.
        // Just past the full-cloud line the wall's order decided only the lower edge of its ring seen under the deck (run
        // 20261026-191329); the ring itself was the wall's tone (UpdateLook). The mist's outer cloud is biased the same way
        // (MistAurora.OuterCloudFudge)
        private const float WallFudge = 2000f;
        private const float LayerFudge = 1500f;
        private const float FrontFudgeStep = 500f;   // x the tier's index from the lowest (the Hurricane's three: 0, 500, 1,000)
        // The front's tiers each in FrontSectors arcs, so one part of the ring can draw before the deck and another after it. As one
        // ring (one system, its bounds centred on the storm's) the whole front drew after the deck from outside, its far side too: its
        // upper tiers 16-22 km off showed over the nearer deck as lighter shapes at the view's sides, its lowest tier as a darker band
        // under them, cut straight where the far clip cuts them (the user, 2026-10-27: "the blocky upper tiers at the view's sides";
        // probe 21, run 20261027-200051: gone with the front drawn under the deck). Each sector sorts as the whole ring would
        // (UpdateOrder); with SplitFront, from outside, a sector past the chord between the deck's tangent points draws first
        // (EYE_LAYER_DRAFT.md §7.1, the user's OK 2026-10-27: "7.1 far side behind the deck"; false: the whole front after the deck).
        internal const int FrontSectors = 12;
        private const float FarFudge = 20000f;
        private const bool SplitFront = true;
        // The sun's light on the storm cloud with its tint taken out by day: the shader adds the sun's light x each puff's colour, and
        // the game's sun is peach by day (FFD2B3), so the cloud's side lit from behind the viewer read pink over the blue-grey of its
        // emission (the user, 2026-10-27: "the game storm's pink tone"; probe 21: with the sun's light off the same cloud read
        // blue-grey, with green puffs that side read green). The puffs' colour x this share of (the sun's weakest channel / each
        // channel), less from day to dawn and dusk (Sun.GetDawnLerp), so sunsets stay golden (EYE_LAYER_DRAFT.md §7.2, the user's OK
        // 2026-10-27: "7.2 sun's tint taken out by day"; 0: the sun's own tint).
        private const float SunNeutral = 1f;
        private const float TintStep = 0.01f;        // the puffs' colours set again when the tint moves this much
        private const float TintRecolour = 0.05f;    // ... and their live puffs too when it jumps this much (a switch, a sleep)
        private Color appliedTint = Color.white;
        private float wallFudge = float.NaN, layerFudge = float.NaN;
        private readonly float[] frontShades = new float[FrontBright.Length];

        internal StormEyeShape Shape { get; private set; }

        private sealed class Tier
        {
            internal ParticleSystem Particles;
            internal ParticleSystemRenderer Renderer;
            internal float SetBack;   // a front tier: its band's outer side this far inside the full-cloud line
            internal int Index;       // from the lowest up
            internal int Sector;      // a front tier's arc (FrontSectors), 0 for the others
            internal Color Colour;    // its puffs' start colour before the sun's tint (UpdateSunTint)
            internal Material Clear;  // its own fog-free copy, for its own shade of white (TierBright, TierNeutral); a front tier's sectors share one
        }

        // The tiers' shades of white where the clouds are toned white (the user: "those cloud in your picture still look lack of
        // layer, maybe you need to use different shade of white"; in the eye the wall, from outside the whole storm cloud), by tier
        // from the lowest up: the white end x TierBright, its fog tint taken out (towards a grey of the same brightness) by
        // TierNeutral. The lowest tier and the layer (LayerBright) are the cloud base the horizon fog, the curtain and the cover
        // are measured against (StormHorizonFog.OutsideWhiteTone, WhiteDeckTone). The dark end (the storm, and by the eye from outside) is not shaded.
        // The QA's shade runs set them (EYE_LAYER_DRAFT.md §5.6); these are "face-bands90" (run 20261026-080018): after a tiered
        // shelf cloud and an eyewall's terraces, each tier brighter than the one under it and the next darker again, the lower ones
        // keeping the fog's cool tint, the upper ones white, at 0.9 so the sun's light shades them a little (at 0.6 and lower it
        // read grey-brown from outside, run -075559); the base (the lowest tier and the layer) at 1, where the emission saturates
        // and the cloud reads the same from any side, as the flat covers under it do (at 0.9 its sunlit side read 183-188 and its
        // shaded side 150-154 over a curtain of 152-154, runs 20261026-094354 and -093202). A puff's own colour doesn't show (the
        // fog-free shader draws its emission).
        internal static float[] TierBright = { 1f, 1.17f, 0.945f, 1.305f, 1.035f, 1.35f };
        internal static float[] TierNeutral = { 0f, 0.5f, 0.2f, 0.7f, 0.4f, 0.9f };
        // the layer's white end x this (the lowest tier's)
        internal static float LayerBright = 1f;

        // The storm cloud after the user's in-game test of the 1.2.1 re-release (2026-10-27, EYE_LAYER_DRAFT.md §4 and §5.11: "Make
        // both the hurricane and vanilla storm cloud lower", "Hurricane cloud have this weird looking cloud layer", "more layered like
        // real life storm cloud"), the steps they picked from probe 20's pictures: lower (StormEyeShape.BaseLower), rounder upper
        // front puffs (UpperFlatness), a deeper front band (FrontDepth) and tones by height on the front (FrontBright); not the
        // rain veils, so the Hurricane keeps its cover from outside. The scud was removed after the user's in-game test (2026-10-27,
        // BetterStorm_FAILED.md).
        // the front's shades of white by tier from the lowest up: tones by height (the references: the base the darkest, brighter
        // toward the top) on the front only, so the eye's wall and the base keep their white (the user, 2026-10-25: "everything should
        // look white from outside of storm, and inside of eye"; on every tier the eye's wall turned grey, run 20261027-181054)
        private static readonly float[] FrontBright = { 0.85f, 1.1f, 1.35f };
        private const float FrontMean = 1.1f;   // their mean
        // The front's tones by height kept far off (the user, 2026-10-27: "and hurricane looks 3D with the black band at bottom when
        // closed by, but at 8km it the black band at bottom looks out of place"; of x 1, 0.5 and 0 pictured, "x0.7"): their spread about
        // their mean x 1 out to depth FarToneFrom (the range past the storm's radius), x FarToneShare from FarToneTo on (a far cloud's
        // contrast falls with the haze in front of it; these fog-free tiers keep theirs whole). At 2.0 at noon the base read 144 under
        // a body of 177 at x 1, 164 at x 0.5 (run 20261027-185238)
        private const float FarToneShare = 0.7f;
        private const float FarToneFrom = 1.3f;
        private const float FarToneTo = 2f;

        // what the eye changed on a game storm, put back when it is removed
        private sealed class SavedLayer
        {
            internal Vector3 LocalPosition;
            internal Quaternion LocalRotation;
            internal int MaxParticles;
            internal ParticleSystem.MinMaxCurve Rate;
            internal ParticleSystem.MinMaxCurve StartSize;
            internal ParticleSystem.MinMaxCurve StartRotation;
            internal ParticleSystem.MinMaxGradient StartColor;
            internal bool SizeEnabled, SizeSeparate, RotationEnabled;
            internal ParticleSystem.MinMaxCurve SizeX, SizeY, SizeZ;
            internal Vector3 Flip;
            internal ParticleSystemShapeType ShapeType;
            internal float Angle, Radius, RadiusThickness, Length, Arc;
            internal Vector3 ShapeRotation, ShapePosition, ShapeScale;
            internal Material Material;
            internal float Fudge;
            internal float CloudDistance;
        }

        // the eye of a storm, null when it has none (a game storm's storm cloud has none)
        internal static StormEyeShape ShapeOf(WanderingStorm storm)
        {
            return storm != null && eyes.TryGetValue(storm, out StormEye eye) && eye != null && eye.Shape.HasEye ? eye.Shape : null;
        }

        // the eye's clearness as shown (UpdateClearness, last set in its LateUpdate), 0 for a storm without an eye
        internal static float ClearnessOf(WanderingStorm storm)
        {
            return storm != null && eyes.TryGetValue(storm, out StormEye eye) && eye != null && eye.Shape.HasEye
                ? Mathf.Max(0f, eye.shownClearness)
                : 0f;
        }

        internal static StormEye Build(WanderingStorm storm, StormEyeShape shape)
        {
            ParticleSystem layer = storm != null ? StormAccess.BottomParticles(storm) : null;
            ParticleSystemRenderer layerRenderer = layer != null ? layer.GetComponent<ParticleSystemRenderer>() : null;
            if (layerRenderer == null || layerRenderer.sharedMaterial == null || storm.GetComponent<StormEye>() != null)
            {
                BetterStormPlugin.Instance?.LogFeatureInfo("Storm eye: none for " + (storm != null ? storm.name : "a missing storm") +
                    " (no cloud layer, or it has one already).");
                return null;
            }

            StormEye eye = storm.gameObject.AddComponent<StormEye>();
            eye.Shape = shape;
            eye.storm = storm;
            eye.controller = storm.GetComponent<ModStormController>();
            eye.layer = layer;
            eye.layerRenderer = layerRenderer;
            eye.own = layerRenderer.sharedMaterial;
            eye.saved = Save(layer, layerRenderer);
            Shader noFog = Shader.Find("LitCloudsNoFog");
            if (noFog != null && eye.own.HasProperty(EmisColorId))
            {
                eye.cloudClear = new Material(eye.own) { name = "Better Storm storm cloud (no fog)", shader = noFog, renderQueue = eye.own.renderQueue };
                eye.wallClear = new Material(eye.cloudClear) { name = "Better Storm eye wall (no fog)" };
            }
            else
            {
                BetterStormPlugin.Instance?.LogFeatureInfo("Storm eye (" + storm.name + "): the fog-free cloud shader was not found; its clouds keep the fogged one.");
            }

            // the layer: the lowest tier, flat puffs, a ring from the wall out (its outer edge follows the range, UpdateRing)
            Vector3 centre = storm.transform.position;
            Place(layer, centre, StormEyeShape.LoweredBase);
            ParticleSystem.MainModule layerMain = layer.main;
            layerMain.startSize = new ParticleSystem.MinMaxCurve(StormEyeShape.PuffMin, StormEyeShape.PuffMax);
            Flatten(layer);

            // the wall's tiers, straight up from the cloud base to the wall's top, turning slowly
            float[] wallBottoms = shape.HasEye ? StormEyeShape.TierBottoms(shape.WallTop, StormEyeShape.WallTierStep) : new float[0];
            for (int i = 0; i < wallBottoms.Length; i++)
            {
                Tier tier = eye.AddTier(eye.walls, "Better Storm " + shape.Label + " eye wall" + (i > 0 ? " " + (i + 1) : ""), centre, wallBottoms[i], i);
                ParticleSystem.MainModule main = tier.Particles.main;
                main.startSize = new ParticleSystem.MinMaxCurve(StormEyeShape.WallPuffMin, StormEyeShape.PuffMax);
                Volume(tier.Particles, shape.WallEmitInner, shape.WallEmitOuter, StormEyeShape.TierDepth,
                    StormEyeShape.Puffs(shape.WallEmitInner, shape.WallEmitOuter, StormEyeShape.WallPerKm2 / wallBottoms.Length));
                ParticleSystem.VelocityOverLifetimeModule velocity = tier.Particles.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.orbitalX = 0f;
                velocity.orbitalY = StormEyeShape.WallTurn;
                velocity.orbitalZ = 0f;
            }

            // the front's tiers, each one higher and farther in (their bands set with the layer's edge, UpdateRing); the ones over the
            // lowest rounder and lifted (UpperFlatness, UpperLift)
            // lowest, each in FrontSectors arcs round the centre (UpdateOrder)
            float[] frontBottoms = StormEyeShape.TierBottoms(shape.WallTop, StormEyeShape.FrontTierStep, shape.FrontTiers);
            for (int i = 0; i < frontBottoms.Length; i++)
            {
                Material shared = null;
                for (int k = 0; k < FrontSectors; k++)
                {
                    Tier tier = eye.AddTier(eye.fronts, "Better Storm " + shape.Label + " storm front " + (i + 1) + " sector " + (k + 1), centre,
                        frontBottoms[i] + (i > 0 ? StormEyeShape.UpperLift : 0f), i, shared);
                    shared = tier.Clear;
                    tier.Sector = k;
                    tier.SetBack = i * StormEyeShape.FrontSetBack;
                    tier.Particles.transform.rotation = Quaternion.Euler(0f, k * 360f / FrontSectors, 0f);
                    if (i > 0)
                    {
                        ParticleSystem.SizeOverLifetimeModule size = tier.Particles.sizeOverLifetime;
                        size.y = Scaled(size.x, StormEyeShape.UpperFlatness);
                    }
                }
            }

            eye.UpdateRing(WeatherStorms.instance);
            eyes[storm] = eye;
            return eye;
        }

        // The game's storms: the storm cloud, no eye, the game's radius kept, the cloud shown out to the outer edge (their template
        // stays without it: the custom storms are cloned from it first).
        internal static void BuildForGameStorm(WanderingStorm storm)
        {
            float distance = StormAccess.ParticleDistance(storm);
            StormEye eye = Build(storm, StormEyeShape.GameStorm);
            if (eye == null)
            {
                return;
            }

            eye.saved.CloudDistance = distance;
            StormAccess.ParticleDistance(storm) = StormEyeShape.GameStormCloudDistance;
        }

        // Takes a game storm's storm cloud off at once and puts its layer and cloud distance back (Better Storm's shutdown).
        internal static void RemoveFromGameStorm(WanderingStorm storm)
        {
            StormEye eye = storm != null ? storm.GetComponent<StormEye>() : null;
            if (eye == null)
            {
                return;
            }

            SavedLayer s = eye.saved;
            if (eye.layer != null)
            {
                eye.layer.transform.localPosition = s.LocalPosition;
                eye.layer.transform.localRotation = s.LocalRotation;
                ParticleSystem.MainModule main = eye.layer.main;
                main.maxParticles = s.MaxParticles;
                main.startSize = s.StartSize;
                main.startRotation = s.StartRotation;
                main.startColor = s.StartColor;
                ParticleSystem.EmissionModule emission = eye.layer.emission;
                emission.rateOverTime = s.Rate;
                ParticleSystem.SizeOverLifetimeModule size = eye.layer.sizeOverLifetime;
                size.separateAxes = true;
                size.x = s.SizeX;
                size.y = s.SizeY;
                size.z = s.SizeZ;
                size.separateAxes = s.SizeSeparate;
                size.enabled = s.SizeEnabled;
                ParticleSystem.RotationOverLifetimeModule rotation = eye.layer.rotationOverLifetime;
                rotation.enabled = s.RotationEnabled;
                ParticleSystem.ShapeModule shape = eye.layer.shape;
                shape.shapeType = s.ShapeType;
                shape.angle = s.Angle;
                shape.radius = s.Radius;
                shape.radiusThickness = s.RadiusThickness;
                shape.length = s.Length;
                shape.arc = s.Arc;
                shape.rotation = s.ShapeRotation;
                shape.position = s.ShapePosition;
                shape.scale = s.ShapeScale;
            }

            if (eye.layerRenderer != null)
            {
                eye.layerRenderer.sharedMaterial = s.Material;
                eye.layerRenderer.flip = s.Flip;
                eye.layerRenderer.sortingFudge = s.Fudge;
            }

            if (s.CloudDistance > 0f)
            {
                StormAccess.ParticleDistance(storm) = s.CloudDistance;
            }

            DestroyImmediate(eye);   // OnDestroy takes its tiers and materials
        }

        // A storm cloned from one with an eye drops the copied parts (a custom storm never carries the game storms' eye).
        internal static void StripFromClone(GameObject clone)
        {
            StormEyePart[] parts = clone.GetComponentsInChildren<StormEyePart>(true);
            for (int i = 0; i < parts.Length; i++)
            {
                DestroyImmediate(parts[i].gameObject);
            }

            StormEye[] copies = clone.GetComponentsInChildren<StormEye>(true);
            for (int i = 0; i < copies.Length; i++)
            {
                DestroyImmediate(copies[i]);
            }
        }

        private static SavedLayer Save(ParticleSystem layer, ParticleSystemRenderer renderer)
        {
            ParticleSystem.ShapeModule shape = layer.shape;
            ParticleSystem.SizeOverLifetimeModule size = layer.sizeOverLifetime;
            return new SavedLayer
            {
                LocalPosition = layer.transform.localPosition,
                LocalRotation = layer.transform.localRotation,
                MaxParticles = layer.main.maxParticles,
                Rate = layer.emission.rateOverTime,
                StartSize = layer.main.startSize,
                StartRotation = layer.main.startRotation,
                StartColor = layer.main.startColor,
                SizeEnabled = size.enabled,
                SizeSeparate = size.separateAxes,
                SizeX = size.x,
                SizeY = size.y,
                SizeZ = size.z,
                RotationEnabled = layer.rotationOverLifetime.enabled,
                Flip = renderer.flip,
                Fudge = renderer.sortingFudge,
                ShapeType = shape.shapeType,
                Angle = shape.angle,
                Radius = shape.radius,
                RadiusThickness = shape.radiusThickness,
                Length = shape.length,
                Arc = shape.arc,
                ShapeRotation = shape.rotation,
                ShapePosition = shape.position,
                ShapeScale = shape.scale,
                Material = renderer.sharedMaterial
            };
        }

        // a tier: a clone of the (flattened) layer at its own height, in the tier's tone
        private Tier AddTier(List<Tier> list, string name, Vector3 centre, float bottom, int index, Material shared = null)
        {
            GameObject go = Instantiate(layer.gameObject, layer.transform.parent);
            go.name = name;
            go.AddComponent<StormEyePart>();
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            ps.Clear(true);
            Place(ps, centre, bottom);
            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            float tone = StormEyeShape.Tone(index);
            Color colour = new Color(PuffColour.r * tone, PuffColour.g * tone, PuffColour.b * tone, PuffColour.a);
            main.startColor = colour;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;   // follows the layer's (LateUpdate)
            Material template = list == walls ? wallClear : cloudClear;
            Tier tier = new Tier
            {
                Particles = ps,
                Renderer = go.GetComponent<ParticleSystemRenderer>(),
                Index = index,
                Colour = colour,
                Clear = shared ?? (template != null ? new Material(template) { name = name + " (no fog)" } : null)
            };
            list.Add(tier);
            return tier;
        }

        // over the storm's centre at `bottom` above the sea (the world shifts only sideways), level
        private static void Place(ParticleSystem ps, Vector3 centre, float bottom)
        {
            ps.transform.position = new Vector3(centre.x, bottom, centre.z);
            ps.transform.rotation = Quaternion.identity;
        }

        // Flat puffs (the user: "more layed"): their size over life (if any) kept across, x Flatness upward; level (no roll), half
        // of them mirrored for variety.
        private static void Flatten(ParticleSystem ps)
        {
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            ParticleSystem.MinMaxCurve across = size.enabled ? size.x : new ParticleSystem.MinMaxCurve(1f);
            size.enabled = true;
            size.separateAxes = true;
            size.x = across;
            size.y = Scaled(across, StormEyeShape.Flatness);
            size.z = across;
            ParticleSystem.MainModule main = ps.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);   // about ±3°: the layers stay level
            ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
            rotation.enabled = false;
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.flip = new Vector3(0.5f, 0f, 0f);
            }
        }

        private static ParticleSystem.MinMaxCurve Scaled(ParticleSystem.MinMaxCurve curve, float factor)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.TwoConstants:
                    return new ParticleSystem.MinMaxCurve(curve.constantMin * factor, curve.constantMax * factor);
                case ParticleSystemCurveMode.Curve:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * factor, curve.curve);
                case ParticleSystemCurveMode.TwoCurves:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * factor, curve.curveMin, curve.curveMax);
                default:
                    return new ParticleSystem.MinMaxCurve(curve.constant * factor);
            }
        }

        // A volume straight up from the system's own place: a ring from inner to outer, `height` deep, `arc` degrees of it.
        private static void Volume(ParticleSystem ps, float inner, float outer, float height, int puffs, float arc = 360f)
        {
            ParticleSystem.MainModule main = ps.main;
            main.maxParticles = puffs;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.ConeVolume;
            shape.angle = 0f;
            shape.radius = outer;
            shape.radiusThickness = StormEyeShape.RingThickness(inner, outer);
            shape.length = height;
            shape.arc = arc;
            shape.rotation = new Vector3(-90f, 0f, 0f);   // up from the bottom
            shape.position = Vector3.zero;
            shape.scale = Vector3.one;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = puffs / Mathf.Max(1f, main.startLifetime.constantMax);
        }

        private void LateUpdate()
        {
            if (layer == null)
            {
                return;
            }

            WeatherStorms weatherStorms = WeatherStorms.instance;
            bool on = layer.emission.enabled;
            SetEmission(walls, on);
            SetEmission(fronts, on);
            UpdateRing(weatherStorms);
            if (on && !emitting)
            {
                // filled at once, not over the puffs' minute of life
                float lifetime = Mathf.Max(1f, layer.main.startLifetime.constantMax);
                Prewarm(layer, lifetime);
                for (int i = 0; i < walls.Count; i++)
                {
                    Prewarm(walls[i].Particles, lifetime);
                }

                for (int i = 0; i < fronts.Count; i++)
                {
                    Prewarm(fronts[i].Particles, lifetime);
                }
            }

            emitting = on;
            Camera view = Camera.main;   // not cached: the user plays with a free camera
            if (view != null)
            {
                UpdateLook(weatherStorms, view);
            }
        }

        // the range the game takes for this storm's depth (WeatherStorms.GetNormalizedDistance) when it is in control, the
        // region's for the others
        private float Range(WeatherStorms weatherStorms)
        {
            if (weatherStorms != null && StormAccess.CurrentStorm(weatherStorms) == storm)
            {
                return StormInfluenceService.GetWeatherRange(weatherStorms, controller);
            }

            Region region = GameplayRegionResolver.GetCurrentRegion();
            return region != null ? region.stormRange : 0f;
        }

        private static float CloudyBorder(WeatherStorms weatherStorms)
        {
            return weatherStorms != null ? StormAccess.CloudyBorder(weatherStorms) : 0.66f;
        }

        // the layer out to the full-cloud line (radius + cloudyBorder x range), live as the range changes; the front's tiers inside it
        private void UpdateRing(WeatherStorms weatherStorms)
        {
            float inner = Shape.WallEmitOuter;
            float outer = storm.GetRadius() + CloudyBorder(weatherStorms) * Range(weatherStorms);
            outer = Mathf.Max(outer, inner + StormEyeShape.FrontWidth + StormEyeShape.FrontSetBack * fronts.Count);
            if (Mathf.Abs(outer - ringOuter) < 1f)
            {
                return;
            }

            ringOuter = outer;
            Volume(layer, inner, outer, StormEyeShape.TierDepth, StormEyeShape.Puffs(inner, outer, StormEyeShape.LayerPerKm2));
            for (int i = 0; i < fronts.Count; i++)
            {
                float bandOuter = outer - fronts[i].SetBack, bandInner = Mathf.Max(inner, bandOuter - StormEyeShape.FrontWidth);
                Volume(fronts[i].Particles, bandInner, bandOuter, StormEyeShape.FrontDepth,
                    StormEyeShape.Puffs(bandInner, bandOuter, StormEyeShape.FrontPerKm2 / FrontSectors), 360f / FrontSectors);
            }
        }

        private void UpdateLook(WeatherStorms weatherStorms, Camera view)
        {
            Vector3 viewer = view.transform.position;
            Vector3 offset = transform.position - viewer;
            offset.y = 0f;
            float distance = offset.magnitude;
            float clearness = UpdateClearness(distance);
            float radius = storm.GetRadius(), range = Range(weatherStorms);
            float depth = range > 0f ? Mathf.Clamp01((distance - radius) / range) : (distance > radius ? 1f : 0f);
            float outside = Mathf.InverseLerp(CloudyBorder(weatherStorms), 1f, depth);
            // from outside, OutsideWhiteFrom the moment the deck turns fog-free and white within the first tenth of the way out, as
            // the cover (StormHorizonFog.DeckTone): ramped over the whole way out, the deck's far side read as dark flat puffs low over
            // the horizon from just past the line (the user, 2026-10-26: "there is a dark cloud you can see from outside"; probe 16b,
            // run 20261026-181835: at depths 0.67 and 0.69, gone with the layer hidden); ramped from 0 over the first tenth, they
            // still showed over the first 0.01 of depth past the line (run 20261026-183242: dark at 0.665 and 0.67, white from 0.675)
            float outsideWhite = outside > 0f ? Mathf.Lerp(OutsideWhiteFrom, 1f, Mathf.InverseLerp(0f, StormHorizonFog.WhiteWithin, outside)) : 0f;
            float whiteness = Mathf.Max(outsideWhite, clearness);
            deckClear = whiteness > (deckClear ? 0f : SwapOn);
            // the deck's (the layer's and the front's) dark end x the base's shade of white too, so it stays a little over its white
            // end as built (0.85 and 0.8): at 0.85 against a white end of 0.72 the deck read 155-157 at depth 0.8 over a cover of 146
            // (run 20261026-093202)
            deckFog = RenderSettings.fogColor;
            Color white = deckFog * WhiteTone, dark = deckFog * (StormTone * LayerBright);
            Color wallDark = RenderSettings.fogColor * WallStormTone;
            white.a = 1f;
            dark.a = 1f;
            wallDark.a = 1f;

            if (cloudClear != null)
            {
                bool clear = deckClear;
                Use(layerRenderer, clear ? cloudClear : own);
                if (clear)
                {
                    Color layerWhite = white * LayerBright;
                    layerWhite.a = white.a;
                    cloudClear.SetColor(EmisColorId, Color.Lerp(dark, layerWhite, whiteness));
                }

                // the front's tones by height, their spread about their mean falling far off (FarToneShare)
                float far = range > 0f ? (distance - radius) / range : 0f;
                float spread = Mathf.Lerp(1f, FarToneShare, Mathf.InverseLerp(FarToneFrom, FarToneTo, far));
                for (int i = 0; i < frontShades.Length; i++)
                {
                    frontShades[i] = FrontMean + (FrontBright[i] - FrontMean) * spread;
                }

                for (int i = 0; i < fronts.Count; i++)
                {
                    ToneTier(fronts[i], clear, dark, white, whiteness, frontShades);
                }
            }

            // the wall: near the eye fog-free, dark like the storm outside it and white across it (the user: "before crossing
            // the cloud wall all cloud should be dark, and after crossing cloud wall it should be all white"); farther off
            // the layer's look, and from outside the deck's tone: ramped from its own dark end (x 0.49), just past the full-cloud
            // line its ring read 0.91 x the deck (141 against 155 at depth 0.68), a grey ellipse at about 10° up seen through the gap
            // under the deck's base (the user, 2026-10-26: "there is a dark cloud you can see from outside"; runs 20261026-183757
            // and -191329; gone with the wall hidden, run -182754, whatever the draw order, run -191329)
            bool near = distance < Shape.WallClearSwitch;
            if (wallClear != null)
            {
                bool clear = near || deckClear;
                for (int i = 0; i < walls.Count; i++)
                {
                    ToneTier(walls[i], clear, near ? wallDark : dark, white, whiteness, null);
                }
            }

            UpdateOrder(distance, viewer);
            UpdateSunTint();

            // the game's storms have no cover (the user, 2026-10-27: "cancel the fog dome in vanilla storm if you haven't"): from
            // outside, the sky under their cloud base shows as the game has it, and under the cloud the game's own storm sky
            if (!Shape.HasEye)
            {
                cover?.Show(0f);
                return;
            }

            // the deck's live puffs (it fills as it turns on): the cover is as thick
            float deckShown = Mathf.Clamp01((float)layer.particleCount / Mathf.Max(1, layer.main.maxParticles));
            // outside the full-cloud line the covers take the white deck's tone the moment it turns fog-free (it read 162-168 over
            // the band's 84-86 while the band's tone still ramped, run 20261026-095346)
            float deckOutside = clearness > 0f ? outside : deckClear ? Mathf.Max(outside, StormHorizonFog.WhiteWithin) : 0f;
            StormHorizonFog.DeckTone(deckOutside, clearness, out float deckRendered, out float deckTone, out float deckLift);

            if (cover == null)
            {
                cover = new FogDome("Better Storm " + Shape.Label + " cover", 0f, 0f, false, true);
                cover.Cover(coverTop, coverAlpha, CoverFade);
                cover.CoverBand(coverFrom, coverTo);
            }

            // the cover, as thick as the deck (its live puffs), not drawn where no direction needs it
            float coverShown = deckShown > 0f && UpdateCover(offset, distance, clearness, viewer.y, view.farClipPlane) ? deckShown : 0f;
            cover.Tone = deckTone;
            cover.RenderedTone = deckRendered;
            cover.SunLift = deckLift;
            // in the eye the band over it (StormHorizonFog) reads the colour as the camera renders: the same there, so no step
            cover.BaseFog = clearness > 0f ? (Color?)null : deckFog;
            cover.Show(coverShown);
        }

        // the eye's clearness as shown: toward the distance's by at most 1 / EyeOpenSeconds a second (the game's time)
        private float UpdateClearness(float distance)
        {
            float target = Shape.Clearness(distance);
            shownClearness = shownClearness < 0f
                ? target
                : Mathf.MoveTowards(shownClearness, target, Time.deltaTime / EyeOpenSeconds);
            return shownClearness;
        }

        // the storm cloud's draw order by the viewer's place (WallFudge, LayerFudge, FrontFudgeStep): the wall's and the layer's set when
        // the place changes, the front's sectors every frame
        private void UpdateOrder(float distance, Vector3 viewer)
        {
            UpdateFrontOrder(distance, viewer);
            float wall = distance < Shape.WallEmitOuter ? -WallFudge : WallFudge;
            float deck = distance >= ringOuter ? LayerFudge : -LayerFudge;
            if (wall == wallFudge && deck == layerFudge)
            {
                return;
            }

            wallFudge = wall;
            layerFudge = deck;
            for (int i = 0; i < walls.Count; i++)
            {
                if (walls[i].Renderer != null)
                {
                    walls[i].Renderer.sortingFudge = wall;
                }
            }

            if (layerRenderer != null)
            {
                layerRenderer.sortingFudge = deck;
            }
        }

        // Each front sector sorts as the whole ring would: its fudge takes back its own distance from the view and puts the ring's in (Unity
        // sorts transparent renderers by the distance to their bounds' centre plus the fudge, a lower one drawn later). From outside, with
        // SplitFront, a sector whose puffs lie past the chord between the deck's tangent points (the deck stands between it and the view)
        // draws before the rest of the storm cloud.
        private void UpdateFrontOrder(float distance, Vector3 viewer)
        {
            Vector3 centre = transform.position;
            float toViewX = viewer.x - centre.x, toViewZ = viewer.z - centre.z;
            bool split = SplitFront && distance > ringOuter;
            float chord = split ? ringOuter * ringOuter / distance : 0f;
            for (int i = 0; i < fronts.Count; i++)
            {
                ParticleSystemRenderer renderer = fronts[i].Renderer;
                if (renderer == null)
                {
                    continue;
                }

                Vector3 bounds = renderer.bounds.center;
                float ring = Vector3.Distance(viewer, new Vector3(centre.x, bounds.y, centre.z));
                float fudge = Mathf.Min(FrontFudgeStep * fronts[i].Index, LayerFudge - 1f) + ring - Vector3.Distance(viewer, bounds);
                if (split && ((bounds.x - centre.x) * toViewX + (bounds.z - centre.z) * toViewZ) / distance < chord)
                {
                    fudge += FarFudge;
                }

                renderer.sortingFudge = fudge;
            }
        }

        // The puffs' start colours x the sun's tint taken out (SunNeutral); the live puffs too when it jumps
        private void UpdateSunTint()
        {
            Color tint = Color.white;
            Light sun = RenderSettings.sun;
            if (SunNeutral > 0f && sun != null)
            {
                Color c = sun.color;
                float least = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                float share = SunNeutral * (Sun.sun != null ? 1f - Sun.sun.GetDawnLerp() : 1f);
                if (least > 0.01f)
                {
                    tint = new Color(Mathf.Lerp(1f, least / c.r, share), Mathf.Lerp(1f, least / c.g, share), Mathf.Lerp(1f, least / c.b, share), 1f);
                }
            }

            float moved = Mathf.Max(Mathf.Abs(tint.r - appliedTint.r), Mathf.Max(Mathf.Abs(tint.g - appliedTint.g), Mathf.Abs(tint.b - appliedTint.b)));
            if (moved < TintStep)
            {
                return;
            }

            bool live = moved >= TintRecolour;
            appliedTint = tint;
            ParticleSystem.MainModule main = layer.main;
            ParticleSystem.MinMaxGradient start = saved.StartColor;
            main.startColor = start.mode == ParticleSystemGradientMode.TwoColors
                ? new ParticleSystem.MinMaxGradient(Tinted(start.colorMin, tint), Tinted(start.colorMax, tint))
                : new ParticleSystem.MinMaxGradient(Tinted(start.color, tint));
            if (live)
            {
                Recolour(layer, tint);
            }

            Tint(walls, tint, live);
            Tint(fronts, tint, live);
        }

        private static Color Tinted(Color colour, Color tint)
        {
            return new Color(colour.r * tint.r, colour.g * tint.g, colour.b * tint.b, colour.a);
        }

        private static void Tint(List<Tier> tiers, Color tint, bool live)
        {
            for (int i = 0; i < tiers.Count; i++)
            {
                if (tiers[i].Particles != null)
                {
                    ParticleSystem.MainModule main = tiers[i].Particles.main;
                    main.startColor = Tinted(tiers[i].Colour, tint);
                    if (live)
                    {
                        Recolour(tiers[i].Particles, tint);
                    }
                }
            }
        }

        private static ParticleSystem.Particle[] recolourBuffer;

        // the live puffs' colours set from their system's start colour, as if just born (their alpha kept)
        private static void Recolour(ParticleSystem ps, Color tint)
        {
            int most = ps.main.maxParticles;
            if (recolourBuffer == null || recolourBuffer.Length < most)
            {
                recolourBuffer = new ParticleSystem.Particle[most];
            }

            Color start = ps.main.startColor.color;
            int n = ps.GetParticles(recolourBuffer);
            for (int i = 0; i < n; i++)
            {
                Color32 old = recolourBuffer[i].startColor;
                recolourBuffer[i].startColor = new Color(start.r, start.g, start.b, old.a / 255f);
            }

            ps.SetParticles(recolourBuffer, n);
        }

        // The cover's top and alpha in each of its dome's directions (FogDome.Cover), for a viewer `distance` from the centre
        // (`offset`: the centre from the viewer, level). The bare sky under the deck shows from the horizon up to where the deck's
        // base (DeckBase over the sea, out to the full-cloud line) ends that way, or the camera's far clip cuts it: inside the deck,
        // where each direction leaves it; from outside, toward the deck, under its far side (while its near side is within the far
        // clip); past its sides, as at its tangents, feathered out. A cover that would reach high (by the deck's edge, looking out)
        // fades out, so it is gone as the edge is crossed and the open sky past the storm shows. Run 20261026-001722: facing away
        // from the storm at depths 0.33-0.66 a bright band of sky between the horizon fog's top and the deck's edge; from 1.6
        // white patches at the view's sides past the curtain. Under the deck, out of the eye, the cover takes the whole sky behind
        // the clouds (they draw over it): through the deck's thin single tier the noon sky and the sun's glare showed (6,000 out,
        // 50° up, run 20261026-070049: with the lid hidden 101-190 around the sun, the lid's fading rim let it through); across the
        // wall (`clearness`) it comes down to the far deck's base, so the eye's sky opens, and in the eye it also covers the band
        // behind the wall (coverFrom, coverTo). False when every direction's alpha is 0.
        private bool UpdateCover(Vector3 offset, float distance, float clearness, float viewerHeight, float farClip)
        {
            float edge = ringOuter;
            float height = Mathf.Max(1f, DeckBase - viewerHeight);
            float vx = -offset.x, vz = -offset.z;                       // the viewer from the centre
            float outsideBy = distance * distance - edge * edge;        // over 0 outside the edge
            float tangent = outsideBy > 0f ? Mathf.Sqrt(outsideBy) : 0f;
            float sideHalf = outsideBy > 0f ? Mathf.Asin(Mathf.Clamp01(edge / distance)) * Mathf.Rad2Deg : 180f;
            float toCentre = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
            // in the eye the wall's band from inside, from the deck's base to the top of its tiers (Shape.WallTop) at its outer side
            float wallHeight = Mathf.Max(1f, Shape.WallTop - viewerHeight);
            float insideWall = distance * distance - Shape.WallEmitOuter * Shape.WallEmitOuter;   // under 0 inside the wall's outer side
            if (distance - edge > farClip)
            {
                return false;   // the whole deck past the far clip
            }

            bool any = false;
            for (int i = 0; i < FogDome.Columns; i++)
            {
                float along = vx * FogDome.ColumnX[i] + vz * FogDome.ColumnZ[i];
                float disc = along * along - outsideBy;
                float reach, alpha;
                if (outsideBy <= 0f)
                {
                    reach = -along + Mathf.Sqrt(Mathf.Max(0f, disc));     // inside: where this way leaves the deck
                    alpha = 1f;
                }
                else if (along < 0f && disc > 0f)
                {
                    float root = Mathf.Sqrt(disc);
                    reach = -along + root;                              // toward the deck: under its far side
                    alpha = WithinClip(-along - root, farClip);         // its near side drawn
                }
                else
                {
                    reach = tangent;                                    // past its sides: as at its tangents
                    float around = 360f * i / FogDome.Segments;         // the column's angle, degrees from +x toward +z
                    float past = Mathf.Abs(Mathf.DeltaAngle(around, toCentre)) - sideHalf;
                    alpha = WithinClip(tangent, farClip) *
                        (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(CoverSideFull, CoverSideFull + CoverSideFeather, past)));
                }

                float top = Mathf.Atan2(height, Mathf.Clamp(reach, 1f, farClip)) * Mathf.Rad2Deg;
                // under the deck the whole sky (the user's pick, 2026-10-26: "whole-sky cover"): covered only up to the far base, as
                // from outside, the bare noon sky showed white through the deck's gaps and down the eye's column (run 20261026-223913:
                // those patches turned magenta with the sky cleared to magenta, and stayed with the weather's clouds, the layer or the
                // front hidden); an overcast lighter than the deck drew the eye wall's dark ring from the core (run -225305)
                coverTop[i] = outsideBy <= 0f ? Mathf.Lerp(90f, top + CoverMargin, clearness) : top + CoverMargin;
                // in the eye also behind the wall, from its base to its tiers' top that way: the bare sky showed through the wall's thin
                // puffs as blue streaks (the user, 2026-10-26: "You can still see there is gap in the cloud wall"; run 20261026-191952,
                // from the game storm's centre 27-37° up, blue over red +21 to +26 against the wall's +13). Not under the wall's base:
                // covered down to the horizon, the deck's underside turned the cover's flat grey too (run 20261026-195215: 158-170, blue
                // over red +13, where it read 181-188, -5 to -8)
                float wallReach = insideWall < 0f ? -along + Mathf.Sqrt(along * along - insideWall) : 0f;
                coverFrom[i] = insideWall < 0f ? Mathf.Atan2(height, wallReach) * Mathf.Rad2Deg : 0f;
                coverTo[i] = insideWall < 0f ? Mathf.Atan2(wallHeight, wallReach) * Mathf.Rad2Deg : 0f;
                coverAlpha[i] = alpha * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(CoverHighFrom, CoverHighTo, top)));
                any |= coverAlpha[i] > 0f;
            }

            return any;
        }

        // 1 for a point well within the camera's far clip, 0 past it
        private static float WithinClip(float reach, float farClip)
        {
            return Mathf.InverseLerp(farClip, farClip * 0.8f, reach);
        }

        // a tier's look: its own fog-free copy in its shade of white where the clouds are toned (or fog-free by the eye), else the
        // layer's own fogged material
        private void ToneTier(Tier tier, bool clear, Color dark, Color white, float whiteness, float[] brights)
        {
            bool own = !clear || tier.Clear == null;
            Use(tier.Renderer, own ? this.own : tier.Clear);
            if (!own)
            {
                tier.Clear.SetColor(EmisColorId, Color.Lerp(dark, Shaded(white, tier.Index, brights), whiteness));
            }
        }

        // `brights`: the shades by tier from the lowest up (null: TierBright)
        private static Color Shaded(Color white, int index, float[] brights)
        {
            float[] shades = brights ?? TierBright;
            float bright = shades[Mathf.Clamp(index, 0, shades.Length - 1)];
            float neutral = TierNeutral[Mathf.Clamp(index, 0, TierNeutral.Length - 1)];
            float grey = white.grayscale;
            Color shaded = Color.Lerp(white, new Color(grey, grey, grey, white.a), neutral) * bright;
            shaded.a = white.a;
            return shaded;
        }

        private static void Use(Renderer renderer, Material material)
        {
            if (renderer != null && renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void SetEmission(List<Tier> tiers, bool enabled)
        {
            for (int i = 0; i < tiers.Count; i++)
            {
                ParticleSystem ps = tiers[i].Particles;
                if (ps != null)
                {
                    ParticleSystem.EmissionModule emission = ps.emission;
                    if (emission.enabled != enabled)
                    {
                        emission.enabled = enabled;
                    }
                }
            }
        }

        private static void Prewarm(ParticleSystem ps, float seconds)
        {
            if (ps != null)
            {
                ps.Simulate(seconds, false, false, false);
                ps.Play(false);
            }
        }

        // Switched off with its storm (the config's "Enable Hurricane"): the cover is an object of its own, drawn for every camera,
        // so it stayed on screen at its last alpha once LateUpdate stopped; and switched on again, the storm cloud is filled at once
        // (LateUpdate's prewarm), not over its puffs' minute of life
        private void OnDisable()
        {
            cover?.Show(0f);
            emitting = false;
        }

        private void OnDestroy()
        {
            // by reference: the storm may be destroyed in the same teardown, and its entry must go all the same
            if ((object)storm != null && eyes.TryGetValue(storm, out StormEye registered) && registered == this)
            {
                eyes.Remove(storm);
            }

            cover?.Shutdown();
            DestroyTiers(walls);
            DestroyTiers(fronts);
            if (cloudClear != null)
            {
                Destroy(cloudClear);
            }

            if (wallClear != null)
            {
                Destroy(wallClear);
            }
        }

        private static void DestroyTiers(List<Tier> tiers)
        {
            for (int i = 0; i < tiers.Count; i++)
            {
                if (tiers[i].Particles != null)
                {
                    Destroy(tiers[i].Particles.gameObject);
                }

                if (tiers[i].Clear != null && tiers[i].Sector == 0)
                {
                    Destroy(tiers[i].Clear);
                }
            }

            tiers.Clear();
        }
    }

    // The eye's wind: only the target is changed (the base wind, saved with the game, never). Entering or leaving the
    // eye sets the new target that frame and rolls a fresh gust (and on leaving a fresh target) at once; the wind then
    // moves to it at the lerp speed in force (the user, 2026-10-25).
    internal static class StormEyeWind
    {
        private static Vector3 normalTarget;   // the target as the game and every other mod rolled it

        internal static bool InEye { get; private set; }

        internal static void Tick()
        {
            Wind wind = Wind.instance;
            if (wind == null)
            {
                InEye = false;
                return;
            }

            bool inEye = StormInfluenceService.TryGetCurrent(out StormInfluence influence) &&
                influence.Eye != null &&
                influence.Eye.InEye(influence.CenterDistance, InEye);
            if (!inEye)
            {
                if (InEye)
                {
                    Exit(wind);
                }
                return;
            }

            if (!InEye)
            {
                InEye = true;
                normalTarget = DoldrumsWind.WindTarget(wind);
                Apply(wind, influence);
                StormAccess.GustTimer(wind) = 0f;   // a fresh gust from the eye's target next frame
                return;
            }

            Apply(wind, influence);
        }

        // SetNewWindTarget postfix, after every other mod
        internal static void AfterRoll(Wind wind)
        {
            if (!InEye)
            {
                return;
            }

            normalTarget = DoldrumsWind.WindTarget(wind);
            if (StormInfluenceService.TryGetCurrent(out StormInfluence influence) && influence.Eye != null)
            {
                Apply(wind, influence);
                DoldrumsWind.SetNewGustTarget?.Invoke(wind, null);
            }
        }

        internal static void RestoreImmediately()
        {
            Wind wind = Wind.instance;
            if (InEye && wind != null)
            {
                Exit(wind);
            }

            InEye = false;
        }

        private static void Apply(Wind wind, StormInfluence influence)
        {
            if (WindTotemCompatibility.OrbControlsWind)
            {
                DoldrumsWind.WindTarget(wind) = normalTarget;
                return;
            }

            Vector3 direction = normalTarget.sqrMagnitude > 0.0001f ? normalTarget : Wind.currentBaseWind;
            direction.y = 0f;
            DoldrumsWind.WindTarget(wind) = direction.sqrMagnitude > 0.0001f
                ? direction.normalized * influence.Eye.EyeWind(influence.CenterDistance)
                : normalTarget;
        }

        private static void Exit(Wind wind)
        {
            DoldrumsWind.WindTarget(wind) = normalTarget;
            StormAccess.WindTimer(wind) = 0f;   // the game rolls a fresh storm target next frame,
            StormAccess.GustTimer(wind) = 0f;   // and a gust from it
            InEye = false;
        }
    }

    [HarmonyPatch(typeof(Wind), "SetNewWindTarget")]
    internal static class StormEyeWindTargetPatch
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
            StormEyeWind.AfterRoll(__instance);
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
        // its storm cloud (fog-free) and aurora are seen out to the camera's far clip (21,500), even near land (the user, 2026-10-25)
        internal const float CloudDistance = 21500f;
        // its storm cloud wraps the aurora (the user: "the storm for mist should be bigger than aurora, so you can see aurora
        // going in and out of the storm"): a disc inside the ring and a ring just outside it, low enough that the curtains'
        // tops (1,700-2,800) stand over it (probe 7, 2026-10-25: 4,200 puffs 1,250 deep hid the curtains whole). The ring
        // hugs the curtains (3,420-3,700 with their folds and lean): its puffs' visible edge ends about 500 past the circle
        // (the user: "storm match with aurora width, storm only goes out a little bit of aurora"; it reached 4,900 before)
        internal const float CloudBottom = 200f;
        internal const float CloudDepth = 900f;
        internal const float CloudPuffMin = 400f;
        internal const float CloudPuffMax = 1000f;
        internal const float CloudInnerRadius = 3350f;
        internal const int CloudInnerPuffs = 1600;
        internal const float CloudRingInner = 3600f;
        internal const float CloudRingOuter = 3850f;
        internal const int CloudRingPuffs = 350;
        internal const float WanderSpeed = 20f;                // about 38.9 knots (the user, 2026-10-27: "speed 20"; 18 from 2026-10-25, 16 before)
        // 16 knots (the user, 2026-10-24) = 8.23 units/s; the game's Speedometer: knots = speed x 1.944
        internal const float FollowSpeed = 16f / 1.944f;
        // how long it follows: the config "Follow Time", one of these (the user, 2026-10-25)
        internal const int DefaultFollowHours = 48;
        internal static readonly int[] FollowHourOptions = { 12, 24, 48, 72 };
        internal const double CooldownHours = 168.0;
        internal const float RainPeak = 3f;
        internal const float RainStartNormalized = 0.66f;
        internal const float RainFullNormalized = 0.33f;
        internal const float MaxWanderDegrees = 150f;
        internal const float SpinDegreesPerSecond = 120f;
        internal const float SpinBlendDistance = 200f;
        internal const float CoreCloudMultiplier = 2f;
        internal const float FogStart = OuterEdge;             // the fog starts at the outer edge
        // the user, 2026-10-27: "lower to 0.25 and release as 1.2.1 again" (0.3 from 2026-10-25, 0.35 first; 0.2 is the game's thickest)
        internal const float MistFogPeak = 0.25f;
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

        // linear in m (the user, 2026-10-27: "make the instrument interference more severe before reaching core radius"; m x m before)
        internal static float WanderAmplitude(float m)
        {
            return m * MaxWanderDegrees;
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
        private static bool wasInside;      // inside the core: the capture line (the user, 2026-10-25; the outer edge before)
        private static bool syncInside = true;

        internal static bool IsFollowing { get; private set; }

        internal static double FollowHours
        {
            get
            {
                BetterStormPlugin plugin = BetterStormPlugin.Instance;
                return plugin != null && plugin.MistFollowHours != null
                    ? plugin.MistFollowHours.Value
                    : MagneticMistRules.DefaultFollowHours;
            }
        }

        internal static void UpdateMovement(ModStormController controller, Transform player)
        {
            Transform mist = controller.transform;
            Vector3 toPlayer = player.position - mist.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            // it captures only on crossing its core inward; in the band it wanders
            bool insideNow = controller.Storm.active && distance < MagneticMistRules.CoreRadius;
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
                // the other storms looked at only when all else allows a capture (as an argument, a loop over every storm each frame)
                if (haveClock &&
                    MagneticMistRules.ShouldCapture(
                        wasInside,
                        insideNow,
                        now,
                        cooldownUntil,
                        false) &&
                    !AnyOtherStormAffectsPlayer(controller, player))
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

                // the follow time keeps running while paused
                if (haveClock && now - followStart >= FollowHours)
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

        // Debug "Push to outer edge": the follow ends, without a cooldown. The player, now at the outer
        // edge, is outside the core, so a capture needs a real crossing of the core later or a Summon.
        internal static void DebugPushedToEdge()
        {
            IsFollowing = false;
            pauseUntil = 0.0;
            returning = false;
            leaving = false;
            wasInside = false;
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

            string left = (FollowHours - (now - followStart)).ToString("0.0") + " h left";
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

    // A fog-coloured dome around each camera as it renders (MIST_HORIZON_DRAFT.md): the sky, the upper clouds (LitCloudsNoFog)
    // and far land take no fog, so a bright horizon stays whatever the fog. Drawn after the sky, the sea and the weather's clouds
    // (queue 2999, centred on the view so it sorts last among them), before the storm clouds (3000) and the rain (3001). Its alpha
    // by elevation: from 0 where it meets the sea up to 1 at the horizon (the user's picture, 2026-10-25, showed a line at that
    // join, the sea in front of the dome a few levels off its colour), 1 up to `fullTo` degrees, fading to 0 at `fadeTo`.
    // The fog colour each camera last rendered with. A mod may change RenderSettings.fogColor after Better Storm reads it as a
    // camera renders (Camera.onPreCull): the user's Dizzy Gamma multiplies it by their gamma (0.53) in Camera.onPreRender, last
    // in that list, once a frame, and doesn't put it back. Read again as each camera finishes, it is the colour the fogged clouds
    // far off were drawn in; a frame late, which the weather's slow change doesn't show.
    // The sun's light on the white storm cloud, which the flat covers (fog domes, the rain curtain) under it don't get: seen with
    // the sun behind the viewer the cloud's sunlit side reads brighter. Runs 20261026-095346 and -100406 (the sun 62° and 44° up,
    // 168-176° off the view's heading): the cloud base 8 % and 17 % over the curtain, about 1 + 0.2 x the sun's light from behind.
    internal static class StormSun
    {
        // toward the sun, x its light's intensity (at most 1); zero without a sun
        internal static Vector3 ToSun()
        {
            Light sun = RenderSettings.sun;
            return sun != null && sun.isActiveAndEnabled ? -sun.transform.forward * Mathf.Clamp01(sun.intensity) : Vector3.zero;
        }

        // how much of the sun lights a cloud seen that way (x, z: a level direction): 0 facing the sun or with it overhead, up to
        // 1 with it low behind the viewer
        internal static float Front(Vector3 toSun, float x, float z)
        {
            return Mathf.Max(0f, -(toSun.x * x + toSun.z * z));
        }
    }

    internal static class RenderFog
    {
        private static readonly Dictionary<Camera, Color> rendered = new Dictionary<Camera, Color>();
        private static readonly List<Camera> gone = new List<Camera>();
        private static bool hooked;

        internal static void Hook()
        {
            if (!hooked)
            {
                Camera.onPostRender += OnPostRender;
                hooked = true;
            }
        }

        // the colour `camera` last rendered with; the colour now while it has not rendered yet
        internal static Color Of(Camera camera)
        {
            return camera != null && rendered.TryGetValue(camera, out Color colour) ? colour : RenderSettings.fogColor;
        }

        private static void OnPostRender(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            if (rendered.Count > 16 && !rendered.ContainsKey(camera))
            {
                // cameras destroyed since (a free camera, a scene change)
                foreach (Camera key in rendered.Keys)
                {
                    if (key == null)
                    {
                        gone.Add(key);
                    }
                }

                for (int i = 0; i < gone.Count; i++)
                {
                    rendered.Remove(gone[i]);
                }

                gone.Clear();
            }

            rendered[camera] = RenderSettings.fogColor;
        }

        internal static void Shutdown()
        {
            if (hooked)
            {
                Camera.onPostRender -= OnPostRender;
                hooked = false;
            }

            rendered.Clear();
        }
    }

    internal sealed class FogDome
    {
        private const float Radius = 300f;               // the mist's: inside the far clip (21,500); its fog hides everything there
        // a storm's cover and horizon fog at the camera's far clip instead (x FarShare), behind every island, ship and sea within it,
        // so only the bare sky shows them. At 300 they hid everything past 300 m: under the storm cloud a flat grey from the far sea up
        // (the user, 2026-10-26, after testing 1.2.1 in game: "Inside the hurricane affect area it's very foggy, did you change
        // anything? before the visibility is better ... If it's from you fog dome, don't let it cover so low")
        private const float FarShare = 0.95f;
        internal const int RenderQueue = 2999;           // the clouds' queue less one: drawn before them
        // the legacy particle shader applies the scene's fog while it draws, so the dome is the fog colour at draw time
        // (with all the user's mods a mod changed RenderSettings.fogColor after OnPreCull: the dome 14 levels darker than the sea,
        // run 20261025-022019); Sprites/Default (no fog) only if it is missing
        internal const string ShaderName = "Legacy Shaders/Particles/Alpha Blended";
        private const string FallbackShaderName = "Sprites/Default";
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        internal const int Segments = 96;                // around the horizon: 3.75° a column for a cover set by direction
        internal const int Columns = Segments + 1;       // the last one on the first
        // each column's level direction (BuildMesh's angle `around`): x and z
        internal static readonly float[] ColumnX = ColumnAxis(true);
        internal static readonly float[] ColumnZ = ColumnAxis(false);
        // ring elevations in degrees, dense just under the horizon where the ramp lies (a deck 1-20 above the sea: 0.2-3.8°), and
        // up to the zenith for a cover's top (Cover)
        private static readonly float[] Rings =
        {
            -90f, -60f, -30f, -15f, -8f, -5f, -3.5f, -2.5f, -1.8f, -1.3f, -1f, -0.75f, -0.5f, -0.35f, -0.2f, -0.1f, 0f,
            0.5f, 1f, 2f, 3f, 4f, 6f, 8f, 11f, 15f, 20f, 25f, 30f, 35f, 40f, 45f, 50f, 60f, 70f, 80f, 90f
        };
        // a cover's colours are made again when a column's top moves more than this many degrees, or its alpha this much
        private const float CoverStep = 0.25f;
        private const float CoverAlphaStep = 0.02f;
        private static PropertyInfo crestInstance;
        private static PropertyInfo crestSeaLevel;
        private static bool crestLooked;

        private readonly string name;
        private readonly float fullTo;
        private readonly float fadeTo;
        private readonly bool fogged;
        private bool atFarClip;                          // not readonly: the QA's pictures set it back to 300 for a before shot
        private GameObject dome;
        private Mesh mesh;
        private Material material;
        private MeshRenderer domeRenderer;
        private Color[] colours;
        private float rampDegrees = -1f;                 // the join's depth below the horizon the colours were made for
        // the cover by direction (Cover), the caller's arrays, and the tops and alphas the colours were made for
        private float[] coverTop, coverAlpha;
        private float coverFade = 1f;
        private float[] builtTop, builtAlpha;
        // a second covered stretch by direction (CoverBand), and the stretches the colours were made for
        private float[] bandFrom, bandTo;
        private float[] builtFrom, builtTo;
        private readonly float[] lit = new float[Columns];
        private bool hooked;
        private bool shaderMissingLogged;
        private bool usesTint;
        private float seaLevel;

        // fogged: drawn with the fogged particle shader (the fog colour as the scene draws it); otherwise Sprites/Default, the
        // fog colour as read before each camera renders, x Tone. atFarClip: at the camera's far clip (a storm's), else 300 (the mist's)
        internal FogDome(string name, float fullTo, float fadeTo, bool fogged, bool atFarClip = false)
        {
            this.name = name;
            this.fullTo = fullTo;
            this.fadeTo = fadeTo;
            this.fogged = fogged;
            this.atFarClip = atFarClip;
        }

        internal float Alpha { get; private set; }

        // its colour: the fog colour read as each camera renders x Tone, plus the fog colour that camera rendered with x RenderedTone
        // (RenderFog; a mod may change the fog colour after the dome reads it)
        internal float Tone { get; set; } = 1f;

        internal float RenderedTone { get; set; }

        // the fog colour Tone is taken of, when the owner gives one (the white deck's, StormEye.deckFog); else the one read as each
        // camera renders
        internal Color? BaseFog { get; set; }

        // the sun's light on the cloud the dome stands for (StormSun): column i x (1 + SunLift x StormSun.Front that way); its
        // colours made again when the sun moves more than SunStep or SunLift by SunLiftStep
        internal float SunLift { get; set; }

        private const float SunStep = 0.02f;
        private const float SunLiftStep = 0.01f;
        private Vector3 builtSun;
        private float builtLift = -1f;

        // A cover by direction instead of the band (fullTo, fadeTo): column i (BuildMesh's angle `around`, 360 i / Segments degrees
        // from +x toward +z) full up to top[i] degrees, gone `fade` degrees higher, x alpha[i]. The arrays (Columns long) stay the
        // caller's: it changes them in place, and the colours follow as each camera renders.
        internal void Cover(float[] top, float[] alpha, float fade)
        {
            coverTop = top;
            coverAlpha = alpha;
            coverFade = Mathf.Max(0.01f, fade);
        }

        // With a cover (Cover), column i also full from from[i] to to[i] degrees (none where to[i] <= from[i]), faded over the cover's
        // `fade` either side, x the cover's alpha[i]. The arrays stay the caller's, as Cover's.
        internal void CoverBand(float[] from, float[] to)
        {
            bandFrom = from;
            bandTo = to;
        }

        // shown at `alpha` this frame (0 hides it); made the first time it is needed, and again after a scene change destroyed it
        internal void Show(float alpha)
        {
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

        private bool Create()
        {
            Shutdown();
            Shader shader = (fogged ? Shader.Find(ShaderName) : null) ?? Shader.Find(FallbackShaderName);
            if (shader == null)
            {
                if (!shaderMissingLogged)
                {
                    shaderMissingLogged = true;
                    BetterStormPlugin.Instance?.LogFeatureWarning(
                        "Shaders " + ShaderName + " and " + FallbackShaderName + " not found; no " + name + ".");
                }
                return false;
            }

            mesh = BuildMesh(name);
            colours = new Color[mesh.vertexCount];
            rampDegrees = -1f;
            dome = new GameObject(name);
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

            Camera.onPreCull += OnPreCull;
            hooked = true;
            RenderFog.Hook();
            return true;
        }

        private static float[] ColumnAxis(bool x)
        {
            var axis = new float[Columns];
            for (int i = 0; i < Columns; i++)
            {
                float around = 2f * Mathf.PI * i / Segments;
                axis[i] = x ? Mathf.Cos(around) : Mathf.Sin(around);
            }

            return axis;
        }

        // a unit sphere in latitude rings (Rings) and Segments around
        private static Mesh BuildMesh(string name)
        {
            var vertices = new Vector3[Rings.Length * Columns];
            for (int ring = 0; ring < Rings.Length; ring++)
            {
                float elevation = Rings[ring] * Mathf.Deg2Rad;
                for (int i = 0; i < Columns; i++)
                {
                    float around = 2f * Mathf.PI * i / Segments;
                    vertices[ring * Columns + i] = new Vector3(
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
                    int a = ring * Columns + i, b = a + 1, c = a + Columns, d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
            }

            var built = new Mesh { name = name, vertices = vertices, triangles = triangles };
            built.RecalculateBounds();
            return built;
        }

        private bool CoverMoved()
        {
            if (coverTop == null)
            {
                return false;
            }

            if (builtTop == null)
            {
                return true;
            }

            for (int i = 0; i < Columns; i++)
            {
                if (Mathf.Abs(coverTop[i] - builtTop[i]) > CoverStep || Mathf.Abs(coverAlpha[i] - builtAlpha[i]) > CoverAlphaStep)
                {
                    return true;
                }

                if (bandFrom != null && (builtFrom == null || Mathf.Abs(bandFrom[i] - builtFrom[i]) > CoverStep || Mathf.Abs(bandTo[i] - builtTo[i]) > CoverStep))
                {
                    return true;
                }
            }

            return false;
        }

        // vertex alpha: 0 at and under the dome's join with the sea (`ramp` degrees below the horizon), up to 1 at the horizon;
        // above it 1 up to fullTo, down to 0 at fadeTo, or with a cover (Cover) each column's own top and alpha
        private void SetRamp(float ramp)
        {
            bool covered = coverTop != null;
            // each column's share of the brightest (the material carries 1 + SunLift)
            Vector3 sun = StormSun.ToSun();
            for (int i = 0; i < Columns; i++)
            {
                lit[i] = (1f + SunLift * StormSun.Front(sun, ColumnX[i], ColumnZ[i])) / (1f + SunLift);
            }

            for (int ring = 0; ring < Rings.Length; ring++)
            {
                float e = Rings[ring];
                float join = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((e + ramp) / ramp));
                float band = e < 0f ? join : e <= fullTo ? 1f : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fullTo, fadeTo, e));
                for (int i = 0; i < Columns; i++)
                {
                    float a = band;
                    if (covered)
                    {
                        float top = coverTop[i];
                        a = coverAlpha[i] * (e < 0f ? join : e <= top ? 1f : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(top, top + coverFade, e)));
                        if (bandFrom != null && bandTo[i] > bandFrom[i])
                        {
                            float inBand = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(bandFrom[i] - coverFade, bandFrom[i], e)) *
                                (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(bandTo[i], bandTo[i] + coverFade, e)));
                            a = Mathf.Max(a, coverAlpha[i] * inBand);
                        }
                    }

                    float c = lit[i];
                    colours[ring * Columns + i] = new Color(c, c, c, a);
                }
            }
            mesh.colors = colours;
            rampDegrees = ramp;
            builtSun = sun;
            builtLift = SunLift;
            if (covered)
            {
                builtTop = builtTop ?? new float[Columns];
                builtAlpha = builtAlpha ?? new float[Columns];
                Array.Copy(coverTop, builtTop, Columns);
                Array.Copy(coverAlpha, builtAlpha, Columns);
                if (bandFrom != null)
                {
                    builtFrom = builtFrom ?? new float[Columns];
                    builtTo = builtTo ?? new float[Columns];
                    Array.Copy(bandFrom, builtFrom, Columns);
                    Array.Copy(bandTo, builtTo, Columns);
                }
            }
        }

        // every camera, as it renders (a free camera too; Camera.main is never cached)
        internal void OnPreCull(Camera camera)
        {
            if (domeRenderer == null || !domeRenderer.enabled || camera == null)
            {
                return;
            }

            Vector3 view = camera.transform.position;
            dome.transform.position = view;
            float radius = atFarClip ? camera.farClipPlane * FarShare : Radius;
            dome.transform.localScale = Vector3.one * radius;
            // where the dome meets the sea, seen from the view: asin(height / radius) under the horizon
            float height = Mathf.Clamp(view.y - seaLevel, 0.5f, radius * 0.5f);
            float ramp = Mathf.Asin(height / radius) * Mathf.Rad2Deg;
            if (Mathf.Abs(ramp - rampDegrees) > rampDegrees * 0.05f || CoverMoved() ||
                Mathf.Abs(SunLift - builtLift) > SunLiftStep || (SunLift > 0f && (StormSun.ToSun() - builtSun).sqrMagnitude > SunStep * SunStep))
            {
                SetRamp(ramp);
            }

            Color fog = ((BaseFog ?? RenderSettings.fogColor) * Tone + RenderFog.Of(camera) * RenderedTone) * (1f + SunLift);
            if (usesTint)
            {
                // the particle shader draws 2 x _TintColor x the vertex colour, then fogs it; the tint only shows where the fog
                // is thin
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

        internal void Shutdown()
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
            builtTop = null;
            builtAlpha = null;
            builtLift = -1f;
            usesTint = false;
        }
    }

    // MIST_HORIZON_DRAFT.md: in the mist's core a sea line stays whatever its density. The whole dome (the sky covered from the
    // horizon up), with the mist's fog curve as its alpha (0 at 7,000, 1 at the core radius).
    internal static class MistFogDome
    {
        internal const string ShaderName = FogDome.ShaderName;
        private static readonly FogDome dome = new FogDome("Better Storm Mist Fog Dome", 90f, 90f, true);

        internal static float Alpha
        {
            get { return dome.Alpha; }
        }

        internal static void Tick()
        {
            // the same condition and curve as the mist's fog (WeatherSkyModifiers.Apply)
            dome.Show(StormInfluenceService.TryGetCurrent(out StormInfluence influence) && influence.IsMagneticMist
                ? MagneticMistRules.FogFactor(influence.CenterDistance)
                : 0f);
        }

        // the dome's work for each camera (the QA's cost suite times it under this name)
        private static void OnPreCull(Camera camera)
        {
            dome.OnPreCull(camera);
        }

        internal static void Shutdown()
        {
            dome.Shutdown();
        }
    }

    // A storm's horizon (the user after probe 15: "In your test on horizon, above the sea surface, there is a line of white
    // fog/cloud, please disable that when you're affect by storm, or make it covered up by fog/storm cloud, or else it looks
    // weird"): the line is the bare sky under the storm cloud's base (fog never covers the sky; probe 15's pictures). A fog dome
    // over the horizon only, under the storm clouds (they draw after it), in the eye of a storm that controls the weather. Full up
    // to 20° and gone by 30°, so it also covers the sunset glowing through the soft cloud base at 12-20° up in the eye (run
    // 20261025-230612: the band stayed with the layer and the front hidden). Out of the eye each storm's own cover does it by
    // direction (StormEye.UpdateCover): the band all round left a dark strip under the open sky past the storm's edge, facing out
    // at the full-cloud line (run 20261026-071225). Its tone is the cloud base's (DeckTone; at the fog colour x 0.6 inside the storm and x 1 in the eye the
    // user saw it: "from eye picture you can still see the white line at the horizon"); fog-free, so the tone isn't fogged back up.
    internal static class StormHorizonFog
    {
        private const float FullTo = 20f;
        private const float FadeTo = 30f;
        // the storm cloud's base as a part of the fog colour (EYE_LAYER_DRAFT.md §5.5, run 20261025-231710 at noon: the band at 0.6
        // inside the storm and 1 in the eye was 1.16-1.19 x the cloud base above it). The fogged deck far off reads the fog colour
        // the camera renders with (RenderFog), the fog-free deck the colour before it, from which its emission is set
        // (StormEye.UpdateLook): with the user's Dizzy Gamma at 0.53 the deck read 0.51 x the colour before, about 1 x as rendered.
        // the fogged deck inside the storm (depths 0.33 and 0.66), x the rendered fog: at 0.96 the band read 4-6 % under the deck
        // in runs 20261026-122841, -124317 and -125538 (55 under 57-59, 84-86 under 89-90)
        internal const float FoggedDeckTone = 1f;
        // the storm by the eye seen from outside it, the band's tone as it ramps into the eye (DeckTone): first set for the eye's lid
        // (removed after 1.2.2, EYE_LAYER_DRAFT.md §5.8), which read 28-34 at 2,650 out at 0.6 (noon, run 20261026-000025, the wall
        // hidden)
        internal const float EyeStormTone = 0.6f;
        // the fog-free deck where the clouds turn white (the eye), for the base at a white end of 1 (StormEye.LayerBright). Swept in
        // the eye in run 20261026-125538 (the sun 58° up): facing the sun the deck 160-162, the band 158 / 173 / 186 at 0.62 / 0.69 /
        // 0.76; with the sun behind (StormSun.Front 0.53, a lift of 0.6) the deck 184-189, the band 174 / 188 / 198. Run -124317
        // read 177-178 at 0.76 facing the sun, run -130651 (47° up) 158 / 171 / 183 at 0.6 / 0.66 / 0.72 over 160-161 (the same tone
        // reads up to about 6 % apart between runs), run -131341 (45° up) 158-161 / 170-172 / 184-186 at 0.6 / 0.65 / 0.72 over
        // 160-161, run -131907 (61° up) 153-157 / 166-170 / 180-183 at 0.56 / 0.62 / 0.68 over 160-161. So 0.6, about the mean of
        // the four sweeps' fits (0.635, 0.61, 0.6, 0.584), with a lift of 0.85 (with the sun behind: run -130651 0.66 x 1.48 read
        // 190-193 under a deck of 195-196, run -131341 0.65 x 1.53 192-194 over 188-191, run -131907 0.62 x 1.4 185-188 over 179)
        internal static float WhiteDeckTone = 0.6f;   // not const: the QA's sun-side runs sweep it
        internal const float WhiteWithin = 0.1f;       // outside the full-cloud line the deck reads white by this whiteness
        // the white deck seen from outside the storm with no sun from behind it (its shaded face), and its lift with the sun behind
        // the viewer (x StormSun.Front). The tone swept at depth 1.0, facing the sun: the base 160-162 and the cover 154 / 176 at
        // 0.6 / 0.7 (run 20261026-122841, the sun 55° up; the same with the game's horizon fog hidden), 153 / 170 / 187 at 0.55 /
        // 0.62 / 0.7 (-124317, 51° up), 152 / 164 / 175 at 0.55 / 0.6 / 0.65 (-125538, 58° up), 157 / 169 / 180 (-130651, 47° up),
        // 156 / 168 / 180 (-131341, 45° up): the fits 0.627, 0.587, 0.585, 0.565, 0.567, so 0.59 (-5 to +3.5 %). With the
        // sun behind the cover read 1-5 % under the base at a lift of 0.8 (Front 0.46-0.62, runs -124317 and -125538), so 0.9;
        // with the sun 34° up (Front 0.83) 0.6 x 1.75 reads as the 0.7 x 1.5 that matched in run -104719.
        internal static float OutsideWhiteTone = 0.59f;   // not const: the QA's sun-side runs sweep it
        internal const float OutsideSunLift = 0.9f;
        internal const float EyeSunLift = 0.85f;
        private const float EyeToneWithin = 0.2f;      // across the wall the deck takes the eye's tone by this clearness
        private static readonly FogDome dome = new FogDome("Better Storm storm horizon fog", FullTo, FadeTo, false, true);

        internal static float Alpha
        {
            get { return dome.Alpha; }
        }

        internal static void Tick()
        {
            float alpha = 0f;
            WeatherStorms storms = WeatherStorms.instance;
            if (StormInfluenceService.TryGetCurrent(out StormInfluence influence) && influence.Eye != null)
            {
                float cloudyBorder = storms != null ? StormAccess.CloudyBorder(storms) : 0.66f;
                alpha = Mathf.InverseLerp(0f, EyeToneWithin, influence.EyeClearness);
                DeckTone(Mathf.InverseLerp(cloudyBorder, 1f, influence.NormalizedDistance), influence.EyeClearness, out float rendered, out float before, out float sunLift);
                dome.RenderedTone = rendered;
                dome.Tone = before;
                dome.SunLift = sunLift;
            }

            dome.Show(alpha);
        }

        // the cloud base's tone for a viewer outside the full-cloud line (`outside`: the clouds' whiteness there, 0 at the line, 1
        // at the outer edge) or in the eye (`clearness`), in two parts: x the fog colour as rendered (`rendered`, the fogged deck)
        // and x the colour before (`before`, the fog-free deck). The deck swaps to its fog-free copy as soon as either is over 0
        // (StormEye.UpdateLook); outside, under the open sky's light, it reads white at once; in the eye its light clears with the
        // clearness (StormEyeWeather). `sunLift`: the sun's lift on it (StormSun), on its white, fog-free part only (the fogged deck
        // far off reads the fog colour, unlit).
        internal static void DeckTone(float outside, float clearness, out float rendered, out float before, out float sunLift)
        {
            float white = Mathf.InverseLerp(0f, WhiteWithin, outside);
            rendered = FoggedDeckTone * (1f - white);
            before = OutsideWhiteTone * white;
            float lift = OutsideSunLift;
            if (clearness > 0f)
            {
                // into the eye's tone over its first fifth (it was a step at the clearing's edge; the cover now takes the whole sky)
                float eye = Mathf.InverseLerp(0f, EyeToneWithin, clearness);
                rendered *= 1f - eye;
                before = Mathf.Lerp(before, Mathf.Lerp(EyeStormTone, WhiteDeckTone, clearness), eye);
                lift = Mathf.Lerp(lift, EyeSunLift, eye);
            }

            sunLift = rendered + before > 0f ? lift * before / (rendered + before) : 0f;
        }

        internal static void Shutdown()
        {
            dome.Shutdown();
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
        // A storm's eye opens over the last 50 inside its edge (EYE_LAYER_DRAFT.md §5.7), about 10 s at 10 knots: the storm's clouds
        // left under the opening read dark grey in the eye's sky (sailing in at 5 a second, run 20261026-130651: 594 lower and 1,194
        // upper alive at 2,350 with the cap going from their life to 10 s across the opening; none at 2,300). So they are gone
        // within EyeCloudFade s once the eye is half open.
        private const float EyeCloudsGoneBy = 0.5f;
        private const float EyeCloudFade = 3f;

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

            if (inStorm && influence.EyeClearness > 0f)
            {
                // a storm's eye: its sky cleared like High Pressure's, the clouds already up faded out
                float clouds = 1f - influence.EyeClearness;
                Scale(lowerClouds, clouds);
                Scale(upperClouds, clouds);
                if (Time.time >= nextCloudFade)
                {
                    nextCloudFade = Time.time + CloudFadeInterval;
                    float left = Mathf.InverseLerp(EyeCloudsGoneBy, 0f, influence.EyeClearness);
                    FadeLiveClouds(lowerClouds, left, EyeCloudFade);
                    FadeLiveClouds(upperClouds, left, EyeCloudFade);
                }

                // their colour from the storm's straight to the clear weather's across the wall (the game's blend passes
                // through the rain and cloudy sets' reddish greys: dark clouds beside the white wall, probe 9)
                if (lowerClouds != null && region != null && region.stormWeather != null && region.clearWeather != null)
                {
                    ParticleSystem.MainModule main = lowerClouds.main;
                    main.startColor = Color.Lerp(region.stormWeather.particles.cloudColor, region.clearWeather.particles.cloudColor,
                        influence.EyeClearness);
                }
            }

            if (inStorm && influence.IsMagneticMist)
            {
                // from the fog already there at 7,000 to the peak (0.25) at the core radius
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
        private static void FadeLiveClouds(ParticleSystem particles, float factor, float fadeSeconds = CloudFadeSeconds)
        {
            if (particles == null || particles.particleCount == 0)
            {
                return;
            }

            ParticleSystem.MainModule main = particles.main;
            float cap = Mathf.Lerp(
                fadeSeconds,
                Mathf.Max(fadeSeconds, main.startLifetime.constantMax),
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
