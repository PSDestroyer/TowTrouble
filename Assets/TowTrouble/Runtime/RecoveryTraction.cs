using UnityEngine;

namespace TowTrouble
{
    // Shared force calculations, independent of networking, effects and vehicle poses.
    public static class RecoveryTraction
    {
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
        public static Vector3 MudResistance(Vector3 velocity,Vector3 normal,float perWheelDrag,float mass)
        {return -Vector3.ProjectOnPlane(velocity,normal)*Mathf.Max(0,perWheelDrag)*Mathf.Max(.1f,mass/1350f);}
    }
}
