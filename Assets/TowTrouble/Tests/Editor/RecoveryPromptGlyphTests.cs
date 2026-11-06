using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace TowTrouble.Tests
{
    public sealed class RecoveryPromptGlyphTests
    {
        [Test] public void AtlasContainsEveryKeyboardLetterAndEveryMenuControllerAction()
        {
            Assert.That(RecoveryPromptText.Atlas,Is.Not.Null);
            foreach(char c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789") Assert.That(RecoveryPromptText.Atlas.GetSpriteIndexFromName("Keyboard_"+c),Is.GreaterThanOrEqualTo(0));
            foreach(var family in new[]{RecoveryControllerFamily.Xbox,RecoveryControllerFamily.PlayStation,RecoveryControllerFamily.Nintendo})
                foreach(string token in new[]{RecoveryInputPrompts.ConfirmLabel(family),RecoveryInputPrompts.CancelLabel(family),RecoveryInputPrompts.PreviousTabLabel(family),RecoveryInputPrompts.NextTabLabel(family),RecoveryInputPrompts.WestLabel(family),RecoveryInputPrompts.NorthLabel(family),RecoveryInputPrompts.MenuLabel(family),RecoveryInputPrompts.SelectLabel(family),RecoveryInputPrompts.RightStickPressLabel(family),RecoveryInputPrompts.LeftStickPressLabel(family),"D-pad","Left stick","Right stick"})
                    Assert.That(RecoveryPromptText.Atlas.GetSpriteIndexFromName(RecoveryPromptText.GlyphName(token,true,family)),Is.GreaterThanOrEqualTo(0),family+" "+token);
        }
        [TestCase(RecoveryControllerFamily.Xbox,"A","Xbox_A")]
        [TestCase(RecoveryControllerFamily.PlayStation,"Cross","PS_Cross")]
        [TestCase(RecoveryControllerFamily.Nintendo,"A","Nintendo_A")]
        public void AHintUsesOnlyItsControllersIconFamily(RecoveryControllerFamily family,string key,string name)
        {
            string result=RecoveryPromptText.Format(key+" · Select",true,family);
            Assert.That(result,Does.Contain("name=\""+name+"\""));Assert.That(result,Does.Not.Contain("Keyboard_"));
        }
        [Test] public void KeyboardCapsPreserveCaptionsAndCustomBindings()
        {
            Assert.That(RecoveryPromptText.Format("H · Attach cable",false,RecoveryControllerFamily.Xbox),Does.Contain("Keyboard_H"));
            Assert.That(RecoveryPromptText.Format("WASD / Arrows · Navigate",false,RecoveryControllerFamily.Xbox),Does.Contain("Keyboard_WASD").And.Contain("Keyboard_Arrows").And.Contain("Navigate"));
            Assert.That(RecoveryPromptText.Format("[Q]",false,RecoveryControllerFamily.Xbox),Does.Not.Contain("[").And.Not.Contain("]"));
        }
        [Test] public void SwitchingInputIgnoresStationaryStickStateAndHandlesDisconnect()
        {
            Gamepad pad=null;Keyboard keyboard=null;
            try
            {
                RecoveryInputDevices.Initialize();pad=InputSystem.AddDevice<Gamepad>();keyboard=InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=new Vector2(.6f,0)});InputSystem.Update();Assert.That(RecoveryInputDevices.UsingGamepad,Is.True);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));InputSystem.Update();Assert.That(RecoveryInputDevices.UsingGamepad,Is.False);
                // A repeated native packet with the same held stick must not steal keyboard hints.
                InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=new Vector2(.6f,0)});InputSystem.Update();Assert.That(RecoveryInputDevices.UsingGamepad,Is.False);
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));InputSystem.Update();Assert.That(RecoveryInputDevices.Pad,Is.SameAs(pad));Assert.That(RecoveryInputDevices.UsingGamepad,Is.True);
                InputSystem.RemoveDevice(pad);pad=null;Assert.That(RecoveryInputDevices.Pad==null||RecoveryInputDevices.Pad.added,Is.True);
            }
            finally {if(pad!=null)InputSystem.RemoveDevice(pad);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);RecoveryInputDevices.Initialize();}
        }
        [Test] public void KnownDeviceIdentityWinsOverAnOldManualPromptPreference()
        {
            Gamepad pad=null;int old=RecoveryInputPrompts.FamilyOverride;const string layout="TowGlyphSony";
            try
            {
                InputSystem.RegisterLayout("{\"name\":\""+layout+"\",\"extend\":\"Gamepad\"}",name:layout,matches:new InputDeviceMatcher().WithManufacturer("Sony").WithProduct("Glyph test"));
                pad=(Gamepad)InputSystem.AddDevice(new InputDeviceDescription{manufacturer="Sony",product="Glyph test"});RecoveryInputPrompts.FamilyOverride=3;
                Assert.That(RecoveryInputPrompts.Family(pad),Is.EqualTo(RecoveryControllerFamily.PlayStation));
            }
            finally{if(pad!=null)InputSystem.RemoveDevice(pad);InputSystem.RemoveLayout(layout);RecoveryInputPrompts.FamilyOverride=old;RecoveryInputDevices.Initialize();}
        }
    }
}
