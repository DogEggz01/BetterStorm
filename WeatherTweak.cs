// Better Storm's changes to every storm, the game's own included: the shared rain emitter and its indoor filtering, the
// precipitation's shifting space, rain gameplay and audio, the storm wind curve, gusts and caps, lightning and the thunder
// pool, and which storm controls the weather.
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

    internal readonly struct StormInfluence
    {
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

        internal float NormalizedDistance
        {
            get
            {
                return Mathf.Clamp01(
                    (CenterDistance - Radius) /
                    Mathf.Max(0.0001f, WeatherRange));
            }
        }

        internal StormKind Kind
        {
            get
            {
                return Definition != null
                    ? Definition.Kind
                    : StormKind.Vanilla;
            }
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

        internal bool IsGentleSnow
        {
            get { return Definition != null && Definition.Kind == StormKind.GentleSnow; }
        }

        internal bool IsDoldrums
        {
            get { return Definition != null && Definition.Kind == StormKind.Doldrums; }
        }

        internal bool IsMagneticMist
        {
            get { return Definition != null && Definition.Kind == StormKind.MagneticMist; }
        }
    }

    internal static class StormSelectionRules
    {
        internal const float RetainUntilNormalizedDistance = 0.66f;
        internal const float SandstormYieldNormalizedDistance = 0.5f;

        internal static bool ShouldRetainCurrent(float normalizedDistance)
        {
            return normalizedDistance <= RetainUntilNormalizedDistance;
        }

        internal static bool ShouldSandstormYield(
            StormKind selectedKind,
            StormKind candidateKind,
            float candidateDistance,
            float candidateRadius,
            float candidateWeatherRange)
        {
            return selectedKind == StormKind.Sandstorm &&
                candidateKind != StormKind.Sandstorm &&
                candidateDistance < candidateRadius +
                    candidateWeatherRange *
                    SandstormYieldNormalizedDistance;
        }

        internal static bool ShouldDryThunderstormYield(
            StormKind selectedKind,
            StormKind candidateKind,
            float candidateDistance,
            float candidateRadius)
        {
            return selectedKind == StormKind.DryThunderstorm &&
                candidateKind == StormKind.Squall &&
                candidateDistance < candidateRadius;
        }

        internal static bool IsBetterCandidate(
            float normalizedDistance,
            float distance,
            float bestNormalizedDistance,
            float bestDistance)
        {
            return normalizedDistance < bestNormalizedDistance ||
                (Mathf.Approximately(
                     normalizedDistance,
                     bestNormalizedDistance) &&
                 distance < bestDistance);
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

    [HarmonyPatch(typeof(WeatherStorms), "FindClosestStorm")]
    internal static class StrongestStormPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            WeatherStorms __instance,
            WanderingStorm[] ___storms,
            Transform ___player,
            ref WanderingStorm ___currentStorm)
        {
            if (___storms == null || ___player == null)
            {
                return true;
            }

            WanderingStorm best = null;
            float bestNormalized = float.MaxValue;
            float bestDistance = float.MaxValue;
            StormKind bestKind = StormKind.Vanilla;

            bool currentIsAvailable = false;
            float currentNormalized = float.MaxValue;
            float currentDistance = float.MaxValue;
            StormKind currentKind = StormKind.Vanilla;

            WanderingStorm sandstormOverride = null;
            float sandstormOverrideNormalized = float.MaxValue;
            float sandstormOverrideDistance = float.MaxValue;
            StormKind sandstormOverrideKind = StormKind.Vanilla;

            WanderingStorm dryThunderstormOverride = null;
            float dryThunderstormOverrideNormalized = float.MaxValue;
            float dryThunderstormOverrideDistance = float.MaxValue;

            for (int i = 0; i < ___storms.Length; i++)
            {
                WanderingStorm storm = ___storms[i];
                if (storm == null || !storm.active)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    ___player.position,
                    storm.transform.position);
                StormInfluence influence = StormInfluenceService.Evaluate(
                    __instance,
                    storm,
                    distance);
                float normalized = influence.NormalizedDistance;
                StormKind kind = influence.Kind;

                if (StormSelectionRules.IsBetterCandidate(
                        normalized,
                        distance,
                        bestNormalized,
                        bestDistance))
                {
                    best = storm;
                    bestNormalized = normalized;
                    bestDistance = distance;
                    bestKind = kind;
                }

                if (storm == ___currentStorm)
                {
                    currentIsAvailable = true;
                    currentNormalized = normalized;
                    currentDistance = distance;
                    currentKind = kind;
                }

                if (kind != StormKind.Sandstorm &&
                    StormSelectionRules.ShouldSandstormYield(
                        StormKind.Sandstorm,
                        kind,
                        distance,
                        influence.Radius,
                        influence.WeatherRange) &&
                    StormSelectionRules.IsBetterCandidate(
                        normalized,
                        distance,
                        sandstormOverrideNormalized,
                        sandstormOverrideDistance))
                {
                    sandstormOverride = storm;
                    sandstormOverrideNormalized = normalized;
                    sandstormOverrideDistance = distance;
                    sandstormOverrideKind = kind;
                }

                if (kind == StormKind.Squall &&
                    StormSelectionRules.ShouldDryThunderstormYield(
                        StormKind.DryThunderstorm,
                        kind,
                        distance,
                        influence.Radius) &&
                    StormSelectionRules.IsBetterCandidate(
                        normalized,
                        distance,
                        dryThunderstormOverrideNormalized,
                        dryThunderstormOverrideDistance))
                {
                    dryThunderstormOverride = storm;
                    dryThunderstormOverrideNormalized = normalized;
                    dryThunderstormOverrideDistance = distance;
                }
            }

            WanderingStorm selected = best;
            float selectedDistance = bestDistance;
            StormKind selectedKind = bestKind;

            if (currentIsAvailable &&
                StormSelectionRules.ShouldRetainCurrent(currentNormalized))
            {
                selected = ___currentStorm;
                selectedDistance = currentDistance;
                selectedKind = currentKind;
            }

            if (selectedKind == StormKind.Sandstorm &&
                sandstormOverride != null)
            {
                selected = sandstormOverride;
                selectedDistance = sandstormOverrideDistance;
                selectedKind = sandstormOverrideKind;
            }

            if (selectedKind == StormKind.DryThunderstorm &&
                dryThunderstormOverride != null)
            {
                selected = dryThunderstormOverride;
                selectedDistance = dryThunderstormOverrideDistance;
            }

            ___currentStorm = selected;
            WeatherStorms.currentStormDistance = selected != null
                ? selectedDistance
                : 100000000f;
            return false;
        }
    }

    internal static class BonusCapBridge
    {
        internal static float GetEffectiveCap()
        {
            if (Chainloader.PluginInfos.TryGetValue(
                    BetterStormPlugin.ChaoticWindGuid,
                    out PluginInfo info) &&
                info != null &&
                info.Instance != null &&
                info.Instance.Config.TryGetEntry(
                    "Open Ocean Wind",
                    "Storm + Ocean Bonus Cap",
                    out ConfigEntry<float> entry))
            {
                return entry.Value;
            }

            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            return plugin != null ? plugin.FallbackBonusCap.Value : 40f;
        }
    }

    internal static class WindOverrideState
    {
        private const float StormFinalLerpSpeed = 1f;
        private const float StormCoreGustInterval = 10f;

        private static Wind trackedWind;
        private static float normalChangeTimer;
        private static float normalGustInterval;
        private static float normalFinalLerpSpeed;
        private static bool overridesApplied;
        private static bool wasInsideSquall;
        private static bool wasInsideGentleSnow;

        internal static void Tick()
        {
            Wind wind = Wind.instance;
            if (wind == null)
            {
                return;
            }

            if (trackedWind != wind)
            {
                RestoreImmediately();
                trackedWind = wind;
                CaptureNormal(wind);
            }

            // The Magnetic Mist keeps the vanilla wind rules, like no storm.
            if (!StormInfluenceService.TryGetCurrent(out StormInfluence influence) ||
                influence.IsMagneticMist)
            {
                RestoreForExit(wind);
                wasInsideSquall = false;
                wasInsideGentleSnow = false;
                return;
            }

            if (!overridesApplied)
            {
                CaptureNormal(wind);
            }

            bool insideSquall = influence.IsSquall;
            bool insideGentleSnow = influence.IsGentleSnow;
            bool insideDoldrums = influence.IsDoldrums;
            if ((insideSquall && !wasInsideSquall) ||
                (insideGentleSnow && !wasInsideGentleSnow))
            {
                StormAccess.WindTimer(wind) = 0f;
                StormAccess.GustTimer(wind) = 0f;
            }

            // The Doldrums keeps the normal gust interval (no stretch).
            float gustInterval = insideDoldrums
                ? normalGustInterval
                : Mathf.Lerp(
                    normalGustInterval,
                    StormCoreGustInterval,
                    influence.WindLerp);
            wind.finalLerpSpeed = insideGentleSnow
                ? GentleSnowRules.FinalLerpSpeed
                : insideDoldrums
                    ? DoldrumsRules.FinalLerpSpeed
                    : StormFinalLerpSpeed;
            wind.gustChangeTimer = gustInterval;
            wind.changeTimer = insideDoldrums
                ? DoldrumsRules.ChangeTimer
                : insideSquall
                    ? GetNormalChangeTimer() * 0.5f
                    : GetNormalChangeTimer();

            if (!overridesApplied)
            {
                StormAccess.GustTimer(wind) = Mathf.Min(
                    StormAccess.GustTimer(wind),
                    gustInterval);
            }

            overridesApplied = true;
            wasInsideSquall = insideSquall;
            wasInsideGentleSnow = insideGentleSnow;
        }

        internal static void NotifyDebugSummon(StormKind kind)
        {
            Wind wind = Wind.instance;
            if ((kind != StormKind.Squall &&
                 kind != StormKind.GentleSnow) ||
                wind == null)
            {
                return;
            }

            StormAccess.WindTimer(wind) = 0f;
            StormAccess.GustTimer(wind) = 0f;
        }

        internal static void RestoreImmediately()
        {
            if (trackedWind != null && overridesApplied)
            {
                RestoreForExit(trackedWind);
            }

            trackedWind = null;
            overridesApplied = false;
            wasInsideSquall = false;
            wasInsideGentleSnow = false;
        }

        private static void CaptureNormal(Wind wind)
        {
            normalChangeTimer = wind.changeTimer;
            normalGustInterval = wind.gustChangeTimer;
            normalFinalLerpSpeed = wind.finalLerpSpeed;
        }

        private static float GetNormalChangeTimer()
        {
            return ChaoticWindCompatibility.TryGetFloat(
                "Wind Timing",
                "Wind Change Timer",
                out float configured)
                ? configured
                : normalChangeTimer;
        }

        private static float GetNormalFinalLerpSpeed()
        {
            if (ChaoticWindCompatibility.TryGetBool(
                    "Wind Smoothing",
                    "Override Final Lerp Speed",
                    out bool overrideEnabled) &&
                overrideEnabled &&
                ChaoticWindCompatibility.TryGetFloat(
                    "Wind Smoothing",
                    "Final Lerp Speed",
                    out float configured))
            {
                return configured;
            }

            return normalFinalLerpSpeed;
        }

        private static void RestoreForExit(Wind wind)
        {
            if (!overridesApplied)
            {
                CaptureNormal(wind);
                return;
            }

            wind.changeTimer = GetNormalChangeTimer();
            wind.gustChangeTimer = normalGustInterval;
            wind.finalLerpSpeed = GetNormalFinalLerpSpeed();
            StormAccess.GustTimer(wind) = Mathf.Min(
                StormAccess.GustTimer(wind),
                normalGustInterval);
            overridesApplied = false;
        }
    }

    internal static class StormWindIlRewriter
    {
        private static readonly MethodInfo InverseLerp = AccessTools.Method(
            typeof(Mathf),
            nameof(Mathf.InverseLerp),
            new[] { typeof(float), typeof(float), typeof(float) });
        private static readonly FieldInfo CurrentStormDistance = AccessTools.Field(
            typeof(WeatherStorms),
            nameof(WeatherStorms.currentStormDistance));
        private static readonly MethodInfo StormLerpHelper = AccessTools.Method(
            typeof(StormInfluenceService),
            nameof(StormInfluenceService.GetEffectiveWindLerp));
        private static readonly MethodInfo BonusCapHelper = AccessTools.Method(
            typeof(BonusCapBridge),
            nameof(BonusCapBridge.GetEffectiveCap));

        internal static IEnumerable<CodeInstruction> RewriteVanilla(
            IEnumerable<CodeInstruction> instructions)
        {
            return Rewrite(
                instructions,
                "vanilla Wind.SetNewWindTarget",
                2,
                ChaoticWindCompatibility.IsLoaded);
        }

        internal static IEnumerable<CodeInstruction> RewriteClimate(
            IEnumerable<CodeInstruction> instructions)
        {
            return Rewrite(
                instructions,
                "Climate custom-wind prefix",
                -1,
                ChaoticWindCompatibility.IsLoaded);
        }

        private static IEnumerable<CodeInstruction> Rewrite(
            IEnumerable<CodeInstruction> instructions,
            string target,
            int expectedVanillaCapLoads,
            bool chaoticWindLoaded)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            List<int> stormCalls = FindInverseLerpCalls(
                code,
                13000f,
                500f,
                CurrentStormDistance);

            if (stormCalls.Count != 1)
            {
                LogPatternError(
                    target,
                    "storm curves=" + stormCalls.Count +
                    "; expected exactly one. The method was left unchanged.");
                return code;
            }

            List<int> capLoads = FindFloatLoads(code, 20f);
            if (!chaoticWindLoaded)
            {
                bool validCapCount = expectedVanillaCapLoads >= 0
                    ? capLoads.Count == expectedVanillaCapLoads
                    : capLoads.Count == 1 || capLoads.Count == 2;
                if (!validCapCount)
                {
                    LogPatternError(
                        target,
                        "cap loads=" + capLoads.Count +
                        "; expected " +
                        (expectedVanillaCapLoads >= 0
                            ? expectedVanillaCapLoads.ToString()
                            : "one or two") +
                        ". The method was left unchanged.");
                    return code;
                }
            }

            code[stormCalls[0]].operand = StormLerpHelper;
            if (!chaoticWindLoaded)
            {
                ReplaceLoadsWithCall(code, capLoads, BonusCapHelper);
            }

            BetterStormPlugin.Instance?.LogFeatureInfo(
                "Patched storm wind curve in " + target +
                (chaoticWindLoaded
                    ? "; Chaotic Wind retains bonus-cap ownership."
                    : "; Better Storm fallback cap enabled."));
            return code;
        }

        private static List<int> FindInverseLerpCalls(
            List<CodeInstruction> code,
            float minimum,
            float maximum,
            FieldInfo valueField)
        {
            List<int> matches = new List<int>();
            for (int i = 3; i < code.Count; i++)
            {
                if (Equals(code[i].operand, InverseLerp) &&
                    LoadsFloat(code[i - 3], minimum) &&
                    LoadsFloat(code[i - 2], maximum) &&
                    code[i - 1].opcode == OpCodes.Ldsfld &&
                    Equals(code[i - 1].operand, valueField))
                {
                    matches.Add(i);
                }
            }

            return matches;
        }

        private static List<int> FindFloatLoads(
            List<CodeInstruction> code,
            float value)
        {
            List<int> matches = new List<int>();
            for (int i = 0; i < code.Count; i++)
            {
                if (LoadsFloat(code[i], value))
                {
                    matches.Add(i);
                }
            }

            return matches;
        }

        private static bool LoadsFloat(CodeInstruction instruction, float value)
        {
            return instruction.opcode == OpCodes.Ldc_R4 &&
                instruction.operand is float loaded &&
                loaded == value;
        }

        private static void ReplaceLoadsWithCall(
            List<CodeInstruction> code,
            List<int> indexes,
            MethodInfo helper)
        {
            for (int i = 0; i < indexes.Count; i++)
            {
                int index = indexes[i];
                CodeInstruction original = code[index];
                CodeInstruction replacement = new CodeInstruction(
                    OpCodes.Call,
                    helper);
                replacement.labels.AddRange(original.labels);
                replacement.blocks.AddRange(original.blocks);
                code[index] = replacement;
            }
        }

        private static void LogPatternError(string target, string detail)
        {
            BetterStormPlugin.Instance?.LogFeatureError(
                "Wind IL pattern mismatch in " + target + ": " + detail);
        }
    }

    internal static class ClimateCompatibility
    {
        internal static void Install(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.TryGetValue(
                    BetterStormPlugin.ClimateGuid,
                    out PluginInfo climate) ||
                climate == null ||
                climate.Instance == null)
            {
                return;
            }

            Type patchType = AccessTools.TypeByName(
                "Climate.WeatherPatches+ReplaceWindPatches");
            MethodInfo target = AccessTools.Method(patchType, "SetNewWindTarget");
            if (target == null)
            {
                BetterStormPlugin.Instance?.LogFeatureError(
                    "Climate wind prefix was not found; Better Storm could not " +
                    "install its storm-curve compatibility patch.");
                return;
            }

            try
            {
                HarmonyMethod transpiler = new HarmonyMethod(
                    typeof(ClimateCompatibility),
                    nameof(Transpiler))
                {
                    priority = Priority.Last,
                    after = new[] { BetterStormPlugin.ChaoticWindGuid }
                };
                harmony.Patch(target, transpiler: transpiler);
                BetterStormPlugin.Instance?.LogFeatureInfo(
                    "Installed Climate custom-wind storm compatibility.");
            }
            catch (Exception exception)
            {
                BetterStormPlugin.Instance?.LogFeatureError(
                    "Climate wind compatibility could not be installed: " +
                    exception);
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return StormWindIlRewriter.RewriteClimate(instructions);
        }
    }

    [HarmonyPatch(typeof(Wind), "SetNewWindTarget")]
    internal static class WindSetNewTargetPatch
    {
        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(new[] { BetterStormPlugin.ChaoticWindGuid })]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            return StormWindIlRewriter.RewriteVanilla(instructions);
        }
    }

    [HarmonyPatch(typeof(Wind), "SetNewGustTarget")]
    internal static class StormGustPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            ref Vector3 ___currentGustTarget,
            Vector3 ___currentWindTarget)
        {
            if (!StormInfluenceService.TryGetCurrent(out StormInfluence influence) ||
                influence.IsMagneticMist)
            {
                return true;
            }

            if (influence.IsGentleSnow)
            {
                ___currentGustTarget = ___currentWindTarget;
                return false;
            }

            if (influence.IsDoldrums)
            {
                // The orb's wind gets vanilla gusts (0.67-1.33).
                if (WindTotemCompatibility.OrbControlsWind)
                {
                    return true;
                }

                ___currentGustTarget = ___currentWindTarget *
                    UnityEngine.Random.Range(
                        DoldrumsRules.GustMin,
                        DoldrumsRules.GustMax(influence.WeatherLerp));
                return false;
            }

            ___currentGustTarget = ___currentWindTarget *
                UnityEngine.Random.Range(1f, 1.33f);
            return false;
        }
    }

    [HarmonyPatch(typeof(Wind), "Awake")]
    internal static class BetterStormWindAwakePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            WindOverrideState.Tick();
        }
    }

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

    internal readonly struct StormRainProfile
    {
        internal readonly float Emission;
        internal readonly int MaximumParticles;
        internal readonly float StreakLength;
        internal readonly float SimulationSpeed;
        internal readonly float MinimumSize;
        internal readonly float MaximumSize;
        internal readonly float RainVolume;

        internal StormRainProfile(float emission, float streakLength, float simulationSpeed,
            float minimumSize, float maximumSize, float rainVolume)
        {
            Emission = emission;
            MaximumParticles = StormRainRules.CalculateParticleCapacity(emission);
            StreakLength = streakLength;
            SimulationSpeed = simulationSpeed;
            MinimumSize = minimumSize;
            MaximumSize = maximumSize;
            RainVolume = rainVolume;
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
                kind == StormKind.Hurricane || kind == StormKind.MagneticMist;
        }

        internal static bool ShouldUseStormRain(bool insideStorm, StormKind kind, float baseRain)
        {
            return insideStorm && IsRainStorm(kind) && baseRain > 0f;
        }

        internal static StormRainProfile GetProfile(StormKind kind)
        {
            switch (kind)
            {
                case StormKind.Vanilla: return new StormRainProfile(7000f, 6f, 1.8f, 0.08f, 0.16f, 0.35f);
                case StormKind.Squall: return new StormRainProfile(25000f, 12f, 3f, 0.12f, 0.24f, 0.6f);
                case StormKind.Hurricane: return new StormRainProfile(14000f, 8f, 2.3f, 0.08f, 0.20f, 0.5f);
                // Its audio value is unused: the mist keeps the game's rain sound.
                case StormKind.MagneticMist: return new StormRainProfile(1200f, 6f, 1f, 0.06f, 0.14f, 0f);
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
            main.startSize = new ParticleSystem.MinMaxCurve(profile.MinimumSize, profile.MaximumSize);
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
                CustomPrecipitationParticles.FollowWorldShifts(customRain);
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
            CustomPrecipitationParticles.FollowWorldShifts(customRain);

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

        // World shifts move the shifting world's children, not the shifting
        // world itself (FloatingOriginManager.NewShift). Owned drops simulate
        // relative to this empty child, so each shift carries them along.
        private static Transform shiftingSpace;

        internal static void FollowWorldShifts(ParticleSystem particles)
        {
            if (particles == null)
            {
                return;
            }

            Transform space = GetShiftingSpace();
            ParticleSystem.MainModule main = particles.main;
            if (space == null)
            {
                // No floating origin yet: the released world-space behaviour.
                if (main.simulationSpace != ParticleSystemSimulationSpace.World)
                {
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                }
                return;
            }

            if (main.simulationSpace != ParticleSystemSimulationSpace.Custom ||
                main.customSimulationSpace != space)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Custom;
                main.customSimulationSpace = space;
            }
        }

        internal static void DestroyShiftingSpace()
        {
            if (shiftingSpace != null)
            {
                UnityEngine.Object.Destroy(shiftingSpace.gameObject);
            }

            shiftingSpace = null;
        }

        private static Transform GetShiftingSpace()
        {
            Transform world = Refs.shiftingWorld;
            if (world == null)
            {
                return null;
            }

            if (shiftingSpace != null && shiftingSpace.parent == world)
            {
                return shiftingSpace;
            }

            DestroyShiftingSpace();
            GameObject anchor = new GameObject("Better Storm Precipitation Space");
            anchor.transform.SetParent(world, false);
            shiftingSpace = anchor.transform;
            return shiftingSpace;
        }
    }

    internal static class RainEffects
    {
        internal const int RaisedSplashMaximumParticles = 24000;
        internal const float SplashRadius = 70f;
        internal const float SplashInnerRadius = 0.5f;
        // Unity thickness is the fraction of the outer radius occupied by the ring.
        internal const float SplashRadiusThickness = 1f - SplashInnerRadius / SplashRadius;
        internal const float SplashEmissionPerRainIntensity = 1000f;
        internal const float SquallEmissionMultiplier = 1.2f;

        private static ParticleSystem trackedRainSplash;
        private static int originalSplashMaximumParticles;
        private static float originalSplashRadius;
        private static float originalSplashRadiusThickness;
        private static ParticleSystemCullingMode originalSplashCullingMode;

        internal static float GetEmissionMultiplier(StormKind kind)
        {
            return kind == StormKind.Squall ? SquallEmissionMultiplier : 1f;
        }

        internal static void ApplySplashSettings(ParticleSystem splash)
        {
            if (trackedRainSplash != splash)
            {
                RestoreRainSettings();
                trackedRainSplash = splash;
                if (splash != null)
                {
                    originalSplashMaximumParticles = splash.main.maxParticles;
                    originalSplashRadius = splash.shape.radius;
                    originalSplashRadiusThickness = splash.shape.radiusThickness;
                    originalSplashCullingMode = splash.main.cullingMode;
                }
            }
            if (splash == null) return;
            ParticleSystem.MainModule main = splash.main;
            main.maxParticles = RaisedSplashMaximumParticles;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            ParticleSystem.ShapeModule shape = splash.shape;
            shape.radius = SplashRadius;
            shape.radiusThickness = SplashRadiusThickness;
        }

        internal static void RestoreRainSettings()
        {
            if (trackedRainSplash != null)
            {
                ParticleSystem.MainModule main = trackedRainSplash.main;
                if (main.maxParticles == RaisedSplashMaximumParticles)
                    main.maxParticles = originalSplashMaximumParticles;
                if (main.cullingMode == ParticleSystemCullingMode.AlwaysSimulate)
                    main.cullingMode = originalSplashCullingMode;
                ParticleSystem.ShapeModule shape = trackedRainSplash.shape;
                if (Mathf.Approximately(shape.radius, SplashRadius))
                    shape.radius = originalSplashRadius;
                if (Mathf.Approximately(shape.radiusThickness, SplashRadiusThickness))
                    shape.radiusThickness = originalSplashRadiusThickness;
            }
            trackedRainSplash = null;
            originalSplashMaximumParticles = 0;
            originalSplashRadius = 0f;
            originalSplashRadiusThickness = 0f;
        }

        internal static void ResumeStormSplashes()
        {
            // Called after vanilla Rain.Update has applied indoor suppression.
            // Only the currently owned storm splash emitter may be restarted.
            if (!GameState.playing || GameState.indoors || trackedRainSplash == null ||
                !trackedRainSplash.gameObject.activeInHierarchy) return;
            var emission = trackedRainSplash.emission;
            if (emission.enabled && emission.rateOverTime.constantMax > 0f && trackedRainSplash.isStopped)
                trackedRainSplash.Play(false);
        }
        internal static void ApplyRainGameplay()
        {
            if (TryGetActiveRainIntensity(out float rainIntensity))
            {
                GameState.rainIntensity = rainIntensity;
            }
        }

        internal static void ApplyAudio(
            AudioSource rainLight,
            AudioSource rainMedium,
            AudioSource rainHeavy)
        {
            if (!GameState.playing ||
                !StormInfluenceService.TryGetCurrent(out StormInfluence influence) ||
                !TryGetStormRain(influence, out float rainIntensity, out float strength))
            {
                return;
            }

            // The Magnetic Mist's light rain plays the game's own crossfade (light loop at 3).
            if (influence.Kind == StormKind.MagneticMist)
                return;

            float volume = StormRainRules.GetProfile(influence.Kind).RainVolume;
            SetAudioVolume(rainLight, 0f);
            SetAudioVolume(rainMedium, GetLayerVolume(1f, strength, volume));
            SetAudioVolume(rainHeavy, GetLayerVolume(1f, strength, volume));
        }

        internal static float GetLayerVolume(
            float layerMix,
            float rainStrength,
            float rainVolume)
        {
            return Mathf.Clamp01(layerMix) * Mathf.Clamp01(rainStrength) *
                Mathf.Max(0f, rainVolume);
        }

        private static bool TryGetActiveRainIntensity(out float rainIntensity)
        {
            rainIntensity = 0f;
            return StormInfluenceService.TryGetCurrent(out StormInfluence influence) &&
                TryGetStormRain(influence, out rainIntensity, out float unused);
        }

        internal static bool TryGetStormRain(
            StormInfluence influence, out float rainIntensity, out float visualStrength)
        {
            rainIntensity = 0f;
            visualStrength = 0f;
            BetterStormPlugin settings = BetterStormPlugin.Instance;
            Weather weather = Weather.instance;
            WeatherStorms storms = WeatherStorms.instance;
            if (settings == null || weather == null || storms == null ||
                !influence.Inside || !StormRainRules.IsRainStorm(influence.Kind))
                return false;
            if (influence.Definition != null && !settings.IsEnabled(influence.Definition.Id))
                return false;

            Region region = weather.currentRegion;
            if (region == null || region.stormWeather == null ||
                region.stormWeather.particles == null)
                return false;

            float baseRain;
            float peak;
            if (influence.Kind == StormKind.MagneticMist)
            {
                peak = MagneticMistRules.RainPeak;
                baseRain = MagneticMistRain.Current;
                rainIntensity = Mathf.Max(0f, baseRain);
            }
            else if (influence.Kind == StormKind.Vanilla)
            {
                WeatherParticlesSettings particles = StormAccess.FinalParticles(weather);
                baseRain = particles != null ? particles.rainDensity : 0f;
                peak = region.stormWeather.particles.rainDensity;
                rainIntensity = Mathf.Max(0f, baseRain);
            }
            else
            {
                if (region.rainWeather == null || region.rainWeather.particles == null ||
                    region.cloudyWeather == null || region.cloudyWeather.particles == null)
                    return false;
                peak = influence.Definition.RainTarget.Value;
                baseRain = StormRainRules.GetBaseRain(
                    influence.NormalizedDistance, StormAccess.CloudyBorder(storms),
                    StormAccess.RainBorder(storms), region.cloudyWeather.particles.rainDensity,
                    region.rainWeather.particles.rainDensity, peak);
                rainIntensity = Mathf.Max(0f, baseRain);
            }
            visualStrength = StormRainRules.GetVisualStrength(baseRain, peak);
            // Pressure-only rain in a clear/cloudy storm band is never captured.
            return StormRainRules.ShouldUseStormRain(influence.Inside, influence.Kind, baseRain);
        }
        private static void SetAudioVolume(
            AudioSource source,
            float volume)
        {
            if (source == null)
            {
                return;
            }

            source.volume = volume;
        }
    }

    [HarmonyPatch(typeof(Weather), "ApplyWeather")]
    [HarmonyAfter(new[] { BetterStormPlugin.ClimateGuid })]
    internal static class WeatherEffectsPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            ParticleSystem ___rain,
            ParticleSystem ___outerRain,
            ParticleSystem ___rainSplash,
            ParticleSystem ___lowerClouds,
            ParticleSystem ___upperClouds)
        {
            if (BetterStormPlugin.Instance == null)
            {
                return;
            }

            // The Doldrums' and the Magnetic Mist's fog, clouds and sun disc.
            WeatherSkyModifiers.Apply(___lowerClouds, ___upperClouds);

            bool insideStorm = StormInfluenceService.TryGetCurrent(out StormInfluence influence);
            if (insideStorm && RainEffects.TryGetStormRain(
                    influence, out float intensity, out float strength) &&
                StormRainVisuals.ApplyWeather(influence.Kind, intensity, strength,
                    ___rain, ___outerRain, ___rainSplash))
            {
                SandstormVisuals.Deactivate();
                GentleSnowVisuals.Deactivate();
                return;
            }

            StormRainVisuals.Deactivate();
            RainEffects.RestoreRainSettings();
            if (!insideStorm || influence.Definition == null)
            {
                SandstormVisuals.Deactivate();
                GentleSnowVisuals.Deactivate();
                return;
            }
            if (influence.IsSandstorm)
            {
                GentleSnowVisuals.Deactivate();
                SandstormVisuals.ApplyWeather(influence, ___rain, ___outerRain, ___rainSplash);
                return;
            }
            if (influence.IsGentleSnow)
            {
                SandstormVisuals.Deactivate();
                GentleSnowVisuals.ApplyWeather(influence, ___rain, ___outerRain, ___rainSplash);
                RenderSettings.fogDensity = 0f;
                return;
            }
            SandstormVisuals.Deactivate();
            GentleSnowVisuals.Deactivate();
            if (influence.Definition.SuppressFog) RenderSettings.fogDensity = 0f;
            if (influence.Definition.SuppressRainParticles)
            {
                CustomPrecipitationParticles.SetEmissionRate(___rain, 0f);
                CustomPrecipitationParticles.SetEmissionRate(___outerRain, 0f);
                CustomPrecipitationParticles.SetEmissionRate(___rainSplash, 0f);
                GameState.rainIntensity = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(Rain), "Update")]
    [HarmonyAfter(new[] { BetterStormPlugin.ClimateGuid })]
    internal static class CustomStormRainAudioPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            DryStormGameplay.SuppressRainGameplay();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            AudioSource ___rainLight,
            AudioSource ___rainMedium,
            AudioSource ___rainHeavy)
        {
            RainEffects.ApplyAudio(
                ___rainLight,
                ___rainMedium,
                ___rainHeavy);
            StormRainVisuals.Tick();
            RainEffects.ResumeStormSplashes();
        }
    }

    [HarmonyPatch(typeof(BoatDamage), "UpdateWaterAndDrag")]
    internal static class CustomStormShipWaterPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            DryStormGameplay.SuppressRainGameplay();
            RainEffects.ApplyRainGameplay();
        }
    }

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
            foreach (BoatRefs boat in UnityEngine.Object.FindObjectsOfType<BoatRefs>())
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
