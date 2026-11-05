using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static void BuildApproachRefuge(Transform parent,Vector3 center,string title,float yaw)
        {
            var group=new GameObject(title+" - timber recovery refuge").transform;group.SetParent(parent,false);
            var wood=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop aged timber.mat");
            var siding=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop painted boards.mat");
            var roof=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop roof metal.mat");
            var trim=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop ivory trim.mat");
            var foundation=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop foundation.mat")??wood;
            var glass=ScenicMaterial("Refuge deep blue windows",new Color(.11f,.23f,.25f));
            var safety=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop safety yellow.mat");
            var rotation=Quaternion.Euler(0,yaw,0);
            System.Func<string,Vector3,Vector3,Material,bool,GameObject> block=(name,offset,size,material,collider)=>
            {var go=Cube(name,center+rotation*offset,size,material,group,collider);go.transform.rotation=rotation;go.isStatic=true;return go;};
            // Small built structure with a real foundation, porch, roof thickness and visible joinery.
            block("Stone plinth",new Vector3(0,-.15f,0),new Vector3(7.6f,.55f,5.6f),foundation,true);
            block("Timber cabin shell",new Vector3(0,1.65f,.2f),new Vector3(7,3.1f,4.8f),siding,true);
            for(int i=0;i<23;i++)
            {
                float x=-3.3f+i*.3f;
                block("Front narrow siding batten",new Vector3(x,1.6f,-2.24f),new Vector3(.045f,3.05f,.07f),wood,false);
                block("Rear narrow siding batten",new Vector3(x,1.6f,2.64f),new Vector3(.045f,3.05f,.07f),wood,false);
            }
            foreach(float x in new[]{-3.53f,3.53f})foreach(float z in new[]{-2.26f,2.65f})block("Ivory corner trim",new Vector3(x,1.67f,z),new Vector3(.14f,3.25f,.14f),trim,false);
            block("Cabin door",new Vector3(-.8f,1.25f,-2.3f),new Vector3(1.15f,2.3f,.13f),wood,false);
            block("Door threshold",new Vector3(-.8f,.2f,-2.5f),new Vector3(1.4f,.2f,.45f),foundation,false);
            block("Door handle",new Vector3(-.43f,1.2f,-2.40f),new Vector3(.06f,.24f,.08f),safety,false);
            foreach(float x in new[]{-2.35f,1.85f})
            {
                block("Window casing",new Vector3(x,1.85f,-2.34f),new Vector3(1.4f,1.35f,.18f),trim,false);
                block("Recessed window glass",new Vector3(x,1.85f,-2.445f),new Vector3(1.18f,1.12f,.03f),glass,false);
                block("Window mullion",new Vector3(x,1.85f,-2.47f),new Vector3(.055f,1.12f,.04f),wood,false);
                block("Window sill",new Vector3(x,1.15f,-2.42f),new Vector3(1.55f,.1f,.35f),wood,false);
            }
            foreach(int side in new[]{-1,1})
            {
                var sheet=block("Pitched metal roof",new Vector3(side*1.95f,3.54f,.15f),new Vector3(4.1f,.19f,6.3f),roof,false);
                sheet.transform.rotation=rotation*Quaternion.Euler(0,0,-side*17);
                for(int i=0;i<11;i++)
                {
                    var seam=block("Standing roof seam",new Vector3(side*1.95f,3.65f,-2.8f+i*.59f),new Vector3(4.1f,.055f,.045f),roof,false);seam.transform.rotation=sheet.transform.rotation;
                }
            }
            block("Roof ridge cap",new Vector3(0,4.17f,.15f),new Vector3(.28f,.14f,6.4f),roof,false);
            block("Porch deck",new Vector3(0,.08f,-3.4f),new Vector3(8.1f,.16f,2.3f),wood,true);
            var canopy=block("Porch canopy",new Vector3(0,2.98f,-3.5f),new Vector3(8.4f,.15f,2.9f),roof,false);canopy.transform.rotation=rotation*Quaternion.Euler(-7,0,0);
            foreach(float x in new[]{-3.7f,3.7f})
            {
                block("Porch support",new Vector3(x,1.45f,-4.35f),new Vector3(.17f,2.9f,.17f),wood,true);
                var brace=block("Porch diagonal brace",new Vector3(x-Mathf.Sign(x)*.28f,2.55f,-4.35f),new Vector3(.13f,.8f,.13f),wood,false);brace.transform.rotation=rotation*Quaternion.Euler(0,0,Mathf.Sign(x)*-40);
            }
            block("Dispatch fascia",new Vector3(0,2.98f,-4.92f),new Vector3(7.9f,.65f,.16f),siding,false);
            Text(title,center+rotation*new Vector3(0,3.01f,-5.02f),yaw,.43f,group);
            block("Porch bench seat",new Vector3(2.5f,.58f,-3.4f),new Vector3(1.8f,.12f,.48f),wood,false);
            block("Porch bench back",new Vector3(2.5f,.94f,-3.17f),new Vector3(1.8f,.44f,.08f),wood,false);
            foreach(float x in new[]{1.85f,3.15f})block("Bench support",new Vector3(x,.3f,-3.4f),new Vector3(.13f,.6f,.4f),roof,false);
            block("Chimney",new Vector3(-2.2f,4.15f,1.4f),new Vector3(.55f,1.8f,.55f),foundation,false);
            block("Chimney cap",new Vector3(-2.2f,5.08f,1.4f),new Vector3(.74f,.12f,.74f),roof,false);
            var stumpPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("P_FirStump1"));
            if(stumpPrefab!=null){var stump=ArtPrefab("P_FirStump1",Ground(center+rotation*new Vector3(5.5f,0,1.4f)),.8f,group,true);stump.name="Wood chopping stump";}
            // Small timber store, clear of the bay and access apron.
            for(int i=0;i<8;i++)block("Stacked timber",new Vector3(4.55f,.15f+(i/3)*.22f,.5f+(i%3)*.25f),new Vector3(1.9f,.18f,.22f),wood,false);
        }
        private static void FitApproachDelivery(string id,Vector3 center,Vector3 refuge,Transform parent)
        {
            var marker=environment.GetComponentsInChildren<Transform>(true).First(t=>t.name.StartsWith("Dropoff "+id+" -"));
            Undo.RecordObject(marker,"Move mountain delivery marker");
            foreach(var t in marker.Cast<Transform>().ToArray())Undo.DestroyObjectImmediate(t.gameObject);
            marker.position=Ground(center);center=marker.position;
            var wood=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop aged timber.mat");
            var trim=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop ivory trim.mat");
            var ink=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop roof metal.mat");
            // Short painted corners replace the large box crossing the road. No blocking collider.
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                var p=Ground(center+new Vector3(sx*7,0,sz*7));
                Cube("Painted delivery corner",p+new Vector3(-sx*1.1f,.015f,0),new Vector3(2.2f,.025f,.12f),trim,marker,false);
                Cube("Painted delivery corner",p+new Vector3(0,.015f,-sz*1.1f),new Vector3(.12f,.025f,2.2f),trim,marker,false);
            }
            Text("PREDARE",center+Vector3.up*.04f,90,.75f,marker);
            var sign=Ground(center+new Vector3(12,0,7.5f));
            foreach(float dx in new[]{-1.7f,1.7f}){var foot=Ground(sign+Vector3.right*dx);float top=sign.y+2.35f;Cube("Destination timber post",foot+Vector3.up*(top-foot.y)*.5f,new Vector3(.14f,top-foot.y,.14f),wood,marker);}
            Cube("Destination sign",sign+Vector3.up*1.95f,new Vector3(4.1f,1.05f,.18f),ink,marker,false);
            var map=UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();
            var delivery=map.Deliveries.Single(b=>b.Id==id);
            Text(delivery.Name,sign+new Vector3(0,2,-.11f),0,.39f,marker,delivery.English);
            FitDestinationCaption(DestinationCaption(marker), marker.Find("Destination sign"));
            foreach(var child in marker.Cast<Transform>())Undo.RegisterCreatedObjectUndo(child.gameObject,"Create mountain delivery markings");
            // Logical delivery and visible markings share exactly the same off-road location.
            Undo.RecordObject(map,"Fit mountain delivery bay");
            delivery.Position=center;
            foreach(var site in plan.Sites.Where(s=>s.Delivery==id))map.Sites.Single(s=>s.Id==site.Id).Delivery=center;
            var serialized=new SerializedObject(map);var checkpoints=serialized.FindProperty("roadCheckpoints");var values=new Vector3[checkpoints.arraySize];for(int i=0;i<values.Length;i++)values[i]=checkpoints.GetArrayElementAtIndex(i).vector3Value;
            map.Configure(map.Sites,map.Deliveries,values);EditorUtility.SetDirty(map);
        }
    }
}
