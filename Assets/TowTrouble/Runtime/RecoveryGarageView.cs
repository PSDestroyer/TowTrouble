using UnityEngine;
namespace TowTrouble
{
    public struct GarageViewFrame
    {
        public Vector3 Focus;
        public float Yaw,Elevation,Distance;
    }
    // Pure local-space framing rules shared by the live camera and static captures.
    public static class RecoveryGarageView
    {
        public static Bounds Fallback(GarageSlot slot,Bounds body)
        {
            var p=body.center;var size=body.size;
            switch(slot)
            {
                case GarageSlot.Assembly:case GarageSlot.Paint:return body;
                case GarageSlot.FrontBumper:p.y=body.min.y+size.y*.25f;p.z=body.max.z;size=new Vector3(size.x,.7f,.7f);break;
                case GarageSlot.RearBumper:case GarageSlot.Cable:case GarageSlot.Winch:p.y=body.min.y+size.y*.25f;p.z=body.min.z;size=new Vector3(size.x,.7f,.7f);break;
                case GarageSlot.Wheels:case GarageSlot.Suspension:p.x=body.min.x;p.y=body.min.y+.5f;p.z=body.max.z-body.size.z*.24f;size=Vector3.one;break;
                case GarageSlot.Roof:p.y=body.max.y;size=new Vector3(size.x,.7f,size.z*.45f);break;
                case GarageSlot.Engine:case GarageSlot.Bonnet:case GarageSlot.Intake:p.z=body.center.z+body.size.z*.25f;size=new Vector3(size.x*.65f,size.y*.6f,size.z*.4f);break;
                case GarageSlot.Exhaust:p.x=body.min.x;p.y=body.min.y+size.y*.3f;size=new Vector3(.8f,1,body.size.z*.45f);break;
                default:p.y=body.min.y+size.y*.25f;size=new Vector3(size.x,size.y*.5f,size.z*.65f);break;
            }
            return new Bounds(p,size);
        }
        public static GarageViewFrame Frame(GarageSlot slot,Bounds body,Bounds detail,float fov,float aspect)
            =>Frame(slot,body,detail,fov,aspect,Viewport(aspect,new Rect(0,0,1,1)));
        public static Rect Viewport(float aspect,Rect safe)=>RecoveryScreenLayout.ReferenceViewport(
            new Rect(RecoveryGarageLayout.PreviewX+12,364,RecoveryGarageLayout.PreviewWidth-24,218),aspect,safe);
        public static GarageViewFrame Frame(GarageSlot slot,Bounds body,Bounds detail,float fov,float aspect,Rect viewport)
        {
            float yaw=-38,elevation=19;
            switch(slot)
            {
                case GarageSlot.FrontBumper:yaw=-28;elevation=13;break;
                case GarageSlot.RearBumper:case GarageSlot.Cable:case GarageSlot.Winch:yaw=-152;elevation=13;break;
                case GarageSlot.Wheels:yaw=-67;elevation=12;break;
                case GarageSlot.Suspension:yaw=-65;elevation=16;break;
                case GarageSlot.Exhaust:yaw=detail.center.x>body.center.x+.1f?75:-75;elevation=16;break;
                case GarageSlot.Fenders:yaw=detail.center.x>body.center.x+.1f?52:-52;elevation=24;break;
                case GarageSlot.Roof:elevation=43;break;
                case GarageSlot.Chassis:case GarageSlot.Transmission:yaw=-65;elevation=18;break;
                case GarageSlot.Engine:case GarageSlot.Intake:yaw=-48;elevation=32;break;
            }
            var frame=new GarageViewFrame{Focus=detail.center,Yaw=yaw,Elevation=elevation};
            frame.Distance=FitDistance(detail,Direction(yaw,elevation),fov,aspect,viewport);
            return frame;
        }
        public static Vector3 Direction(float yaw,float elevation)
            =>Quaternion.Euler(-elevation,yaw,0)*Vector3.forward;
        public static float Distance(Bounds detail,Vector3 direction,float fov,float aspect,float height=.28f)
        {
            var rotation=Quaternion.LookRotation(-direction,Vector3.up);var inverse=Quaternion.Inverse(rotation);float width=0,vertical=0,depth=0;
            for(int i=0;i<8;i++)
            {
                var local=inverse*Vector3.Scale(detail.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                width=Mathf.Max(width,Mathf.Abs(local.x));vertical=Mathf.Max(vertical,Mathf.Abs(local.y));depth=Mathf.Max(depth,Mathf.Abs(local.z));
            }
            float tangent=Mathf.Tan(Mathf.Clamp(fov,20,100)*.5f*Mathf.Deg2Rad);
            float widthFraction=.29f*RecoveryGarageLayout.PreviewWidth/538f;
            return Mathf.Max(3.2f,Mathf.Max(width/(tangent*Mathf.Max(.6f,aspect)*widthFraction),vertical/(tangent*height))+depth*.6f);
        }
        private static float FitDistance(Bounds detail,Vector3 direction,float fov,float aspect,Rect viewport)
        {
            float tangent=Mathf.Tan(Mathf.Clamp(fov,20,100)*.5f*Mathf.Deg2Rad),horizontal=tangent*Mathf.Max(.1f,aspect);
            var inverse=Quaternion.Inverse(Quaternion.LookRotation(-direction,Vector3.up));var center=viewport.center;
            float axial=1f;
            // Fit all eight corners, including their depth, to the actual unobstructed UI rectangle.
            for(int i=0;i<8;i++)
            {
                var p=inverse*Vector3.Scale(detail.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                float horizontalFit=Mathf.Max(((2*viewport.xMin-1)*p.z*horizontal-p.x)/(viewport.width*horizontal),
                    (p.x-(2*viewport.xMax-1)*p.z*horizontal)/(viewport.width*horizontal));
                float verticalFit=Mathf.Max(((2*viewport.yMin-1)*p.z*tangent-p.y)/(viewport.height*tangent),
                    (p.y-(2*viewport.yMax-1)*p.z*tangent)/(viewport.height*tangent));
                axial=Mathf.Max(axial,Mathf.Max(horizontalFit,verticalFit));
            }
            float x=(1-2*center.x)*tangent*aspect,y=(1-2*center.y)*tangent;
            return Mathf.Max(3.2f,axial*Mathf.Sqrt(1+x*x+y*y)+.15f);
        }
        public static void Pose(Vector3 focus,Vector3 direction,float distance,float fov,float aspect,out Vector3 position,out Vector3 look)
            =>Pose(focus,direction,distance,fov,aspect,Viewport(aspect,new Rect(0,0,1,1)),out position,out look);
        public static void Pose(Vector3 focus,Vector3 direction,float distance,float fov,float aspect,Rect viewport,out Vector3 position,out Vector3 look)
        {
            var rotation=Quaternion.LookRotation(-direction,Vector3.up);float tangent=Mathf.Tan(fov*.5f*Mathf.Deg2Rad);
            float x=(1-2*viewport.center.x)*tangent*aspect,y=(1-2*viewport.center.y)*tangent;
            float axial=distance/Mathf.Sqrt(1+x*x+y*y);
            var offset=rotation*new Vector3(x*axial,y*axial,0);
            position=focus+direction.normalized*axial+offset;look=focus+offset;
        }
        public static Vector3 OutsideBody(Vector3 camera,Bounds body)
        {
            body.Expand(.65f);
            if(!body.Contains(camera))return camera;
            var ray=camera-body.center;if(ray.sqrMagnitude<.001f)ray=Vector3.back;
            var inverse=new Ray(body.center,ray.normalized);
            float exit=float.PositiveInfinity;
            for(int i=0;i<3;i++)if(Mathf.Abs(inverse.direction[i])>.001f)exit=Mathf.Min(exit,body.extents[i]/Mathf.Abs(inverse.direction[i]));
            return body.center+inverse.direction*(exit+.05f);
        }
    }
}
