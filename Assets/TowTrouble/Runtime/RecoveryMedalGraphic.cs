using UnityEngine;
namespace TowTrouble
{
    // Vector UI icon: medals remain readable without relying on platform symbol fonts.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RecoveryMedalGraphic:UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;var center=r.center;float radius=Mathf.Min(r.width,r.height)*.48f;
            mesh.AddVert(center,color,Vector2.zero);
            for(int i=0;i<10;i++){float angle=(90+i*36)*Mathf.Deg2Rad;float length=radius*(i%2==0?1:.46f);mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*length,color,Vector2.zero);}
            for(int i=0;i<10;i++)mesh.AddTriangle(0,i+1,(i+1)%10+1);
        }
    }
}
