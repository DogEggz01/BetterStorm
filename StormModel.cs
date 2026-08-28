using System;
using System.Collections.Generic;
using System.Reflection;
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
        Sandstorm
    }

    internal enum StormKind
    {
        Vanilla,
        Squall,
        Hurricane,
        DryThunderstorm,
        Sandstorm
    }

    internal readonly struct LightningProfile
    {
        internal readonly float Interval;
        internal readonly float SpawnRadius;
        internal readonly float SchedulingDistance;
        internal readonly float AudioSourceMaxDistance;
        internal readonly float LightRange;
        internal readonly float StrikeHeight;
        internal readonly float LightIntensity;

        internal LightningProfile(
            float interval,
            float spawnRadius,
            float schedulingDistance,
            float lightRange,
            float strikeHeight,
            float lightIntensity)
            : this(
                interval,
                spawnRadius,
                schedulingDistance,
                schedulingDistance,
                lightRange,
                strikeHeight,
                lightIntensity)
        {
        }

        internal LightningProfile(
            float interval,
            float spawnRadius,
            float schedulingDistance,
            float audioSourceMaxDistance,
            float lightRange,
            float strikeHeight,
            float lightIntensity)
        {
            Interval = interval;
            SpawnRadius = spawnRadius;
            SchedulingDistance = schedulingDistance;
            AudioSourceMaxDistance = audioSourceMaxDistance;
            LightRange = lightRange;
            StrikeHeight = strikeHeight;
            LightIntensity = lightIntensity;
        }

        internal float VisualMaximum
        {
            get { return LightRange + StormCatalog.VisualPadding; }
        }
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
                EnableDescription = "Enable the priority-3 Hurricane.",
                SaveKey = "DogEggz.BetterStorm.hurricane-position.v2",
                Id = CustomStormId.Hurricane,
                Kind = StormKind.Hurricane,
                InitialOffset = new Vector3(0f, 0f, -24000f),
                Priority = 3,
                Radius = 6000f,
                ParticleDistance = 9960f,
                MoveSpeed = 5.5f,
                UsesFixedWeatherRange = false,
                WindCoefficient = 34f,
                RainTarget = 13f,
                AllowedClimateSeasons =
                    ClimateSeasonMask.Summer | ClimateSeasonMask.Autumn,
                SupportsLightning = true,
                Lightning = new LightningProfile(
                    interval: 12f,
                    spawnRadius: 3000f,
                    schedulingDistance: 12500f,
                    lightRange: 7980f,
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
                    lightRange: 2250f,
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
                MoveSpeed = 12f,
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
                    schedulingDistance: 2000f,
                    lightRange: 1500f,
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
            }

            return true;
        }
    }

    internal static class StormAccess
    {
        internal static readonly AccessTools.FieldRef<WeatherStorms, WanderingStorm[]> Storms =
            AccessTools.FieldRefAccess<WeatherStorms, WanderingStorm[]>("storms");
        internal static readonly AccessTools.FieldRef<WeatherStorms, float> CurrentStormRange =
            AccessTools.FieldRefAccess<WeatherStorms, float>("currentStormRange");
        internal static readonly AccessTools.FieldRef<WeatherStorms, float> RainBorder =
            AccessTools.FieldRefAccess<WeatherStorms, float>("rainBorder");

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

    internal readonly struct StormInfluence
    {
        internal readonly ModStormController Controller;
        internal readonly StormDefinition Definition;
        internal readonly float CenterDistance;
        internal readonly float Radius;
        internal readonly float WeatherRange;
        internal readonly float OuterEdge;
        internal readonly float WeatherLerp;

        internal StormInfluence(
            ModStormController controller,
            float centerDistance,
            float radius,
            float weatherRange)
        {
            Controller = controller;
            Definition = controller != null ? controller.Definition : null;
            CenterDistance = centerDistance;
            Radius = radius;
            WeatherRange = weatherRange;
            OuterEdge = radius + weatherRange;
            WeatherLerp = Mathf.InverseLerp(OuterEdge, radius, centerDistance);
        }

        internal bool Inside
        {
            get { return CenterDistance < OuterEdge; }
        }

        internal float WindLerp
        {
            get { return Mathf.InverseLerp(OuterEdge, Radius * 0.5f, CenterDistance); }
        }

        internal bool IsSquall
        {
            get { return Definition != null && Definition.Kind == StormKind.Squall; }
        }

        internal bool IsSandstorm
        {
            get { return Definition != null && Definition.Kind == StormKind.Sandstorm; }
        }
    }

    internal static class StormInfluenceService
    {
        internal static StormInfluence Evaluate(
            WeatherStorms weatherStorms,
            WanderingStorm storm,
            float distance)
        {
            ModStormController controller = storm != null
                ? storm.GetComponent<ModStormController>()
                : null;
            float radius = storm != null ? storm.GetRadius() : 0f;
            float range = GetWeatherRange(weatherStorms, controller);
            return new StormInfluence(controller, distance, radius, range);
        }

        internal static float GetWeatherRange(
            WeatherStorms weatherStorms,
            ModStormController controller)
        {
            if (controller != null && controller.Definition.UsesFixedWeatherRange)
            {
                return controller.Definition.FixedWeatherRange;
            }

            return weatherStorms != null
                ? Mathf.Max(0f, StormAccess.CurrentStormRange(weatherStorms))
                : 0f;
        }

        internal static bool TryGetCurrent(out StormInfluence influence)
        {
            return TryGetCurrent(WeatherStorms.currentStormDistance, out influence);
        }

        internal static bool TryGetCurrent(float distance, out StormInfluence influence)
        {
            influence = default(StormInfluence);
            WeatherStorms weatherStorms = WeatherStorms.instance;
            WanderingStorm storm = weatherStorms != null
                ? weatherStorms.GetCurrentStorm()
                : null;
            if (weatherStorms == null || storm == null || !storm.active)
            {
                return false;
            }

            influence = Evaluate(weatherStorms, storm, distance);
            return influence.Inside;
        }

        internal static float NormalizeForSelection(
            WeatherStorms weatherStorms,
            WanderingStorm storm,
            float distance)
        {
            StormInfluence influence = Evaluate(weatherStorms, storm, distance);
            return Mathf.Clamp01(
                (distance - influence.Radius) /
                Mathf.Max(0.0001f, influence.WeatherRange));
        }

        internal static float GetStormContribution(StormInfluence influence)
        {
            if (!influence.Inside)
            {
                return 0f;
            }

            float coefficient = influence.Definition != null
                ? influence.Definition.WindCoefficient
                : 26f;
            return coefficient * influence.WindLerp;
        }

        internal static float GetEffectiveWindLerp(
            float originalStart,
            float originalEnd,
            float distance)
        {
            if (BetterStormPlugin.Instance == null)
            {
                return Mathf.InverseLerp(originalStart, originalEnd, distance);
            }

            if (!TryGetCurrent(distance, out StormInfluence influence))
            {
                return 0f;
            }

            float coefficient = influence.Definition != null
                ? influence.Definition.WindCoefficient
                : 26f;
            return influence.WindLerp * coefficient / 26f;
        }
    }
}
