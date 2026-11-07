using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryVehicle
    {
        private struct SteeringVisual
        {
            public Transform Wheel;
            public Quaternion Rest;
        }
        private readonly List<SteeringVisual> steeringVisuals=new List<SteeringVisual>();
        private int steeringGeometry=-1;
        private float steeringVisualAngle;

        private void ResetSteeringPresentation()
        {
            foreach(var visual in steeringVisuals)
                if(visual.Wheel!=null)visual.Wheel.localRotation=visual.Rest;
            steeringVisuals.Clear();steeringGeometry=-1;steeringVisualAngle=0;
        }

        private void UpdateSteeringPresentation(float roadWheelAngle,float deltaTime)
        {
            if(steeringGeometry!=geometryVersion)
            {
                ResetSteeringPresentation();
                // The source assets put the hub at the mesh origin and the column on local Y.
                // Bind visible fitted geometry only, once per body swap, including remote cars.
                foreach(var mesh in GetComponentsInChildren<MeshFilter>(true))
                {
                    if(mesh.sharedMesh==null||mesh.sharedMesh.name.IndexOf("SteerWheel",System.StringComparison.OrdinalIgnoreCase)<0)continue;
                    if(!RecoveryVehicleEffects.VisiblePart(mesh.transform,transform))continue;
                    var renderer=mesh.GetComponent<MeshRenderer>();
                    if(renderer==null||!renderer.enabled)continue;
                    steeringVisuals.Add(new SteeringVisual{Wheel=mesh.transform,Rest=mesh.transform.localRotation});
                }
                steeringGeometry=geometryVersion;
            }
            // +/-135 degrees at full lock; retain the existing speed-sensitive road steering.
            float target=Mathf.Clamp(roadWheelAngle/Mathf.Max(1,steering),-1,1)*135;
            steeringVisualAngle=Mathf.Lerp(steeringVisualAngle,target,1-Mathf.Exp(-14*Mathf.Max(0,deltaTime)));
            foreach(var visual in steeringVisuals)
                if(visual.Wheel!=null)visual.Wheel.localRotation=visual.Rest*Quaternion.AngleAxis(steeringVisualAngle,Vector3.up);
        }

        private void OnDisable(){ResetSteeringPresentation();}
    }
}
