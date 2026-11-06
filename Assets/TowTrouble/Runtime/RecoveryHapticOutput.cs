using UnityEngine;
using UnityEngine.InputSystem;
namespace TowTrouble
{
    // Uses the device's virtual API, including console-specific implementations. No family-specific native SDK guesses.
    public sealed class RecoveryHapticOutput
    {
        private Gamepad device;
        private Vector2 sent;
        private double nextSend;
        public void Write(Gamepad target,Vector2 signal,double now)
        {
            if(float.IsNaN(signal.x)||float.IsNaN(signal.y)||float.IsInfinity(signal.x)||float.IsInfinity(signal.y))signal=Vector2.zero;
            signal=new Vector2(Mathf.Clamp(signal.x,0,RecoveryHaptics.MaximumLow),Mathf.Clamp(signal.y,0,RecoveryHaptics.MaximumHigh));
            if(device!=target){Stop();device=target;nextSend=0;}
            if(device==null||!device.added)return;
            if(!device.enabled){Stop();return;}
            if(signal.sqrMagnitude<.000001f){Stop();return;}
            if(now<nextSend||(sent-signal).sqrMagnitude<.000025f)return;
            device.SetMotorSpeeds(signal.x,signal.y);sent=signal;nextSend=now+1.0/30;
        }
        public void Stop()
        {
            if(device!=null&&device.added&&sent.sqrMagnitude>0)device.ResetHaptics();
            sent=Vector2.zero;nextSend=0;
        }
        public void Clear() { Stop();device=null; }
    }
}
