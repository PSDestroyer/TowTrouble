using System;
using UnityEngine;

namespace TowTrouble
{
    public enum RecoveryLoadMaterial { Firm, Mud, Snow, Sand, Silt }

    [Serializable]
    public sealed class RecoveryLoadSettings
    {
        public RecoveryLoadMaterial Material;
        [Min(0)] public float Mass;
        [Min(0)] public float Burial, LipHeight;
        [Range(0,1)] public float BuriedWheels, LipWheels, BellyContact, LockedWheels;
        [Min(0)] public float RetainedWater;
    }

    public struct RecoveryLoadForces
    {
        public float Rolling, Burial, Lip, Belly, Damage;
        public float Total=>Rolling+Burial+Lip+Belly+Damage;
    }

    // Static soil/lip/damage loads; gravity and contact grip remain in the host's
    // actual rigidbody simulation. Tier is deliberately absent from this formula.
    public static class RecoveryLoad
    {
        public static RecoveryLoadForces Forces(RecoveryLoadSettings load,float radius)
        {
            if(load==null||load.Mass<=0)return default;
            float weight=(load.Mass+Mathf.Max(0,load.RetainedWater))*9.81f;
            bool mud=load.Material==RecoveryLoadMaterial.Mud||load.Material==RecoveryLoadMaterial.Silt;
            float rolling=mud?.06f:load.Material==RecoveryLoadMaterial.Snow?.04f:load.Material==RecoveryLoadMaterial.Sand?.055f:.025f;
            float soil=mud?1.05f:load.Material==RecoveryLoadMaterial.Snow?.90f:load.Material==RecoveryLoadMaterial.Sand?.80f:.65f;
            return new RecoveryLoadForces {
                Rolling=weight*rolling,
                Burial=weight*soil*Mathf.Clamp(load.Burial/Mathf.Max(.25f,radius),0,1.5f)*Mathf.Clamp01(load.BuriedWheels),
                Lip=weight*Mathf.Sin(Mathf.Atan(Mathf.Max(0,load.LipHeight)/Mathf.Max(.2f,radius*.85f)))*Mathf.Clamp01(load.LipWheels),
                Belly=weight*.42f*Mathf.Clamp01(load.BellyContact),
                Damage=weight*.90f*Mathf.Clamp01(load.LockedWheels)
            };
        }
        public static float AlongSlope(float mass,Vector3 normal,Vector3 direction)
        {
            normal=normal.sqrMagnitude>.1f?normal.normalized:Vector3.up;
            var tangent=Vector3.ProjectOnPlane(direction,normal).normalized;
            return -Vector3.Dot(Physics.gravity*mass,tangent);
        }
        // Steady, low-speed drawbar estimate used by the authoring audit. It is
        // intentionally separate from shock force and from a winch's holding load.
        public static float Drawbar(GaragePerformance car,float grip,float gradeDegrees,float dragPerWheel,float speed=.35f)
        {
            float angle=gradeDegrees*Mathf.Deg2Rad,weight=car.Mass*9.81f;
            float traction=Mathf.Min(car.Drive*4,Mathf.Max(0,weight*Mathf.Cos(angle)*grip));
            return Mathf.Max(0,traction-weight*Mathf.Sin(angle)-4*dragPerWheel*(car.Mass/1350f)*speed-140*speed);
        }
    }
}
