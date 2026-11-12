using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    // Rebuilds only when a configuration changes. Source prefabs/materials stay immutable.
    public sealed partial class RecoveryCustomization : MonoBehaviour
    {
        [SerializeField] private RecoveryGarageCatalog catalog;
        [SerializeField] private RecoveryVehicle vehicle;
        private RecoveryAssembly assembly;
        private readonly List<Material> materials=new List<Material>();
        private string applied="";
        private int boundGeometry=-1,paint=-1;
        public RecoveryGarageCatalog Catalog=>catalog;
        public void Configure(RecoveryVehicle owner,RecoveryGarageCatalog source){vehicle=owner;catalog=source;}
        public void Apply(RecoveryBuild build,int color=0,bool force=false)
        {
            if(catalog==null||build==null)return;
            color=RecoveryPaintPalette.Resolve(build,catalog,color);
            build=build.Copy();build.Model=vehicle.ModelIndex;build.Normalize(catalog);
            string key=JsonUtility.ToJson(build);
            if(!force&&applied==key&&paint==color&&boundGeometry==vehicle.GeometryVersion)return;
            Clear();applied=key;paint=color;
            var prefab=catalog.Assembly(build);if(prefab==null)return;
            var root=Instantiate(prefab,transform,false);root.name="Garage configuration";assembly=root.GetComponent<RecoveryAssembly>();
            if(assembly==null)throw new InvalidOperationException("Garage assembly lacks wheel geometry: "+prefab.name);
            foreach(GarageSlot slot in Enum.GetValues(typeof(GarageSlot)))
            {
                if(slot==GarageSlot.Assembly)continue;var p=catalog.Find(build.Item(slot,catalog));
                if(p==null||p.Prefabs==null||p.Prefabs.Length==0||!catalog.Compatible(p,build))continue;
                var hiddenSockets=new HashSet<Transform>();
                for(int i=0;i<p.Prefabs.Length;i++)
                {
                    var socket=Find(assembly.transform,p.Sockets[Mathf.Min(i,p.Sockets.Length-1)]);if(socket==null)continue;
                    if(hiddenSockets.Add(socket))HideOriginals(socket,slot);
                    var addition=Instantiate(p.Prefabs[i],socket,false);addition.name="Equipped "+p.Id;
                    // Sockets on replacement trim do not move the original chassis or tow points.
                    foreach(var t in addition.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("SOCKET_"))t.gameObject.SetActive(false);
                    if(slot==GarageSlot.Wheels)
                    {
                        var bounds=BoundsOf(addition);float desired=(assembly.Radii!=null&&assembly.Radii.Length==4?assembly.Radii[i]:assembly.Radius)*2/Mathf.Max(.01f,assembly.transform.lossyScale.y);float factor=desired/Mathf.Max(.01f,bounds.size.y/socket.lossyScale.y);addition.transform.localScale*=Mathf.Clamp(factor,.1f,4f);
                    }
                }
            }
            var palette=RecoveryPaintPalette.Current;bool stockBody=RecoveryPaintPalette.StockBody(build,catalog);
            var bodySnow = vehicle.GetComponent<WinterBodySnow>();
            var reused=new Dictionary<(Material,bool),Material>();
            var tires=new HashSet<Renderer>();foreach(var wheel in assembly.Wheels)foreach(var renderer in wheel.GetComponentsInChildren<Renderer>(true))tires.Add(renderer);
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var values=renderer.sharedMaterials;
                for(int i=0;i<values.Length;i++)
                {
                    var original=values[i];if(original==null)continue;bool tire=tires.Contains(renderer)&&catalog.TireShader!=null;
                    if(reused.TryGetValue((original,tire),out var existing)){values[i]=existing;continue;}
                    var material=new Material(tire?catalog.TireShader:original.shader){name=original.name+" - fitted paint"};materials.Add(material);reused[(original,tire)]=material;
                    var source=original.HasProperty("_BaseMap")?original.GetTexture("_BaseMap"):original.mainTexture;
                    var texture=palette!=null?palette.TextureFor(source,color,vehicle.ModelIndex,stockBody):source;
                    if(tire)
                    {
                        material.SetTexture("_Albedo_Map",texture);material.SetColor("_Tint",Color.white);material.SetTexture("_Mud_Texture",catalog.MudTexture);material.SetColor("_Mud_Tint",new Color(.68f,.53f,.35f));material.SetFloat("_Mud_Amount",0);material.SetFloat("_Smoothness",.38f);
                    }
                    else
                    {
                        material.CopyPropertiesFromMaterial(original);
                        if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",texture);
                        if (!tires.Contains(renderer)) bodySnow?.UseSnowShader(material);
                    }
                    values[i]=material;
                }
                renderer.sharedMaterials=values;
            }
            vehicle.UseAssembly(assembly,GaragePerformance.Calculate(build,catalog));boundGeometry=vehicle.GeometryVersion;
            bodySnow?.Bind(vehicle, root);
        }
        private static void HideOriginals(Transform socket,GarageSlot slot)
        {
            // Keep the nested structural sockets when swapping a bonnet or chassis skin.
            foreach(var renderer in socket.GetComponentsInChildren<Renderer>(true))
            {
                bool nested=false;for(var t=renderer.transform.parent;t!=null&&t!=socket;t=t.parent)if(t.name.StartsWith("SOCKET_")){nested=true;break;}
                if(!nested)renderer.enabled=false;
            }
        }
        private static Transform Find(Transform root,string name){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
        private static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=new Bounds(go.transform.position,Vector3.zero);foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        public void ClearPreview(){Clear();applied="";boundGeometry=-1;}
        private void Clear()
        {
            if (vehicle != null) vehicle.GetComponent<WinterBodySnow>()?.Unbind();
            if(assembly!=null){assembly.gameObject.SetActive(false);if(Application.isPlaying)Destroy(assembly.gameObject);else DestroyImmediate(assembly.gameObject);assembly=null;}
            foreach(var m in materials)if(m!=null){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}materials.Clear();
        }
        private void OnDestroy(){Clear();}
    }
}
