using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace BetterStorm
{
    [Flags]
    internal enum ClimateSeasonMask
    {
        None = 0,
        Winter = 1,
        Spring = 2,
        Summer = 4,
        Autumn = 8,
        All = Winter | Spring | Summer | Autumn
    }

    internal static class ClimateSeasonCompatibility
    {
        private const int DefaultYearLength = 92;

        internal static bool Allows(StormDefinition definition)
        {
            if (definition == null ||
                definition.AllowedClimateSeasons == ClimateSeasonMask.All)
            {
                return true;
            }

            BetterStormPlugin plugin = BetterStormPlugin.Instance;
            if (plugin == null ||
                plugin.DisableSeasonLimits.Value ||
                !TryGetActiveClimateYear(out int yearLength))
            {
                return true;
            }

            ClimateSeasonMask season = GetSeason(GameState.day, yearLength);
            return (definition.AllowedClimateSeasons & season) != 0;
        }

        internal static ClimateSeasonMask GetSeason(int day, int yearLength)
        {
            int validYearLength = Math.Max(4, yearLength);
            int dayInYear = day % validYearLength;
            if (dayInYear < 0)
            {
                dayInYear += validYearLength;
            }

            int daysPerSeason = validYearLength / 4;
            if (dayInYear < daysPerSeason)
            {
                return ClimateSeasonMask.Winter;
            }
            if (dayInYear < daysPerSeason * 2)
            {
                return ClimateSeasonMask.Spring;
            }
            if (dayInYear < daysPerSeason * 3)
            {
                return ClimateSeasonMask.Summer;
            }
            return ClimateSeasonMask.Autumn;
        }

        private static bool TryGetActiveClimateYear(out int yearLength)
        {
            yearLength = DefaultYearLength;
            if (!Chainloader.PluginInfos.TryGetValue(
                    BetterStormPlugin.ClimateGuid,
                    out PluginInfo climate) ||
                climate == null ||
                climate.Instance == null ||
                !climate.Instance.Config.TryGetEntry(
                    "Settings",
                    "Enable Custom Winds",
                    out ConfigEntry<bool> customWinds) ||
                !customWinds.Value)
            {
                return false;
            }

            if (climate.Instance.Config.TryGetEntry(
                    "Settings",
                    "Days In A Year",
                    out ConfigEntry<int> configuredYearLength))
            {
                yearLength = configuredYearLength.Value;
            }

            return yearLength >= 4;
        }
    }
}
