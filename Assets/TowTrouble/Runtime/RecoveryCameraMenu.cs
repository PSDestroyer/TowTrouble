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
            var content=RecoveryScreenLayout.Content(aspect,RecoveryScreenLayout.CameraSafeArea(camera));
            float safeWidth=content.width,safeHeight=content.height;
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
            var center=content.position+Vector2.Scale(new Vector2(.648f,.560f),content.size);
            var offset=rotation*new Vector3((1-2*center.x)*halfHeight*aspect,(1-2*center.y)*halfHeight,0);
            position=focus+direction*distance+offset;look=focus+offset;
        }
        private bool titleCameraActive;
        private RecoveryVehicle titleVehicle;
        private int titleGeometry=-1;
        private Bounds titleBounds;
        private void TitleCameraPose(RecoveryVehicle car,out Vector3 position,out Vector3 look)
        {
            if(titleVehicle!=car||titleGeometry!=car.GeometryVersion)
            {
                titleVehicle=car;titleGeometry=car.GeometryVersion;
                var customization=car.GetComponent<RecoveryCustomization>();
                if(customization!=null)customization.GarageBounds(GarageSlot.Assembly,null,out titleBounds,out _);
                else titleBounds=new Bounds(new Vector3(0,1,0),new Vector3(2.2f,2.5f,4.5f));
            }
            var camera=GetComponent<Camera>();float fov=camera!=null?camera.fieldOfView:57,aspect=camera!=null?camera.aspect:16f/9;
            float tangent=Mathf.Tan(fov*.5f*Mathf.Deg2Rad);
            var content=RecoveryScreenLayout.Content(aspect,RecoveryScreenLayout.CameraSafeArea(camera));
            // Front three-quarter view: the workshop and wooded hillside sit behind
            // the car. Reserve the upper left for the logo and the bottom for Continue.
            var direction=car.transform.TransformDirection(RecoveryGarageView.Direction(-53,13));
            var rotation=Quaternion.LookRotation(-direction,Vector3.up);var inverse=Quaternion.Inverse(rotation);
            float distance=5.1f;
            for(int i=0;i<8;i++)
            {
                var corner=car.transform.TransformVector(Vector3.Scale(titleBounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                var point=inverse*corner;
                distance=Mathf.Max(distance,Mathf.Abs(point.x)/(tangent*aspect*.49f*content.width)-point.z,Mathf.Abs(point.y)/(tangent*.70f*content.height)-point.z);
            }
            distance+=.15f;
            var focus=car.transform.TransformPoint(titleBounds.center);float halfHeight=distance*tangent;
            var center=content.position+Vector2.Scale(new Vector2(.70f,.59f),content.size);
            var offset=rotation*new Vector3((1-2*center.x)*halfHeight*aspect,(1-2*center.y)*halfHeight,0);
            position=focus+direction*distance+offset;look=focus+offset;
        }
        private RecoveryVehicle menuVehicle;
        private int menuGeometry=-1;
        private Bounds menuBounds;
        private void MenuPose(RecoveryVehicle car,out Vector3 position,out Vector3 look)
        {
            if(menuVehicle!=car||menuGeometry!=car.GeometryVersion)
            {
                menuVehicle=car;menuGeometry=car.GeometryVersion;
                var customization=car.GetComponent<RecoveryCustomization>();
                if(customization!=null)customization.GarageBounds(GarageSlot.Assembly,null,out menuBounds,out _);
                else menuBounds=new Bounds(new Vector3(0,1,0),new Vector3(2.2f,2.5f,4.5f));
            }
            var camera=GetComponent<Camera>();float fov=camera!=null?camera.fieldOfView:57,aspect=camera!=null?camera.aspect:16f/9;
            float tangent=Mathf.Tan(fov*.5f*Mathf.Deg2Rad);
            var content=RecoveryScreenLayout.Content(aspect,RecoveryScreenLayout.CameraSafeArea(camera));
            // Follow the car's front (+Z), including when it is parked facing the workshop.
            // The low three-quarter angle keeps the grille, bonnet and side in view.
            float drift=session.Profile.reducedMotion?0:Mathf.Sin(Time.unscaledTime*.12f)*.7f;
            var direction=car.transform.TransformDirection(RecoveryGarageView.Direction(-58+drift,15));
            var rotation=Quaternion.LookRotation(-direction,Vector3.up);var inverse=Quaternion.Inverse(rotation);
            float distance=5;
            for(int i=0;i<8;i++)
            {
                var corner=car.transform.TransformVector(Vector3.Scale(menuBounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                var point=inverse*corner;
                distance=Mathf.Max(distance,Mathf.Abs(point.x)/(tangent*aspect*.46f*content.width)-point.z,Mathf.Abs(point.y)/(tangent*.46f*content.height)-point.z);
            }
            distance+=.25f;
            var focus=car.transform.TransformPoint(menuBounds.center);float halfHeight=distance*tangent;
            // Reserve the left panel and the lower-right specification card.
            var center=content.position+Vector2.Scale(new Vector2(.68f,.55f),content.size);
            var offset=rotation*new Vector3((1-2*center.x)*halfHeight*aspect,(1-2*center.y)*halfHeight,0);
            position=focus+direction*distance+offset;look=focus+offset;
        }
    }
}
