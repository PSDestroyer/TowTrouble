using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static partial class DemoBuilder
    {
        // Rotate a COPY of the vendor terrain and its painted maps; Terrain itself
        // cannot be rotated. Source scenes, terrain data and prefabs stay intact.
        private static void ImportSoStylizedValley(Transform parent)
        {
            var target=parent.gameObject.scene;
            var source=EditorSceneManager.OpenScene("Assets/SoStylized/Demo/M_Demonstration_Mega.unity",OpenSceneMode.Additive);
            var rotation=Quaternion.Euler(0,90,0);
            var anchor=new Vector3(250,6,-15);
            var destination=new Vector3(0,0,18);
            var sourceTerrain=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).First();
            var original=sourceTerrain.terrainData;
            var data=UnityEngine.Object.Instantiate(original);data.name="Valea Verde - Recovery Terrain";
            string path=Root+"/Art/Meshes/ValleyRecoveryTerrain.asset";
            if(AssetDatabase.LoadAssetAtPath<TerrainData>(path)!=null)AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(data,path);
            Vector3 origin=new Vector3(-485,-106,-232);
            int n=data.heightmapResolution;var old=original.GetHeights(0,0,n,n);var heights=new float[n,n];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                int nz=n-1-x,nx=z;float wx=origin.x+nx/(float)(n-1)*data.size.x,wz=origin.z+nz/(float)(n-1)*data.size.z;
                float h=old[z,x];
                if(wz>-14&&wz<150&&Mathf.Abs(wx)<55)
                {
                    float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,55,Mathf.Abs(wx)));
                    blend*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-14,-7,wz))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(140,150,wz)));
                    h=Mathf.Lerp(h,(Height(wx,wz)-origin.y)/data.size.y,blend);
                }
                heights[nz,nx]=h;
            }
            data.SetHeights(0,0,heights);
            int a=original.alphamapResolution,layers=original.alphamapLayers;var alpha=original.GetAlphamaps(0,0,a,a);var rotated=new float[a,a,layers];
            for(int z=0;z<a;z++)for(int x=0;x<a;x++)for(int l=0;l<layers;l++)rotated[a-1-x,z,l]=alpha[z,x,l];
            data.SetAlphamaps(0,0,rotated);
            int d=original.detailResolution;
            for(int layer=0;layer<original.detailPrototypes.Length;layer++)
            {
                var sourceDetail=original.GetDetailLayer(0,0,d,d,layer);var detail=new int[d,d];
                for(int z=0;z<d;z++)for(int x=0;x<d;x++)
                {
                    int nz=d-1-x,nx=z;float wx=origin.x+nx/(float)d*data.size.x,wz=origin.z+nz/(float)d*data.size.z;
                    detail[nz,nx]=Mathf.Abs(wx)<6.8f&&wz>-10&&wz<145?0:sourceDetail[z,x];
                }
                data.SetDetailLayer(0,0,layer,detail);
            }
            var trees=new List<TreeInstance>();
            foreach(var tree in original.treeInstances)
            {
                var t=tree;t.position=new Vector3(tree.position.z,tree.position.y,1-tree.position.x);t.rotation+=Mathf.PI*.5f;
                var wp=origin+Vector3.Scale(t.position,data.size);if(Mathf.Abs(wp.x)<14&&wp.z>-10&&wp.z<145)continue;trees.Add(t);
            }
            data.SetTreeInstances(trees.ToArray(),true);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(target);
            var terrainGo=Terrain.CreateTerrainGameObject(data);terrainGo.name="So Stylized - authored valley";terrainGo.transform.SetParent(parent,false);terrainGo.transform.position=origin;terrainGo.layer=8;
            var terrain=terrainGo.GetComponent<Terrain>();terrain.materialTemplate=sourceTerrain.materialTemplate;terrain.heightmapPixelError=5;terrain.basemapDistance=600;terrain.detailObjectDistance=95;terrain.detailObjectDensity=.7f;terrain.treeDistance=450;terrain.treeBillboardDistance=200;terrain.drawInstanced=true;
            var grounded=new List<GameObject>();
            var route=new Bounds(new Vector3(0,2,68),new Vector3(25,14,162));int retained=0;
            foreach(var go in source.GetRootGameObjects())
            {
                if(go==sourceTerrain.gameObject||go.GetComponentInChildren<Camera>(true)!=null||go.GetComponent<Volume>()!=null||go.name=="P_Sky")continue;
                go.transform.position=rotation*(go.transform.position-anchor)+destination;go.transform.rotation=rotation*go.transform.rotation;
                bool water=go.name=="P_Water"||go.name.Contains("Waterfall");
                if(!water&&go.GetComponentsInChildren<Renderer>(true).Any(r=>r.bounds.Intersects(route)))continue;
                if((go.name.Contains("Cliff")||go.name.Contains("Shelf"))&&go.GetComponentsInChildren<Renderer>(true).Any(r=>r.bounds.Intersects(new Bounds(new Vector3(0,12,65),new Vector3(58,45,170)))))continue;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,target);go.transform.SetParent(parent,true);
                foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=8;
                if(Mathf.Abs(go.transform.position.x)<60&&go.transform.position.z>-20&&go.transform.position.z<160)
                {
                    bool plant=go.name.StartsWith("P_Oak")||go.name.StartsWith("P_Pine")||go.name.StartsWith("P_Fir")||go.name.StartsWith("P_Birch")||go.name.StartsWith("P_Grass")||go.name.StartsWith("P_Bush")||go.name.StartsWith("P_Flower")||go.name.StartsWith("P_Boulder")||go.name.StartsWith("P_RockClassic");
                    if(plant){var sourcePoint=Quaternion.Inverse(rotation)*(go.transform.position-destination)+anchor;float oldGround=original.GetInterpolatedHeight((sourcePoint.x+500)/1000,(sourcePoint.z+500)/1000)-106;float offset=go.transform.position.y-oldGround;{var pos=go.transform.position;pos.y=terrain.SampleHeight(pos)+origin.y+Mathf.Clamp(offset,-.3f,.2f);go.transform.position=pos;}}
                }
                retained++;
            }
            EditorSceneManager.CloseScene(source,true);EditorUtility.SetDirty(data);
            // Later vehicle texture/audio imports refresh the AssetDatabase. Persist
            // the modified terrain first so that refresh cannot reload its source copy.
            AssetDatabase.SaveAssetIfDirty(data);
            Debug.Log("[TowTrouble] Authored So Stylized valley adapted: "+retained+" environment groups, "+trees.Count+" painted trees. Vendor terrain unchanged.");
        }
    }
}
