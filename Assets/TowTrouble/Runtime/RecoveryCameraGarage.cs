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
                // Gameplay may be on the other side of a wall. Start at the framed pose,
                // rather than orbiting through that wall on the first garage frames.
                garageFocus=target.Focus;garageDistance=target.Distance;
                garageYaw=target.Yaw;garageElevation=target.Elevation;
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
            ResolveGarageObstruction(focus,ref position,ref look);
        }
        private void ResolveGarageObstruction(Vector3 focus,ref Vector3 position,ref Vector3 look)
        {
            var arm=position-focus;var aim=look-focus;
            float bestFraction=-1;Vector3 bestArm=arm,bestAim=aim;
            // Keep the full framing distance whenever a neighbouring orbit is free.
            // Evaluate only the preferred angle in the normal, unobstructed case.
            for(int i=0;i<13;i++)
            {
                float angle=i==0?0:((i+1)/2)*30*(i%2==1?1:-1);
                var turn=Quaternion.AngleAxis(angle,Vector3.up);
                var candidateArm=turn*arm;var candidateAim=turn*aim;
                float fraction=1;
                if(CameraObstruction(focus,focus+candidateArm,out var hit,QueryTriggerInteraction.Ignore))
                    fraction=Mathf.Clamp01((hit.distance-.6f)/Mathf.Max(.001f,candidateArm.magnitude));
                // A line can miss a wall touching the lens/near plane.
                if(fraction==1&&Physics.CheckSphere(focus+candidateArm,.35f,1<<8,QueryTriggerInteraction.Ignore))fraction=0;
                if(fraction>bestFraction){bestFraction=fraction;bestArm=candidateArm;bestAim=candidateAim;}
                if(fraction==1)break;
            }
            // Scaling both offsets preserves the car's viewport position. Moving
            // the camera alone can turn it away from the car after a short hit.
            position=focus+bestArm*Mathf.Max(.001f,bestFraction);
            look=focus+bestAim*Mathf.Max(.001f,bestFraction);
        }
    }
}
