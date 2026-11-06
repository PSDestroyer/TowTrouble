using System;
using UnityEngine;
namespace TowTrouble
{
    [Serializable] public sealed class RecoveryPaintAtlas
    {
        public Texture2D Source;
        public Texture2D[] Colors;
        public Color FactoryColor;
    }
    // Owned, baked atlas variants recolor only the paint UV strip. Trim, glass and decals remain authored.
    public sealed class RecoveryPaintPalette : ScriptableObject
    {
        public RecoveryPaintAtlas[] Atlases=Array.Empty<RecoveryPaintAtlas>();
        private static RecoveryPaintPalette instance;
        public static RecoveryPaintPalette Current=>instance!=null?instance:instance=Resources.Load<RecoveryPaintPalette>("RecoveryPaintPalette");
        private static readonly Color[] Colors={new Color(.98f,.93f,.74f),new Color(.43f,.48f,.24f),new Color(.95f,.65f,.16f),new Color(.38f,.43f,.2f)};
        public static Color Swatch(int paint,int model)=>Colors[paint>0?Mathf.Clamp(paint-1,0,2):Mathf.Clamp(model,0,3)];
        public static int Resolve(RecoveryBuild build,RecoveryGarageCatalog catalog,int fallback=0)
        {
            if(build?.Items!=null)foreach(var id in build.Items){var part=catalog.Find(id);if(part?.Slot==GarageSlot.Paint)return Mathf.Clamp(part.PaintIndex,0,3);}
            return Mathf.Clamp(fallback,0,3);
        }
        public static bool StockBody(RecoveryBuild build,RecoveryGarageCatalog catalog)=>build.Item(GarageSlot.Assembly,catalog)==catalog.Stock(GarageSlot.Assembly)?.Id;
        public Texture TextureFor(Texture source,int paint,int model,bool stockBody)
        {
            foreach(var atlas in Atlases)if(atlas.Source==source)
            {if(paint==0&&!stockBody)return source;int index=paint>0?Mathf.Clamp(paint-1,0,2):Mathf.Clamp(model,0,3);return atlas.Colors[index];}
            return source;
        }
        public Color SwatchFor(RecoveryBuild build,RecoveryGarageCatalog catalog,int paint)
        {
            if(paint>0||StockBody(build,catalog))return Swatch(paint,build.Model);
            var prefab=catalog.Assembly(build);
            if(prefab!=null)foreach(var r in prefab.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)
            {if(m==null||!m.HasProperty("_BaseMap"))continue;var source=m.GetTexture("_BaseMap");foreach(var atlas in Atlases)if(atlas.Source==source)return atlas.FactoryColor;}
            return Color.white;
        }
        public static bool InPaintStrip(int x,int y,int width,int height)=>x<width/4&&y>=height/4;
    }
}
