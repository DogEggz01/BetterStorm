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
            for (int i = 0; i < StormCatalog.Custom.Length; i++)
            {
                StormDefinition definition = StormCatalog.Custom[i];
                DrawControls(definition.Name, definition.Id);
            }
            GUILayout.EndVertical();
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
            target.y = controller.transform.position.y;
            controller.transform.position = target;
            controller.PrepareDebugMove(true);
            WindOverrideState.NotifyDebugSummon(controller.Definition.Kind);

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
            target.y = controller.transform.position.y;
            controller.transform.position = target;
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
