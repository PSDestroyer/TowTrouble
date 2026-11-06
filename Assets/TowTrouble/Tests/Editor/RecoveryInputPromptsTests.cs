using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace TowTrouble.Tests
{
    public sealed class RecoveryInputPromptsTests
    {
        [TestCase(RecoveryControllerFamily.Xbox,"X","Y","Menu","View","LT/RT","RS")]
        [TestCase(RecoveryControllerFamily.PlayStation,"Square","Triangle","Options","Share","L2/R2","R3")]
        [TestCase(RecoveryControllerFamily.Nintendo,"Y","X","+","-","ZL/ZR","R-stick")]
        public void GarageAuxiliaryActionsHaveMatchingPlatformLabels(RecoveryControllerFamily family,string west,string north,string menu,string select,string triggers,string reset)
        {
            Assert.That(RecoveryInputPrompts.WestLabel(family),Is.EqualTo(west));
            Assert.That(RecoveryInputPrompts.NorthLabel(family),Is.EqualTo(north));
            Assert.That(RecoveryInputPrompts.MenuLabel(family),Is.EqualTo(menu));
            Assert.That(RecoveryInputPrompts.SelectLabel(family),Is.EqualTo(select));
            Assert.That(RecoveryInputPrompts.TriggerLabels(family),Is.EqualTo(triggers));
            Assert.That(RecoveryInputPrompts.RightStickPressLabel(family),Is.EqualTo(reset));
        }
        [TestCase("XInputController", "Microsoft", "", RecoveryControllerFamily.Xbox)]
        [TestCase("DualShock4GamepadHID", "", "", RecoveryControllerFamily.PlayStation)]
        [TestCase("DualSenseGamepadHID", "Sony", "Wireless Controller", RecoveryControllerFamily.PlayStation)]
        [TestCase("SwitchProControllerHID", "", "", RecoveryControllerFamily.Nintendo)]
        [TestCase("Gamepad", "Nintendo", "Pro Controller", RecoveryControllerFamily.Nintendo)]
        [TestCase("Gamepad", "", "Unknown", RecoveryControllerFamily.Xbox)]
        public void DeviceIdentitySelectsThePromptFamily(string layout, string manufacturer, string product, RecoveryControllerFamily family)
            => Assert.That(RecoveryInputPrompts.Identify(layout, manufacturer, product), Is.EqualTo(family));

        [TestCase("Microsoft", false, "A", "B", "LB", "RB")]
        [TestCase("Sony", false, "Cross", "Circle", "L1", "R1")]
        [TestCase("Nintendo", true, "A", "B", "L", "R")]
        public void ConfirmAndCancelMatchEachDevicesPrintedButtons(string manufacturer, bool eastConfirms, string confirm, string cancel, string left, string right)
        {
            // A generic device with an identity matcher avoids native HID dependencies.
            string layout = "RecoveryPromptTest" + manufacturer;
            var original = Gamepad.current;
            Gamepad pad = null;
            InputSystem.RegisterLayout("{\"name\":\"" + layout + "\",\"extend\":\"Gamepad\"}", name: layout, matches: new InputDeviceMatcher().WithManufacturer(manufacturer).WithProduct("Recovery prompt test"));
            try
            {
                pad = (Gamepad)InputSystem.AddDevice(new UnityEngine.InputSystem.Layouts.InputDeviceDescription { manufacturer = manufacturer, product = "Recovery prompt test" });
                var family = RecoveryInputPrompts.Family(pad);
                Assert.That(RecoveryInputPrompts.Confirm(pad), Is.SameAs(eastConfirms ? pad.buttonEast : pad.buttonSouth));
                Assert.That(RecoveryInputPrompts.Cancel(pad), Is.SameAs(eastConfirms ? pad.buttonSouth : pad.buttonEast));
                Assert.That(RecoveryInputPrompts.ConfirmLabel(family), Is.EqualTo(confirm));
                Assert.That(RecoveryInputPrompts.CancelLabel(family), Is.EqualTo(cancel));
                Assert.That(RecoveryInputPrompts.PreviousTabLabel(family), Is.EqualTo(left));
                Assert.That(RecoveryInputPrompts.NextTabLabel(family), Is.EqualTo(right));
            }
            finally
            {
                if (pad != null) InputSystem.RemoveDevice(pad);
                InputSystem.RemoveLayout(layout);
                if (original != null && original.added) original.MakeCurrent();
            }
        }
    }
}
