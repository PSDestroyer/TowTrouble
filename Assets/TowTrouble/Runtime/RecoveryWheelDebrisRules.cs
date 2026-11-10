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
        // A trail of suspended fines needs its own budget: a fraction of the clod
        // rate left only two or three tiny puffs alive at normal driving speed.
        public static float PowderRate(RecoveryWheelDebrisKind kind,float speed,float slip,float angularSpeed,bool grounded,bool limited=false)
        {
            float coarse=Rate(kind,speed,slip,angularSpeed,grounded);
            // Wheel rate already includes slip. Squaring that response again made
            // a spinning, stationary tire much quieter than ordinary rolling.
            float activity=Mathf.Max(0,speed-.4f)+Mathf.Min(10,Mathf.Abs(angularSpeed)*.55f)*Mathf.Sqrt(Mathf.Clamp01(slip));
            float maximum=limited?8:14;
            switch(kind)
            {
                case RecoveryWheelDebrisKind.Dirt:return grounded?Mathf.Min(maximum,activity*3.4f):0;
                case RecoveryWheelDebrisKind.Sand:return grounded?Mathf.Min(maximum,activity*2.8f):0;
                case RecoveryWheelDebrisKind.Snow:return grounded?Mathf.Min(maximum,activity*2.4f):0;
                case RecoveryWheelDebrisKind.Grass:return grounded?Mathf.Min(limited?3:4,activity*.9f):0;
                case RecoveryWheelDebrisKind.Mud:return coarse*.10f;
                default:return coarse*.32f;
            }
        }
        public static Color PowderColor(RecoveryWheelDebrisKind kind)
        {
            var color=Color(kind);
            switch(kind)
            {
                case RecoveryWheelDebrisKind.Dirt:color=new Color(.65f,.51f,.34f,.52f);break;
                case RecoveryWheelDebrisKind.Sand:color.a=.50f;break;
                case RecoveryWheelDebrisKind.Snow:color.a=.54f;break;
                case RecoveryWheelDebrisKind.Grass:color=new Color(.53f,.47f,.30f,.34f);break;
                default:color.a=.20f;break;
            }
            return color;
        }
        public static float PowderSize(RecoveryWheelDebrisKind kind,float speed,float slip=0,bool limited=false)
        {
            float size=Mathf.Lerp(.44f,limited?.62f:.68f,Mathf.Clamp01(speed/10))*(1+.18f*Mathf.Clamp01(slip));
            return kind==RecoveryWheelDebrisKind.Mud||kind==RecoveryWheelDebrisKind.Stone?size*.5f:kind==RecoveryWheelDebrisKind.Grass?size*.75f:size;
        }
        public static float PowderLifetime(RecoveryWheelDebrisKind kind,bool limited=false)
            =>kind==RecoveryWheelDebrisKind.Mud||kind==RecoveryWheelDebrisKind.Stone?.55f:kind==RecoveryWheelDebrisKind.Grass?1.1f:limited?1.25f:1.5f;

        public static float DistanceGain(float distance,bool limited)
            =>1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(limited?30:55,limited?50:80,distance));

        public static bool PowderWheel(int wheel,Vector4 slip,bool limited)
        {
            if(!limited)return true;
            // Rear trails cover the same tracks as the fronts. Under wheelspin,
            // instead keep the two tires actually doing the most spinning.
            if(Mathf.Max(Mathf.Max(slip.x,slip.y),Mathf.Max(slip.z,slip.w))<=.15f)return wheel>=2;
            int rank=0;
            for(int other=0;other<4;other++)
                if(slip[other]>slip[wheel]||slip[other]==slip[wheel]&&other>wheel)rank++;
            return rank<2;
        }

        // Shared by runtime initialization and the authoring menu, so existing
        // scenes receive the fix without resaving an unrelated dirty scene.
        public static void ConfigurePowder(ParticleSystem powder,bool limited=false)
        {
            powder.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=powder.main;main.playOnAwake=false;main.loop=true;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.scalingMode=ParticleSystemScalingMode.Shape;
            main.maxParticles=limited?16:24;main.startSpeed=0;main.startSize=.60f;main.startLifetime=limited?1.25f:1.5f;
            main.gravityModifier=0;
            var emission=powder.emission;emission.enabled=false;
            var shape=powder.shape;shape.enabled=false;
            var size=powder.sizeOverLifetime;size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.32f),new Keyframe(.18f,1),new Keyframe(.60f,2.2f),new Keyframe(1,2.8f)));
            var fade=new Gradient();fade.SetKeys(new[]{new GradientColorKey(UnityEngine.Color.white,0),new GradientColorKey(UnityEngine.Color.white,1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.16f),new GradientAlphaKey(.45f,.65f),new GradientAlphaKey(0,1)});
            var color=powder.colorOverLifetime;color.enabled=true;color.color=fade;
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
