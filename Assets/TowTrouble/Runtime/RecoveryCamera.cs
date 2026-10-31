using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed class RecoveryCamera : MonoBehaviour
    {
        private DemoSession session;
        private Vector3 velocity;
        private float orbit, elevation=8f;
        public void Bind(DemoSession owner){session=owner;}
        private void LateUpdate()
        {
            if(session==null)return;
            var p=session.Profile;bool lobby=!session.Connected||session.World.Phase==RecoveryPhase.Lobby;
            Vector3 position,look;
            if(lobby||session.GarageOpen)
            {
                var car=session.World.Drivers[Mathf.Max(0,session.LocalSlot)].transform;var center=car.position;
                if(session.View=="Vehicles"||session.GarageOpen)
                {
                    if(Mouse.current!=null&&Mouse.current.leftButton.isPressed&&(UnityEngine.EventSystems.EventSystem.current==null||!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))orbit+=Mouse.current.delta.x.ReadValue()*.16f*p.cameraSensitivity;
                    if(Gamepad.current!=null)orbit+=Gamepad.current.rightStick.x.ReadValue()*Time.unscaledDeltaTime*80*p.cameraSensitivity;
                    if(session.GarageOpen)
                    {
                        var box=car.GetComponent<BoxCollider>();var focus=box==null?center+Vector3.up:box.bounds.center;
                        var offset=Quaternion.Euler(0,orbit,0)*new Vector3(9,3.4f,-12);var right=Vector3.Cross(Vector3.up,-offset).normalized;
                        position=focus+offset;look=focus+right*1.7f+Vector3.up*.45f;if(Physics.Linecast(focus,position,out var obstruction,1<<8,QueryTriggerInteraction.Ignore))position=obstruction.point+obstruction.normal*.7f;
                    }
                    else {position=center+Quaternion.Euler(0,orbit,0)*new Vector3(7,3.8f,9);look=center+new Vector3(2.2f,1,-2.2f);}
                }
                else if(session.View=="Lobby") {position=new Vector3(10,6,41);look=new Vector3(0,1,30);}
                else {position=center+new Vector3(12,4.4f,5);look=center+new Vector3(1.5f,1,-3.5f);if(!p.reducedMotion)position.x+=Mathf.Sin(Time.unscaledTime*.12f)*.15f;}
            }
            else if(session.World.Phase==RecoveryPhase.Complete)
            {var target=session.World.Casualty.transform.position;position=target+new Vector3(8,4.6f,9);look=target+Vector3.up;}
            else
            {
                int slot=Mathf.Max(0,session.LocalSlot);var car=session.World.Drivers[slot].transform;bool moving=false;
                if(!session.MenuOpen)
                {
                    if(Mouse.current!=null&&Mouse.current.rightButton.isPressed){var delta=Mouse.current.delta.ReadValue();orbit+=delta.x*.12f*p.cameraSensitivity;elevation=Mathf.Clamp(elevation+delta.y*(p.invertCamera?1:-1)*.015f*p.cameraSensitivity,3,13);moving=true;}
                    if(Gamepad.current!=null){var stick=Gamepad.current.rightStick.ReadValue();if(stick.sqrMagnitude>.05f){orbit+=stick.x*Time.unscaledDeltaTime*90*p.cameraSensitivity;elevation=Mathf.Clamp(elevation+stick.y*(p.invertCamera?-1:1)*Time.unscaledDeltaTime*5,3,13);moving=true;}if(Gamepad.current.buttonNorth.wasPressedThisFrame){orbit=0;elevation=8;}}
                    if(Keyboard.current!=null&&Keyboard.current.cKey.wasPressedThisFrame){orbit=0;elevation=8;}
                }
                if(!moving)orbit=Mathf.LerpAngle(orbit,0,Time.deltaTime*2.5f);
                var rotation=Quaternion.Euler(0,car.eulerAngles.y+orbit,0);var target=session.World.Casualty.transform.position;var center=session.World.HasContract&&Vector3.Distance(car.position,target)<28?Vector3.Lerp(car.position,target,.38f):car.position;
                position=center+rotation*new Vector3(9.5f,elevation,-14);look=center+Vector3.up*1.2f+car.forward;
                if(Physics.Linecast(look,position,out var hit,1<<8))position=hit.point+hit.normal*.5f;
            }
            transform.position=Vector3.SmoothDamp(transform.position,position,ref velocity,p.reducedMotion?.1f:.24f,Mathf.Infinity,Time.unscaledDeltaTime);
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(look-transform.position),1-Mathf.Exp(-7*Time.unscaledDeltaTime));
        }
    }
}
