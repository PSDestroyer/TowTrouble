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
            if(categoryChanged)garageManualOrbit=0;
            if(categoryChanged||garageGeometry!=car.GeometryVersion||focusedGaragePart!=session.GarageFocusPart)
            {
                focusedGarageSlot=session.GarageFocusSlot;focusedGaragePart=session.GarageFocusPart;garageGeometry=car.GeometryVersion;
                if(garageCustomization!=null)garageCustomization.GarageBounds(focusedGarageSlot,focusedGaragePart,out garageBody,out garageDetail);
                else {garageBody=new Bounds(new Vector3(0,1,0),new Vector3(2,2,4));garageDetail=RecoveryGarageView.Fallback(focusedGarageSlot,garageBody);}
            }
            float fov=garageLens!=null?garageLens.fieldOfView:57,aspect=garageLens!=null?garageLens.aspect:16f/9;
            var target=RecoveryGarageView.Frame(focusedGarageSlot,garageBody,garageDetail,fov,aspect);
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
            // Environment clipping uses the actual workpiece, independently of the UI look offset.
            if(Physics.Linecast(focus,position,out var hit,1<<8,QueryTriggerInteraction.Ignore))position=hit.point+hit.normal*.45f;
            RecoveryGarageView.Pose(focus,(position-focus).normalized,Vector3.Distance(position,focus),fov,aspect,out position,out look);
        }
    }
}
