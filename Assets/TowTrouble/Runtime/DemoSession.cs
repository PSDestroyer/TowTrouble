using System.Collections;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TowTrouble
{
    public sealed class DemoSession : MonoBehaviour
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
        public bool BoardOpen=>View=="Board";
        public int LastReward=>Profile.lastPayout;
        private bool identitySent, recorded;
        public RecoveryProfile Profile {get;private set;}
        public int SelectedContract {get;private set;}
        public bool Solo {get;private set;}
        public string View {get;set;}="Title";
        public bool UsingGamepad=>Gamepad.current!=null && lastGamepad;
        private bool lastGamepad;
        private float lastKeyboard;
        private UnityEngine.Rendering.Universal.ColorAdjustments grade;
        private void Awake(){Profile=RecoveryProfile.Load();SelectedContract=Profile.lastContract;ApplySettings();}
        public void ApplySettings(){AudioListener.volume=Profile.master;RecoveryVehicle.EffectsGain=Profile.effects;if(QualitySettings.GetQualityLevel()!=Mathf.Clamp(Profile.quality,0,QualitySettings.names.Length-1))QualitySettings.SetQualityLevel(Mathf.Clamp(Profile.quality,0,QualitySettings.names.Length-1),true);if(grade==null){var volume=FindFirstObjectByType<UnityEngine.Rendering.Volume>();if(volume!=null&&volume.profile!=null)volume.profile.TryGet(out grade);}if(grade!=null)grade.postExposure.Override(Mathf.Log(Profile.brightness,2)+.1f);}
        public void SelectContract(int value){SelectedContract=Mathf.Clamp(value,0,RecoveryContract.All.Length-1);if(Profile.Unlocked(SelectedContract)){Profile.lastContract=SelectedContract;Profile.Save();}if(Profile.Unlocked(SelectedContract)&&Connected&&IsHost&&world.Phase==RecoveryPhase.Lobby)world.ConfigureSession(SelectedContract,!Solo,world.FreeRoam);}
        public void SelectVehicle(int value){Profile.vehicle=Mathf.Clamp(value,0,3);Profile.Save();if(Connected&&world.Phase==RecoveryPhase.Lobby)world.SetDriver(Profile.name,Profile.vehicle,Profile.paint);else world.PreviewVehicle(Profile.vehicle);world.PreviewPaint(Profile.paint);}
        public void SelectPaint(int value){Profile.paint=Mathf.Clamp(value,0,3);Profile.Save();if(Connected&&world.Phase==RecoveryPhase.Lobby)world.SetDriver(Profile.name,Profile.vehicle,Profile.paint);else world.PreviewPaint(Profile.paint);}
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
        public bool IsHost => network != null && network.ServerManager != null && network.ServerManager.Started;
        public int LocalSlot => Connected ? world.Slot(network.ClientManager.Connection.ClientId) : -1;
        public bool MenuOpen => menuOpen;
        public string Status => status;

        public void Configure(NetworkManager manager, RecoveryWorld recoveryWorld, RecoveryCamera cameraRig)
        { network = manager; world = recoveryWorld; recoveryCamera = cameraRig; }

        private void Start()
        {
            Application.targetFrameRate = 60;
            network.ServerManager.SetFrameRate(60);
            network.ClientManager.SetFrameRate(60);
            network.ClientManager.OnClientConnectionState += ClientState;
            recoveryCamera.Bind(this);world.PreviewVehicle(Profile.vehicle);world.PreviewPaint(Profile.paint);
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
            Gamepad.current?.ResetHaptics();Time.timeScale=1;
            if (network != null && network.ClientManager != null) network.ClientManager.OnClientConnectionState -= ClientState;
        }

        private void ClientState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            { Connecting = false; wasConnected = true; status = "Connected. Get your crew ready."; }
            else if (args.ConnectionState == LocalConnectionState.Stopped && !leaving)
            {
                Connecting = false;
                status = wasConnected ? "Connection closed. Return to the menu to reconnect." : "Could not connect. Check the host address and UDP port 7777.";
                if (IsHost) network.ServerManager.StopConnection(true);
            }
        }

        public void Host(bool solo,bool exploration=false)
        {
            if (Connecting || Connected || leaving) return;
            Solo=solo;explorationRequested=exploration;garageIdentitySent=false;identitySent=false;recorded=false;
            quickStart = solo; readySent = false; Connecting = true; connectDeadline = Time.unscaledTime + 12f;
            status = "Starting FishNet host...";
            network.TransportManager.Transport.SetMaximumClients(2);
            network.TransportManager.Transport.SetPort(7777);
            network.TransportManager.Transport.SetClientAddress("127.0.0.1");
            if (!network.ServerManager.StartConnection()) { Connecting = false; status = "Port 7777 is unavailable."; return; }
            network.ClientManager.StartConnection();
        }

        public void Join(string address)
        {
            if (Connecting || Connected || leaving) return;
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
            if(!Connected||!world.FreeRoam||world.Phase==RecoveryPhase.Lobby||world.Phase==RecoveryPhase.Complete)return;
            if(BoardOpen){CloseOverlay();return;}if(menuOpen)return;
            world.Drive(Vector2.zero,true,false);menuOpen=true;View="Board";if(Solo)Time.timeScale=0;
        }
        public void OpenGarage()
        {
            if(!Connected||!world.FreeRoam||menuOpen||!world.AtGarage(LocalSlot)||GarageCatalog==null)return;
            world.SetGarageState(true);world.Drive(Vector2.zero,true,false);menuOpen=true;View="Garage";if(Solo)Time.timeScale=0;
        }
        public void PreviewGarage(RecoveryBuild build)
        {if(GarageOpen)world.Drivers[LocalSlot].GetComponent<RecoveryCustomization>().Apply(build,Profile.paint,true);}
        public void CommitGarage(RecoveryBuild build)
        {
            if(!GarageOpen||GarageCatalog==null)return;build=build.Copy();build.Normalize(GarageCatalog,Profile.Owns);Profile.builds[Profile.vehicle]=build;Profile.Save();PreviewGarage(build);SendGarageBuild();
        }
        public void SendGarageBuild()
        {if(Connected&&GarageCatalog!=null){var build=Profile.Build(Profile.vehicle);build.Normalize(GarageCatalog,Profile.Owns);world.SetGarageBuild(JsonUtility.ToJson(build));}}
        public void CloseOverlay()
        {
            if(GarageOpen){world.SetGarageState(false);var build=Profile.Build(Profile.vehicle);world.Drivers[LocalSlot].GetComponent<RecoveryCustomization>()?.Apply(build,Profile.paint,true);SendGarageBuild();}
            menuOpen=false;View="Gameplay";Time.timeScale=1;
        }
        public void AcceptBoardContract(int index)
        {if(!BoardOpen||!IsHost||world.Phase!=RecoveryPhase.Explore)return;CloseOverlay();world.AcceptContract(index);}
        public void RecordCompletion()
        {
            if(!Connected||world.Phase!=RecoveryPhase.Complete||recorded)return;
            recorded=true;if(participatingRun==world.RunId){RecoveryEconomy.Claim(Profile,world.RunId,world.ContractIndex,world.Resets(0)+world.Resets(1)==0,world.Snaps==0);Profile.Save();}
        }
        public void FinishExplorationContract()
        {if(!Connected||!IsHost)return;CloseOverlay();world.ReturnToExplore();}


        public void Leave()
        {
            if (leaving) return;
            leaving = true;Time.timeScale=1; network.ClientManager.StopConnection();
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
            if (Connecting && Time.unscaledTime > connectDeadline)
            {
                Connecting = false; network.ClientManager.StopConnection();
                if (IsHost) network.ServerManager.StopConnection(true);
                status = "Connection timed out. Check the address and host firewall.";
            }
            if(Keyboard.current!=null&&Keyboard.current.anyKey.wasPressedThisFrame){lastKeyboard=Time.unscaledTime;lastGamepad=false;}
            var activePad=Gamepad.current;if(activePad!=null&&(activePad.buttonSouth.wasPressedThisFrame||activePad.buttonEast.wasPressedThisFrame||activePad.buttonNorth.wasPressedThisFrame||activePad.buttonWest.wasPressedThisFrame||activePad.leftStickButton.wasPressedThisFrame||activePad.startButton.wasPressedThisFrame||activePad.leftShoulder.isPressed||activePad.rightShoulder.isPressed||activePad.leftStick.ReadValue().sqrMagnitude>.05f||activePad.rightStick.ReadValue().sqrMagnitude>.05f||activePad.dpad.ReadValue().sqrMagnitude>.05f||activePad.rightTrigger.ReadValue()>.05f||activePad.leftTrigger.ReadValue()>.05f))lastGamepad=true;
            if (!Connected || LocalSlot < 0) return;
            if(!identitySent){identitySent=true;if(IsHost)world.ConfigureSession(SelectedContract,!Solo,explorationRequested);world.SetDriver(Profile.name,Profile.vehicle,Profile.paint);}
            if(world.FreeRoam&&!garageIdentitySent){garageIdentitySent=true;SendGarageBuild();}
            if(GarageOpen&&world.Phase!=RecoveryPhase.Explore)CloseOverlay();
            if(world.HasContract)participatingRun=world.RunId;
            if(world.Phase==RecoveryPhase.Complete&&!recorded)RecordCompletion();
            if(world.Phase!=RecoveryPhase.Complete)recorded=false;
            if (quickStart && world.Phase == RecoveryPhase.Lobby)
            {
                if (!readySent) { readySent = true; world.SetReady(true); }
                if (world.CanStart) { quickStart = false; world.StartRecovery(); }
            }
            bool overlayWasOpen=BoardOpen||GarageOpen;
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            if(world.FreeRoam&&View=="Gameplay")
            {
                if((keyboard!=null&&keyboard[Profile.ReadKey(Profile.boardKey,Key.Tab)].wasPressedThisFrame)||(pad!=null&&pad.leftStickButton.wasPressedThisFrame))ToggleBoard();
                if((keyboard!=null&&keyboard[Profile.ReadKey(Profile.garageKey,Key.G)].wasPressedThisFrame)||(pad!=null&&pad.buttonWest.wasPressedThisFrame))OpenGarage();
            }
            else if(BoardOpen&&((keyboard!=null&&(keyboard[Profile.ReadKey(Profile.boardKey,Key.Tab)].wasPressedThisFrame||keyboard.escapeKey.wasPressedThisFrame))||(pad!=null&&(pad.buttonEast.wasPressedThisFrame||pad.leftStickButton.wasPressedThisFrame))))CloseOverlay();
            else if(GarageOpen&&((keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)||(pad!=null&&pad.buttonEast.wasPressedThisFrame)))CloseOverlay();
            if(!overlayWasOpen&&(View=="Gameplay"||View=="Pause")&&((keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)||(pad!=null&&pad.startButton.wasPressedThisFrame)))ToggleMenu();
            if (world.Phase == RecoveryPhase.Lobby || world.Phase == RecoveryPhase.Complete){pad?.ResetHaptics();return;}
            if (!menuOpen)
            {
                if ((keyboard != null && keyboard[Profile.ReadKey(Profile.hookKey,Key.E)].wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame)) world.ToggleCable();
                if ((keyboard != null && keyboard[Profile.ReadKey(Profile.resetKey,Key.R)].wasPressedThisFrame) || (pad != null && pad.selectButton.wasPressedThisFrame)) world.Unstuck();
            }
            if(!menuOpen){if(keyboard!=null){if(keyboard.digit1Key.wasPressedThisFrame)world.SendSignal(1);if(keyboard.digit2Key.wasPressedThisFrame)world.SendSignal(2);if(keyboard.digit3Key.wasPressedThisFrame)world.SendSignal(3);}if(pad!=null&&pad.dpad.up.wasPressedThisFrame)world.SendSignal(1);if(pad!=null&&pad.dpad.right.wasPressedThisFrame)world.SendSignal(2);if(pad!=null&&pad.dpad.down.wasPressedThisFrame)world.SendSignal(3);}
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
                    brake = keyboard[Profile.ReadKey(Profile.brakeKey,Key.Space)].isPressed; winch = keyboard[Profile.ReadKey(Profile.reelKey,Key.Q)].isPressed;extend=keyboard[Profile.ReadKey(Profile.extendKey,Key.F)].isPressed;
                }
                if (pad != null && UsingGamepad)
                {
                    input.x = pad.leftStick.x.ReadValue(); input.y = pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue();
                    brake = pad.buttonEast.isPressed; winch = pad.leftShoulder.isPressed;extend=pad.rightShoulder.isPressed;
                }
            }
            world.Drive(input, brake, winch,extend);
            if(pad!=null){float rumble=Profile.vibration&&world.Attached(LocalSlot)&&world.Tension(LocalSlot)>.8f&&!menuOpen?.15f:0f;pad.SetMotorSpeeds(rumble,rumble*.6f);}
        }
    }
}
