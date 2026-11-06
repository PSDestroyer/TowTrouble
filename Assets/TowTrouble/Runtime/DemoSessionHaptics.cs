using UnityEngine;
using UnityEngine.InputSystem;
namespace TowTrouble
{
    public sealed partial class DemoSession
    {
        private readonly RecoveryHaptics hapticSignal=new RecoveryHaptics();
        private readonly RecoveryHapticOutput hapticOutput=new RecoveryHapticOutput();
        private Gamepad hapticDevice;
        private bool hapticFocus=true,hapticPaused,wasHapticInput;
        private void OnEnable(){hapticFocus=Application.isFocused;InputSystem.onDeviceChange+=HapticDeviceChanged;}
        private void HapticDeviceChanged(InputDevice device,InputDeviceChange change)
        {
            if(device==hapticDevice&&(change==InputDeviceChange.Disconnected||change==InputDeviceChange.Removed||change==InputDeviceChange.Disabled||change==InputDeviceChange.Reconnected||change==InputDeviceChange.Enabled))StopHaptics();
        }
        private void LateUpdate()
        {
            var pad=RecoveryInputDevices.Pad;bool input=UsingGamepad;
            if(hapticDevice!=pad||wasHapticInput!=input){hapticOutput.Clear();hapticSignal.Reset();hapticDevice=pad;wasHapticInput=input;}
            bool connected=Connected&&LocalSlot>=0&&LocalSlot<2&&world!=null;
            var frame=new RecoveryHapticFrame();
            if(connected)
            {
                int slot=LocalSlot;
                frame=new RecoveryHapticFrame { Active=world.HapticTelemetryReady&&input&&Profile!=null&&Profile.vibration&&!menuOpen&&View=="Gameplay"&&hapticFocus&&!hapticPaused&&Time.timeScale>0&&world.Phase!=RecoveryPhase.Lobby&&world.Phase!=RecoveryPhase.Complete,
                    Slot=slot,Run=world.RunId,Resets=world.Resets(slot),Attached=world.Attached(slot),Tension=world.Tension(slot),Winch=world.Winch(slot),
                    Recovering=world.Phase==RecoveryPhase.Recover,Delivered=world.Phase==RecoveryPhase.Deliver,Events=world.HapticEvents(slot) };
            }
            var signal=hapticSignal.Step(frame,Time.unscaledDeltaTime,Profile!=null?Profile.vibrationIntensity:0);
            hapticOutput.Write(pad,signal,Time.realtimeSinceStartupAsDouble);
        }
        private void StopHaptics(){hapticSignal.Reset();hapticOutput.Clear();hapticDevice=null;}
        private void OnDisable(){InputSystem.onDeviceChange-=HapticDeviceChanged;StopHaptics();}
        private void OnApplicationFocus(bool focused){hapticFocus=focused;if(!focused)StopHaptics();}
        private void OnApplicationPause(bool paused){hapticPaused=paused;if(paused)StopHaptics();}
        private void OnApplicationQuit(){StopHaptics();}
    }
}
