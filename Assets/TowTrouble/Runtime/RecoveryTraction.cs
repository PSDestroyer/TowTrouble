using UnityEngine;

namespace TowTrouble
{
    // Shared force calculations, independent of networking, effects and vehicle poses.
    public static class RecoveryTraction
    {
        public static Vector2 CableBrake(Vector3 reaction,Vector3 forward,Vector3 right,int wheels)
        {return new Vector2(-Vector3.Dot(reaction,forward),-Vector3.Dot(reaction,right))/Mathf.Max(1,wheels);}
        public static float Grip(float coefficient,float throttle,float aggressiveLoss)
        {
            float aggression=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1,Mathf.Abs(throttle)));
            return Mathf.Max(.05f,coefficient)*(1-Mathf.Clamp01(aggressiveLoss)*aggression);
        }
        public static Vector2 LimitForces(Vector2 requested,float forwardLimit,float sideLimit)
        {
            forwardLimit=Mathf.Max(1,forwardLimit);sideLimit=Mathf.Max(1,sideLimit);
            float use=new Vector2(requested.x/forwardLimit,requested.y/sideLimit).magnitude;
            return requested/Mathf.Max(1,use);
        }
        public static float Slip(float requestedDrive,float availableGrip)
        {return Mathf.Abs(requestedDrive)<1?0:Mathf.Clamp01(1-Mathf.Max(0,availableGrip)/Mathf.Abs(requestedDrive));}
        // Compacted snow/sand still resist rolling; recovery pockets represent deep loose material.
        public static float SoftGroundDrag(RecoveryGroundKind kind,float depth,float equipmentGrip=1)
        {
            float drag=kind==RecoveryGroundKind.Snow?Mathf.Lerp(110,340,Mathf.Clamp01(depth)):
                kind==RecoveryGroundKind.Sand?Mathf.Lerp(75,260,Mathf.Clamp01(depth)):0;
            return drag/Mathf.Sqrt(Mathf.Max(1,equipmentGrip));
        }
        public static float CleanCoverage(float amount,RecoveryGroundKind kind,float speed,float dt)
        {
            float rate=kind==RecoveryGroundKind.Snow?.035f:kind==RecoveryGroundKind.Sand?.025f:.008f;
            return Mathf.MoveTowards(Mathf.Clamp01(amount),0,Mathf.Max(0,dt)*(rate+Mathf.Max(0,speed)*.003f));
        }
        public static Vector3 MudResistance(Vector3 velocity,Vector3 normal,float perWheelDrag,float mass)
        {return -Vector3.ProjectOnPlane(velocity,normal)*Mathf.Max(0,perWheelDrag)*Mathf.Max(.1f,mass/1350f);}
    }
}
