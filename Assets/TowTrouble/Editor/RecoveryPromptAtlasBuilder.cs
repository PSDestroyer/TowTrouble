using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace TowTrouble.Editor
{
    public static class RecoveryPromptAtlasBuilder
    {
        private const string Target = "Assets/TowTrouble/Art/UI/Resources/InputGlyphs.asset";
        [MenuItem("Tow Trouble/UI/Rebuild Input Glyph Atlas")]
        public static void Rebuild()
        {
            var sources = new SortedDictionary<string,string>(StringComparer.Ordinal);
            foreach(var file in Directory.GetFiles("Assets/TowTrouble/Art/UI/InputKeyboard", "*.png")) sources[Path.GetFileNameWithoutExtension(file)] = file;
            string root = "Assets/_PlatformSpeciffics/";
            foreach(var family in new[]{"Xbox", "PS"})
            {
                foreach(var file in Directory.GetFiles(root + family + "Glyphs", family + "_*.png")) sources[Path.GetFileNameWithoutExtension(file)] = file;
            }
            sources["PS_Dpad"] = root+"PSGlyphs/PS_DpadUp.png";
            sources["Xbox_L3"] = root+"XboxGlyphs/left-joystick-press.png";
            sources["Xbox_RS"] = root+"XboxGlyphs/Xbox_R3.png";
            sources["PS_Share"] = root+"PSGlyphs/PS_Create.png";
            var nintendo = new Dictionary<string,string> { {"A","button_a"},{"B","button_b"},{"X","button_x"},{"Y","button_y"},{"L","button_l"},{"R","button_r"},{"ZL","button_zl"},{"ZR","button_zr"},{"+","button_plus"},{"-","button_minus"},{"Dpad","dpad"},{"LeftStick","stick_l"},{"RightStick","stick_r"},{"R-stick","stick_r_press"},{"L-stick","stick_l_press"} };
            foreach(var pair in nintendo) sources["Nintendo_"+pair.Key] = root+"Nintendo Switch/Double/switch_"+pair.Value+".png";
            var textures = new List<Texture2D>();
            try
            {
                foreach(var pair in sources)
                {
                    if (!File.Exists(pair.Value)) throw new FileNotFoundException(pair.Value);
                    var source = new Texture2D(2,2,TextureFormat.RGBA32,false);source.LoadImage(File.ReadAllBytes(pair.Value));source.name=pair.Key;
                    if(pair.Key=="PS_Dpad")
                    {
                        var pixels=source.GetPixels32();
                        foreach(string direction in new[]{"Down","Left","Right"})
                        {
                            var part=new Texture2D(2,2,TextureFormat.RGBA32,false);part.LoadImage(File.ReadAllBytes(root+"PSGlyphs/PS_Dpad"+direction+".png"));
                            var more=part.GetPixels32();if(more.Length!=pixels.Length)throw new InvalidDataException("PlayStation d-pad exports need matching sizes.");
                            for(int p=0;p<pixels.Length;p++)if(more[p].a>pixels[p].a)pixels[p]=more[p];UnityEngine.Object.DestroyImmediate(part);
                        }
                        source.SetPixels32(pixels);source.Apply();
                    }
                    // Normalize large source exports before packing, avoiding a full controller sheet in the player.
                    int h = 112, w = Mathf.Clamp(Mathf.RoundToInt(source.width*(112f/source.height)),32,220);
                    var rt = RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                    var previous = RenderTexture.active;
                    Graphics.Blit(source,rt);RenderTexture.active=rt;
                    var copy=new Texture2D(w,h,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,w,h),0,0);copy.Apply();copy.name=pair.Key;
                    RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(source);textures.Add(copy);
                }
                var atlas=new Texture2D(2048,2048,TextureFormat.RGBA32,false);atlas.name="Tow Trouble input glyphs";atlas.filterMode=FilterMode.Bilinear;atlas.wrapMode=TextureWrapMode.Clamp;
                var rects=atlas.PackTextures(textures.ToArray(),4,2048,false);
                var asset=AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(Target);
                if(asset==null){asset=ScriptableObject.CreateInstance<TMP_SpriteAsset>();asset.name="InputGlyphs";AssetDatabase.CreateAsset(asset,Target);}
                foreach(var sub in AssetDatabase.LoadAllAssetsAtPath(Target)) if(sub!=asset) UnityEngine.Object.DestroyImmediate(sub,true);
                var serialized=new SerializedObject(asset);serialized.FindProperty("m_Version").stringValue="1.1.0";serialized.ApplyModifiedPropertiesWithoutUndo();
                asset.spriteCharacterTable.Clear();asset.spriteGlyphTable.Clear();
                string atlasPath="Assets/TowTrouble/Art/UI/InputGlyphsAtlas.png";
                File.WriteAllBytes(atlasPath,atlas.EncodeToPNG());AssetDatabase.ImportAsset(atlasPath);
                var importer=(TextureImporter)AssetImporter.GetAtPath(atlasPath);importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
                asset.spriteSheet=AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                asset.faceInfo=new FaceInfo { familyName="Tow Trouble",styleName="Input",pointSize=32,scale=1,ascentLine=28,descentLine=-4,lineHeight=32 };
                var material=new Material(Shader.Find("TextMeshPro/Sprite"));material.name="Input glyph material";material.mainTexture=asset.spriteSheet;asset.material=material;
                AssetDatabase.AddObjectToAsset(material,asset);
                for(int i=0;i<textures.Count;i++)
                {
                    var r=rects[i];var gr=new GlyphRect(Mathf.RoundToInt(r.x*atlas.width),Mathf.RoundToInt(r.y*atlas.height),Mathf.RoundToInt(r.width*atlas.width),Mathf.RoundToInt(r.height*atlas.height));
                    float width=32f*gr.width/gr.height;
                    var glyph=new TMP_SpriteGlyph((uint)i,new GlyphMetrics(width,32,0,26,width+3),gr,1,0);
                    var character=new TMP_SpriteCharacter(0xFFFE,asset,glyph){name=textures[i].name,scale=1};
                    asset.spriteGlyphTable.Add(glyph);asset.spriteCharacterTable.Add(character);
                }
                asset.UpdateLookupTables();EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
                UnityEngine.Object.DestroyImmediate(atlas);
                Debug.Log("Input glyph atlas: "+textures.Count+" glyphs, "+asset.spriteSheet.width+"x"+asset.spriteSheet.height+". Source: PlatformSpecifics + original Tow Trouble keyboard keycaps.");
            }
            finally { foreach(var texture in textures) UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
