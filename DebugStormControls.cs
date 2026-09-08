using BepInEx.Configuration;
using UnityEngine;

namespace BetterStorm
{
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
                controller.ShouldBeActive() &&
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
