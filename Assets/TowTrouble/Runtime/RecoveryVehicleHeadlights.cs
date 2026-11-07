using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryVehicleEffects
    {
        private readonly List<Light> headlights=new List<Light>();
        private float nightAmount;
        public void SetNightLighting(float amount){nightAmount=Mathf.Clamp01(amount);}
        private void BindHeadlights()
        {
            if(profile.Headlights!=null)
            foreach(var mesh in GetComponentsInChildren<MeshFilter>(true))
            {
                if(!VisiblePart(mesh.transform,vehicle.transform))continue;
                var renderer=mesh.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled)continue;
                VehicleHeadlampPorts ports=null;
                foreach(var p in profile.Headlights)if(p.Mesh==mesh.sharedMesh){ports=p;break;}
                if(ports==null)continue;
                foreach(var lamp in lamps)if(lamp.Renderer==renderer)lamp.Headlight=true;
                foreach(var point in ports.Positions)
                {
                    if(headlights.Count==2)break;
                    CreateHeadlight(vehicle.transform.InverseTransformPoint(mesh.transform.TransformPoint(point)));
                }
                if(headlights.Count==2)break;
            }
            // Bonnet kits with no lamp mesh retain a usable pair of recovery lights.
            if(headlights.Count==0)
            {
                var hitch=vehicle.transform.InverseTransformPoint(vehicle.FrontHitch);
                CreateHeadlight(hitch+new Vector3(-.48f,.32f,-.14f));CreateHeadlight(hitch+new Vector3(.48f,.32f,-.14f));
            }
        }
        private void CreateHeadlight(Vector3 point)
        {
            var go=new GameObject("Night headlight");go.transform.SetParent(vehicle.transform,false);
            go.transform.localPosition=point+Vector3.forward*.08f;go.transform.localRotation=Quaternion.Euler(7,0,0);
            var light=go.AddComponent<Light>();light.type=LightType.Spot;light.color=new Color(1,.88f,.66f);
            light.range=36;light.spotAngle=60;light.innerSpotAngle=36;light.intensity=0;light.shadows=LightShadows.None;light.enabled=false;
            light.renderMode=LightRenderMode.ForcePixel;headlights.Add(light);
        }
        private void UpdateHeadlights()
        {
            float intensity=vehicle.EngineRunning?nightAmount*24:0;
            foreach(var light in headlights)if(light!=null){light.intensity=intensity;light.enabled=intensity>.02f;}
        }
        private void ClearHeadlights()
        {
            foreach(var light in headlights)if(light!=null)
            {light.enabled=false;if(Application.isPlaying)Destroy(light.gameObject);else DestroyImmediate(light.gameObject);}
            headlights.Clear();
        }
    }
}
