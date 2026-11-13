using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryCamera : MonoBehaviour
    {
        [Header("Player Follow")]
        [Tooltip("Distance behind the player car when it is framed alone. Smaller values bring the camera closer; recovery framing keeps its wider view.")]
        [SerializeField, Min(1f)] private float playerFollowDistance = 9f;

        private DemoSession session;
        private Vector3 velocity;
        private float orbit, elevation=8f;
        private float extractionFraming;
        private bool gameplayCameraActive;
        public void Bind(DemoSession owner)
        {
            session = owner; velocity = Vector3.zero; gameplayCameraActive = false;
            if (isActiveAndEnabled) IndexCameraTrees();
        }
        private void LateUpdate()
        {
            if(session==null){DisableTitleLighting();return;}
            if (session.Connecting)
            {
                // Keep the last valid menu view while authority, slot and spawn are being prepared.
                velocity = Vector3.zero; gameplayCameraActive = false;
                UpdateTitleLighting(false); return;
            }
            if(session.ContractIntroCamera(out var incidentPosition,out var incidentLook))
            {
                velocity=Vector3.zero;gameplayCameraActive=false;
                transform.SetPositionAndRotation(incidentPosition,Quaternion.LookRotation(incidentLook-incidentPosition));
                UpdateTitleLighting(false);UpdateTreeVisibility(false);return;
            }
            if(!session.GarageOpen)garageCameraActive=false;
            var p=session.Profile;bool lobby=!session.ReadyForPresentation||session.World.Phase==RecoveryPhase.Lobby;
            bool title=session.View=="Title";
            bool enteringTitle=title&&!titleCameraActive;titleCameraActive=title;
            bool choosingStarter=session.View=="Starter"||session.View=="Confirm"&&!p.starterChosen;
            if(!choosingStarter)starterCameraActive=false;
            var towedPickup = session.Expedition?.TowedPickup;
            bool frameWheel=towedPickup!=null||(session.World.HasContract&&!session.World.ClearingObstacle&&(session.World.UsesArcadeExtraction||(session.World.FreeRoam&&(session.World.Contract.SituationKind==RecoverySituationKind.Ditch||session.World.Contract.SituationKind==RecoverySituationKind.Embankment))));
            extractionFraming=Mathf.MoveTowards(extractionFraming,frameWheel?1:0,Time.unscaledDeltaTime*2);
            Vector3 position,look;
            if(lobby||session.GarageOpen)
            {
                var car=session.World.Drivers[Mathf.Max(0,session.LocalSlot)].transform;var center=car.position;
                if(title)TitleCameraPose(session.World.Drivers[Mathf.Max(0,session.LocalSlot)],out position,out look);
                else if(choosingStarter||session.GarageOpen)
                {
                    float rotationInput=0;
                    if(!session.GarageCameraInputBlocked&&Mouse.current!=null&&Mouse.current.leftButton.isPressed&&(UnityEngine.EventSystems.EventSystem.current==null||!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))rotationInput+=Mouse.current.delta.x.ReadValue()*.16f*p.cameraSensitivity;
                    if(!session.GarageCameraInputBlocked&&RecoveryInputDevices.Pad!=null)rotationInput+=RecoveryInputDevices.Pad.rightStick.x.ReadValue()*Time.unscaledDeltaTime*80*p.cameraSensitivity;
                    if(session.GarageOpen)
                        GarageCameraPose(session.World.Drivers[Mathf.Max(0,session.LocalSlot)],rotationInput,p.reducedMotion,Time.unscaledDeltaTime,out position,out look);
                    else
                    {
                        bool reset=Keyboard.current?.cKey.wasPressedThisFrame==true||RecoveryInputDevices.Pad?.rightStickButton.wasPressedThisFrame==true;
                        StarterCameraPose(session.World.Drivers[Mathf.Max(0,session.LocalSlot)],session.View=="Starter"?rotationInput:0,reset,out position,out look);
                    }
                }
                else if(session.View=="Lobby") {position=session.World.Map!=null?center+new Vector3(10,6,11):new Vector3(10,6,41);look=session.World.Map!=null?center+Vector3.up:new Vector3(0,1,30);}
                else {MenuPose(session.World.Drivers[Mathf.Max(0,session.LocalSlot)],out position,out look);}
            }
            else if(session.World.Phase==RecoveryPhase.Complete)
            {var target=session.World.TowTargetPosition;position=target+new Vector3(8,4.6f,9);look=target+Vector3.up;}
            else
            {
                int slot=Mathf.Max(0,session.LocalSlot);var car=session.World.Drivers[slot].transform;bool moving=false;
                if(!session.MenuOpen)
                {
                    if(Mouse.current!=null&&Mouse.current.rightButton.isPressed){var delta=Mouse.current.delta.ReadValue();orbit+=delta.x*.12f*p.cameraSensitivity;elevation=Mathf.Clamp(elevation+delta.y*(p.invertCamera?1:-1)*.015f*p.cameraSensitivity,3,13);moving=true;}
                    if(RecoveryInputDevices.Pad!=null){var stick=RecoveryInputDevices.Pad.rightStick.ReadValue();if(stick.sqrMagnitude>.05f){orbit+=stick.x*Time.unscaledDeltaTime*90*p.cameraSensitivity;elevation=Mathf.Clamp(elevation+stick.y*(p.invertCamera?-1:1)*Time.unscaledDeltaTime*5,3,13);moving=true;}if(RecoveryInputDevices.Pad.buttonNorth.wasPressedThisFrame){orbit=0;elevation=8;}}
                    if(Keyboard.current!=null&&Keyboard.current.cKey.wasPressedThisFrame){orbit=0;elevation=8;}
                }
                if(!moving)orbit=Mathf.LerpAngle(orbit,0,Time.deltaTime*2.5f);
                var rotation=Quaternion.Euler(0,car.eulerAngles.y+orbit,0);
                var target = towedPickup != null ? towedPickup.transform.position : session.World.TowTargetPosition;
                float separation = Vector3.Distance(car.position, target);
                bool includeTarget = towedPickup != null || (session.World.HasContract && separation < 28f);
                float targetWeight = Mathf.Lerp(.38f,.5f,extractionFraming);
                var center = includeTarget ? Vector3.Lerp(car.position, target, targetWeight) : car.position;
                var followOffset=includeTarget
                    ?new Vector3(9.5f,elevation,-14f)
                    :new Vector3(6f,elevation*.7f,-Mathf.Max(1f,playerFollowDistance));
                if(includeTarget)
                {
                    // The authored wheel ditches are on the left. Keep both vehicles in
                    // frame from that side, and retain the player's manual orbit.
                    float spread=Mathf.Clamp(separation/10f,.9f,2.2f);
                    var wheelView=new Vector3(-8*spread,elevation+3*spread,-6*spread);
                    followOffset=Vector3.Lerp(followOffset,wheelView,extractionFraming);
                }
                position=center+rotation*followOffset;look=center+Vector3.up*1.2f+car.forward;
                if (towedPickup != null) position = FitTowPair(position, look, car, towedPickup.transform);
                if(CameraObstruction(look,position,out var hit,QueryTriggerInteraction.UseGlobal))position=hit.point+hit.normal*.5f;
            }
            // Preview modes own their framing and circular orbit. A second position
            // interpolation could cut through the car during a front/rear transition.
            if(enteringTitle)velocity=Vector3.zero;
            ApplyPresentationPose(position, look, title||session.GarageOpen||choosingStarter,
                !lobby&&!session.GarageOpen, p.reducedMotion, Time.unscaledDeltaTime, title||choosingStarter||session.GarageOpen);
            // Title and main menu share the same warm vehicle lighting.
            UpdateTitleLighting(title || session.View == "Main");
            UpdateTreeVisibility(lobby);
        }

        private Vector3 FitTowPair(Vector3 position, Vector3 look, Transform driver, Transform pickup)
        {
            if (treeCamera == null) return position;

            var view = Quaternion.LookRotation(look - position);
            var inverseView = Quaternion.Inverse(view);
            // Preserve the classic angle and move back only when a vehicle approaches an edge.
            float vertical = Mathf.Tan(treeCamera.fieldOfView * .5f * Mathf.Deg2Rad) * .9f;
            float horizontal = vertical * treeCamera.aspect;
            float retreat = Mathf.Max(RequiredRetreat(driver), RequiredRetreat(pickup));
            return position - (view * Vector3.forward) * retreat;

            float RequiredRetreat(Transform vehicle)
            {
                if (!vehicle.TryGetComponent<BoxCollider>(out var box)) return 0f;
                var extents = box.size * .5f + Vector3.one * .35f;
                float distance = 0f;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    var corner = box.center + new Vector3(x * extents.x, y * extents.y, z * extents.z);
                    var point = inverseView * (vehicle.TransformPoint(corner) - position);
                    float depth = Mathf.Max(Mathf.Abs(point.x) / horizontal, Mathf.Abs(point.y) / vertical);
                    distance = Mathf.Max(distance, depth - point.z);
                }
                return distance;
            }
        }

        private void ApplyPresentationPose(Vector3 position, Vector3 look, bool preview, bool gameplay, bool reducedMotion, float dt, bool previewRotation)
        {
            // Menu and gameplay poses may be on opposite sides of scenery. Enter at the final safe pose.
            bool enteringGameplay = gameplay&&!gameplayCameraActive;
            bool snap = preview || enteringGameplay;
            gameplayCameraActive = gameplay;
            if (snap) velocity = Vector3.zero;
            var final = snap ? position : Vector3.SmoothDamp(transform.position,position,ref velocity,reducedMotion?.1f:.24f,Mathf.Infinity,dt);
            // The smoothed position needs the same protection as the destination camera arm.
            if (gameplay && CameraObstruction(look,final,out var hit,QueryTriggerInteraction.UseGlobal))
            { final = hit.point+hit.normal*.5f; velocity = Vector3.zero; }
            transform.position = final;
            var rotation = Quaternion.LookRotation(look-final);
            transform.rotation = enteringGameplay || preview&&previewRotation ? rotation : Quaternion.Slerp(transform.rotation,rotation,1-Mathf.Exp(-7*dt));
        }
    }
}
