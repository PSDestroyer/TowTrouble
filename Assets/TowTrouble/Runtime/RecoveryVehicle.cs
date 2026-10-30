using UnityEngine;

namespace TowTrouble
{
    [System.Serializable]
    public sealed class RecoveryVehicleModel
    {
        public GameObject Root;
        public Transform[] Wheels;
        public Vector3[] Anchors;
        public float Radius=.47f, Mass=1350f, Drive=3100f, Steering=30f;
    }

    [System.Serializable]
    public sealed class RecoveryPaintSlot { public Renderer Renderer; public int Slot; public Material[] Colors; }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class RecoveryVehicle : MonoBehaviour
    {
        [SerializeField] private WheelCollider[] wheels;
        [SerializeField] private Transform[] wheelVisuals;
        [SerializeField] private Transform frontHitch;
        [SerializeField] private Transform rearHitch;
        [SerializeField] private bool casualty;
        [SerializeField] private RecoveryVehicleModel[] models;
        [SerializeField] private int modelIndex;
        [SerializeField] private RecoveryPaintSlot[] paintSlots;
        private int paintIndex;
        public void ConfigurePaints(RecoveryPaintSlot[] value){paintSlots=value;}
        public void SetPaint(int index){paintIndex=Mathf.Clamp(index,0,3);if(paintSlots==null)return;foreach(var slot in paintSlots){var values=slot.Renderer.sharedMaterials;values[slot.Slot]=slot.Colors[paintIndex];slot.Renderer.sharedMaterials=values;}}
        private float mudDrag, mudLimit=27f;
        private Vector3 safePosition;
        private Quaternion safeRotation;
        private float radius=.47f, driveForce=3100f, steering=30f;
        private Rigidbody body;
        private bool simulate;
        private Vector2 input;
        private bool brake;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 remotePosition;
        private Quaternion remoteRotation;
        private float remoteSpeed;
        private bool hasRemote;
        private AudioSource engine;
        private ParticleSystem dust;
        private Vector3[] suspensionPositions;
        private readonly float[] wheelAngles=new float[4],wheelRates=new float[4],wheelSlips=new float[4],wheelLoads=new float[4];
        private bool resistanceNeedsMud;
        private float mudFraction,engineEffort,towLoad;
        private Vector4 remoteRates,remoteSlip;
        private Vector2 remoteControls;
        private float remoteMud,remoteEffort;
        private static Vector4 Pack(float[] values)=>new Vector4(values[0],values[1],values[2],values[3]);
        public Vector4 WheelAngularVelocity=>simulate?Pack(wheelRates):remoteRates;
        public Vector4 WheelSlip=>simulate?Pack(wheelSlips):remoteSlip;
        public Vector2 ControlInput=>simulate?input:remoteControls;
        public float MudFraction=>simulate?mudFraction:remoteMud;
        public float EngineEffort=>simulate?engineEffort:remoteEffort;
        public float SlipAtWheel(int index)=>WheelSlip[index];
        public void SetTowForce(float force){towLoad=Mathf.Clamp01(force/Mathf.Max(1,driveForce*4));}
        private void ClearWheelFeedback()
        {
            System.Array.Clear(wheelRates,0,4);System.Array.Clear(wheelSlips,0,4);System.Array.Clear(wheelAngles,0,4);
            for(int i=0;i<4;i++)wheelLoads[i]=(body!=null?body.mass:1350)*Physics.gravity.magnitude*.25f;
            mudFraction=engineEffort=towLoad=remoteMud=remoteEffort=0;remoteRates=remoteSlip=Vector4.zero;remoteControls=Vector2.zero;
        }

        public static float EffectsGain=.8f;
        public Rigidbody Body => body;
        public Vector3 WheelAnchor(int index)=>wheels[index].transform.position;
        public Transform WheelVisual(int index)=>wheelVisuals[index];
        public bool TryGetWheelContact(int index,out RaycastHit hit)
        {var w=wheels[index];var up=transform.up;return Physics.Raycast(w.transform.position+up*.3f,-up,out hit,w.radius+w.suspensionDistance+.3f,1<<8,QueryTriggerInteraction.Ignore);}
        public Vector3 FrontHitch => frontHitch.position;
        public Vector3 RearHitch => rearHitch.position;
        public float Speed => simulate ? body.linearVelocity.magnitude : remoteSpeed;
        public bool InMud => MudFraction>0;
        public int ModelIndex => modelIndex;
        public static readonly string[] ModelNames={"Scout","Hauler","Buddy","Ranger"};
        public static readonly float[] Masses={1350f,1750f,1150f,1550f};
        public static readonly float[] PullForces={3100f,3900f,2800f,3500f};
        public static readonly float[] SteeringAngles={30f,25f,35f,29f};
        public void ConfigureModels(RecoveryVehicleModel[] values){models=values;SetModel(0);}
        public void SetModel(int index)
        {
            if(models==null||models.Length==0)return;
            modelIndex=Mathf.Clamp(index,0,models.Length-1);var m=models[modelIndex];
            for(int i=0;i<models.Length;i++)models[i].Root.SetActive(i==modelIndex);
            wheelVisuals=m.Wheels;radius=m.Radius;driveForce=m.Drive;steering=m.Steering;
            for(int i=0;i<wheels.Length;i++){wheels[i].transform.localPosition=m.Anchors[i];wheels[i].radius=radius;}
            if(body==null)body=GetComponent<Rigidbody>();body.mass=casualty?950f:m.Mass;
            if(suspensionPositions!=null)for(int i=0;i<wheels.Length;i++)suspensionPositions[i]=wheelVisuals[i].position;
        }
        public void SetSpawn(Vector3 position,Quaternion rotation,float drag=0f,float mudEnd=27f,bool mudOnly=false)
        {startPosition=position;startRotation=rotation;safePosition=position;safeRotation=rotation;mudDrag=drag;mudLimit=mudEnd;resistanceNeedsMud=mudOnly;ResetVehicle(true);}

        public void Configure(WheelCollider[] colliders, Transform[] visuals, Transform front, Transform rear, bool isCasualty)
        {
            wheels = colliders; wheelVisuals = visuals; frontHitch = front; rearHitch = rear; casualty = isCasualty;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.centerOfMass = new Vector3(0f, .45f, 0f);
            body.sleepThreshold = 0f;
            startPosition = transform.position; startRotation = transform.rotation; safePosition=startPosition;safeRotation=startRotation;SetModel(modelIndex);
            remotePosition = startPosition; remoteRotation = startRotation;
            engine = GetComponent<AudioSource>();
            dust = GetComponentInChildren<ParticleSystem>();
            suspensionPositions = new Vector3[wheels.Length];
            for (int i = 0; i < wheels.Length; i++) suspensionPositions[i] = wheelVisuals[i].position;
            SetSimulation(false);
        }

        public void SetSimulation(bool value)
        {
            if (body == null) body = GetComponent<Rigidbody>();
            simulate = value;
            body.interpolation = value ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            body.isKinematic = !value;
            // WheelCollider assets supply the authored suspension geometry. Contact forces
            // are integrated here so towing, brakes and mud share one server simulation.
            foreach (var w in wheels) w.enabled = false;
            if (value) body.WakeUp();
            if (!value) input = Vector2.zero;
        }

        public void SetInput(Vector2 value, bool handbrake)
        {
            input = Vector2.ClampMagnitude(value, 1f); brake = handbrake;
            if (simulate && input.sqrMagnitude > .01f) body.WakeUp();
        }

        public void ResetVehicle(bool original)
        {
            var p = original ? startPosition : safePosition + Vector3.up * .35f;
            var r = original ? startRotation : safeRotation;
            body.position = p; body.rotation = r;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            transform.SetPositionAndRotation(p, r); input = Vector2.zero;brake=false; hasRemote = false;ClearWheelFeedback();
        }

        public void Receive(Vector3 position, Quaternion rotation, float speed)
        {
            remotePosition = position; remoteRotation = rotation; remoteSpeed = speed; hasRemote = true;
            if (Vector3.Distance(transform.position, position) > 12f) transform.SetPositionAndRotation(position, rotation);
        }

        public void Receive(VehicleFrame frame)
        {
            Receive(frame.Position,frame.Rotation,frame.Speed);
            remoteRates=frame.WheelRates;remoteSlip=frame.Slip;remoteControls=frame.Controls;remoteMud=frame.Mud;remoteEffort=frame.Effort;
        }

        private void FixedUpdate()
        {
            if (!simulate) return;
            float forwardSpeed=Vector3.Dot(body.linearVelocity,transform.forward);
            float steer=input.x*Mathf.Lerp(steering,15,Mathf.Abs(forwardSpeed)/14);
            float blend=1-Mathf.Exp(-10*Time.fixedDeltaTime);
            bool grounded=false;int mudWheels=0;
            for(int i=0;i<wheels.Length;i++)
            {
                var w=wheels[i];var up=transform.up;float reach=w.radius+w.suspensionDistance+.3f;
                if(!TryGetWheelContact(i,out var hit))
                {
                    suspensionPositions[i]=w.transform.position-up*w.suspensionDistance;
                    wheelSlips[i]=Mathf.Lerp(wheelSlips[i],0,blend);wheelLoads[i]=0;
                    wheelRates[i]=Mathf.Lerp(wheelRates[i],casualty||brake?0:input.y*18,blend);
                    continue;
                }
                grounded=true;
                var pointVelocity=body.GetPointVelocity(w.transform.position);
                float compression=Mathf.Clamp(reach-hit.distance,0,w.suspensionDistance);
                float support=Mathf.Clamp(compression*37000-Vector3.Dot(pointVelocity,up)*4500,0,18000);
                body.AddForceAtPosition(up*support,w.transform.position);
                float staticLoad=body.mass*Physics.gravity.magnitude/wheels.Length;
                wheelLoads[i]=Mathf.Lerp(wheelLoads[i],Mathf.Clamp(support,staticLoad*.3f,staticLoad*1.8f),blend);
                var forward=Vector3.ProjectOnPlane(Quaternion.AngleAxis(i<2?steer:0,up)*transform.forward,hit.normal).normalized;
                var right=Vector3.Cross(hit.normal,forward).normalized;
                float longitudinal=Vector3.Dot(pointVelocity,forward),lateral=Vector3.Dot(pointVelocity,right);
                bool muddy=hit.collider.TryGetComponent<RecoveryMudTerrain>(out var mud);
                if(muddy)mudWheels++;
                float drive=casualty||brake?0:input.y*driveForce*Mathf.Clamp01((13-Mathf.Abs(forwardSpeed))/3);
                float rolling=casualty?100:Mathf.Abs(input.y)<.05f?240:35;
                float braking=Mathf.Clamp(-longitudinal*(brake?1300:rolling),-5500,5500);
                float grip=RecoveryTraction.Grip(muddy?mud.LongitudinalGrip:1.25f,brake?0:input.y,muddy?mud.AggressiveThrottleLoss:0);
                float forwardLimit=wheelLoads[i]*grip,sideLimit=wheelLoads[i]*(muddy?mud.LateralGrip:1.3f);
                var tire=RecoveryTraction.LimitForces(new Vector2(drive+braking,-lateral*1400),forwardLimit,sideLimit);
                body.AddForceAtPosition(forward*tire.x+right*tire.y,hit.point);
                if(muddy)
                {
                    float resistance=mud.ResistancePerWheel;
                    // Contract resistance is divided over the actual mud contacts.
                    // It never continues merely because the car has a small world Z.
                    if(casualty&&resistanceNeedsMud)resistance=Mathf.Max(resistance,mudDrag/wheels.Length/(body.mass/1350f));
                    body.AddForceAtPosition(RecoveryTraction.MudResistance(pointVelocity,hit.normal,resistance,body.mass),hit.point);
                }
                float slip=casualty||brake?0:RecoveryTraction.Slip(drive,forwardLimit);
                wheelSlips[i]=Mathf.Lerp(wheelSlips[i],slip,blend);
                wheelRates[i]=longitudinal/radius+(casualty||brake?0:Mathf.Sign(input.y)*wheelSlips[i]*18);
                suspensionPositions[i]=hit.point+hit.normal*w.radius;
            }
            mudFraction=mudWheels/(float)wheels.Length;
            if(grounded)body.AddForce(-transform.up*body.linearVelocity.magnitude*70);
            // Preserve contract 02's ditch resistance. Contract 03 uses wheel contacts.
            if(casualty&&!resistanceNeedsMud&&mudDrag>0&&transform.position.z<mudLimit)body.AddForce(-body.linearVelocity*mudDrag);
            float averageSlip=(wheelSlips[0]+wheelSlips[1]+wheelSlips[2]+wheelSlips[3])*.25f;
            float effort=Mathf.Clamp01(Mathf.Abs(input.y)*(.18f+towLoad*.45f+mudFraction*.37f)+averageSlip*.55f);
            engineEffort=Mathf.Lerp(engineEffort,effort,blend);
            if(grounded&&mudWheels==0&&Mathf.Abs(transform.position.x)<8&&transform.position.y>-.2f&&Vector3.Dot(transform.up,Vector3.up)>.94f&&Speed<4)
            {safePosition=transform.position;safeRotation=Quaternion.Euler(0,transform.eulerAngles.y,0);}
            if(transform.position.y<-10)ResetVehicle(false);
        }

        private void LateUpdate()
        {
            if(!simulate&&hasRemote)
            {
                float blend=1-Mathf.Exp(-14*Time.deltaTime);
                transform.SetPositionAndRotation(Vector3.Lerp(transform.position,remotePosition,blend),Quaternion.Slerp(transform.rotation,remoteRotation,blend));
            }
            var controls=ControlInput;var rates=WheelAngularVelocity;
            float steer=controls.x*Mathf.Lerp(steering,15,Speed/14);
            for(int i=0;i<wheels.Length;i++)
            {
                wheelAngles[i]=Mathf.Repeat(wheelAngles[i]+rates[i]*Time.deltaTime*Mathf.Rad2Deg,360);
                if(simulate||hasRemote)
                {
                    var p=suspensionPositions[i];
                    if(!simulate)p=TryGetWheelContact(i,out var hit)?hit.point+hit.normal*radius:wheels[i].transform.position-transform.up*wheels[i].suspensionDistance;
                    wheelVisuals[i].SetPositionAndRotation(p,transform.rotation*Quaternion.Euler(wheelAngles[i],i<2?steer:0,0));
                }
            }
            if(engine!=null)
            {
                var slip=WheelSlip;float average=(slip.x+slip.y+slip.z+slip.w)*.25f;
                float pitch=.6f+Speed*.06f+Mathf.Abs(controls.y)*.32f+average*.65f+EngineEffort*.1f;
                engine.pitch=Mathf.Lerp(engine.pitch,pitch,1-Mathf.Exp(-6*Time.unscaledDeltaTime));
                engine.volume=casualty||Time.timeScale<.01f?0:(.09f+Mathf.Abs(controls.y)*.08f+EngineEffort*.10f)*EffectsGain;
            }
            if(dust!=null)
            {
                var emission=dust.emission;
                emission.rateOverTime=Speed>1?Mathf.Min(Speed*2,22)*(1-MudFraction):0;
            }
        }
    }
}
