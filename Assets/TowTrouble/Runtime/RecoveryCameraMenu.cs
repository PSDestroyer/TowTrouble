using UnityEngine;
namespace TowTrouble
{
    public sealed partial class RecoveryCamera
    {
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
