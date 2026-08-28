using HarmonyLib;
using UnityEngine;

namespace BetterStorm
{
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
        private const float SandParticleSize = 0.015f;
        private const float SandParticleSpawnRadius = 60f;
        private const float SandstormFogDensity = 0.03f;
        private const float NoiseStrength = 1.2f;
        private const float WindInfluence = 0.3f;
        private const float HorizontalVelocityMultiplier = 2f;
        private const int MaximumParticles = 30000;
        private const float EmissionRateMultiplier = 200f;
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
            0.7647059f,
            0.6f,
            0.2823529f,
            1f);
        private static readonly Color DistantStormColor = SandFogColor;

        private static ParticleSystem sourceRain;
        private static ParticleSystem sourceOuterRain;
        private static ParticleSystem sandRain;
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
                ClearParticles(rain);
                ClearParticles(outerRain);
                ClearParticles(rainSplash);
            }
            active = true;
            SuppressVanillaRain(rain, outerRain, rainSplash);
            GameState.rainIntensity = 0f;

            if (!TryGetRainStageLerp(influence, out float rainStageLerp))
            {
                if (rainStageActive)
                {
                    SetEmissionRate(sandRain, 0f);
                    ClearParticles(sandRain);
                }
                rainStageActive = false;
                return;
            }

            rainStageActive = true;
            SetEmissionRate(
                sandRain,
                SandParticleIntensity * rainStageLerp * EmissionRateMultiplier);
            RenderSettings.fogDensity = Mathf.Lerp(
                RenderSettings.fogDensity,
                SandstormFogDensity,
                rainStageLerp);
            RenderSettings.fogColor = Color.Lerp(
                RenderSettings.fogColor,
                SandFogColor,
                rainStageLerp);
        }

        internal static void Tick()
        {
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
            Color target = SandFogColor;
            target.a = current.a;
            main.startColor = Color.Lerp(current, target, rainStageLerp);
        }

        internal static void ApplyStormCloudColors(
            ParticleSystem topParticles,
            ParticleSystem bottomParticles)
        {
            ApplyStormCloudColor(
                topParticles,
                DistantStormColor,
                DistantStormAlpha);
            ApplyStormCloudColor(
                bottomParticles,
                DistantStormColor,
                OverheadStormAlpha);
        }

        internal static void Deactivate()
        {
            rainStageActive = false;
            if (!active)
            {
                return;
            }

            SetEmissionRate(sandRain, 0f);
            ClearParticles(sandRain);
            active = false;
        }

        internal static void Shutdown()
        {
            Deactivate();
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
            return StormInfluenceService.TryGetCurrent(out influence) &&
                influence.IsSandstorm &&
                BetterStormPlugin.Instance != null &&
                BetterStormPlugin.Instance.IsEnabled(CustomStormId.Sandstorm) &&
                GameplayRegionResolver.GetCurrentRegion() != null &&
                GameplayRegionResolver.GetCurrentRegion().gameObject.name ==
                    "Region Al'ankh";
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

            Rain[] rainControllers = clone.GetComponentsInChildren<Rain>(true);
            for (int i = 0; i < rainControllers.Length; i++)
            {
                rainControllers[i].enabled = false;
            }

            LitParticles[] lighting =
                clone.GetComponentsInChildren<LitParticles>(true);
            for (int i = 0; i < lighting.Length; i++)
            {
                lighting[i].enabled = false;
            }

            AudioSource[] audioSources =
                clone.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audioSources.Length; i++)
            {
                audioSources[i].Stop();
                audioSources[i].enabled = false;
            }

            ParticleSystem particles = clone.GetComponent<ParticleSystem>();
            if (particles == null)
            {
                UnityEngine.Object.Destroy(clone);
                return null;
            }

            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = StartLifetime;
            main.startSize3D = false;
            main.startSize = SandParticleSize;
            main.startSpeed = 0f;
            main.maxParticles = MaximumParticles;
            main.gravityModifier = 0f;
            main.startColor = SandParticleColor;

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

            SetEmissionRate(particles, 0f);
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

        private static void ApplyStormCloudColor(
            ParticleSystem particles,
            Color baseColor,
            float alpha)
        {
            if (particles == null)
            {
                return;
            }

            Color color = baseColor;
            color.a = Mathf.Clamp01(alpha);
            ParticleSystemRenderer renderer =
                particles.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                ParticleSystem.MainModule main = particles.main;
                main.startColor = color;
                return;
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
                return;
            }

            if (sourceMaterial == null || tintShader == null)
            {
                ParticleSystem.MainModule main = particles.main;
                main.startColor = color;
                return;
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
        }

        private static void ApplyTintAndAlpha(
            ParticleSystem particles,
            Material material,
            Color baseColor,
            float alpha)
        {
            float opacity = Mathf.Clamp01(alpha);
            Color materialTint = baseColor;
            materialTint.r *= 0.5f;
            materialTint.g *= 0.5f;
            materialTint.b *= 0.5f;
            materialTint.a = opacity * 0.5f;
            material.SetColor("_TintColor", materialTint);

            Color particleColor = Color.white;
            particleColor.a = opacity;
            ParticleSystem.MainModule main = particles.main;
            main.startColor = particleColor;
            ApplyColorToExistingParticles(particles, particleColor);
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
            SetEmissionRate(rain, 0f);
            SetEmissionRate(outerRain, 0f);
            SetEmissionRate(rainSplash, 0f);
        }

        internal static void SetEmissionRate(
            ParticleSystem particles,
            float rate)
        {
            if (particles == null)
            {
                return;
            }

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = rate;
        }

        private static void ClearParticles(ParticleSystem particles)
        {
            if (particles != null)
            {
                particles.Clear(true);
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

    [HarmonyPatch(typeof(Weather), "ApplyWeather")]
    [HarmonyAfter(new[] { BetterStormPlugin.ClimateGuid })]
    internal static class CustomStormWeatherPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            ParticleSystem ___rain,
            ParticleSystem ___outerRain,
            ParticleSystem ___rainSplash)
        {
            if (BetterStormPlugin.Instance == null)
            {
                return;
            }

            if (!StormInfluenceService.TryGetCurrent(
                    out StormInfluence influence) ||
                influence.Definition == null)
            {
                SandstormVisuals.Deactivate();
                return;
            }

            if (influence.Definition.Kind == StormKind.Sandstorm)
            {
                SandstormVisuals.ApplyWeather(
                    influence,
                    ___rain,
                    ___outerRain,
                    ___rainSplash);
                return;
            }

            SandstormVisuals.Deactivate();
            if (influence.Definition.SuppressFog)
            {
                RenderSettings.fogDensity = 0f;
            }

            if (influence.Definition.SuppressRainParticles)
            {
                SandstormVisuals.SetEmissionRate(___rain, 0f);
                SandstormVisuals.SetEmissionRate(___outerRain, 0f);
                SandstormVisuals.SetEmissionRate(___rainSplash, 0f);
                GameState.rainIntensity = 0f;
                return;
            }

            if (influence.Definition.RainTarget.HasValue)
            {
                ApplyRainTarget(
                    influence,
                    influence.Definition.RainTarget.Value,
                    ___rain,
                    ___outerRain,
                    ___rainSplash);
            }
        }

        private static void ApplyRainTarget(
            StormInfluence influence,
            float rainTarget,
            ParticleSystem rain,
            ParticleSystem outerRain,
            ParticleSystem rainSplash)
        {
            WeatherStorms weatherStorms = WeatherStorms.instance;
            Weather weather = Weather.instance;
            if (weatherStorms == null ||
                weather == null ||
                weather.currentRegion == null ||
                weather.currentRegion.rainWeather == null ||
                weather.currentRegion.rainWeather.particles == null ||
                rain == null ||
                outerRain == null ||
                rainSplash == null)
            {
                return;
            }

            float rainBorder = StormAccess.RainBorder(weatherStorms);
            float normalizedDistance = 1f - influence.WeatherLerp;
            if (normalizedDistance > rainBorder)
            {
                return;
            }

            float stormBandLerp = Mathf.InverseLerp(
                rainBorder,
                0f,
                normalizedDistance);
            float rainIntensity = Mathf.Lerp(
                weather.currentRegion.rainWeather.particles.rainDensity,
                rainTarget,
                stormBandLerp);
            SandstormVisuals.SetEmissionRate(rain, rainIntensity * 75f);
            SandstormVisuals.SetEmissionRate(outerRain, rainIntensity * 125f);
            SandstormVisuals.SetEmissionRate(rainSplash, rainIntensity * 250f);
            GameState.rainIntensity = rainIntensity;
        }
    }

    [HarmonyPatch(typeof(Rain), "Update")]
    internal static class DryStormRainAudioPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            DryStormGameplay.SuppressRainGameplay();
        }
    }

    [HarmonyPatch(typeof(BoatDamage), "Update")]
    internal static class DryStormShipWaterPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            DryStormGameplay.SuppressRainGameplay();
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
}
