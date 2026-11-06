using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static class RecoveryPaintAuthoring
    {
        private const string Root="Assets/TowTrouble/Art/Paint/Resources";
        [MenuItem("Tow Trouble/Rebuild Vehicle Paint Palette")]
        public static void Menu()=>Bake();
        public static string Bake()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Bake paint in Edit Mode.");
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var path=Root+"/RecoveryPaintPalette.asset";var palette=AssetDatabase.LoadAssetAtPath<RecoveryPaintPalette>(path);
            if(palette==null){palette=ScriptableObject.CreateInstance<RecoveryPaintPalette>();AssetDatabase.CreateAsset(palette,path);}
            string[] sources={"Assets/Hot_Rod_Oldtimers/Materials/Oldtimers_Albedo_Red_Gradient.png","Assets/Hot_Rod_OffRoad/Materials/HotRods_Albedo_Military_Gradient.png"};
            var atlases=new RecoveryPaintAtlas[sources.Length];
            for(int a=0;a<sources.Length;a++)
            {
                var source=AssetDatabase.LoadAssetAtPath<Texture2D>(sources[a]);if(source==null)throw new InvalidOperationException(sources[a]);
                var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                Texture2D copy=null;Color[] pixels;
                try{Graphics.Blit(source,rt);RenderTexture.active=rt;copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();pixels=copy.GetPixels();}
                finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);}
                float peak=0;for(int y=source.height/4;y<source.height;y++)for(int x=0;x<source.width/4;x++){var c=pixels[y*source.width+x];peak=Mathf.Max(peak,c.r,c.g,c.b);}
                var entry=new RecoveryPaintAtlas{Source=source,Colors=new Texture2D[4],FactoryColor=pixels[(source.height-1)*source.width+source.width/8]};
                for(int color=0;color<4;color++)
                {
                    var tint=RecoveryPaintPalette.Swatch(0,color);var edited=(Color[])pixels.Clone();
                    for(int y=source.height/4;y<source.height;y++)for(int x=0;x<source.width/4;x++)
                    {int i=y*source.width+x;var p=pixels[i];float shade=Mathf.Max(p.r,p.g,p.b)/Mathf.Max(.001f,peak);edited[i]=new Color(tint.r*shade,tint.g*shade,tint.b*shade,p.a);}
                    var texture=new Texture2D(source.width,source.height,TextureFormat.RGBA32,true);texture.SetPixels(edited);texture.Apply();
                    string file=Root+"/"+(a==0?"Classic":"Offroad")+"_"+color+".png";
                    try{File.WriteAllBytes(file,texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
                    AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceUpdate);var importer=(TextureImporter)AssetImporter.GetAtPath(file);
                    importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.sRGBTexture=true;importer.wrapMode=source.wrapMode;importer.filterMode=source.filterMode;importer.SaveAndReimport();
                    entry.Colors[color]=AssetDatabase.LoadAssetAtPath<Texture2D>(file);
                }
                atlases[a]=entry;
            }
            palette.Atlases=atlases;EditorUtility.SetDirty(palette);AssetDatabase.SaveAssetIfDirty(palette);return "Baked 8 paint atlases; source materials and scenes unchanged.";
        }
    }
}
