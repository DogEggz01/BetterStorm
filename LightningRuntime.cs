using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterStorm
{
    [HarmonyPatch(typeof(WanderingStormLightning), "LightningStrike")]
    internal static class UniversalLightningPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(WanderingStormLightning __instance)
        {
            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            if (plugin == null)
            {
                return true;
            }

            ModStormController controller =
                __instance.GetComponentInParent<ModStormController>();
            if (controller != null &&
                (!plugin.IsEnabled(controller.Definition.Id) ||
                 !controller.Definition.SupportsLightning))
            {
                return false;
            }

            LightningProfile profile = controller != null
                ? controller.Definition.Lightning
                : StormCatalog.VanillaLightning;

            float interval = Mathf.Max(0.01f, profile.Interval);
            StormAccess.LightningCooldown(__instance) =
                UnityEngine.Random.Range(interval, interval * 3f);

            Vector2 offset = UnityEngine.Random.insideUnitCircle *
                profile.SpawnRadius;
            __instance.transform.localPosition = new Vector3(
                offset.x,
                profile.StrikeHeight,
                offset.y);

            Vector3 strikeWorldPosition = __instance.transform.position;
            if (Refs.observerMirror == null)
            {
                return false;
            }

            float distance = Vector3.Distance(
                strikeWorldPosition,
                Refs.observerMirror.transform.position);
            float thunderDelay = Mathf.Max(0f, distance / 340f - 1.47f);

            if (distance <= profile.SchedulingDistance)
            {
                AudioClip[] choices = distance > StormCatalog.CloseThunderDistance
                    ? __instance.farThunders
                    : __instance.closeThunders;
                if (choices != null && choices.Length > 0)
                {
                    AudioClip clip = choices[
                        UnityEngine.Random.Range(0, choices.Length)];
                    ThunderPoolRegistry.Play(
                        __instance,
                        strikeWorldPosition,
                        clip,
                        thunderDelay,
                        profile.AudioSourceMaxDistance);
                }
            }

            Light light = StormAccess.LightningLight(__instance);
            if (light == null)
            {
                light = __instance.GetComponent<Light>();
            }

            ParticleSystem particles = StormAccess.LightningParticles(__instance);
            if (particles == null)
            {
                particles = __instance.GetComponent<ParticleSystem>();
            }

            if (light != null)
            {
                light.range = profile.LightRange;
                StormAccess.LightningIntensity(__instance) =
                    profile.LightIntensity;
                GlobalLightningSettings.ApplyToLight(light);
            }

            bool mayShowVisual = light != null &&
                particles != null &&
                distance <= profile.VisualMaximum;
            if (mayShowVisual)
            {
                light.enabled = true;
                light.intensity = profile.LightIntensity;
                StormAccess.LightningLightTimer(__instance) = 0f;
                particles.Play();
            }

            return false;
        }
    }

    internal static class ThunderPoolRegistry
    {
        private static readonly Dictionary<
            WanderingStormLightning,
            ThunderEmitterPool> Pools =
            new Dictionary<WanderingStormLightning, ThunderEmitterPool>();

        internal static void Play(
            WanderingStormLightning owner,
            Vector3 fixedWorldPosition,
            AudioClip clip,
            float delay,
            float maximumDistance)
        {
            if (owner == null || clip == null)
            {
                return;
            }

            if (!Pools.TryGetValue(owner, out ThunderEmitterPool pool) ||
                pool == null)
            {
                AudioSource template = StormAccess.LightningAudio1(owner);
                if (template == null)
                {
                    template = StormAccess.LightningAudio2(owner);
                }
                if (template == null)
                {
                    AudioSource[] sources = owner.GetComponents<AudioSource>();
                    if (sources.Length > 0)
                    {
                        template = sources[0];
                    }
                }
                if (template == null)
                {
                    return;
                }

                pool = new ThunderEmitterPool(owner, template);
                Pools[owner] = pool;
            }

            pool.Play(
                fixedWorldPosition,
                clip,
                delay,
                maximumDistance);
        }

        internal static void Reset(ModStormController controller)
        {
            if (controller == null)
            {
                return;
            }

            WanderingStormLightning[] lightning =
                controller.GetComponentsInChildren<WanderingStormLightning>(true);
            for (int i = 0; i < lightning.Length; i++)
            {
                if (Pools.TryGetValue(
                        lightning[i],
                        out ThunderEmitterPool pool))
                {
                    pool.StopAll();
                }
            }
        }

        internal static void ResetAll()
        {
            foreach (ThunderEmitterPool pool in Pools.Values)
            {
                pool?.StopAll();
            }
        }

        internal static void Shutdown()
        {
            foreach (ThunderEmitterPool pool in Pools.Values)
            {
                pool?.Dispose();
            }
            Pools.Clear();
        }
    }

    internal sealed class ThunderEmitterPool
    {
        private const int MaximumEmitters = 30;

        private sealed class Slot
        {
            internal GameObject Object;
            internal AudioSource Source;
            internal double BusyUntilDsp;
        }

        private readonly WanderingStormLightning owner;
        private readonly AudioSource template;
        private readonly GameObject root;
        private readonly List<Slot> slots = new List<Slot>();
        private double nextExhaustionWarningDsp;

        internal ThunderEmitterPool(
            WanderingStormLightning owner,
            AudioSource template)
        {
            this.owner = owner;
            this.template = template;
            root = new GameObject("Better Storm fixed thunder pool");

            if (Refs.shiftingWorld != null)
            {
                root.transform.SetParent(Refs.shiftingWorld, true);
            }
            else
            {
                UnityEngine.Object.DontDestroyOnLoad(root);
            }

            AddSlot();
            AddSlot();
        }

        internal void Play(
            Vector3 worldPosition,
            AudioClip clip,
            float delay,
            float maximumDistance)
        {
            double now = AudioSettings.dspTime;
            Slot slot = null;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].BusyUntilDsp <= now)
                {
                    slot = slots[i];
                    break;
                }
            }

            if (slot == null && slots.Count < MaximumEmitters)
            {
                slot = AddSlot();
            }

            if (slot == null)
            {
                if (now >= nextExhaustionWarningDsp)
                {
                    nextExhaustionWarningDsp = now + 10d;
                    BetterStormPlugin.Instance?.LogFeatureWarning(
                        "Thunder emitter pool exhausted for " + owner.name + ".");
                }
                return;
            }

            slot.Object.transform.position = worldPosition;
            slot.Source.minDistance = StormCatalog.AudioMinimumDistance;
            slot.Source.maxDistance = maximumDistance;
            slot.Source.clip = clip;

            double scheduledStart = now + Mathf.Max(0f, delay);
            slot.BusyUntilDsp = scheduledStart + clip.length + 0.1d;
            slot.Source.PlayScheduled(scheduledStart);
        }

        internal void StopAll()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Source != null)
                {
                    slots[i].Source.Stop();
                    slots[i].Source.clip = null;
                }
                slots[i].BusyUntilDsp = 0d;
            }
        }

        internal void Dispose()
        {
            StopAll();
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        private Slot AddSlot()
        {
            GameObject emitter = new GameObject("Fixed thunder emitter");
            emitter.transform.SetParent(root.transform, false);
            AudioSource source = emitter.AddComponent<AudioSource>();
            CopySettings(template, source);
            source.playOnAwake = false;
            source.loop = false;
            source.minDistance = StormCatalog.AudioMinimumDistance;

            Slot slot = new Slot
            {
                Object = emitter,
                Source = source,
                BusyUntilDsp = 0d
            };
            slots.Add(slot);
            return slot;
        }

        private static void CopySettings(AudioSource from, AudioSource to)
        {
            to.outputAudioMixerGroup = from.outputAudioMixerGroup;
            to.mute = from.mute;
            to.bypassEffects = from.bypassEffects;
            to.bypassListenerEffects = from.bypassListenerEffects;
            to.bypassReverbZones = from.bypassReverbZones;
            to.priority = from.priority;
            to.volume = from.volume;
            to.pitch = from.pitch;
            to.panStereo = from.panStereo;
            to.spatialBlend = from.spatialBlend;
            to.reverbZoneMix = from.reverbZoneMix;
            to.dopplerLevel = from.dopplerLevel;
            to.spread = from.spread;
            to.rolloffMode = from.rolloffMode;
            to.maxDistance = from.maxDistance;

            AudioSourceCurveType[] curveTypes =
            {
                AudioSourceCurveType.CustomRolloff,
                AudioSourceCurveType.SpatialBlend,
                AudioSourceCurveType.ReverbZoneMix,
                AudioSourceCurveType.Spread
            };
            for (int i = 0; i < curveTypes.Length; i++)
            {
                AnimationCurve curve = from.GetCustomCurve(curveTypes[i]);
                if (curve != null)
                {
                    to.SetCustomCurve(
                        curveTypes[i],
                        new AnimationCurve(curve.keys));
                }
            }
        }
    }

    internal static class GlobalLightningSettings
    {
        private static readonly Dictionary<Light, LightShadows> OriginalShadows =
            new Dictionary<Light, LightShadows>();

        internal static void ApplyToLiveStorms()
        {
            WanderingStormLightning[] lightning =
                Resources.FindObjectsOfTypeAll<WanderingStormLightning>();
            for (int i = 0; i < lightning.Length; i++)
            {
                Light light = StormAccess.LightningLight(lightning[i]);
                if (light == null)
                {
                    light = lightning[i].GetComponent<Light>();
                }
                ApplyToLight(light);
            }
        }

        internal static void ApplyToLight(Light light)
        {
            if (light == null)
            {
                return;
            }

            if (!OriginalShadows.ContainsKey(light))
            {
                OriginalShadows.Add(light, light.shadows);
            }

            light.shadows = LightShadows.Hard;
        }

        internal static void RestoreSnapshots()
        {
            foreach (KeyValuePair<Light, LightShadows> pair in OriginalShadows)
            {
                if (pair.Key != null)
                {
                    pair.Key.shadows = pair.Value;
                }
            }
            OriginalShadows.Clear();
        }
    }
}
