using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Visuals/Polish Workshop Building")]
        public static void PolishWorkshopBuilding()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Workshop authoring requires Edit Mode.");
            VisualContext();
            var root = environment.Find("Workshop service base");
            var world = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            if (root == null || world == null) throw new InvalidOperationException("Workshop service base is missing.");
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Polish workshop building exterior");
            foreach (var asset in AssetDatabase.FindAssets("", new[] { GarageServiceFolder })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadMainAssetAtPath))
                if (asset is Mesh || asset is Material) Undo.RegisterCompleteObjectUndo(asset,"Polish workshop building assets");
            try
            {
                BuildGarageFacade(root,world.GaragePosition);
                Physics.SyncTransforms();
                var facade = root.Find("Workshop exterior service frontage");
                foreach (var filter in facade.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh == null || filter.sharedMesh.bounds.size.sqrMagnitude < .0001f)
                        throw new InvalidOperationException("Missing workshop geometry: " + filter.name);
                if (facade.GetComponentsInChildren<Light>(true).Length > 0 || facade.GetComponentsInChildren<MonoBehaviour>(true).Length > 0)
                    throw new InvalidOperationException("Exterior workshop must not include runtime scripts or lights.");
                foreach (var t in facade.GetComponentsInChildren<Transform>(true)) { t.gameObject.isStatic=true; t.gameObject.layer=8; }
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save polished workshop.");
                Undo.CollapseUndoOperations(undo);
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        // Dedicated finishes keep the roof/doors readable in shade without
        // recolouring supply props, mountain shelters or the whole county.
        private static Material WorkshopFinish(string role)
        {
            Color color; float smoothness=.18f;
            switch(role)
            {
                case "Wall": color=new Color(.67f,.36f,.22f); break;
                case "WallLight": color=new Color(.76f,.44f,.28f); break;
                case "Roof": color=new Color(.29f,.37f,.42f); smoothness=.22f; break;
                case "RoofEdge": color=new Color(.23f,.30f,.34f); break;
                case "Trim": color=new Color(.88f,.83f,.68f); break;
                case "Timber": color=new Color(.49f,.34f,.22f); break;
                case "Door": color=new Color(.37f,.58f,.57f); break;
                case "DoorLight": color=new Color(.44f,.66f,.64f); break;
                case "Glass": color=new Color(.39f,.61f,.68f); smoothness=.62f; break;
                case "Metal": color=new Color(.19f,.25f,.28f); smoothness=.28f; break;
                case "Foundation": color=new Color(.52f,.51f,.45f); break;
                case "Dust": color=new Color(.57f,.47f,.32f); break;
                case "Lamp": color=new Color(1,.86f,.61f); break;
                default: throw new ArgumentException("Unknown workshop finish: " + role);
            }
            string name="Workshop facade " + role;
            string path=GarageServiceFolder+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            bool existing=mat!=null;
            if(!existing)mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
            mat.SetColor("_BaseColor",color); mat.SetFloat("_Smoothness",smoothness); mat.SetFloat("_Metallic",0);
            if(role=="Timber")
                mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Suntail Village/Textures/Building Modules/Wood_1_AlbedoSmoothness.png"));
            // Low surface fill preserves the painted colour in the north-facing
            // facade's deep shade; only the lamp lens is a bright emissive accent.
            float fill=role=="Lamp"?.65f:role=="Glass"?.18f:role=="Door"||role=="DoorLight"?.16f:
                role=="Wall"||role=="WallLight"?.065f:role=="Trim"?.05f:0;
            if(fill>0)
            {
                mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor",color*fill);
                // URP's material validator disables _EMISSION with None. Baked
                // emission retains this local surface fill without a live light.
                mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            else { mat.DisableKeyword("_EMISSION"); mat.SetColor("_EmissionColor",Color.black); }
            if(existing){EditorUtility.SetDirty(mat);return mat;}
            return VisualAsset(mat,"GarageServiceBase/"+name+".mat");
        }

        private static void BuildPolishedWorkshopDoors(Transform facade)
        {
            var front=PolishGroup(facade,"Framed workshop frontage");
            var wall=WorkshopFinish("Wall"); var wallLight=WorkshopFinish("WallLight");
            var trim=WorkshopFinish("Trim"); var blue=WorkshopFinish("Door"); var light=WorkshopFinish("DoorLight");
            var metal=WorkshopFinish("Metal"); var glass=WorkshopFinish("Glass"); var stone=WorkshopFinish("Foundation");
            using(var shapes=new GarageFacadeShapes())
            {
                // Wide enough for the game's haulers, with substantial side and
                // central wall piers instead of two doors filling the entire shell.
                foreach(float x in new[]{-8.32f,0,8.32f})
                {
                    float width=x==0?2.48f:1.56f;
                    shapes.BevelBox(wall,new Vector3(x,2.7f,15.18f),new Vector3(width,5.4f,.46f),.055f);
                    shapes.BevelBox(stone,new Vector3(x,.30f,14.94f),new Vector3(width+.06f,.60f,.18f),.045f);
                    GarageFacadeCollider(front,"Front wall pier",new Vector3(x,2.7f,15.18f),new Vector3(width,5.4f,.46f));
                    for(int course=0;course<4;course++)
                        shapes.Box(wallLight,new Vector3(x,1.35f+course*.95f,14.938f),new Vector3(width-.15f,.07f,.025f));
                }
                shapes.Box(wall,new Vector3(0,5.02f,15.18f),new Vector3(18,.76f,.46f));
                shapes.BevelBox(trim,new Vector3(0,5.23f,14.89f),new Vector3(18.45f,.22f,.32f),.04f);
                foreach(float side in new[]{-1f,1f})
                {
                    float x=side*4.45f;
                    shapes.Box(metal,new Vector3(x,2.25f,15.02f),new Vector3(6.5f,4.5f,.22f));
                    for(int panel=0;panel<6;panel++)
                    {
                        float y=.42f+panel*.70f;
                        shapes.BevelBox(panel%2==0?blue:light,new Vector3(x,y,14.86f),new Vector3(6.16f,.66f,.16f),.045f);
                        // Recessed broad panels read at gameplay distance.
                        if(panel<4)foreach(float dx in new[]{-1.53f,1.53f})
                            shapes.BevelBox(blue,new Vector3(x+dx,y,14.763f),new Vector3(2.83f,.45f,.055f),.025f);
                    }
                    foreach(float dx in new[]{-3.23f,3.23f})
                        shapes.BevelBox(trim,new Vector3(x+dx,2.29f,14.67f),new Vector3(.25f,4.58f,.32f),.035f);
                    shapes.BevelBox(trim,new Vector3(x,4.58f,14.67f),new Vector3(6.72f,.24f,.32f),.04f);
                    for(int pane=0;pane<3;pane++)
                    {
                        float px=x-2.02f+pane*2.02f;
                        shapes.BevelBox(metal,new Vector3(px,3.9f,14.73f),new Vector3(1.73f,.59f,.10f),.035f);
                        shapes.BevelBox(glass,new Vector3(px,3.9f,14.665f),new Vector3(1.54f,.42f,.055f),.025f);
                        shapes.Box(light,new Vector3(px,4.035f,14.633f),new Vector3(1.42f,.035f,.01f));
                    }
                    shapes.BevelBox(metal,new Vector3(x,.24f,14.72f),new Vector3(6.18f,.27f,.10f),.025f);
                    foreach(float dx in new[]{-.4f,.4f})
                        shapes.Box(metal,new Vector3(x+dx,1.13f,14.66f),new Vector3(.10f,.15f,.16f));
                    shapes.BevelBox(trim,new Vector3(x,1.13f,14.56f),new Vector3(.92f,.10f,.10f),.02f);
                    GarageFacadeCollider(front,"Closed workshop door",new Vector3(x,2.25f,15.02f),new Vector3(6.5f,4.5f,.24f));
                }
                shapes.Finish(front,"Polished workshop doors and masonry");
            }
            ConfigureDetailLod(front.gameObject,900);
        }
    }
}
