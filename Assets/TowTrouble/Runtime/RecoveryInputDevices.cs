using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace TowTrouble
{
    // Prompts and controls share the same local device; remote players never select it.
    public static class RecoveryInputDevices
    {
        private static Gamepad activePad;
        private static bool gamepadUsed;
        public static Gamepad Pad => activePad != null && activePad.added && activePad.enabled ? activePad : ConnectedPad();
        public static bool UsingGamepad => gamepadUsed && Pad != null;
        public static int Revision { get; private set; }
        private static Gamepad ConnectedPad()
        { foreach (var pad in Gamepad.all) if (pad.added && pad.enabled) return pad; return null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        { InputSystem.onEvent -= OnInput; InputSystem.onDeviceChange -= OnDeviceChange; activePad = null; gamepadUsed = false; Revision = 0; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            InputSystem.onEvent -= OnInput; InputSystem.onEvent += OnInput;
            InputSystem.onDeviceChange -= OnDeviceChange; InputSystem.onDeviceChange += OnDeviceChange;
            activePad = ConnectedPad(); gamepadUsed = activePad != null; Revision++;
        }
        private static void Select(Gamepad pad)
        { bool usePad = pad != null; if (gamepadUsed == usePad && (!usePad || activePad == pad)) return; if (pad != null) activePad = pad; gamepadUsed = usePad; Revision++; }
        private static void OnInput(InputEventPtr evt, InputDevice device)
        {
            if (evt.type != StateEvent.Type && evt.type != DeltaStateEvent.Type) return;
            if (!(device is Gamepad) && !(device is Keyboard) && !(device is Mouse)) return;
            foreach (var control in evt.EnumerateChangedControls(device, .3f))
            {
                if (control.noisy || control.synthetic) continue;
                if (device is Gamepad pad) { Select(pad); return; }
                if (device is Keyboard && control is KeyControl) { Select(null); return; }
                if (device is Mouse mouse)
                {
                    if (control is ButtonControl || control == mouse.scroll || control.parent == mouse.scroll ||
                        (control == mouse.delta || control.parent == mouse.delta) && mouse.delta.ReadValueFromEvent(evt).sqrMagnitude > 16)
                    { Select(null); return; }
                }
            }
        }
        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!(device is Gamepad pad)) return;
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected || change == InputDeviceChange.Enabled) Select(pad);
            else if (pad == activePad && (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected || change == InputDeviceChange.Disabled))
            { activePad = ConnectedPad(); gamepadUsed = activePad != null; Revision++; }
        }
    }
}
