using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryCustomization
    {
        // Queried only when category, preview part or vehicle geometry changes.
        // Return vehicle-local bounds so the camera still follows a moving co-op car.
        public void GarageBounds(GarageSlot slot,string partId,out Bounds body,out Bounds detail)
        {
            var visual=assembly!=null?assembly.transform:vehicle.CurrentModel.Root.transform;
            if(!LocalBounds(visual,vehicle.transform,out body))body=new Bounds(new Vector3(0,1,0),new Vector3(2,2,4));
            detail=RecoveryGarageView.Fallback(slot,body);
            // Some factory bodies have no separate bumper mesh. Frame their
            // authored tow point instead of guessing a height from tire bounds.
            if(slot==GarageSlot.FrontBumper||slot==GarageSlot.RearBumper||slot==GarageSlot.Cable||slot==GarageSlot.Winch)
                detail.center=vehicle.transform.InverseTransformPoint(slot==GarageSlot.FrontBumper?vehicle.FrontHitch:vehicle.RearHitch);
            if(slot==GarageSlot.Assembly||slot==GarageSlot.Paint)return;
            if(slot==GarageSlot.Wheels||slot==GarageSlot.Suspension)
            {
                var wheel=assembly!=null&&assembly.Wheels.Length>0?assembly.Wheels[0]:vehicle.WheelVisual(0);
                if(wheel!=null&&LocalBounds(wheel,vehicle.transform,out var bounds))detail=bounds;
                if(slot==GarageSlot.Suspension)detail.Expand(new Vector3(.25f,.55f,.25f));
                return;
            }
            var selected=catalog!=null?catalog.Find(partId):null;
            string socket=selected!=null&&selected.Sockets!=null&&selected.Sockets.Length>0?selected.Sockets[0]:DefaultSocket(slot);
            if(string.IsNullOrEmpty(socket))return;
            var point=Find(visual,socket);
            if(point!=null&&LocalBounds(point,vehicle.transform,out var visible,true))detail=visible;
        }
        private static string DefaultSocket(GarageSlot slot)
        {
            switch(slot)
            {
                case GarageSlot.FrontBumper:return "SOCKET_Bumper_Front";
                case GarageSlot.RearBumper:return "SOCKET_Bumper_Rear";
                case GarageSlot.Bonnet:return "SOCKET_Bonnet";
                case GarageSlot.Fenders:return "SOCKET_Fenders_Front";
                case GarageSlot.Roof:return "SOCKET_Attachment_Top";
                case GarageSlot.Intake:case GarageSlot.Engine:return "SOCKET_Motor";
                case GarageSlot.Exhaust:return "SOCKET_Motor_Exhaust";
                case GarageSlot.Chassis:return "SOCKET_Chassis_Front";
                default:return null;
            }
        }
        private static bool LocalBounds(Transform root,Transform vehicleSpace,out Bounds result,bool directSocket=false)
        {
            result=default;bool found=false;
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))
            {
                // Smoke, dust, trails and tow lines are not vehicle geometry. Their
                // simulation bounds can extend far beyond the currently fitted part.
                if(!(r is MeshRenderer)&&!(r is SkinnedMeshRenderer))continue;
                if(!r.enabled||!r.gameObject.activeInHierarchy)continue;
                // Bonnet/chassis sockets also own structural child sockets. Their
                // wheels, lamps and other trim must not widen the selected skin.
                if(directSocket)
                {
                    bool nested=false;
                    for(var t=r.transform.parent;t!=null&&t!=root;t=t.parent)
                        if(t.name.StartsWith("SOCKET_")){nested=true;break;}
                    if(nested)continue;
                }
                var b=r.localBounds;
                for(int i=0;i<8;i++)
                {
                    var corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var p=vehicleSpace.InverseTransformPoint(r.transform.TransformPoint(corner));
                    if(!found){result=new Bounds(p,Vector3.zero);found=true;}else result.Encapsulate(p);
                }
            }
            return found;
        }
    }
}
