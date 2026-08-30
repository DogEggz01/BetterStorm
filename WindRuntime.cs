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

            if (!StormInfluenceService.TryGetCurrent(out StormInfluence influence))
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
            if ((insideSquall && !wasInsideSquall) ||
                (insideGentleSnow && !wasInsideGentleSnow))
            {
                StormAccess.WindTimer(wind) = 0f;
                StormAccess.GustTimer(wind) = 0f;
            }

            float gustInterval = Mathf.Lerp(
                normalGustInterval,
                StormCoreGustInterval,
                influence.WindLerp);
            wind.finalLerpSpeed = insideGentleSnow
                ? GentleSnowRules.FinalLerpSpeed
                : StormFinalLerpSpeed;
            wind.gustChangeTimer = gustInterval;
            wind.changeTimer = insideSquall
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
            if (!StormInfluenceService.TryGetCurrent(out StormInfluence influence))
            {
                return true;
            }

            if (influence.IsGentleSnow)
            {
                ___currentGustTarget = ___currentWindTarget;
                return false;
            }

            ___currentGustTarget = ___currentWindTarget *
                UnityEngine.Random.Range(1f, 1.33f);
            return false;
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

    [HarmonyPatch(typeof(Wind), "Awake")]
    internal static class BetterStormWindAwakePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            WindOverrideState.Tick();
        }
    }
}
