using UnityEngine;
using UnityEngine.UI;

namespace TowTrouble
{
    public enum RecoveryPlateStyle { Charcoal, Gold, Paper, Custom, Shade }
    // Resolution-independent sign plates: dark extrusion, warm bevel and restrained wear.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RecoveryPlateGraphic : MaskableGraphic
    {
        public RecoveryPlateStyle Style=RecoveryPlateStyle.Charcoal;
        public Color Tint=Color.white;
        public float Opacity=1;
        private bool focused,pressed;
        public void State(bool focus,bool down){if(focus==focused&&down==pressed)return;focused=focus;pressed=down;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;if(r.width<1||r.height<1)return;
            if(Style==RecoveryPlateStyle.Shade){bool header=r.height<250;Poly(vh,r,0,new Color(.025f,.035f,.045f,header?.84f:.70f),new Color(.025f,.035f,.045f,header?.70f:.30f));return;}
            float bevel=r.height<43?2:3.5f,cut=Mathf.Min(12,r.height*.15f),depth=pressed?1:Mathf.Min(5,r.height*.075f);
            Poly(vh,Shift(r,2,-depth-1),cut,new Color(.01f,.015f,.025f,.65f),new Color(.01f,.015f,.025f,.65f));
            Poly(vh,r,cut,new Color(.055f,.065f,.085f),new Color(.025f,.03f,.045f));
            Color rim=focused?new Color(1,.72f,.24f):Style==RecoveryPlateStyle.Gold?new Color(.72f,.42f,.09f):new Color(.30f,.32f,.35f);
            Poly(vh,Inset(r,1.5f),Mathf.Max(2,cut-1),rim,rim*.82f);
            Color top,bottom;
            if(Style==RecoveryPlateStyle.Gold){top=new Color(1,.89f,.48f);bottom=new Color(.96f,.55f,.12f);}
            else if(Style==RecoveryPlateStyle.Paper){top=new Color(1,.96f,.79f);bottom=new Color(.83f,.75f,.54f);}
            else if(Style==RecoveryPlateStyle.Custom){top=Tint;bottom=Tint*.72f;}
            else{top=new Color(.19f,.22f,.25f);bottom=new Color(.10f,.125f,.15f);}
            if(pressed){top*=.82f;bottom*=.82f;}
            var inner=Inset(r,bevel);Poly(vh,inner,Mathf.Max(1,cut-bevel),top,bottom);
            float margin=bevel+cut;
            Quad(vh,new Rect(r.xMin+margin,r.yMax-bevel-1,r.width-margin*2,1.2f),new Color(1,.98f,.83f,Style==RecoveryPlateStyle.Gold?.7f:.17f));
            if(r.width>200&&r.height>80)
            {
                Screw(vh,new Vector2(r.xMin+12,r.yMax-12));Screw(vh,new Vector2(r.xMax-12,r.yMin+12));
            }
            if(Style==RecoveryPlateStyle.Gold&&r.width>120)
            {
                Line(vh,new Vector2(r.xMin+18,r.yMin+7),new Vector2(r.xMin+29,r.yMin+8),1,new Color(.5f,.23f,.06f,.22f));
                Line(vh,new Vector2(r.xMax-30,r.yMax-8),new Vector2(r.xMax-22,r.yMax-10),1,new Color(1,1,.8f,.5f));
            }
        }
        private Color Finish(Color c){c*=color;c.a*=Opacity;return c;}
        private void Poly(VertexHelper vh,Rect r,float cut,Color top,Color bottom)
        {
            int start=vh.currentVertCount;vh.AddVert(r.center,Finish(Color.Lerp(bottom,top,.5f)),Vector2.zero);
            var p=new[]{new Vector2(r.xMin+cut,r.yMin),new Vector2(r.xMax-cut,r.yMin),new Vector2(r.xMax,r.yMin+cut),new Vector2(r.xMax,r.yMax-cut),new Vector2(r.xMax-cut,r.yMax),new Vector2(r.xMin+cut,r.yMax),new Vector2(r.xMin,r.yMax-cut),new Vector2(r.xMin,r.yMin+cut)};
            foreach(var v in p)vh.AddVert(v,Finish(Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,v.y))),Vector2.zero);
            for(int i=0;i<8;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%8);
        }
        private void Quad(VertexHelper vh,Rect r,Color c){int n=vh.currentVertCount;c=Finish(c);vh.AddVert(new Vector2(r.xMin,r.yMin),c,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),c,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMax),c,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        private void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c){var d=(b-a).normalized;var p=new Vector2(-d.y,d.x)*width*.5f;int n=vh.currentVertCount;c=Finish(c);foreach(var v in new[]{a-p,a+p,b+p,b-p})vh.AddVert(v,c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        private void Screw(VertexHelper vh,Vector2 center){int n=vh.currentVertCount;vh.AddVert(center,Finish(new Color(.27f,.29f,.31f)),Vector2.zero);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*2.6f,Finish(new Color(.06f,.075f,.09f)),Vector2.zero);}for(int i=0;i<8;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%8);Line(vh,center-new Vector2(1.3f,1),center+new Vector2(1.3f,1),.8f,new Color(.57f,.59f,.57f));}
        private static Rect Inset(Rect r,float n)=>new Rect(r.x+n,r.y+n,r.width-n*2,r.height-n*2);
        private static Rect Shift(Rect r,float x,float y)=>new Rect(r.x+x,r.y+y,r.width,r.height);
    }
}
