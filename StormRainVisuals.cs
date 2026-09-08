using System;
using UnityEngine;

namespace BetterStorm
{
    internal readonly struct StormRainProfile
    {
        internal readonly float Emission;
        internal readonly int MaximumParticles;
        internal readonly float StreakLength;
        internal readonly float SimulationSpeed;

        internal StormRainProfile(float emission, float streakLength, float simulationSpeed)
        {
            Emission = emission;
            MaximumParticles = StormRainRules.CalculateParticleCapacity(emission);
            StreakLength = streakLength;
            SimulationSpeed = simulationSpeed;
        }
    }

    internal static class StormRainRules
    {
        internal const float Radius = 120f;
        internal const float SpawnHeight = 50f;
        internal const float MinimumLifetime = 2f;
        internal const float MaximumLifetime = 2.6f;
        internal const float MinimumSize = 0.04f;
        internal const float MaximumSize = 0.10f;
        internal const float MinimumAlpha = 0.02f;
        internal const float MaximumAlpha = 0.12f;
        internal const float FallSpeed = 25f;
        internal const float WindInfluence = 0.6f;
        internal const float Gravity = 0f;
        internal const float MaximumScreenSize = 0.5f;
        internal const float Turbulence = 0f;
        internal const float TurbulenceFrequency = 0.22f;
        internal const float TurbulenceScrollSpeed = 0.2f;

        internal static int CalculateParticleCapacity(float emission)
        {
            // Emission and particle age use the same simulation clock. Keep at
            // least 20% headroom at the longest lifetime, rounded up to 2,000.
            return (int)Math.Ceiling(emission * MaximumLifetime * 1.2 / 2000.0) * 2000;
        }

        internal static bool IsRainStorm(StormKind kind)
        {
            return kind == StormKind.Vanilla || kind == StormKind.Squall ||
                kind == StormKind.Hurricane;
        }

        internal static bool ShouldUseStormRain(bool insideStorm, StormKind kind, float baseRain)
        {
            return insideStorm && IsRainStorm(kind) && baseRain > 0f;
        }

        internal static StormRainProfile GetProfile(StormKind kind)
        {
            switch (kind)
            {
                case StormKind.Vanilla: return new StormRainProfile(7000f, 6f, 1.8f);
                case StormKind.Squall: return new StormRainProfile(25000f, 12f, 3f);
                case StormKind.Hurricane: return new StormRainProfile(14000f, 8f, 2.3f);
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        internal static float GetBaseRain(float distance, float cloudyBorder,
            float rainBorder, float cloudyRain, float lightRain, float peakRain)
        {
            if (distance >= cloudyBorder || cloudyBorder <= 0f) return 0f;
            if (distance > rainBorder)
                return Mathf.Max(0f, Mathf.Lerp(cloudyRain, lightRain,
                    Mathf.InverseLerp(cloudyBorder, rainBorder, distance)));
            return Mathf.Max(0f, Mathf.Lerp(lightRain, peakRain,
                Mathf.InverseLerp(rainBorder, 0f, distance)));
        }

        internal static float GetVisualStrength(float baseRain, float peakRain)
        {
            return peakRain > 0f ? Mathf.Clamp01(baseRain / peakRain) : 0f;
        }

    }

    internal static class StormRainVisuals
    {
        private static readonly Color RainColor = new Color(0.8455882f, 0.9552738f, 1f, 1f);
        private static ParticleSystem sourceOuterRain;
        private static ParticleSystem customRain;
        private static ParticleSystemRenderer renderer;
        private static ParticleSystemRenderer sourceRenderer;
        private static bool active;

        internal static bool ApplyWeather(StormKind kind, float intensity, float strength,
            ParticleSystem rain, ParticleSystem outerRain, ParticleSystem splash)
        {
            if (!StormRainRules.IsRainStorm(kind) || rain == null || outerRain == null)
                return false;
            EnsureInitialized(outerRain);
            // Keep upstream rain usable if the custom emitter could not be created.
            if (customRain == null) return false;

            StormRainProfile profile = StormRainRules.GetProfile(kind);
            ParticleSystem.MainModule main = customRain.main;
            // A switch from Squall to weaker rain must not delete existing drops.
            main.maxParticles = Math.Max(profile.MaximumParticles, customRain.particleCount);
            main.simulationSpeed = profile.SimulationSpeed;
            if (renderer != null) renderer.lengthScale = profile.StreakLength;
            active = true;
            UpdateMotion();
            CustomPrecipitationParticles.SetEmissionRate(rain, 0f);
            CustomPrecipitationParticles.SetEmissionRate(outerRain, 0f);
            CustomPrecipitationParticles.SetEmissionRate(customRain,
                profile.Emission * Mathf.Clamp01(strength));
            RainEffects.ApplySplashSettings(splash);
            CustomPrecipitationParticles.SetEmissionRate(splash,
                intensity * RainEffects.SplashEmissionPerRainIntensity *
                RainEffects.GetEmissionMultiplier(kind));
            GameState.rainIntensity = intensity;
            return true;
        }

        internal static void Tick()
        {
            if (customRain == null) return;
            RefreshMaterial();
            if (active) UpdateMotion();
        }

        internal static void Deactivate()
        {
            CustomPrecipitationParticles.SetEmissionRate(customRain, 0f);
            active = false;
        }

        internal static void Shutdown()
        {
            Deactivate();
            CustomPrecipitationParticles.Clear(customRain);
            if (customRain != null) UnityEngine.Object.Destroy(customRain.gameObject);
            sourceOuterRain = null;
            customRain = null;
            renderer = null;
            sourceRenderer = null;
        }

        private static void EnsureInitialized(ParticleSystem source)
        {
            Transform owner = Refs.shiftingWorld != null
                ? Refs.shiftingWorld : source.transform.parent;
            if (customRain != null && sourceOuterRain == source)
            {
                if (customRain.transform.parent != owner)
                    customRain.transform.SetParent(owner, true);
                RefreshMaterial();
                return;
            }
            Shutdown();
            sourceOuterRain = source;
            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject,
                source.transform.position, Quaternion.identity, owner);
            clone.name = "Better Storm Rain";
            clone.transform.localScale = Vector3.one;
            CustomPrecipitationParticles.DisableInheritedComponents(clone);
            customRain = clone.GetComponent<ParticleSystem>();
            if (customRain == null)
            {
                UnityEngine.Object.Destroy(clone);
                return;
            }
            renderer = clone.GetComponent<ParticleSystemRenderer>();
            sourceRenderer = source.GetComponent<ParticleSystemRenderer>();
            RefreshMaterial();

            ParticleSystem.MainModule main = customRain.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                StormRainRules.MinimumLifetime, StormRainRules.MaximumLifetime);
            main.startSize3D = false;
            main.startSize = new ParticleSystem.MinMaxCurve(
                StormRainRules.MinimumSize, StormRainRules.MaximumSize);
            // Keep the native random start size constant throughout each drop's life.
            var sizeOverLifetime = customRain.sizeOverLifetime;
            sizeOverLifetime.enabled = false;
            var sizeBySpeed = customRain.sizeBySpeed;
            sizeBySpeed.enabled = false;
            main.startSpeed = 0f;
            main.gravityModifier = StormRainRules.Gravity;
            Color minimum = RainColor;
            Color maximum = RainColor;
            minimum.a = StormRainRules.MinimumAlpha;
            maximum.a = StormRainRules.MaximumAlpha;
            main.startColor = new ParticleSystem.MinMaxGradient(minimum, maximum);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.ShapeModule shape = customRain.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = StormRainRules.Radius;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            shape.scale = Vector3.one;
            shape.position = Vector3.zero;
            shape.rotation = new Vector3(90f, 0f, 0f);

            ParticleSystem.NoiseModule noise = customRain.noise;
            noise.enabled = StormRainRules.Turbulence > 0f;
            noise.separateAxes = true;
            noise.strengthX = StormRainRules.Turbulence;
            noise.strengthY = 0f;
            noise.strengthZ = StormRainRules.Turbulence;
            noise.frequency = StormRainRules.TurbulenceFrequency;
            noise.scrollSpeed = StormRainRules.TurbulenceScrollSpeed;
            noise.damping = true;
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.cameraVelocityScale = 0f;
                renderer.velocityScale = 0f;
                renderer.maxParticleSize = StormRainRules.MaximumScreenSize;
            }
            IndoorPrecipitationExclusion.Attach(customRain);
            ParticleSystem.EmissionModule emission = customRain.emission;
            emission.enabled = true;
            CustomPrecipitationParticles.SetEmissionRate(customRain, 0f);
            customRain.Clear(true);
            customRain.Play(true);
        }

        private static void UpdateMotion()
        {
            Camera camera = Camera.main;
            if (camera != null)
                customRain.transform.position = camera.transform.position +
                    Vector3.up * StormRainRules.SpawnHeight;
            Vector3 wind = Wind.currentWind * StormRainRules.WindInfluence;
            ParticleSystem.VelocityOverLifetimeModule velocity = customRain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = wind.x;
            velocity.y = -StormRainRules.FallSpeed;
            velocity.z = wind.z;
        }

        private static void RefreshMaterial()
        {
            if (renderer == null || sourceRenderer == null) return;
            // Borrow the live upstream material so day/night tint stays current.
            // Never modify or destroy this shared material.
            Material source = sourceRenderer.sharedMaterial;
            if (source != null && renderer.sharedMaterial != source)
                renderer.sharedMaterial = source;
        }
    }
}
