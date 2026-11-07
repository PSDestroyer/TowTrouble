using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryCamera
    {
        private bool garageCameraActive;
        private RecoveryVehicle garageVehicle;
        private RecoveryCustomization garageCustomization;
        private Camera garageLens;
        private GarageSlot focusedGarageSlot;
        private string focusedGaragePart;
        private int garageGeometry;
        private int garageResetSeen;
        private Bounds garageBody,garageDetail;
        private float garageYaw,garageElevation,garageDistance,garageManualOrbit;
        private float garageYawVelocity,garageElevationVelocity,garageDistanceVelocity;
        private Vector3 garageFocus,garageFocusVelocity;
        private void GarageCameraPose(RecoveryVehicle car,float rotationInput,bool reducedMotion,float dt,out Vector3 position,out Vector3 look)
        {
            bool entering=!garageCameraActive||garageVehicle!=car;
            bool categoryChanged=entering||focusedGarageSlot!=session.GarageFocusSlot;
            if(entering)
            {
                garageVehicle=car;garageCustomization=car.GetComponent<RecoveryCustomization>();garageLens=GetComponent<Camera>();
                garageManualOrbit=0;garageYawVelocity=garageElevationVelocity=garageDistanceVelocity=0;garageFocusVelocity=velocity=Vector3.zero;
            }
            if(categoryChanged||garageResetSeen!=session.GarageViewReset){garageManualOrbit=0;garageResetSeen=session.GarageViewReset;}
            if(categoryChanged||garageGeometry!=car.GeometryVersion||focusedGaragePart!=session.GarageFocusPart)
            {
                focusedGarageSlot=session.GarageFocusSlot;focusedGaragePart=session.GarageFocusPart;garageGeometry=car.GeometryVersion;
                if(garageCustomization!=null)garageCustomization.GarageBounds(focusedGarageSlot,focusedGaragePart,out garageBody,out garageDetail);
                else {garageBody=new Bounds(new Vector3(0,1,0),new Vector3(2,2,4));garageDetail=RecoveryGarageView.Fallback(focusedGarageSlot,garageBody);}
            }
            float fov=garageLens!=null?garageLens.fieldOfView:57,aspect=garageLens!=null?garageLens.aspect:16f/9;
            var viewport=RecoveryGarageView.Viewport(aspect,RecoveryScreenLayout.CameraSafeArea(garageLens));
            var target=RecoveryGarageView.Frame(focusedGarageSlot,garageBody,garageDetail,fov,aspect,viewport);
            if(entering)
            {
                garageFocus=garageBody.center;var start=car.transform.InverseTransformPoint(transform.position)-garageFocus;
                garageDistance=Mathf.Max(3.2f,start.magnitude);garageYaw=Mathf.Atan2(start.x,start.z)*Mathf.Rad2Deg;
                garageElevation=Mathf.Asin(Mathf.Clamp(start.y/garageDistance,-.95f,.95f))*Mathf.Rad2Deg;
            }
            garageCameraActive=true;garageManualOrbit+=rotationInput;float smooth=reducedMotion?.14f:.34f;
            garageYaw=Mathf.SmoothDampAngle(garageYaw,target.Yaw+garageManualOrbit,ref garageYawVelocity,smooth,Mathf.Infinity,dt);
            garageElevation=Mathf.SmoothDamp(garageElevation,target.Elevation,ref garageElevationVelocity,smooth,Mathf.Infinity,dt);
            garageDistance=Mathf.SmoothDamp(garageDistance,target.Distance,ref garageDistanceVelocity,smooth,Mathf.Infinity,dt);
            garageFocus=Vector3.SmoothDamp(garageFocus,target.Focus,ref garageFocusVelocity,smooth,Mathf.Infinity,dt);
            var local=RecoveryGarageView.OutsideBody(garageFocus+RecoveryGarageView.Direction(garageYaw,garageElevation)*garageDistance,garageBody);
            var focus=car.transform.TransformPoint(garageFocus);position=car.transform.TransformPoint(local);
            RecoveryGarageView.Pose(focus,(position-focus).normalized,Vector3.Distance(position,focus),fov,aspect,viewport,out position,out look);
            var safePosition=car.transform.TransformPoint(RecoveryGarageView.OutsideBody(car.transform.InverseTransformPoint(position),garageBody));
            look+=safePosition-position;position=safePosition;
            // Test the final camera position after the viewport offset, rather than an intermediate pose.
            if(CameraObstruction(focus,position,out var hit,QueryTriggerInteraction.Ignore))position=hit.point+hit.normal*.45f;
        }
    }
}
