using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryCamera
    {
        private bool starterCameraActive;
        private RecoveryVehicle starterVehicle;
        private int starterModel=-1;
        private float starterOrbit;
        private Bounds starterBounds;
        private void StarterCameraPose(RecoveryVehicle car,float rotationInput,bool reset,out Vector3 position,out Vector3 look)
        {
            if(!starterCameraActive||starterVehicle!=car||starterModel!=car.ModelIndex)
            {
                starterVehicle=car;starterModel=car.ModelIndex;starterOrbit=0;velocity=Vector3.zero;
                var customization=car.GetComponent<RecoveryCustomization>();
                if(customization!=null)customization.GarageBounds(GarageSlot.Assembly,null,out starterBounds,out _);
                else starterBounds=new Bounds(new Vector3(0,1,0),new Vector3(2.2f,2.5f,4.5f));
            }
            starterCameraActive=true;if(reset)starterOrbit=0;starterOrbit+=rotationInput;
            // Front is local +Z. A world-space camera offset showed the back of cars parked at 180 degrees.
            var localDirection=RecoveryGarageView.Direction(-48+starterOrbit,19);
            var direction=car.transform.TransformDirection(localDirection);
            var rotation=Quaternion.LookRotation(-direction,Vector3.up);var inverse=Quaternion.Inverse(rotation);
            var camera=GetComponent<Camera>();float fov=camera!=null?camera.fieldOfView:57,aspect=camera!=null?camera.aspect:16f/9;
            float tangent=Mathf.Tan(fov*.5f*Mathf.Deg2Rad);
            float safeWidth=Mathf.Min(1,(16f/9)/aspect),safeHeight=Mathf.Min(1,aspect/(16f/9));
            float widthFraction=.54f*safeWidth,heightFraction=.47f*safeHeight,distance=4;
            for(int i=0;i<8;i++)
            {
                var corner=car.transform.TransformVector(Vector3.Scale(starterBounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                var point=inverse*corner;
                distance=Mathf.Max(distance,Mathf.Abs(point.x)/(tangent*aspect*widthFraction)-point.z,Mathf.Abs(point.y)/(tangent*heightFraction)-point.z);
            }
            distance+=.35f;
            var focus=car.transform.TransformPoint(starterBounds.center);float halfHeight=distance*tangent;
            // Move the camera and aim together: keep the complete car above the summary and beside the choices.
            var offset=rotation*new Vector3(-2*.148f*safeWidth*halfHeight*aspect,-2*.060f*safeHeight*halfHeight,0);
            position=focus+direction*distance+offset;look=focus+offset;
        }
        private void MenuPose(RecoveryVehicle car,out Vector3 position,out Vector3 look)
        {
            Bounds bounds=new Bounds(car.transform.position+Vector3.up,new Vector3(2.2f,2.5f,4.5f));bool found=false;
            foreach(var renderer in car.GetComponentsInChildren<MeshRenderer>())
                if(renderer.enabled&&renderer.gameObject.activeInHierarchy&&renderer.bounds.size.sqrMagnitude<400){if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}
            var camera=GetComponent<Camera>();float fov=camera!=null?camera.fieldOfView:50,aspect=camera!=null?camera.aspect:16f/9;
            float tangent=Mathf.Tan(fov*.5f*Mathf.Deg2Rad);
            float span=Mathf.Max(bounds.size.x,bounds.size.z);
            float distance=Mathf.Max(8,Mathf.Max(bounds.size.y/(2*tangent*.39f),span/(2*tangent*aspect*.27f))+1.3f);
            Vector3 direction=new Vector3(12,5,7).normalized;position=bounds.center+direction*distance;
            Quaternion rotation=Quaternion.LookRotation(-direction);float halfHeight=distance*tangent;
            look=bounds.center+rotation*Vector3.right*(halfHeight*aspect*(1-2*(.5f+.15f*Mathf.Min(1,(16f/9f)/aspect))));
            if(!session.Profile.reducedMotion)position.x+=Mathf.Sin(Time.unscaledTime*.12f)*.12f;
        }
    }
}
