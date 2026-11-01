using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static Material PolishMat(string name,Color color)
        {
            var path=PolishFolder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        private static GameObject PolishBox(Transform parent,string name,Vector3 p,Vector3 size,Material material,Vector3 rotation=default,PrimitiveType type=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(rotation);go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;return go;
        }
        private static Vector3 FindPolishLandmark(Vector3 reference,float radius)
        {
            var trees=environment.GetComponentsInChildren<LODGroup>().Where(g=>g.name.Contains("Tree")||g.name.Contains("Sapling")).Select(g=>g.transform.position).ToArray();
            Vector3 best=default;float score=float.MaxValue;
            for(int z=-40;z<=40;z+=4)for(int x=-40;x<=40;x+=4)
            {
                var p=Ground(reference+new Vector3(x,0,z));if(!PolishClear(p,radius)||RiverDistance(p)<radius+8||!DetailPositionAllowed(p,polishMap,radius))continue;
                var local=p-terrain.transform.position;float slope=terrain.terrainData.GetSteepness(local.x/terrain.terrainData.size.x,local.z/terrain.terrainData.size.z);if(slope>16||trees.Any(t=>Flat(t-p).sqrMagnitude<(radius+3)*(radius+3)))continue;
                float cost=Flat(p-reference).sqrMagnitude+slope*8;if(cost<score){score=cost;best=p;}
            }
            if(score==float.MaxValue)throw new InvalidOperationException("No clear landmark location near "+reference);return best;
        }
        private static Vector3 FindPolishJetty(RecoverySite site)
        {
            var lakeCenter=environment.GetComponentsInChildren<MeshFilter>().First(f=>f.name.StartsWith("Lake -")).GetComponent<Renderer>().bounds.center;Vector3 best=default;float score=float.MaxValue;
            var reference=site.Position+Vector3.Cross(Vector3.up,site.ExitDirection)*40;
            for(int z=-55;z<=55;z+=2)for(int x=-55;x<=55;x+=2)
            {
                var p=Ground(reference+new Vector3(x,0,z));if(p.y<10.8f||p.y>11.3f||!PolishClear(p,6))continue;
                var inward=Flat(lakeCenter-p).normalized;if(Ground(p+inward*7).y>10.15f)continue;
                float cost=Flat(p-reference).sqrMagnitude;if(cost<score){score=cost;best=p;}
            }
            if(score==float.MaxValue)throw new InvalidOperationException("No naturally shallow jetty shore");return best;
        }
        private static Vector3 FindPolishBeach(Vector3 reference)
        {
            var data=terrain.terrainData;Vector3 best=default;float score=float.MaxValue;
            for(int z=-80;z<=80;z+=4)for(int x=-80;x<=80;x+=4)
            {
                var p=Ground(reference+new Vector3(x,0,z));if(p.y>22||!PolishClear(p,7)||!DetailPositionAllowed(p,polishMap,7))continue;
                int ax=Mathf.Clamp(Mathf.RoundToInt((p.x-terrain.transform.position.x)/data.size.x*(data.alphamapWidth-1)),0,data.alphamapWidth-1),az=Mathf.Clamp(Mathf.RoundToInt((p.z-terrain.transform.position.z)/data.size.z*(data.alphamapHeight-1)),0,data.alphamapHeight-1);var paint=data.GetAlphamaps(ax,az,1,1);if(paint[0,0,2]<.55f)continue;
                float cost=Flat(p-reference).sqrMagnitude;if(cost<score){score=cost;best=p;}
            }
            if(score==float.MaxValue)throw new InvalidOperationException("No sandy beach landmark site");return best;
        }
        private static void PolishRegionalLandmarks(Transform root)
        {
            var group=PolishGroup(root,"Regional landmarks");var wood=PolishMat("Weathered wood",new Color(.34f,.25f,.15f));var red=PolishMat("Faded rescue red",new Color(.48f,.17f,.12f));var metal=PolishMat("Oxidised metal",new Color(.23f,.28f,.29f));var light=PolishMat("Pale timber",new Color(.64f,.58f,.43f));var rubber=PolishMat("Stored rubber",new Color(.06f,.07f,.065f));var stone=PolishMat("Foundation stone",new Color(.38f,.39f,.35f));
            var yard=PolishGroup(group,"Garage recovery supply yard");yard.position=Ground(nodes["g"].Point+new Vector3(20,0,23));
            for(int i=0;i<3;i++)
            {
                float x=i*2.4f;PolishBox(yard,"Storage rack upright",new Vector3(x,1.1f,0),new Vector3(.14f,2.2f,.14f),wood);PolishBox(yard,"Timber shelf",new Vector3(x+.9f,.75f,0),new Vector3(1.9f,.15f,1),wood);
                for(int j=0;j<3;j++)PolishBox(yard,"Stored tire",new Vector3(x+.3f+j*.5f,1.05f,0),new Vector3(.65f,.16f,.65f),rubber,new Vector3(90,0,0),PrimitiveType.Cylinder);
            }
            PolishBox(yard,"Cable reel",new Vector3(1.8f,.65f,3),new Vector3(1,.6f,1),metal,new Vector3(0,0,90),PrimitiveType.Cylinder);PolishBox(yard,"Workbench",new Vector3(5,1,3),new Vector3(2.8f,.18f,1.1f),wood);
            for(int x=0;x<2;x++)PolishBox(yard,"Workbench legs",new Vector3(4+x*2,.45f,3),new Vector3(.18f,.9f,1),metal);
            ConfigureDetailLod(yard.gameObject,260);
            var logging=PolishGroup(group,"Forest logging clearing");logging.position=FindPolishLandmark(polishMap.Sites[3].Position+new Vector3(-38,0,-24),8);
            for(int i=0;i<8;i++){var p=Ground(logging.position+new Vector3((i%4)*1.5f,0,(i/4)*2.4f));PolishProp("P_FirFallen1",p,.45f,72+i%2*5,logging,230);}
            for(int i=0;i<4;i++)PolishProp("P_FirStump1",Ground(logging.position+new Vector3(-3+i*2,0,-3)),.6f,i*53,logging,190);
            PolishBox(logging,"Old loading frame",new Vector3(1.5f,2.7f,4),new Vector3(5,.22f,.25f),wood);foreach(float x in new[]{-.8f,3.8f})PolishBox(logging,"Frame post",new Vector3(x,1.3f,4),new Vector3(.25f,2.6f,.25f),wood);
            ConfigureDetailLod(logging.gameObject,250);
            var mountain=PolishGroup(group,"Mountain rescue shelter");mountain.position=FindPolishLandmark(polishMap.Sites[10].Position+new Vector3(-32,0,24),8);BuildPolishShelter(mountain,red,wood,metal,stone);
            var lake=PolishGroup(group,"Weathered lakeside jetty");var lakeSite=polishMap.Sites[4];lake.position=FindPolishJetty(lakeSite);
            var lakeCenter=environment.GetComponentsInChildren<MeshFilter>().First(f=>f.name.StartsWith("Lake -")).GetComponent<Renderer>().bounds.center;lake.rotation=Quaternion.LookRotation(Flat(lakeCenter-lake.position));
            // A short damaged jetty, with irregular boards and exposed posts.
            for(int i=0;i<12;i++)if(i!=8&&i!=10)PolishBox(lake,"Weathered jetty plank",new Vector3((i%3-1)*.07f,.25f,i*.6f),new Vector3(2.5f,.16f,.51f),i%4==0?light:wood,new Vector3(0,(i%3-1)*2,0));
            foreach(float z in new[]{0f,3,6})foreach(float x in new[]{-1.1f,1.1f})PolishBox(lake,"Jetty timber pile",new Vector3(x,-.6f,z),new Vector3(.2f,2.4f,.2f),wood,new Vector3(0,z*2,3));ConfigureDetailLod(lake.gameObject,270);
            var snow=PolishGroup(group,"Snow route observation hut");snow.position=FindPolishLandmark(polishMap.Sites[14].Position+new Vector3(-35,0,24),7);BuildPolishShelter(snow,metal,wood,PolishMat("Snow capped roof",new Color(.82f,.87f,.89f)),stone);
            PolishBox(snow,"Weather mast",new Vector3(5,3.5f,0),new Vector3(.15f,7,.15f),metal);PolishBox(snow,"Weather crossarm",new Vector3(5,6.5f,0),new Vector3(2.2f,.12f,.12f),metal);for(int i=0;i<4;i++)PolishBox(snow,"Wind direction flag",new Vector3(5.5f+i*.35f,6.1f,-.08f),new Vector3(.36f,.45f-i*.06f,.06f),i%2==0?red:light);
            ConfigureDetailLod(snow.gameObject,320);
            var beach=PolishGroup(group,"Beach fishing equipment camp");beach.position=FindPolishBeach(polishMap.Sites[19].Position);beach.rotation=Quaternion.Euler(0,25,0);
            foreach(float x in new[]{-2f,2f})PolishBox(beach,"Drying rack upright",new Vector3(x,1.5f,0),new Vector3(.18f,3,.18f),wood);for(int i=0;i<3;i++)PolishBox(beach,"Fishing pole rack",new Vector3(0,1+i*.65f,0),new Vector3(4.3f,.1f,.1f),wood);
            for(int i=0;i<5;i++)PolishBox(beach,"Float buoy",new Vector3(-1.5f+i*.7f,1.4f,-.12f),new Vector3(.3f,.5f,.3f),i%2==0?red:light,default,PrimitiveType.Sphere);
            PolishBox(beach,"Storage chest",new Vector3(2,.5f,2),new Vector3(1.3f,1,1),wood);ConfigureDetailLod(beach.gameObject,230);
            var quarry=PolishGroup(group,"Abandoned desert quarry hoist");quarry.position=FindPolishLandmark(polishMap.Sites[22].Position+new Vector3(-40,0,25),10);
            foreach(float x in new[]{-3f,3f})PolishBox(quarry,"Old hoist upright",new Vector3(x,3,0),new Vector3(.3f,6,.3f),metal);PolishBox(quarry,"Old hoist beam",new Vector3(0,5.8f,0),new Vector3(7,.4f,.4f),red);PolishBox(quarry,"Hoist pulley",new Vector3(0,5,0),new Vector3(.8f,.12f,.8f),metal,new Vector3(90,0,0),PrimitiveType.Cylinder);PolishBox(quarry,"Hanging hoist cable",new Vector3(0,3.8f,0),new Vector3(.045f,2.5f,.045f),metal);
            for(int i=0;i<5;i++)PolishProp("P_RockDesert_Clump04",Ground(quarry.position+new Vector3(-6+i*3,0,5)),1.2f+i%2,34+i*41,quarry,270);for(int i=0;i<3;i++)PolishBox(quarry,"Quarry supply box",new Vector3(4+i*.9f,.4f,-2),new Vector3(.8f,.8f,.8f),wood,new Vector3(0,i*8,0));ConfigureDetailLod(quarry.gameObject,320);
        }
        private static void BuildPolishShelter(Transform parent,Material walls,Material wood,Material roof,Material stone)
        {
            foreach(float x in new[]{-2.1f,2.1f})foreach(float z in new[]{-1.7f,1.7f})
            {
                var p=parent.TransformPoint(new Vector3(x,0,z));float bottom=Ground(p).y-parent.position.y-.2f;float height=Mathf.Max(.5f,.35f-bottom);
                PolishBox(parent,"Grounded stone support",new Vector3(x,.35f-height*.5f,z),new Vector3(.5f,height,.5f),stone);
            }
            PolishBox(parent,"Embedded stone footing",new Vector3(0,.05f,0),new Vector3(5,.55f,4),stone);
            for(int i=0;i<8;i++)
            {float y=.5f+i*.29f;PolishBox(parent,"Back timber siding",new Vector3(0,y,1.8f),new Vector3(4.5f,.27f,.14f),walls);foreach(float x in new[]{-2.2f,2.2f})PolishBox(parent,"Side timber siding",new Vector3(x,y,0),new Vector3(.14f,.27f,3.7f),walls);}
            foreach(float x in new[]{-2.2f,2.2f})PolishBox(parent,"Front timber post",new Vector3(x,1.5f,-1.8f),new Vector3(.22f,2.5f,.22f),wood);
            foreach(float side in new[]{-1f,1f})PolishBox(parent,"Pitched metal roof",new Vector3(side*1.2f,3.05f,0),new Vector3(2.7f,.16f,4.5f),roof,new Vector3(0,0,-side*23));
            PolishBox(parent,"Shelter bench",new Vector3(0,.75f,1),new Vector3(3,.16f,.6f),wood);PolishBox(parent,"Bench support",new Vector3(0,.4f,1),new Vector3(2,.7f,.2f),wood);ConfigureDetailLod(parent.gameObject,300);
        }
    }
}
