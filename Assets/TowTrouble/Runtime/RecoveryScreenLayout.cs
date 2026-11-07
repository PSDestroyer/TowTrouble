using UnityEngine;
namespace TowTrouble
{
    // Menus keep their authored composition; camera framing uses the same fitted rectangle.
    public static class RecoveryScreenLayout
    {
        public const float Width=1600,Height=900;
        public const int MinimumWidth=1280,MinimumHeight=720;
        public static bool Readable(Vector2Int size)=>size.x>=MinimumWidth&&size.y>=MinimumHeight;
        public static Rect Safe(Rect safe)
        {
            float left=Mathf.Clamp01(safe.xMin),bottom=Mathf.Clamp01(safe.yMin);
            float right=Mathf.Clamp01(safe.xMax),top=Mathf.Clamp01(safe.yMax);
            return right>left&&top>bottom?Rect.MinMaxRect(left,bottom,right,top):new Rect(0,0,1,1);
        }
        public static Rect Content(float aspect,Rect safe)
        {
            safe=Safe(safe);aspect=Mathf.Max(.1f,aspect);
            float height=Mathf.Min(safe.height,safe.width*aspect/(Width/Height));
            var size=new Vector2(height*(Width/Height)/aspect,height);
            return new Rect(safe.center-size*.5f,size);
        }
        public static Rect ReferenceViewport(Rect reference,float aspect,Rect safe)
        {
            var content=Content(aspect,safe);
            return new Rect(content.x+reference.x/Width*content.width,
                content.y+(1-(reference.y+reference.height)/Height)*content.height,
                reference.width/Width*content.width,reference.height/Height*content.height);
        }
        public static Rect CameraSafeArea(Camera camera)
        {
            if(camera==null||camera.targetTexture!=null||Screen.width<=0||Screen.height<=0)return new Rect(0,0,1,1);
            // Normalized coordinates also work for Editor cameras with a different target size.
            var pixels=Screen.safeArea;var safe=Safe(new Rect(pixels.x/Screen.width,pixels.y/Screen.height,pixels.width/Screen.width,pixels.height/Screen.height));
            var view=camera.rect;
            return Safe(new Rect((safe.x-view.x)/Mathf.Max(.001f,view.width),(safe.y-view.y)/Mathf.Max(.001f,view.height),
                safe.width/Mathf.Max(.001f,view.width),safe.height/Mathf.Max(.001f,view.height)));
        }
    }
}
