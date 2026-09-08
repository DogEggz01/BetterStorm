using System.Collections.Generic;
using UnityEngine;

namespace BetterStorm
{
    // Native overlap testing on existing visible-boat volumes. No particle
    // readback, per-particle C# callbacks, world queries or walk-copy raycasts.
    internal sealed class IndoorPrecipitationExclusion : MonoBehaviour
    {
        private sealed class Interior
        {
            internal Transform BoatModel;
            internal InteriorEffectsTrigger Volume;
            internal Collider Collider;

            internal bool IsActive => BoatModel != null && Volume != null &&
                Volume.isActiveAndEnabled && Collider != null && Collider.enabled &&
                Collider.isTrigger && Collider.gameObject.activeInHierarchy &&
                Collider.transform.IsChildOf(BoatModel);
        }

        private static readonly List<Interior> interiors = new List<Interior>();
        private static float nextDiscovery;
        private ParticleSystem particles;
        private Interior[] selected;
        private Interior[] candidates;
        private float[] distances;
        private float nextSelection;
        private int lastReportedCount = -1;

        internal static void Attach(ParticleSystem target)
        {
            if (target == null || target.GetComponent<IndoorPrecipitationExclusion>() != null) return;
            var exclusion = target.gameObject.AddComponent<IndoorPrecipitationExclusion>();
            exclusion.particles = target;
            var trigger = target.trigger;
            exclusion.selected = new Interior[trigger.maxColliderCount];
            exclusion.candidates = new Interior[trigger.maxColliderCount];
            exclusion.distances = new float[trigger.maxColliderCount];
            trigger.enabled = false;
            trigger.inside = ParticleSystemOverlapAction.Kill;
            trigger.enter = ParticleSystemOverlapAction.Kill;
            trigger.outside = ParticleSystemOverlapAction.Ignore;
            trigger.exit = ParticleSystemOverlapAction.Ignore;
            trigger.radiusScale = 1f;
            for (int i = 0; i < trigger.maxColliderCount; i++) trigger.SetCollider(i, null);
            var collision = target.collision;
            collision.enabled = false;
            nextDiscovery = 0f;
        }

        internal static void ResetCaches()
        {
            interiors.Clear();
            nextDiscovery = 0f;
        }

        private static void DiscoverInteriors()
        {
            if (Time.unscaledTime < nextDiscovery) return;
            nextDiscovery = Time.unscaledTime + 1f;
            interiors.Clear();
            foreach (BoatRefs boat in Object.FindObjectsOfType<BoatRefs>())
            {
                if (boat.boatModel == null) continue;
                foreach (InteriorEffectsTrigger volume in
                    boat.boatModel.GetComponentsInChildren<InteriorEffectsTrigger>(true))
                {
                    // Include inactive cabin options now; activation/removal is
                    // handled without rescanning the scene or changing physics.
                    foreach (Collider collider in volume.GetComponents<Collider>())
                        interiors.Add(new Interior { BoatModel = boat.boatModel,
                            Volume = volume, Collider = collider });
                }
            }
        }

        private void Update()
        {
            if (particles == null || selected == null) return;
            var trigger = particles.trigger;
            bool invalidated = false;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] == null || selected[i].IsActive) continue;
                selected[i] = null;
                trigger.SetCollider(i, null);
                invalidated = true;
            }
            if (!GameState.playing)
            {
                ClearSlots();
                return;
            }
            // Continue for lingering snow/sand after emission has stopped.
            // With no live particles there is no reason to discover shelters.
            if (particles.particleCount == 0) return;
            if (!invalidated && Time.unscaledTime < nextSelection) return;
            nextSelection = Time.unscaledTime + 0.25f;
            DiscoverInteriors();
            Camera camera = Camera.main;
            Vector3 position = camera != null ? camera.transform.position : transform.position;
            for (int i = 0; i < candidates.Length; i++)
            {
                candidates[i] = null;
                distances[i] = float.PositiveInfinity;
            }
            foreach (Interior interior in interiors)
            {
                if (!interior.IsActive) continue;
                float distance = interior.Collider.bounds.SqrDistance(position);
                // Selection range only, never an artificial blocking sphere.
                if (distance > 200f * 200f) continue;
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (distance >= distances[i]) continue;
                    for (int j = candidates.Length - 1; j > i; j--)
                    { candidates[j] = candidates[j - 1]; distances[j] = distances[j - 1]; }
                    candidates[i] = interior;
                    distances[i] = distance;
                    break;
                }
            }
            int count = 0;
            for (int i = 0; i < selected.Length; i++)
            {
                Collider previous = selected[i]?.Collider;
                selected[i] = candidates[i];
                Collider current = selected[i]?.Collider;
                if (previous != current) trigger.SetCollider(i, current);
                if (current != null) count++;
            }
            trigger.enabled = count > 0;
            if (lastReportedCount != count)
            {
                lastReportedCount = count;
                BetterStormPlugin.Instance?.LogFeatureInfo(particles.name + ": " + count +
                    "/" + selected.Length + " native ship interior triggers registered.");
            }
        }

        private void ClearSlots()
        {
            if (particles == null || selected == null) return;
            var trigger = particles.trigger;
            trigger.enabled = false;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] == null) continue;
                selected[i] = null;
                trigger.SetCollider(i, null);
            }
            nextSelection = 0f;
        }

        private void OnDisable() => ClearSlots();
    }
}
