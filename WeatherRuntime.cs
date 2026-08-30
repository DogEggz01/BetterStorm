using HarmonyLib;
using UnityEngine;

namespace BetterStorm
{
    internal static class CustomPrecipitationParticles
    {
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

        internal static void Clear(ParticleSystem particles)
        {
            if (particles != null)
            {
                particles.Clear(true);
            }
        }

        internal static void DisableInheritedComponents(GameObject clone)
        {
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

            CustomPrecipitationParticles.SetEmissionRate(particles, 0f);
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
            main.simulationSpace = ParticleSystemSimulationSpace.World;

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
                !influence.IsGentleSnow)
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
                GentleSnowVisuals.Deactivate();
                return;
            }

            if (influence.Definition.Kind == StormKind.Sandstorm)
            {
                GentleSnowVisuals.Deactivate();
                SandstormVisuals.ApplyWeather(
                    influence,
                    ___rain,
                    ___outerRain,
                    ___rainSplash);
                return;
            }

            if (influence.Definition.Kind == StormKind.GentleSnow)
            {
                SandstormVisuals.Deactivate();
                GentleSnowVisuals.ApplyWeather(
                    influence,
                    ___rain,
                    ___outerRain,
                    ___rainSplash);
                RenderSettings.fogDensity = 0f;
                return;
            }

            SandstormVisuals.Deactivate();
            GentleSnowVisuals.Deactivate();
            if (influence.Definition.SuppressFog)
            {
                RenderSettings.fogDensity = 0f;
            }

            if (influence.Definition.SuppressRainParticles)
            {
                CustomPrecipitationParticles.SetEmissionRate(___rain, 0f);
                CustomPrecipitationParticles.SetEmissionRate(___outerRain, 0f);
                CustomPrecipitationParticles.SetEmissionRate(___rainSplash, 0f);
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
            CustomPrecipitationParticles.SetEmissionRate(
                rain,
                rainIntensity * 75f);
            CustomPrecipitationParticles.SetEmissionRate(
                outerRain,
                rainIntensity * 125f);
            CustomPrecipitationParticles.SetEmissionRate(
                rainSplash,
                rainIntensity * 250f);
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
