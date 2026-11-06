using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class DemoSession
    {
        private RecoveryProfile careerProfile;
        private bool trainingRequested, trainingAttached, trainingWinched;
        private Vector3 trainingStart;
        private int trainingStep;
        private float trainingMoved, trainingCamera, trainingWinchHeld;
        private GameObject trainingDressing;
        private Mesh trainingConeMesh;
        private Material trainingOrange,trainingDark;
        public bool Training => careerProfile != null;
        public int TrainingStep => trainingStep;
        public bool TrainingAttached => trainingAttached;
        public bool TrainingWinched => trainingWinched;
        public void RequestTraining()
        {
            if (Connected || Connecting) return;
            careerProfile = Profile;
            Profile = JsonUtility.FromJson<RecoveryProfile>(JsonUtility.ToJson(careerProfile));
            Profile.SuppressSaving = true;
            Profile.builds = null; Profile.EnsureGarage();
            Profile.ownedParts.Clear(); Profile.presets.Clear();
            Profile.credits = 5000; // Practice-only budget, including a fresh career with zero credits.
            trainingRequested = true; trainingStep = 0;
            trainingMoved = trainingCamera = trainingWinchHeld = 0; trainingAttached = trainingWinched = false;
            Host(true, true);
        }
        private void TickTraining()
        {
            if (!Training) return;
            if (!Connected) { if (!Connecting && !trainingRequested) EndTraining(false); return; }
            if (world.Phase == RecoveryPhase.Lobby) return;
            if (trainingRequested)
            {
                trainingRequested = false;
                trainingStart = world.Drivers[LocalSlot].transform.position;
            }
            if (trainingStep == 6 && GarageOpen) trainingStep = 7;
            if (View != "Gameplay" || menuOpen) return;
            var car = world.Drivers[LocalSlot];
            if (trainingStep == 0)
            {
                if (Vector3.Distance(car.transform.position, trainingStart) > 5 && car.Speed > .5f) trainingMoved += Time.deltaTime;
                if (trainingMoved > .6f) trainingStep = 1;
            }
            else if (trainingStep == 1)
            {
                bool camera = Keyboard.current?.cKey.wasPressedThisFrame == true ||
                    Mouse.current != null && Mouse.current.rightButton.isPressed && Mouse.current.delta.ReadValue().sqrMagnitude > 1 ||
                    RecoveryInputDevices.Pad != null && (RecoveryInputDevices.Pad.rightStick.ReadValue().sqrMagnitude > .2f || RecoveryInputDevices.Pad.buttonNorth.wasPressedThisFrame);
                if (camera) trainingCamera += Time.unscaledDeltaTime + .15f;
                if (trainingCamera > .3f) { BeginPracticeRecovery(); trainingStep = 2; }
            }
            else if (trainingStep == 2 && world.Attached(LocalSlot)) { trainingAttached = true; trainingStep = 3; }
            else if (trainingStep == 3)
            {
                if (world.Attached(LocalSlot) && (Keyboard.current != null && Keyboard.current[Profile.ReadKey(Profile.reelKey, Key.Q)].isPressed || RecoveryInputDevices.Pad?.leftShoulder.isPressed == true)) {trainingWinchHeld+=Time.deltaTime;trainingWinched=trainingWinchHeld>.4f;world.TrainingWinchUsed=trainingWinched;}
                if (world.Phase == RecoveryPhase.Deliver) trainingStep = 4;
            }
            else if (trainingStep == 4 && world.Phase == RecoveryPhase.Complete) trainingStep = 5;
            else if (trainingStep == 6 && GarageOpen) trainingStep = 7;
        }
        private void BeginPracticeRecovery()
        {
            Vector3 target = TrainingGround(world.GaragePosition + new Vector3(-2.5f, 0, -26));
            Vector3 delivery = TrainingGround(world.GaragePosition + new Vector3(-2.5f, 0, -5));
            var contract = new RecoveryContract(1, 0, "Practice recovery", "Pull the practice car free and deliver it to the workshop.", "Workshop yard", "Pull steadily", target, delivery, 2.2f, 0) {
                IsOpenWorld = true, EnglishTitle = "Practice recovery", EnglishDescription = "Pull the practice car free and deliver it to the workshop.",
                DeliveryEnglish = "the practice bay", DeliveryName = "Practice bay", TargetModel = 2,
                ExitDirection = Vector3.forward, Load = new RecoveryLoadSettings { Mass = 850, Material = RecoveryLoadMaterial.Firm },
                ExtractionProfile = new RecoveryExtractionSettings { Enabled = true, Travel = 2.2f, BreakawayForce = 1900, MovingForce = 1000, MovingDrag = 300, ReleaseDuration = .45f }
            };
            world.BeginTrainingRecovery(contract, TrainingGround(target + Vector3.forward * 7));
            BuildTrainingDressing(target,delivery);
        }
        private static Vector3 TrainingGround(Vector3 point)
        {
            float height = point.y;
            foreach (var terrain in Terrain.activeTerrains)
            { var local = point - terrain.transform.position; var size = terrain.terrainData.size; if (local.x >= 0 && local.z >= 0 && local.x <= size.x && local.z <= size.z) { height = terrain.SampleHeight(point) + terrain.transform.position.y; break; } }
            return new Vector3(point.x, height + .15f, point.z);
        }
        private void BuildTrainingDressing(Vector3 target,Vector3 delivery)
        {
            ClearTrainingDressing();trainingDressing=new GameObject("Temporary training markers");
            trainingOrange=new Material(Shader.Find("Universal Render Pipeline/Lit"));trainingOrange.color=new Color(1,.48f,.1f);
            trainingDark=new Material(trainingOrange);trainingDark.color=new Color(.13f,.15f,.16f);
            trainingConeMesh=new Mesh{name="Training cone"};var vertices=new Vector3[24];var triangles=new int[72];
            for(int i=0;i<12;i++){float angle=i*Mathf.PI/6;vertices[i]=new Vector3(Mathf.Cos(angle)*.22f,0,Mathf.Sin(angle)*.22f);vertices[i+12]=new Vector3(Mathf.Cos(angle)*.035f,.65f,Mathf.Sin(angle)*.035f);int next=(i+1)%12;int j=i*6;triangles[j]=i;triangles[j+1]=i+12;triangles[j+2]=next;triangles[j+3]=next;triangles[j+4]=i+12;triangles[j+5]=next+12;}
            trainingConeMesh.vertices=vertices;trainingConeMesh.triangles=triangles;trainingConeMesh.RecalculateNormals();
            foreach(var p in new[]{target+Vector3.left*3,target+Vector3.right*3,delivery+new Vector3(-4,0,-4),delivery+new Vector3(4,0,-4),delivery+new Vector3(-4,0,4),delivery+new Vector3(4,0,4)})
            {var cone=new GameObject("Practice cone",typeof(MeshFilter),typeof(MeshRenderer));cone.transform.SetParent(trainingDressing.transform,false);cone.transform.position=TrainingGround(p);cone.GetComponent<MeshFilter>().sharedMesh=trainingConeMesh;cone.GetComponent<MeshRenderer>().sharedMaterial=trainingOrange;
             var foot=GameObject.CreatePrimitive(PrimitiveType.Cube);foot.name="Cone base";foot.transform.SetParent(cone.transform,false);foot.transform.localScale=new Vector3(.58f,.06f,.58f);foot.GetComponent<Renderer>().sharedMaterial=trainingDark;Destroy(foot.GetComponent<Collider>());}
        }
        private void ClearTrainingDressing()
        {
            if(trainingDressing!=null){trainingDressing.SetActive(false);Destroy(trainingDressing);trainingDressing=null;}
            if(trainingConeMesh!=null)Destroy(trainingConeMesh);if(trainingOrange!=null)Destroy(trainingOrange);if(trainingDark!=null)Destroy(trainingDark);
        }
        public void ContinueTrainingAfterDelivery()
        {
            if (!Training || world.Phase != RecoveryPhase.Complete) return;
            world.EndTrainingRecovery(); CloseOverlay(); trainingStep = 6;
        }
        public void EndTraining(bool completed)
        {
            if (!Training) return;
            ClearTrainingDressing();
            if (Connected) { if (GarageOpen) CloseOverlay(); world.EndTrainingRecovery(); }
            RecoveryMenuSettings.CopyPreferences(Profile, careerProfile);
            Profile = careerProfile; careerProfile = null; trainingRequested = false;
            Profile.tutorialOffered = true; Profile.tutorialCompleted |= completed; Profile.Save();
            if (Connected) { SendGarageBuild(); CloseOverlay(); }
        }
    }
}
