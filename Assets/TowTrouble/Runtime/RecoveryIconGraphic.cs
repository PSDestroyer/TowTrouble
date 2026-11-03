using UnityEngine;
using UnityEngine.UI;
namespace TowTrouble
{
    public enum RecoveryIcon { Hook, Garage, Wheel, Engine, Spring, Paint, Body, Bonnet, Bumper, Roof, Exhaust, Chassis, Gears, Winch, Coins, Flag, Crew, Settings, Back, Check, Tree, Mountain, Sand, Save, Play, WaypointCar, FinishFlag, DirectionArrow }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RecoveryIconGraphic : MaskableGraphic
    {
        public RecoveryIcon Kind;
        private VertexHelper mesh;
        private Vector2 center;
        private float scale;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();mesh=vh;var r=rectTransform.rect;center=r.center;scale=Mathf.Min(r.width,r.height)/32;
            switch(Kind)
            {
                case RecoveryIcon.Wheel:Circle(0,0,11);Circle(0,0,4);for(int i=0;i<6;i++){float a=i*Mathf.PI/3;L(Mathf.Cos(a)*5,Mathf.Sin(a)*5,Mathf.Cos(a)*9,Mathf.Sin(a)*9);}break;
                case RecoveryIcon.Hook:L(-5,12,5,12);L(0,12,0,1);Arc(0,-3,7,0,-230);L(6,-5,8,0);break;
                case RecoveryIcon.Garage:L(-13,3,0,13);L(0,13,13,3);Box(-10,-12,20,15);L(-5,-12,-5,-1);L(5,-12,5,-1);L(-5,-1,5,-1);break;
                case RecoveryIcon.Engine:Box(-10,-7,18,15);L(-5,12,3,12);L(-1,12,-1,8);L(8,3,13,6);L(13,6,13,-7);L(13,-7,8,-4);L(-14,4,-10,4);L(-14,-3,-10,-3);break;
                case RecoveryIcon.Spring:for(int i=0;i<5;i++){L(-7,-10+i*4,7,-8+i*4);L(7,-8+i*4,-7,-6+i*4);}break;
                case RecoveryIcon.Paint:Box(-11,2,19,10);L(8,7,12,7);L(12,7,12,-1);L(12,-1,0,-1);L(0,-1,0,-12);break;
                case RecoveryIcon.Body:L(-12,-5,-12,3);L(-12,3,-5,3);L(-5,3,-1,10);L(-1,10,8,10);L(8,10,12,3);L(12,3,12,-5);L(-12,-5,12,-5);Circle(-7,-6,3);Circle(8,-6,3);break;
                case RecoveryIcon.Bonnet:L(-12,-7,-7,6);L(-7,6,8,8);L(8,8,12,-7);L(-12,-7,12,-7);L(-5,-1,7,-1);break;
                case RecoveryIcon.Bumper:Box(-12,-4,24,8);L(-9,7,-9,4);L(9,7,9,4);L(-9,-4,-9,-8);L(9,-4,9,-8);break;
                case RecoveryIcon.Roof:L(-12,-4,-12,6);L(12,-4,12,6);L(-12,6,12,6);for(int i=-8;i<12;i+=8)L(i,-4,i,6);break;
                case RecoveryIcon.Exhaust:L(-11,8,-6,8);L(-6,8,-6,-4);L(-6,-4,10,-4);L(10,-4,10,-9);L(-11,3,-11,-9);L(-11,-9,10,-9);break;
                case RecoveryIcon.Chassis:Box(-9,-12,18,24);L(-13,8,13,8);L(-13,-8,13,-8);L(-9,0,9,0);break;
                case RecoveryIcon.Gears:Circle(0,0,8);Circle(0,0,3);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;L(Mathf.Cos(a)*8,Mathf.Sin(a)*8,Mathf.Cos(a)*12,Mathf.Sin(a)*12);}break;
                case RecoveryIcon.Winch:Box(-11,-7,18,14);for(int i=-7;i<8;i+=4)L(i,-7,i,7);L(7,0,13,0);L(13,0,13,-8);Arc(10,-8,3,0,-180);break;
                case RecoveryIcon.Coins:Circle(-3,2,10);L(-3,8,-3,-4);L(-6,5,0,5);L(-6,-1,0,-1);Arc(3,-2,10,75,-100);break;
                case RecoveryIcon.Flag:L(-9,-12,-9,12);L(-9,11,11,11);L(11,11,6,5);L(6,5,11,0);L(11,0,-9,0);break;
                case RecoveryIcon.Crew:Circle(-6,6,4);Circle(7,6,4);Arc(-6,-7,7,0,180);Arc(7,-7,7,0,180);break;
                case RecoveryIcon.Settings:Circle(0,0,5);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;L(Mathf.Cos(a)*8,Mathf.Sin(a)*8,Mathf.Cos(a)*12,Mathf.Sin(a)*12);}Circle(0,0,9);break;
                case RecoveryIcon.Back:L(11,0,-10,0);L(-10,0,-2,8);L(-10,0,-2,-8);break;
                case RecoveryIcon.Check:L(-10,0,-3,-7);L(-3,-7,11,9);break;
                case RecoveryIcon.Tree:L(0,13,-10,-2);L(-10,-2,-5,-2);L(-5,-2,-12,-9);L(-12,-9,12,-9);L(12,-9,5,-2);L(5,-2,10,-2);L(10,-2,0,13);L(0,-9,0,-13);break;
                case RecoveryIcon.Mountain:L(-14,-10,-3,10);L(-3,10,2,1);L(2,1,7,8);L(7,8,14,-10);L(-14,-10,14,-10);L(-6,4,-2,1);L(-2,1,0,5);break;
                case RecoveryIcon.Sand:for(int i=0;i<3;i++)Arc(-1,-10+i*7,12,20,140);break;
                case RecoveryIcon.Save:Box(-11,-12,22,24);Box(-6,4,12,8);Box(-6,-12,12,9);break;
                case RecoveryIcon.WaypointCar: DrawWaypointCar(); break;
                case RecoveryIcon.FinishFlag: DrawFinishFlag(); break;
                case RecoveryIcon.DirectionArrow:
                    SolidRect(-13, -3, 2, 3, color);
                    Triangle(new Vector2(1, 10), new Vector2(14, 0), new Vector2(1, -10));
                    break;
                default:L(-7,-11,11,0);L(11,0,-7,11);L(-7,11,-7,-11);break;
            }
        }
        private void DrawWaypointCar()
        {
            L(-11, 1, -7, 10);
            L(-7, 10, 7, 10);
            L(7, 10, 11, 1);
            SolidRect(-12, -7, 12, 2, color);
            SolidRect(-11, -10, -7, -7, color);
            SolidRect(7, -10, 11, -7, color);
            var cutout = new Color32(33, 35, 39, 255);
            SolidRect(-10, -3, -5, -1, cutout);
            SolidRect(5, -3, 10, -1, cutout);
        }

        private void DrawFinishFlag()
        {
            SolidRect(-11, -13, -8, 12, color);
            SolidRect(-8, 0, 13, 12, color);
            var ink = new Color32(33, 35, 39, 255);
            SolidRect(-8, 6, -1, 12, ink);
            SolidRect(-1, 0, 6, 6, ink);
            SolidRect(6, 6, 13, 12, ink);
        }

        private void SolidRect(float left, float bottom, float right, float top, Color32 tint)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(center + new Vector2(left, bottom) * scale, tint, Vector2.zero);
            mesh.AddVert(center + new Vector2(left, top) * scale, tint, Vector2.zero);
            mesh.AddVert(center + new Vector2(right, top) * scale, tint, Vector2.zero);
            mesh.AddVert(center + new Vector2(right, bottom) * scale, tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        private void Triangle(Vector2 a, Vector2 b, Vector2 c)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(center + a * scale, color, Vector2.zero);
            mesh.AddVert(center + b * scale, color, Vector2.zero);
            mesh.AddVert(center + c * scale, color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }

        private void L(float ax,float ay,float bx,float by)
        {
            Vector2 a=center+new Vector2(ax,ay)*scale,b=center+new Vector2(bx,by)*scale;var v=(b-a).normalized;var p=new Vector2(-v.y,v.x)*1.25f*scale;int n=mesh.currentVertCount;
            foreach(var q in new[]{a-p,a+p,b+p,b-p})mesh.AddVert(q,color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
        private void Box(float x,float y,float w,float h){L(x,y,x+w,y);L(x+w,y,x+w,y+h);L(x+w,y+h,x,y+h);L(x,y+h,x,y);}
        private void Circle(float x,float y,float radius)=>Arc(x,y,radius,0,360);
        private void Arc(float x,float y,float radius,float start,float end){int segments=Mathf.Max(8,Mathf.CeilToInt(Mathf.Abs(end-start)/18));for(int i=0;i<segments;i++){float a=Mathf.Lerp(start,end,i/(float)segments)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(i+1)/(float)segments)*Mathf.Deg2Rad;L(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius);}}
    }
}
