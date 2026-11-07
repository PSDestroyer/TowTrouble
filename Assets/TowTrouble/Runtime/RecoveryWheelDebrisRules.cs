using UnityEngine;

namespace TowTrouble
{
    public enum RecoveryWheelDebrisKind { Mud, Sand, Snow, Dirt, Grass, Stone, Wood }

    public static class RecoveryWheelDebrisRules
    {
        public static RecoveryWheelDebrisKind FromTrack(RecoveryTrackKind kind)
        {
            switch(kind)
            {
                case RecoveryTrackKind.Dirt:return RecoveryWheelDebrisKind.Dirt;
                case RecoveryTrackKind.Sand:return RecoveryWheelDebrisKind.Sand;
                case RecoveryTrackKind.Snow:return RecoveryWheelDebrisKind.Snow;
                case RecoveryTrackKind.Grass:return RecoveryWheelDebrisKind.Grass;
                case RecoveryTrackKind.Wood:return RecoveryWheelDebrisKind.Wood;
                default:return RecoveryWheelDebrisKind.Stone;
            }
        }
        public static Color Color(RecoveryWheelDebrisKind kind)
        {
            switch(kind)
            {
                case RecoveryWheelDebrisKind.Mud:return new Color(.30f,.20f,.10f);
                case RecoveryWheelDebrisKind.Sand:return new Color(.87f,.72f,.44f);
                case RecoveryWheelDebrisKind.Snow:return new Color(.94f,.97f,1);
                case RecoveryWheelDebrisKind.Grass:return new Color(.34f,.45f,.13f);
                case RecoveryWheelDebrisKind.Dirt:return new Color(.54f,.41f,.26f);
                default:return new Color(.48f,.49f,.46f);
            }
        }
        // No movement means no debris, except actual wheelspin at a grounded tire.
        public static float Rate(RecoveryWheelDebrisKind kind,float speed,float slip,float angularSpeed,bool grounded)
        {
            if(!grounded||kind==RecoveryWheelDebrisKind.Wood)return 0;
            float activity=Mathf.Max(0,speed-.4f)+Mathf.Clamp01(slip)*Mathf.Min(8,Mathf.Abs(angularSpeed)*.4f);
            if(kind==RecoveryWheelDebrisKind.Stone)return slip>.35f?Mathf.Min(4,activity*.45f):0;
            float maximum=kind==RecoveryWheelDebrisKind.Grass?10:kind==RecoveryWheelDebrisKind.Dirt?18:32;
            return Mathf.Min(maximum,activity*(kind==RecoveryWheelDebrisKind.Grass?1.2f:3));
        }
        // Rolling direction wins over throttle: braking must not flip the plume.
        public static Vector3 ThrowDirection(Vector3 wheelForward,Vector3 velocity,float angularSpeed,float throttle)
        {
            float travel=Vector3.Dot(velocity,wheelForward);
            float sign=Mathf.Abs(travel)>.2f?Mathf.Sign(travel):Mathf.Abs(angularSpeed)>1?Mathf.Sign(angularSpeed):throttle<-.05f?-1:1;
            return -wheelForward*sign;
        }
    }
}
