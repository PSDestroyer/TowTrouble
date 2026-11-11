using System.Collections;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TowTrouble
{
    // FishNet initializes at -32768. Select its transport before it subscribes to events.
    [DefaultExecutionOrder(short.MinValue - 1)]
    public sealed partial class DemoSession : MonoBehaviour
    {
        [SerializeField] private NetworkManager network;
        [SerializeField] private RecoveryWorld world;
        [SerializeField] private RecoveryCamera recoveryCamera;
        private float nextInput;
        private float connectDeadline;
        private bool quickStart;
        private bool explorationRequested,garageIdentitySent;
        private string participatingRun;
        public RecoveryGarageCatalog GarageCatalog=>world.Drivers[0].GetComponent<RecoveryCustomization>()?.Catalog;
        public bool GarageOpen=>View=="Garage";
        public bool CanOpenGarage=>Connected&&world.FreeRoam&&!menuOpen&&world.AtGarage(LocalSlot)&&GarageCatalog!=null;
        public GarageSlot GarageFocusSlot {get;private set;}=GarageSlot.Assembly;
        public string GarageFocusPart {get;private set;}
        public void FocusGarage(GarageSlot slot,string partId){GarageFocusSlot=slot;GarageFocusPart=partId;}
        public int GarageViewReset { get; private set; }
        public bool GarageCameraInputBlocked { get; set; }
        public void ResetGarageView() { GarageViewReset++; }
        public bool BoardOpen=>View=="Board";
        public int LastReward=>Profile.lastPayout;
        private bool identitySent, recorded;
        public RecoveryProfile Profile {get;private set;}
        public int SelectedContract {get;private set;}
        public bool Solo {get;private set;}
        public string View {get;set;}="Title";
        public bool UsingGamepad=>RecoveryInputDevices.UsingGamepad;
        private int menuInputConsumedFrame = -1;
        public void ConsumeMenuInput() { menuInputConsumedFrame = Time.frameCount; }
        private UnityEngine.Rendering.Universal.ColorAdjustments grade;
        public RecoveryAudioDirector Audio { get; private set; }
        private void Awake(){RecoveryOfflineTransport.Prepare(network);Profile=RecoveryProfile.Load();SelectedContract=Profile.lastContract;ApplySettings();}
        private void StartAudio(){var bank=Resources.Load<RecoveryAudioBank>("RecoveryAudioBank");if(bank==null){Debug.LogWarning("Tow Trouble audio bank is missing.");return;}var rig=new GameObject("Tow Trouble audio");rig.transform.SetParent(transform,false);Audio=rig.AddComponent<RecoveryAudioDirector>();Audio.Configure(this,bank);}
        public void ApplySettings(){RecoveryInputPrompts.FamilyOverride=Profile.controllerFamily;RecoveryLocalization.Select(Profile.language);AudioListener.volume=Profile.master;RecoveryVehicle.EffectsGain=Profile.effects;ApplyGraphicsPreference();if(grade==null){var volume=FindFirstObjectByType<UnityEngine.Rendering.Volume>();if(volume!=null&&volume.profile!=null)volume.profile.TryGet(out grade);}if(grade!=null)grade.postExposure.Override(Mathf.Log(RecoveryPlatform.UsesSwitchRendering?1:Profile.brightness,2)+.1f);}
        public void SelectContract(int value){SelectedContract=Mathf.Clamp(value,0,RecoveryContract.All.Length-1);if(Profile.Unlocked(SelectedContract)){Profile.lastContract=SelectedContract;Profile.Save();}if(Profile.Unlocked(SelectedContract)&&Connected&&IsHost&&world.Phase==RecoveryPhase.Lobby)world.ConfigureSession(SelectedContract,!Solo,world.FreeRoam);}
        public void SelectVehicle(int value)
        {
            if(!Profile.OwnsVehicle(value)||(Connected&&world.Phase!=RecoveryPhase.Lobby))return;
            Profile.vehicle=value;Profile.Save();if(Connected)world.SetDriver(Profile.name,Profile.vehicle,Profile.paint);else world.PreviewVehicle(Profile.vehicle);world.PreviewPaint(Profile.paint);
        }
        public bool BuyVehicle(int value)
        {
            if((Connected&&world.Phase!=RecoveryPhase.Lobby)||!RecoveryEconomy.BuyVehicle(Profile,value))return false;
            SelectVehicle(value);return true;
        }
        public bool ChooseStarter(int value,int paint=0)
        {
            if(Connected||Connecting||!RecoveryEconomy.ChooseStarter(Profile,value,paint))return false;
            Profile.Save();world.PreviewVehicle(value);world.PreviewPaint(Profile.paint);return true;
        }
        public void SelectPaint(int value)
        {
            if(!Profile.starterChosen||GarageCatalog==null||(Connected&&world.Phase!=RecoveryPhase.Lobby&&!GarageOpen))return;
            value=Mathf.Clamp(value,0,3);string id=value==0?"stock_Paint":"paint_"+value;
            if(value>0&&!Profile.Owns(id))return;
            Profile.Build(Profile.vehicle).Set(GarageCatalog.Find(id),GarageCatalog);Profile.paint=value;Profile.paintSelectionRevision=1;Profile.Save();
            if(Connected){if(world.Phase==RecoveryPhase.Lobby)world.SetDriver(Profile.name,Profile.vehicle,value);SendGarageBuild();}
            else PreviewFittedRide();
        }
        public void ResetAtCheckpoint(){if(Connected)world.Unstuck();menuOpen=false;Time.timeScale=1;}
        public void RestartContract(){if(Connected&&IsHost){if(world.FreeRoam){int contract=world.ContractIndex;CloseOverlay();world.ReturnToExplore();world.AcceptContract(contract);}else{world.ReturnToLobby();readySent=false;quickStart=Solo;menuOpen=false;Time.timeScale=1;}}}
        private bool readySent;
        private bool leaving;
        private bool wasConnected;
        private bool menuOpen;
        private string status = "A small crew. A heavy pull.";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal bool ValidationInputActive;
        internal Vector2 ValidationInput;
        internal bool ValidationBrake, ValidationWinch;
#endif

        public RecoveryWorld World => world;
        public NetworkManager Network => network;
        public bool Connected => network != null && network.ClientManager != null && world != null && world.NetworkObject != null && network.ClientManager.Started && world.IsClientInitialized;
        public bool Connecting { get; private set; }
        // A solo launch must finish its initial lobby/setup before showing the playable world.
        public bool ReadyForPresentation => Connected && LocalSlot >= 0 && !Connecting;
        public bool IsHost => network != null && network.ServerManager != null && network.ServerManager.Started;
        public int LocalSlot => Connected ? world.Slot(network.ClientManager.Connection.ClientId) : -1;
        public bool MenuOpen => menuOpen || ContractIntroActive;
        public string Status => status;

        public void Configure(NetworkManager manager, RecoveryWorld recoveryWorld, RecoveryCamera cameraRig)
        { network = manager; world = recoveryWorld; recoveryCamera = cameraRig; }

        private static int travelMode;
        private static string travelAddress;
        public void OpenTutorial(){ RequestTraining(); }
        private void TravelTo(string scene,int mode,string address)
        {travelMode=mode;travelAddress=address;Time.timeScale=1;SceneManager.LoadScene(scene);}
        private void Start()
        {
            if (world.Map == null) { TravelTo("OpenWorldRecovery",0,null); return; }
            if(!RecoveryPlatform.UsesSwitchRendering&&Profile.displayWidth>=800&&Profile.displayHeight>=600&&Profile.displayMode>=0&&Profile.displayMode<=3)Screen.SetResolution(Profile.displayWidth,Profile.displayHeight,(FullScreenMode)Profile.displayMode);
            Application.targetFrameRate = RecoveryPlatform.FrameRateLimit;
            // FishNet reapplies this rendering cap on connection; its simulation tick rate stays unchanged.
            network.ServerManager.SetFrameRate(RecoveryPlatform.FrameRateLimit);
            network.ClientManager.SetFrameRate(RecoveryPlatform.FrameRateLimit);
            network.ClientManager.OnClientConnectionState += ClientState;
            world.ContractAcceptedPresentation += QueueContractIntro;
            recoveryCamera.Bind(this);PreviewFittedRide();StartAudio();
            if(RecoveryPlatform.UsesSwitchRendering)gameObject.AddComponent<RecoverySwitchSceneRendering>().Initialize(recoveryCamera.GetComponent<Camera>());
            int mode=travelMode;string destination=travelAddress;travelMode=0;travelAddress=null;
            if(mode==1||mode==2)Host(mode==1,true);else if(mode==3)Join(destination);
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-tow-host") Host(false);
                if (args[i] == "-tow-solo") Host(true);
                if (args[i] == "-tow-join" && i + 1 < args.Length) Join(args[++i]);
            }
        }

        private void OnDestroy()
        {
            CancelContractIntro();
            if(world!=null)world.ContractAcceptedPresentation -= QueueContractIntro;
            RestoreGraphicsPreference();ClearTrainingDressing();StopHaptics();Time.timeScale=1;
            if (network != null && network.ClientManager != null) network.ClientManager.OnClientConnectionState -= ClientState;
        }

        private void ClientState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            { wasConnected = true; status = "Preparing your vehicle…"; }
            else if (args.ConnectionState == LocalConnectionState.Stopped && !leaving)
            {
                Connecting = false; MenuConnectionFailed();
                status = !RecoveryPlatform.MultiplayerAvailable?"Your game could not start. Please try again.":wasConnected ? "Connection closed. Return to the menu to reconnect." : "Could not connect. Check the host address and UDP port 7777.";
                if (IsHost) network.ServerManager.StopConnection(true);
            }
        }

        public void Host(bool solo,bool exploration=false)
        {
            if(!solo&&!RecoveryPlatform.MultiplayerAvailable){status="Only local play is available on this platform.";return;}
            if(!Profile.starterChosen){View="Starter";return;}
            if (Connecting || Connected || leaving) return;
            if(exploration&&world.Map==null){TravelTo("OpenWorldRecovery",solo?1:2,null);return;}
            Solo=solo;explorationRequested=exploration;garageIdentitySent=false;identitySent=false;recorded=false;
            quickStart = solo; readySent = false; Connecting = true; connectDeadline = Time.unscaledTime + 12f;
            status = "Starting your session…";
            var transport = network.TransportManager.Transport;
            if (RecoveryPlatform.OfflineOnly && !(transport is RecoveryOfflineTransport))
            { Connecting = false; status = "Offline play could not initialize. Return to the main menu and try again."; return; }
            ConfigureHostTransport(transport);
            var startingBuild=Profile.Build(Profile.vehicle).Copy();
            if(GarageCatalog!=null)startingBuild.Normalize(GarageCatalog,Profile.Owns);
            world.PrepareHostVehicle(startingBuild,Profile.paint,exploration||world.Map!=null);
            if (!network.ServerManager.StartConnection()) { Connecting = false; status = RecoveryPlatform.MultiplayerAvailable?"Port 7777 is unavailable.":"Could not start the game. Please try again."; return; }
            network.ClientManager.StartConnection();
        }

        private static void ConfigureHostTransport(Transport transport)
        {
            transport.SetMaximumClients(RecoveryPlatform.MultiplayerAvailable?2:1);
            if (RecoveryPlatform.OfflineOnly) return;
            transport.SetPort(7777);
            transport.SetClientAddress("127.0.0.1");
        }

        public void Join(string address)
        {
            if(!RecoveryPlatform.MultiplayerAvailable){status="Only local play is available on this platform.";return;}
            if(!Profile.starterChosen){View="Starter";return;}
            if (Connecting || Connected || leaving) return;
            if(world.Map==null){TravelTo("OpenWorldRecovery",3,address);return;}
            address = address.Trim();
            if (address.Length == 0 || address.Length > 253) { status = "Enter a valid host address."; return; }
            Solo=false;garageIdentitySent=false;identitySent=false;recorded=false;
            quickStart = false; readySent = false; Connecting = true; connectDeadline = Time.unscaledTime + 12f;
            status = "Connecting to crew...";
            network.TransportManager.Transport.SetPort(7777);
            network.TransportManager.Transport.SetClientAddress(address);
            network.ClientManager.StartConnection();
        }

        public void ToggleReady()
        {
            if (Connected && LocalSlot >= 0) world.SetReady(!world.Ready(LocalSlot));
        }

        public void Deploy() { if (Connected && IsHost) world.StartRecovery(); }
        public void Replay() { if (Connected && IsHost) { menuOpen = false; world.ReturnToLobby(); } }
        public void ToggleMenu() {if(BoardOpen||GarageOpen){CloseOverlay();return;}menuOpen = !menuOpen;if(Solo)Time.timeScale=menuOpen?0:1; }
        public void ToggleBoard()
        {
            if(Training||!Connected||!world.FreeRoam||world.Phase==RecoveryPhase.Lobby||world.Phase==RecoveryPhase.Complete)return;
            if(BoardOpen){CloseOverlay();return;}if(menuOpen)return;
            world.Drive(Vector2.zero,true,false);menuOpen=true;View="Board";if(Solo)Time.timeScale=0;
        }
        public void OpenGarage()
        {
            if(!CanOpenGarage)return;
            world.SetGarageState(true);world.Drive(Vector2.zero,true,false);menuOpen=true;View="Garage";if(Solo)Time.timeScale=0;
        }
        public void PreviewGarage(RecoveryBuild build)
        {if(GarageOpen)world.Drivers[LocalSlot].GetComponent<RecoveryCustomization>().Apply(build,Profile.paint,true);}
        public void CommitGarage(RecoveryBuild build)
        {
            if(!GarageOpen||GarageCatalog==null)return;build=build.Copy();build.Normalize(GarageCatalog,Profile.Owns);Profile.builds[Profile.vehicle]=build;Profile.paint=RecoveryPaintPalette.Resolve(build,GarageCatalog);Profile.paintSelectionRevision=1;Profile.Save();PreviewGarage(build);SendGarageBuild();
        }
        public void SendGarageBuild()
        {if(Connected&&GarageCatalog!=null){var build=Profile.Build(Profile.vehicle);build.Normalize(GarageCatalog,Profile.Owns);world.SetGarageBuild(JsonUtility.ToJson(build));}}
        public void CloseOverlay()
        {
            GarageCameraInputBlocked=false;
            if(GarageOpen){world.SetGarageState(false);var build=Profile.Build(Profile.vehicle);world.Drivers[LocalSlot].GetComponent<RecoveryCustomization>()?.Apply(build,Profile.paint,true);SendGarageBuild();}
            menuOpen=false;View="Gameplay";Time.timeScale=1;
        }
        public void AcceptBoardContract(int index)
        {if(!BoardOpen||!IsHost||world.Phase!=RecoveryPhase.Explore||index<0||index>=world.AvailableContracts.Length)return;CloseOverlay();world.AcceptContract(index);Audio?.Play(RecoverySound.Accept);}
        public void RecordCompletion()
        {
            if(Training||!Connected||world.Phase!=RecoveryPhase.Complete||recorded)return;
            recorded=true;if(participatingRun==world.RunId){RecoveryEconomy.Claim(Profile,world.RunId,world.Contract,world.Resets(0)+world.Resets(1)==0,world.Snaps==0);Profile.Save();}
        }
        public void FinishExplorationContract()
        {if(!Connected||!IsHost)return;CloseOverlay();world.ReturnToExplore();}


        public void Leave()
        {
            if (leaving) return;
            CancelContractIntro();leaving = true;Time.timeScale=1; network.ClientManager.StopConnection();
            if (IsHost) network.ServerManager.StopConnection(true);
            StartCoroutine(Reload());
        }

        private IEnumerator Reload()
        {
            yield return new WaitForSecondsRealtime(.25f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Update()
        {
            TickTraining();
            if (Connecting && Time.unscaledTime > connectDeadline)
            {
                Connecting = false; network.ClientManager.StopConnection();
                if (IsHost) network.ServerManager.StopConnection(true);
                MenuConnectionFailed();status = RecoveryPlatform.MultiplayerAvailable?"Connection timed out. Check the host address and try again.":"Starting the game timed out. Please try again.";
            }
            if (!Connected || LocalSlot < 0) { CancelContractIntro(); return; }
            if (Connecting && (!Solo || world.Phase != RecoveryPhase.Lobby))
            { Connecting = false; status = Solo ? "Your game is ready." : "Connected. Get your crew ready."; }
            if(!identitySent){identitySent=true;if(IsHost)world.ConfigureSession(world.Map!=null?0:SelectedContract,!Solo,explorationRequested||world.Map!=null);world.SetDriver(Profile.name,Profile.vehicle,Profile.paint);}
            if(world.FreeRoam&&!garageIdentitySent){garageIdentitySent=true;SendGarageBuild();}
            if(GarageOpen&&world.Phase!=RecoveryPhase.Explore)CloseOverlay();
            if(world.HasContract)participatingRun=world.RunId;
            if(world.Phase==RecoveryPhase.Complete&&!recorded&&!Training)RecordCompletion();
            if(world.Phase!=RecoveryPhase.Complete)recorded=false;
            if (quickStart && world.Phase == RecoveryPhase.Lobby)
            {
                if (!readySent) { readySent = true; world.SetReady(true); }
                if (world.CanStart) { quickStart = false; world.StartRecovery(); }
            }
            if (TickContractIntro(Time.unscaledDeltaTime)) return;
            // UI submit/cancel can close an overlay before this Update executes.
            // Consume that frame so the same button cannot pause or operate a cable.
            if (menuInputConsumedFrame == Time.frameCount)
            { world.Drive(Vector2.zero, true, false); return; }
            bool overlayWasOpen=BoardOpen||GarageOpen;
            var keyboard = Keyboard.current;
            var pad = RecoveryInputDevices.Pad;
            if(world.FreeRoam&&View=="Gameplay")
            {
                if((keyboard!=null&&keyboard[Profile.ReadKey(Profile.boardKey,Key.Tab)].wasPressedThisFrame)||(pad!=null&&pad.selectButton.wasPressedThisFrame))ToggleBoard();
                if((keyboard!=null&&keyboard[Profile.ReadKey(Profile.garageKey,Key.G)].wasPressedThisFrame)||(pad!=null&&pad.buttonWest.wasPressedThisFrame))OpenGarage();
            }
            if(!overlayWasOpen&&(View=="Gameplay"||View=="Pause")&&((keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)||(pad!=null&&pad.startButton.wasPressedThisFrame)))ToggleMenu();
            if (world.Phase == RecoveryPhase.Lobby || world.Phase == RecoveryPhase.Complete){return;}
            if (!menuOpen)
            {
                if ((keyboard != null && keyboard[Profile.ReadKey(Profile.hookKey,Key.F)].wasPressedThisFrame) || (pad != null && RecoveryInputPrompts.Confirm(pad).wasPressedThisFrame)) world.ToggleCable();
                if ((keyboard != null && keyboard[Profile.ReadKey(Profile.resetKey,Key.R)].wasPressedThisFrame) || (pad != null && pad.leftStickButton.wasPressedThisFrame)) world.Unstuck();
            }
            if(RecoveryPlatform.MultiplayerAvailable&&!menuOpen){if(keyboard!=null){if(keyboard.digit1Key.wasPressedThisFrame)world.SendSignal(1);if(keyboard.digit2Key.wasPressedThisFrame)world.SendSignal(2);if(keyboard.digit3Key.wasPressedThisFrame)world.SendSignal(3);}if(pad!=null&&pad.dpad.up.wasPressedThisFrame)world.SendSignal(1);if(pad!=null&&pad.dpad.right.wasPressedThisFrame)world.SendSignal(2);if(pad!=null&&pad.dpad.down.wasPressedThisFrame)world.SendSignal(3);}
            if (Time.unscaledTime < nextInput) return;
            nextInput = Time.unscaledTime + .05f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ValidationInputActive)
            {
                world.Drive(ValidationInput, ValidationBrake, ValidationWinch);
                return;
            }
#endif
            Vector2 input = Vector2.zero; bool brake = menuOpen; bool winch = false;bool extend=false;
            if (!menuOpen)
            {
                if (keyboard != null)
                {
                    input.x = (keyboard[Profile.ReadKey(Profile.rightKey,Key.D)].isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard[Profile.ReadKey(Profile.leftKey,Key.A)].isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                    input.y = (keyboard[Profile.ReadKey(Profile.forwardKey,Key.W)].isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard[Profile.ReadKey(Profile.reverseKey,Key.S)].isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
                    brake = keyboard[Profile.ReadKey(Profile.brakeKey,Key.Space)].isPressed; winch = keyboard[Profile.ReadKey(Profile.reelKey,Key.Q)].isPressed;extend=keyboard[Profile.ReadKey(Profile.extendKey,Key.E)].isPressed;
                }
                if (pad != null && UsingGamepad)
                {
                    input.x = pad.leftStick.x.ReadValue(); input.y = pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue();
                    brake = RecoveryInputPrompts.Cancel(pad).isPressed; winch = pad.leftShoulder.isPressed;extend=pad.rightShoulder.isPressed;
                }
            }
            world.Drive(input, brake, winch,extend);
        }
    }
}
