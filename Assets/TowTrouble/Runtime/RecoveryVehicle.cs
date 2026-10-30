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
        private float wheelSpin;

        public static float EffectsGain=.8f;
        public Rigidbody Body => body;
        public Vector3 WheelAnchor(int index)=>wheels[index].transform.position;
        public Transform WheelVisual(int index)=>wheelVisuals[index];
        public bool TryGetWheelContact(int index,out RaycastHit hit)
        {var w=wheels[index];var up=transform.up;return Physics.Raycast(w.transform.position+up*.3f,-up,out hit,w.radius+w.suspensionDistance+.3f,1<<8,QueryTriggerInteraction.Ignore);}
        public Vector3 FrontHitch => frontHitch.position;
        public Vector3 RearHitch => rearHitch.position;
        public float Speed => simulate ? body.linearVelocity.magnitude : remoteSpeed;
        public bool InMud => casualty && mudDrag>0 && transform.position.z < mudLimit;
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
        public void SetSpawn(Vector3 position,Quaternion rotation,float drag=0f,float mudEnd=27f)
        {startPosition=position;startRotation=rotation;safePosition=position;safeRotation=rotation;mudDrag=drag;mudLimit=mudEnd;ResetVehicle(true);}

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
            transform.SetPositionAndRotation(p, r); input = Vector2.zero;brake=false; hasRemote = false;
        }

        public void Receive(Vector3 position, Quaternion rotation, float speed)
        {
            remotePosition = position; remoteRotation = rotation; remoteSpeed = speed; hasRemote = true;
            if (Vector3.Distance(transform.position, position) > 12f) transform.SetPositionAndRotation(position, rotation);
        }

        private void FixedUpdate()
        {
            if (!simulate) return;
            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            float steer = input.x * Mathf.Lerp(steering, 15f, Mathf.Abs(forwardSpeed) / 14f);
            bool grounded = false;
            for (int i = 0; i < wheels.Length; i++)
            {
                var w = wheels[i];
                var up = transform.up;
                var origin = w.transform.position + up * .3f;
                float reach = w.radius + w.suspensionDistance + .3f;
                if (!Physics.Raycast(origin, -up, out var hit, reach, 1 << 8, QueryTriggerInteraction.Ignore))
                { suspensionPositions[i] = w.transform.position - up * w.suspensionDistance; continue; }
                grounded = true;
                var pointVelocity = body.GetPointVelocity(w.transform.position);
                float compression = Mathf.Clamp(reach - hit.distance, 0f, w.suspensionDistance);
                float support = Mathf.Clamp(compression * 37000f - Vector3.Dot(pointVelocity, up) * 4500f, 0f, 18000f);
                body.AddForceAtPosition(up * support, w.transform.position);
                var forward = Vector3.ProjectOnPlane(Quaternion.AngleAxis(i < 2 ? steer : 0f, up) * transform.forward, hit.normal).normalized;
                var right = Vector3.Cross(hit.normal, forward).normalized;
                float longitudinal = Vector3.Dot(pointVelocity, forward);
                float lateral = Vector3.Dot(pointVelocity, right);
                float drive = casualty ? 0f : input.y * driveForce * Mathf.Clamp01((13f - Mathf.Abs(forwardSpeed)) / 3f);
                float rolling = casualty ? 100f : Mathf.Abs(input.y) < .05f ? 240f : 35f;
                float braking = Mathf.Clamp(-longitudinal * (brake ? 1300f : rolling), -5500f, 5500f);
                var tireForce = forward * (drive + braking) + right * Mathf.Clamp(-lateral * 1400f, -5000f, 5000f);
                body.AddForceAtPosition(tireForce, hit.point);
                suspensionPositions[i] = hit.point + hit.normal * w.radius;
            }
            if (grounded)
            {
                body.AddForce(-transform.up * body.linearVelocity.magnitude * 70f);
            }
            if (InMud) body.AddForce(-body.linearVelocity * mudDrag);
            if(grounded && Mathf.Abs(transform.position.x)<8f && transform.position.y>-.2f && Vector3.Dot(transform.up,Vector3.up)>.94f && Speed<4f)
            {safePosition=transform.position;safeRotation=Quaternion.Euler(0f,transform.eulerAngles.y,0f);}
            if (transform.position.y < -10f) ResetVehicle(false);
        }

        private void LateUpdate()
        {
            if (!simulate && hasRemote)
            {
                float blend = 1f - Mathf.Exp(-14f * Time.deltaTime);
                transform.SetPositionAndRotation(Vector3.Lerp(transform.position, remotePosition, blend), Quaternion.Slerp(transform.rotation, remoteRotation, blend));
            }
            wheelSpin += (simulate ? Vector3.Dot(body.linearVelocity, transform.forward) : remoteSpeed) * Time.deltaTime * Mathf.Rad2Deg / radius;
            for (int i = 0; i < wheels.Length; i++)
            {
                if (simulate)
                {
                    var q = transform.rotation * Quaternion.Euler(wheelSpin, i < 2 ? input.x * steering : 0f, 0f);
                    wheelVisuals[i].SetPositionAndRotation(suspensionPositions[i], q);
                }
                else wheelVisuals[i].localRotation = Quaternion.Euler(wheelSpin, 0f, 0f);
            }
            if (engine != null)
            {
                engine.pitch = .6f + Speed * .09f;
                engine.volume = casualty ? 0f : (.09f + Mathf.Abs(input.y) * .08f)*EffectsGain;
            }
            if (dust != null)
            {
                var emission = dust.emission;
                emission.rateOverTime = Speed > 1f ? Mathf.Min(Speed * 2f, 22f) : 0f;
            }
        }
    }
}
