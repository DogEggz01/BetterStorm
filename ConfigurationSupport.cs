using System;
using BepInEx.Configuration;
using UnityEngine;

namespace BetterStorm
{
    // BepInEx Configuration Manager discovers this optional metadata shim by
    // its exact type name. Better Storm does not take a hard dependency on it.
    internal sealed class ConfigurationManagerAttributes
    {
        public bool? ShowRangeAsPercent;
        public Action<ConfigEntryBase> CustomDrawer;
        public bool? HideDefaultButton;
        public bool? HideSettingName;
        public string DispName = null;
        public int? Order;
    }

    internal sealed class SteppedAcceptableValueRange : AcceptableValueRange<float>
    {
        private readonly float step;

        internal SteppedAcceptableValueRange(float minimum, float maximum, float step)
            : base(minimum, maximum)
        {
            this.step = step;
        }

        public override object Clamp(object value)
        {
            float clamped = (float)base.Clamp(value);
            float snapped = MinValue +
                Mathf.Floor((clamped - MinValue) / step + 0.5f) * step;
            return Mathf.Clamp(snapped, MinValue, MaxValue);
        }

        public override bool IsValid(object value)
        {
            if (!(value is float floatValue))
            {
                return false;
            }

            return Mathf.Approximately(floatValue, (float)Clamp(floatValue));
        }
    }

    internal sealed class SteppedAcceptableIntRange : AcceptableValueRange<int>
    {
        private readonly int step;

        internal SteppedAcceptableIntRange(int minimum, int maximum, int step)
            : base(minimum, maximum)
        {
            this.step = step;
        }

        public override object Clamp(object value)
        {
            int clamped = (int)base.Clamp(value);
            int snapped = MinValue +
                (int)Mathf.Floor(
                    (clamped - MinValue) / (float)step + 0.5f) * step;
            return Mathf.Clamp(snapped, MinValue, MaxValue);
        }

        public override bool IsValid(object value)
        {
            return value is int intValue && intValue == (int)Clamp(intValue);
        }
    }
}
