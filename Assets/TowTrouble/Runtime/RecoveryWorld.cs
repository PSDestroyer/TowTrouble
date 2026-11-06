using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

namespace TowTrouble
{
    public struct VehicleFrame
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Speed;
        public Vector4 WheelRates,Slip;
        public Vector2 Controls;
        public float Mud,Effort;
        public bool BrakeLights,EngineRunning;
        public VehicleFrame(RecoveryVehicle vehicle)
        {Position=vehicle.transform.position;Rotation=vehicle.transform.rotation;Speed=vehicle.Speed;WheelRates=vehicle.WheelAngularVelocity;Slip=vehicle.WheelSlip;Controls=vehicle.ControlInput;Mud=vehicle.MudFraction;Effort=vehicle.EngineEffort;BrakeLights=vehicle.BrakeLights;EngineRunning=vehicle.EngineRunning;}
    }

    public struct RecoveryFrame
    {
        public VehicleFrame Driver0, Driver1, Casualty;
        public bool Cable0, Cable1;
        public float Tension0, Tension1, Elapsed;
        public int Snaps, Resets0, Resets1;
        public float Pull0, Pull1;
        public WinchFrame Winch0, Winch1;
        public byte ExtractionStage;
        public float ExtractionProgress, ExtractionAlignment, ExtractionStalled, ExtractionReleasedSeconds;
        public Vector3 ObstaclePosition;public Quaternion ObstacleRotation;
    }

    public sealed partial class RecoveryWorld : NetworkBehaviour
    {
        [SerializeField] private RecoveryVehicle[] drivers;
        [SerializeField] private RecoveryVehicle casualty;
        [SerializeField] private LineRenderer[] cables;
        [SerializeField] private GameObject environment;
        [SerializeField] private RecoveryMap map;
        public RecoveryMap Map=>map;
        public void ConfigureMap(RecoveryMap value){map=value;}
        public RecoveryContract[] AvailableContracts=>map!=null?map.Contracts:RecoveryContract.All;
        [SerializeField] private Vector3 garagePosition=new Vector3(16,0,92);
        private readonly SyncVar<int> garageMask=new SyncVar<int>();
        public bool CrewInGarage=>garageMask.Value!=0;
        private readonly SyncVar<bool> freeRoam=new SyncVar<bool>();
        private readonly SyncVar<string> runId=new SyncVar<string>("");
        private readonly SyncVar<string> build0=new SyncVar<string>(""),build1=new SyncVar<string>("");
        private readonly string[] appliedBuild={null,null};
        private readonly int[] appliedBuildPaint={-1,-1},appliedBuildModel={-1,-1};
        public bool FreeRoam=>freeRoam.Value;
        public string RunId=>runId.Value;
        public Vector3 GaragePosition=>garagePosition;
        public bool HasContract=>Phase==RecoveryPhase.HookUp||Phase==RecoveryPhase.Recover||Phase==RecoveryPhase.Deliver;
        public void ConfigureGarage(Vector3 position){garagePosition=position;}
        public bool InGarageArea(int slot)=>slot>=0&&slot<2&&Vector3.Distance(new Vector3(drivers[slot].transform.position.x,0,drivers[slot].transform.position.z),new Vector3(garagePosition.x,0,garagePosition.z))<8&&Mathf.Abs(drivers[slot].transform.position.y-garagePosition.y)<3;
        public bool AtGarage(int slot)=>InGarageArea(slot)&&drivers[slot].Speed<1.2f&&!Attached(slot)&&Phase==RecoveryPhase.Explore;
        private readonly SyncVar<int> contractId=new SyncVar<int>();
        private readonly SyncVar<int> paint0=new SyncVar<int>();
        private readonly SyncVar<int> paint1=new SyncVar<int>();
        private int appliedPaint0=-1,appliedPaint1=-1;
        public void PreviewPaint(int index)
        {
            if(NetworkObject!=null&&IsClientInitialized)return;
            var custom=drivers[0].GetComponent<RecoveryCustomization>();
            if(custom==null||custom.Catalog==null){drivers[0].SetPaint(index);return;}
            var build=new RecoveryBuild{Model=drivers[0].ModelIndex};
            build.Set(custom.Catalog.Find(index<=0?"stock_Paint":"paint_"+Mathf.Clamp(index,1,3)),custom.Catalog);
            custom.Apply(build,0,true);
        }
        private readonly SyncVar<int> vehicle0=new SyncVar<int>();
        private readonly SyncVar<int> vehicle1=new SyncVar<int>(1);
        private readonly SyncVar<string> name0=new SyncVar<string>("Driver 1");
        private readonly SyncVar<string> name1=new SyncVar<string>("Driver 2");
        private readonly SyncVar<bool> cooperative=new SyncVar<bool>();
        private readonly SyncVar<int> signal0=new SyncVar<int>();
        private readonly SyncVar<int> signal1=new SyncVar<int>();
        private readonly bool[] extending=new bool[2];
        private readonly int[] resets=new int[2];
        private readonly float[] pullSeconds=new float[2];
        private int appliedContract=-1;
        private bool singlePreview;
        private int previewSlot;
        public void ShowSinglePreview(bool value,int slot){singlePreview=value;previewSlot=slot;for(int i=0;i<2;i++)if(Phase==RecoveryPhase.Lobby)drivers[i].gameObject.SetActive(!value||i==slot);if(Phase==RecoveryPhase.Lobby)casualty.gameObject.SetActive(false);}
        private readonly SyncVar<int> phase = new SyncVar<int>();
        private readonly SyncVar<int> driver0Id = new SyncVar<int>(-1);
        private readonly SyncVar<int> driver1Id = new SyncVar<int>(-1);
        private readonly SyncVar<bool> ready0 = new SyncVar<bool>();
        private readonly SyncVar<bool> ready1 = new SyncVar<bool>();
        private readonly bool[] connected = new bool[2];
        private readonly bool[] winching = new bool[2];
        private readonly float[] lengths = new float[2];
        private readonly float[] tensions = new float[2];
        private readonly float[] cableStress = new float[2];
        private readonly float[] lastInput = new float[2];
        private readonly float[] lastAction = new float[2];
        private float nextSnapshot;
        private float elapsed;
        private int snaps;
        private float deliveryHold;

        public RecoveryPhase Phase => (RecoveryPhase)phase.Value;
        public RecoveryVehicle[] Drivers => drivers;
        public RecoveryVehicle Casualty => casualty;
        public float Elapsed => elapsed;
        public int Snaps => snaps;
        public int Occupied => (driver0Id.Value >= 0 ? 1 : 0) + (driver1Id.Value >= 0 ? 1 : 0);
        public bool CanStart => Occupied > 0 && ready0.Value && (!cooperative.Value || Occupied==2) && (driver1Id.Value < 0 || ready1.Value);
        public RecoveryContract Contract=>trainingContract??(map!=null&&FreeRoam?map.Contracts[Mathf.Clamp(contractId.Value,0,map.Contracts.Length-1)]:RecoveryContract.Get(contractId.Value));
        public int ContractIndex=>contractId.Value;
        public bool Cooperative=>cooperative.Value;
        public string DriverName(int slot)=>slot==0?name0.Value:name1.Value;
        public int VehicleIndex(int slot)=>slot==0?vehicle0.Value:vehicle1.Value;
        public int Resets(int slot)=>resets[Mathf.Clamp(slot,0,1)];
        public float PullSeconds(int slot)=>pullSeconds[Mathf.Clamp(slot,0,1)];
        public int Signal(int slot)=>slot==0?signal0.Value:signal1.Value;
        public float CableLength(int slot)=>slot>=0?lengths[slot]:0;
        public void ConfigureEnvironment(GameObject value){environment=value;}
        public void PreviewVehicle(int index){if(NetworkObject==null||!IsClientInitialized){drivers[0].GetComponent<RecoveryCustomization>()?.ClearPreview();drivers[0].SetModel(index);}}
        public int Slot(int id) => id == driver0Id.Value && id >= 0 ? 0 : (id == driver1Id.Value && id >= 0 ? 1 : -1);
        public bool Ready(int slot) => slot == 0 ? ready0.Value : ready1.Value;
        public bool OccupiedSlot(int slot) => (slot == 0 ? driver0Id.Value : driver1Id.Value) >= 0;
        public bool Attached(int slot) => slot >= 0 && connected[slot];
        public float Tension(int slot) => slot < 0 ? 0f : tensions[slot];

        public void Configure(RecoveryVehicle[] vehicles, RecoveryVehicle target, LineRenderer[] lines)
        { drivers = vehicles; casualty = target; cables = lines; }

        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkManager.SceneManager.OnClientLoadedStartScenes += ClientLoaded;
            NetworkManager.ServerManager.OnRemoteConnectionState += RemoteChanged;
            phase.Value = 0; driver0Id.Value = -1; driver1Id.Value = -1;garageMask.Value=0;runId.Value="";
            ResetRecovery();
        }

        public override void OnStopServer()
        {
            NetworkManager.SceneManager.OnClientLoadedStartScenes -= ClientLoaded;
            NetworkManager.ServerManager.OnRemoteConnectionState -= RemoteChanged;
            base.OnStopServer();
        }

        private void ClientLoaded(NetworkConnection connection, bool asServer)
        {
            if (!asServer || Slot(connection.ClientId) >= 0) return;
            // Reserve driver 01 for the host even when a remote peer finishes loading first.
            if (connection.IsLocalClient)
            {
                if (driver0Id.Value >= 0) { connection.Disconnect(true); return; }
                driver0Id.Value = connection.ClientId;
            }
            else
            {
                if (driver1Id.Value >= 0) { connection.Disconnect(true); return; }
                driver1Id.Value = connection.ClientId;
            }
            NetworkManager.SceneManager.AddConnectionToScene(connection, gameObject.scene);
        }

        private void RemoteChanged(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != RemoteConnectionState.Stopped) return;
            int slot = Slot(connection.ClientId);
            if (slot < 0) return;
            garageMask.Value&=~(1<<slot);drivers[slot].SetInput(Vector2.zero, true); connected[slot] = false; winching[slot] = false;extending[slot]=false;
            if (slot == 0) { driver0Id.Value = -1; ready0.Value = false; }
            else { driver1Id.Value = -1; ready1.Value = false; }
        }

        [ServerRpc(RequireOwnership=false)]
        public void ConfigureSession(int contract,bool coop,bool roam=false,NetworkConnection sender=null)
        {
            if(sender==null||Slot(sender.ClientId)!=0||Phase!=RecoveryPhase.Lobby)return;
            contractId.Value=Mathf.Clamp(contract,0,(roam?AvailableContracts:RecoveryContract.All).Length-1);cooperative.Value=coop;freeRoam.Value=roam;ResetRecovery();
        }

        [ServerRpc(RequireOwnership=false)]
        public void SetGarageState(bool open,NetworkConnection sender=null)
        {
            int slot=sender==null?-1:Slot(sender.ClientId);if(slot<0||(open&&!AtGarage(slot)))return;
            if(open)garageMask.Value|=1<<slot;else garageMask.Value&=~(1<<slot);
        }
        [ServerRpc(RequireOwnership=false)]
        public void SetGarageBuild(string json,NetworkConnection sender=null)
        {
            int slot=sender==null?-1:Slot(sender.ClientId);if(slot<0||json==null||json.Length>4096||(Phase!=RecoveryPhase.Lobby&&!AtGarage(slot)))return;
            var custom=drivers[slot].GetComponent<RecoveryCustomization>();if(custom==null||custom.Catalog==null)return;
            RecoveryBuild build;try{build=JsonUtility.FromJson<RecoveryBuild>(json);}catch(System.ArgumentException){return;}
            if(build==null||build.Items==null||build.Items.Length>16||build.Model!=VehicleIndex(slot))return;
            build.Normalize(custom.Catalog);json=JsonUtility.ToJson(build);
            if(slot==0)build0.Value=json;else build1.Value=json;appliedBuild[slot]=null;ApplyBuild(slot);
        }
        private void ApplyBuild(int slot)
        {
            if(!FreeRoam)return;var json=slot==0?build0.Value:build1.Value;var custom=drivers[slot].GetComponent<RecoveryCustomization>();if(custom==null)return;
            int color=slot==0?paint0.Value:paint1.Value,model=drivers[slot].ModelIndex;
            if(appliedBuild[slot]!=json||appliedBuildPaint[slot]!=color||appliedBuildModel[slot]!=model){appliedBuild[slot]=json;appliedBuildPaint[slot]=color;appliedBuildModel[slot]=model;var build=string.IsNullOrEmpty(json)?new RecoveryBuild{Model=VehicleIndex(slot)}:JsonUtility.FromJson<RecoveryBuild>(json);custom.Apply(build,color,true);}
        }
        [ServerRpc(RequireOwnership=false)]
        public void AcceptContract(int contract,NetworkConnection sender=null)
        {
            if(sender==null||Slot(sender.ClientId)!=0||!FreeRoam||CrewInGarage||Phase!=RecoveryPhase.Explore||contract<0||contract>=AvailableContracts.Length)return;
            contractId.Value=contract;ApplyContract();ClearContract();
            casualty.gameObject.SetActive(true);casualty.SetSimulation(true);casualty.SetSpawn(Contract.Target,Quaternion.Euler(Contract.Pitch,Contract.Heading,Contract.Roll),Contract.MudDrag,Contract.Extraction-2,!Contract.IsOpenWorld&&contract==2);
            runId.Value=System.Guid.NewGuid().ToString("N");phase.Value=(int)RecoveryPhase.HookUp;BeginScenario();
        }
        [ServerRpc(RequireOwnership=false)]
        public void ReturnToExplore(NetworkConnection sender=null)
        {
            if(sender==null||Slot(sender.ClientId)!=0||!FreeRoam||Phase==RecoveryPhase.Lobby)return;
            StopScenario();ClearContract();phase.Value=(int)RecoveryPhase.Explore;casualty.SetSimulation(false);casualty.gameObject.SetActive(false);
            foreach(var car in drivers)car.SetInput(Vector2.zero,true);
        }
        private void ClearContract()
        {
            elapsed=0;snaps=0;deliveryHold=0;
            for(int i=0;i<2;i++){connected[i]=false;tensions[i]=0;winching[i]=false;extending[i]=false;resets[i]=0;pullSeconds[i]=0;cableStress[i]=0;}
            ResetWinches();
        }

        [ServerRpc(RequireOwnership=false)]
        public void SetDriver(string displayName,int vehicle,int paint=0,NetworkConnection sender=null)
        {
            int slot=sender==null?-1:Slot(sender.ClientId);if(slot<0||(Phase!=RecoveryPhase.Lobby&&!AtGarage(slot)))return;
            displayName=(displayName??"Driver").Replace("<","").Replace(">","").Trim();if(displayName.Length==0)displayName="Driver";if(displayName.Length>20)displayName=displayName.Substring(0,20);
            vehicle=Mathf.Clamp(vehicle,0,3);appliedBuild[slot]=null;
            if(slot==0){name0.Value=displayName;vehicle0.Value=vehicle;paint0.Value=Mathf.Clamp(paint,0,3);ready0.Value=false;}else{name1.Value=displayName;vehicle1.Value=vehicle;paint1.Value=Mathf.Clamp(paint,0,3);ready1.Value=false;}
        }
        [ServerRpc(RequireOwnership=false)]
        public void SendSignal(int value,NetworkConnection sender=null)
        {
            int slot=sender==null?-1:Slot(sender.ClientId);if(slot<0)return;value=Mathf.Clamp(value,1,3);if(slot==0)signal0.Value=value;else signal1.Value=value;
        }
        private void ApplyContract()
        {
            var c=Contract;
            // Contract identity already replicates to every peer, including late joiners.
            // Resolve the target before activation/spawn so wheel geometry agrees with its pose.
            casualty.SetModel(c.TargetModel);casualty.SetPaint(c.TargetPaint);
            casualty.SetRecoveryMass(c.IsOpenWorld&&c.Load!=null?c.Load.Mass+c.Load.RetainedWater:0,c.Load!=null?c.Load.LockedWheels:0);
            if(map!=null){appliedContract=contractId.Value;return;}
            float offset=c.Delivery.z-(appliedContract<0?108f:RecoveryContract.Get(appliedContract).Delivery.z);
            if(environment!=null)foreach(var t in environment.GetComponentsInChildren<Transform>(true))
            {
                if(t.name.StartsWith("Contract tutorial grass"))t.gameObject.SetActive(contractId.Value==0);
                if(t.name.StartsWith("Contract mud"))t.gameObject.SetActive(contractId.Value==2);
                if(t.name=="Delivery boundary"||t.name=="RECOVERY BAY")t.position+=Vector3.forward*offset;
            }
            appliedContract=contractId.Value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetReady(bool value, NetworkConnection sender = null)
        {
            int slot = sender == null ? -1 : Slot(sender.ClientId);
            if (slot == 0) ready0.Value = value;
            else if (slot == 1) ready1.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartRecovery(NetworkConnection sender = null)
        {
            if (sender == null || Slot(sender.ClientId) != 0 || !CanStart || Phase != RecoveryPhase.Lobby) return;
            ResetRecovery(); phase.Value = (int)(FreeRoam?RecoveryPhase.Explore:RecoveryPhase.HookUp);runId.Value=System.Guid.NewGuid().ToString("N");
            foreach (var car in drivers) car.SetSimulation(true);
            casualty.SetSimulation(!FreeRoam);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReturnToLobby(NetworkConnection sender = null)
        {
            if (sender == null || Slot(sender.ClientId) != 0) return;
            phase.Value = 0; ready0.Value = false; ready1.Value = false; ResetRecovery();
        }

        [ServerRpc(RequireOwnership = false)]
        public void Drive(Vector2 input, bool brake, bool winch, bool extend=false, NetworkConnection sender = null)
        {
            int slot = sender == null ? -1 : Slot(sender.ClientId);
            if (slot < 0 || Phase == RecoveryPhase.Lobby || Phase == RecoveryPhase.Complete) return;
            if (!float.IsFinite(input.x) || !float.IsFinite(input.y)) return;
            drivers[slot].SetInput(Vector2.ClampMagnitude(input, 1f), brake);
            winching[slot] = winch; extending[slot]=extend; lastInput[slot] = Time.time;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ToggleCable(NetworkConnection sender = null)
        {
            int slot = sender == null ? -1 : Slot(sender.ClientId);
            if (slot < 0 || !HasContract || Time.time - lastAction[slot] < .35f) return;
            lastAction[slot] = Time.time;
            if (connected[slot]) { connected[slot] = false; tensions[slot] = 0; return; }
            float distance = Vector3.Distance(drivers[slot].RearHitch, TowPoint);
            if (distance > RecoveryRules.AttachRange) return;
            connected[slot] = true; lengths[slot] = Mathf.Max(RecoveryWinch.MinimumLength, distance - .3f);
            winchFrames[slot] = new WinchFrame { Length=lengths[slot], State=WinchState.Holding };
            if (Phase == RecoveryPhase.HookUp) phase.Value = (int)RecoveryPhase.Recover;
        }

        [ServerRpc(RequireOwnership = false)]
        public void Unstuck(NetworkConnection sender = null)
        {
            int slot = sender == null ? -1 : Slot(sender.ClientId);
            if (slot < 0 || Phase == RecoveryPhase.Lobby || Phase == RecoveryPhase.Complete || Time.time - lastAction[slot] < 2f) return;
            lastAction[slot] = Time.time; connected[slot] = false; resets[slot]++; drivers[slot].ResetVehicle(false);
        }

        private void ResetRecovery()
        {
            StopScenario();
            if(appliedContract!=contractId.Value)ApplyContract();
            elapsed = 0; snaps = 0; deliveryHold = 0;
            ResetWinches();
            drivers[0].SetModel(vehicle0.Value);drivers[1].SetModel(vehicle1.Value);
            for (int i = 0; i < 2; i++)
            {
                connected[i] = false; tensions[i] = 0; winching[i] = false; lastInput[i] = Time.time;
                resets[i]=0;pullSeconds[i]=0;extending[i]=false;cableStress[i]=0;
                drivers[i].SetSpawn(FreeRoam?garagePosition+new Vector3(i==0?-2.5f:2.5f,.05f,-2):new Vector3(i==0?-2.3f:2.5f,.05f,30f),FreeRoam?Quaternion.Euler(0,180,0):Quaternion.identity);drivers[i].SetSimulation(false);appliedBuild[i]=null;ApplyBuild(i);
            }
            casualty.SetSpawn(Contract.Target,Quaternion.Euler(Contract.Pitch,Contract.Heading,Contract.Roll),Contract.MudDrag,Contract.Extraction-2f,!Contract.IsOpenWorld&&ContractIndex==2); casualty.SetSimulation(false);
        }

        private void FixedUpdate()
        {
            if (!IsServerInitialized || Phase == RecoveryPhase.Lobby || Phase == RecoveryPhase.Complete) return;
            if(HasContract)elapsed += Time.fixedDeltaTime;
            Vector3 totalPull=Vector3.zero;
            for (int i = 0; i < 2; i++)
            {
                drivers[i].SetTowForce(0);
                if (Time.time - lastInput[i] > .6f) { drivers[i].SetInput(Vector2.zero, true); winching[i] = false;extending[i]=false; }
                float strength=FreeRoam?drivers[i].CableStrength:RecoveryRules.BreakForce;
                if (!connected[i])
                {
                    tensions[i] = 0; loadedCableForces[i] = 0;
                    winchFrames[i] = new WinchFrame { Length=lengths[i] };
                    cableStress[i]=RecoveryRules.CableStressAfter(cableStress[i],0,strength,Time.fixedDeltaTime);
                    continue;
                }
                var a = drivers[i].RearHitch; var b = TowPoint;
                var delta = a - b; float distance = delta.magnitude;
                var direction = distance > .01f ? delta / distance : Vector3.forward;
                float speed = Vector3.Dot(drivers[i].Body.GetPointVelocity(a) - TowBody.GetPointVelocity(b), direction);
                winchFrames[i] = RecoveryWinch.Step(lengths[i],distance,speed,winching[i],extending[i],
                    drivers[i].WinchForce,drivers[i].WinchSpeed,winchFrames[i].Speed,Time.fixedDeltaTime);
                lengths[i] = winchFrames[i].Length;
                // Failure reads unsmoothed, uncapped demand. Taking-up-load smoothing
                // and the physics force ceiling must not act as shock immunity.
                float demand = RecoveryRules.CableDemand(distance, lengths[i], speed);
                tensions[i] = Mathf.Clamp01(demand / strength);
                cableStress[i]=RecoveryRules.CableStressAfter(cableStress[i],demand,strength,Time.fixedDeltaTime);
                if (cableStress[i] >= 1 || distance > 25f)
                { connected[i] = false; tensions[i] = 0; loadedCableForces[i]=0;cableStress[i]=0;winchFrames[i]=new WinchFrame{Length=lengths[i]};snaps++;continue; }
                float force = Mathf.Min(demand, RecoveryRules.MaxCableForce);
                if(UsesArcadeExtraction&&!ClearingObstacle)force=RecoveryExtraction.LoadedCableForce(loadedCableForces[i],force,strength,Time.fixedDeltaTime);
                loadedCableForces[i]=force;
                if(force>1000f)pullSeconds[i]+=Time.fixedDeltaTime;
                drivers[i].SetTowForce(force,-direction*force);
                TowBody.AddForceAtPosition(direction * force, b);totalPull+=direction*force;
                drivers[i].Body.AddForceAtPosition(-direction * force, a);
            }
            TickScenario(totalPull);
            if (!ClearingObstacle && Phase == RecoveryPhase.Recover && (!Training||TrainingWinchUsed) && (UsesArcadeExtraction?extraction!=null&&extraction.IsFree:Contract.IsExtracted(casualty.transform.position)))
                phase.Value = (int)RecoveryPhase.Deliver;
            if (Phase == RecoveryPhase.Deliver)
            {
                deliveryHold = (Contract.ContainsDelivery(casualty.transform.position) && casualty.Speed<2f) ? deliveryHold + Time.fixedDeltaTime : 0;
                if (deliveryHold > 1.2f)
                {
                    phase.Value = (int)RecoveryPhase.Complete;
                    foreach (var car in drivers) car.SetInput(Vector2.zero, true);
                    casualty.SetInput(Vector2.zero, true);
                }
            }
        }

        private void Update()
        {
            PresentObstacle();
            PresentExtraction();
            PresentWinches();
            PresentDistress();
            bool targetVisible=HasContract||Phase==RecoveryPhase.Complete;
            if(casualty.gameObject.activeSelf!=targetVisible)casualty.gameObject.SetActive(targetVisible);
            if(appliedContract!=contractId.Value)ApplyContract();
            if(IsClientInitialized||IsServerInitialized){if(appliedPaint0!=paint0.Value){drivers[0].SetPaint(paint0.Value);appliedPaint0=paint0.Value;}if(appliedPaint1!=paint1.Value){drivers[1].SetPaint(paint1.Value);appliedPaint1=paint1.Value;}}
            for(int i=0;i<2;i++){if((IsClientInitialized||IsServerInitialized)&&drivers[i].ModelIndex!=VehicleIndex(i))drivers[i].SetModel(VehicleIndex(i));bool visible=Phase==RecoveryPhase.Lobby?(!singlePreview||i==previewSlot):(i==0||OccupiedSlot(i));if(drivers[i].gameObject.activeSelf!=visible){drivers[i].gameObject.SetActive(visible);if(IsServerInitialized)drivers[i].SetSimulation(visible&&Phase!=RecoveryPhase.Lobby);}}
            if(FreeRoam&&(IsClientInitialized||IsServerInitialized)){ApplyBuild(0);ApplyBuild(1);}
            if (IsServerInitialized && Time.unscaledTime >= nextSnapshot)
            {
                nextSnapshot = Time.unscaledTime + 1f / 15f;
                Snapshot(new RecoveryFrame { Driver0 = new VehicleFrame(drivers[0]), Driver1 = new VehicleFrame(drivers[1]), Casualty = new VehicleFrame(casualty),
                    Cable0 = connected[0], Cable1 = connected[1], Tension0 = tensions[0], Tension1 = tensions[1], Elapsed = elapsed, Snaps = snaps,Resets0=resets[0],Resets1=resets[1],Pull0=pullSeconds[0],Pull1=pullSeconds[1],Winch0=winchFrames[0],Winch1=winchFrames[1],ObstaclePosition=activeObstacle!=null?activeObstacle.transform.position:Vector3.zero,ObstacleRotation=activeObstacle!=null?activeObstacle.transform.rotation:Quaternion.identity,
                    ExtractionStage=(byte)ExtractionStage,ExtractionProgress=ExtractionProgress,ExtractionAlignment=ExtractionAlignment,ExtractionStalled=ExtractionStalled,ExtractionReleasedSeconds=ExtractionReleasedSeconds });
            }
        }

        [ObserversRpc(BufferLast = true)]
        private void Snapshot(RecoveryFrame frame, Channel channel = Channel.Unreliable)
        {
            if (IsServerInitialized) return;
            ReceiveObstacle(frame.ObstaclePosition,frame.ObstacleRotation);
            ExtractionStage=(ExtractionStage)frame.ExtractionStage;ExtractionProgress=frame.ExtractionProgress;ExtractionAlignment=frame.ExtractionAlignment;ExtractionStalled=frame.ExtractionStalled;ExtractionReleasedSeconds=frame.ExtractionReleasedSeconds;
            drivers[0].Receive(frame.Driver0);
            drivers[1].Receive(frame.Driver1);
            casualty.Receive(frame.Casualty);
            connected[0] = frame.Cable0; connected[1] = frame.Cable1;
            winchFrames[0]=frame.Winch0;winchFrames[1]=frame.Winch1;
            lengths[0]=frame.Winch0.Length;lengths[1]=frame.Winch1.Length;
            tensions[0] = frame.Tension0; tensions[1] = frame.Tension1; elapsed = frame.Elapsed; snaps = frame.Snaps;resets[0]=frame.Resets0;resets[1]=frame.Resets1;pullSeconds[0]=frame.Pull0;pullSeconds[1]=frame.Pull1;
        }

        private void LateUpdate()
        {
            for (int i = 0; i < 2; i++)
            {
                cables[i].enabled = connected[i];
                if (!connected[i]) continue;
                var a = drivers[i].RearHitch; var b = TowPoint;
                float sag = Mathf.Lerp(.5f, .03f, tensions[i]);
                for (int j = 0; j < cables[i].positionCount; j++)
                {
                    float t = j / (float)(cables[i].positionCount - 1);
                    cables[i].SetPosition(j, Vector3.Lerp(a, b, t) - Vector3.up * (4f * t * (1f - t) * sag));
                }
                cables[i].startColor = cables[i].endColor = Color.Lerp(new Color(.12f, .15f, .18f), new Color(1f, .35f, .08f), Mathf.InverseLerp(.65f, 1f, tensions[i]));
            }
        }
    }
}
