using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace TowTrouble
{
    public enum RecoveryControllerFamily { Xbox, PlayStation, Nintendo }

    // Semantic menu actions are separate from physical positions and platform labels.
    public static class RecoveryInputPrompts
    {
        public static int FamilyOverride;
        public static RecoveryControllerFamily Family(Gamepad pad)
            => FamilyOverride>=1&&FamilyOverride<=3?(RecoveryControllerFamily)(FamilyOverride-1):pad == null ? RecoveryControllerFamily.Xbox : Identify(pad.layout, pad.description.manufacturer, pad.description.product);
        public static RecoveryControllerFamily Identify(string layout, string manufacturer, string product)
        {
            string identity = (layout ?? "") + " " + (manufacturer ?? "") + " " + (product ?? "");
            if (Has(identity, "DualShock") || Has(identity, "DualSense") || Has(identity, "PlayStation") || Has(identity, "Sony"))
                return RecoveryControllerFamily.PlayStation;
            if (Has(identity, "Nintendo") || Has(identity, "Switch") || Has(identity, "Joy-Con"))
                return RecoveryControllerFamily.Nintendo;
            return RecoveryControllerFamily.Xbox;
        }
        private static bool Has(string value, string part) => value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        public static ButtonControl Confirm(Gamepad pad) => pad == null ? null : Family(pad) == RecoveryControllerFamily.Nintendo ? pad.buttonEast : pad.buttonSouth;
        public static ButtonControl Cancel(Gamepad pad) => pad == null ? null : Family(pad) == RecoveryControllerFamily.Nintendo ? pad.buttonSouth : pad.buttonEast;
        public static string ConfirmLabel(RecoveryControllerFamily family) => family == RecoveryControllerFamily.PlayStation ? "Cross" : "A";
        public static string CancelLabel(RecoveryControllerFamily family) => family == RecoveryControllerFamily.PlayStation ? "Circle" : "B";
        public static string PreviousTabLabel(RecoveryControllerFamily family) => family == RecoveryControllerFamily.PlayStation ? "L1" : family == RecoveryControllerFamily.Nintendo ? "L" : "LB";
        public static string NextTabLabel(RecoveryControllerFamily family) => family == RecoveryControllerFamily.PlayStation ? "R1" : family == RecoveryControllerFamily.Nintendo ? "R" : "RB";
        public static string WestLabel(RecoveryControllerFamily family) => family == RecoveryControllerFamily.PlayStation ? "Square" : family == RecoveryControllerFamily.Nintendo ? "Y" : "X";
        public static string NorthLabel(RecoveryControllerFamily family) => family == RecoveryControllerFamily.PlayStation ? "Triangle" : family == RecoveryControllerFamily.Nintendo ? "X" : "Y";
        public static string MenuLabel(RecoveryControllerFamily family) => family == RecoveryControllerFamily.PlayStation ? "Options" : family == RecoveryControllerFamily.Nintendo ? "+" : "Menu";
        public static string SelectLabel(RecoveryControllerFamily family) => family == RecoveryControllerFamily.PlayStation ? "Share" : family == RecoveryControllerFamily.Nintendo ? "-" : "View";
        public static string TriggerLabels(RecoveryControllerFamily family) => family == RecoveryControllerFamily.PlayStation ? "L2/R2" : family == RecoveryControllerFamily.Nintendo ? "ZL/ZR" : "LT/RT";
        public static string RightStickPressLabel(RecoveryControllerFamily family) => family == RecoveryControllerFamily.PlayStation ? "R3" : family == RecoveryControllerFamily.Nintendo ? "R-stick" : "RS";
    }
}
