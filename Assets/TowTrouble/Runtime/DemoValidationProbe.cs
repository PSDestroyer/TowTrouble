#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine;

namespace TowTrouble
{
    // Explicit local-file harness for repeatable two-process development smoke tests.
    // It is absent from release builds and inactive unless -tow-probe is supplied.
    public sealed class DemoValidationProbe : MonoBehaviour
    {
        [Serializable] private class Command
        {
            public int sequence;
            public string action;
            public string address = "127.0.0.1";
            public float throttle, steer;
            public bool brake, winch;
            public string screenshot, page;
            public int contract, vehicle;
        }
        [Serializable] private class State
        {
            public int frame, slot, occupied, snaps, contract, vehicle, resets;
            public string selected;
            public string[] overflowing;
            public string page;
            public bool connected, host, ready0, ready1, cable0, cable1;
            public string phase, status;
            public float elapsed, speed0, speed1, targetSpeed, tension0, tension1;
            public Vector3 driver0, driver1, target;
        }
        private string file;
        private DemoSession session;
        private Command command = new Command();
        private int sequence = -1;
        private float nextPoll;
        private bool hadProfile;
        private string originalProfile;
        private Gamepad testPad;
        private float releasePadAt;
        private void Awake(){hadProfile=PlayerPrefs.HasKey("TowTrouble.Profile.v1");originalProfile=PlayerPrefs.GetString("TowTrouble.Profile.v1","");}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-tow-probe") { Enable(args[i + 1]); break; }
        }

        public static void Enable(string commandFile)
        {
            var probe = new GameObject("Development smoke probe").AddComponent<DemoValidationProbe>();
            probe.file = Path.GetFullPath(commandFile);
            Application.runInBackground = true;
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(file) || Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + .15f;
            if(testPad!=null&&releasePadAt>0&&Time.unscaledTime>=releasePadAt){InputSystem.QueueStateEvent(testPad,new GamepadState());releasePadAt=0;}
            if (session == null) session = FindFirstObjectByType<DemoSession>();
            if (session == null) return;
            try
            {
                if (File.Exists(file))
                {
                    var value = JsonUtility.FromJson<Command>(File.ReadAllText(file));
                    if (value != null)
                    {
                        command = value;
                        if (command.sequence != sequence)
                        {
                            sequence = command.sequence;
                            switch (command.action)
                            {
                                case "click": var button=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==command.page&&b.gameObject.activeInHierarchy&&b.interactable);button?.onClick.Invoke();break;
                                case "pad": if(testPad==null)testPad=InputSystem.AddDevice<Gamepad>();var padButton=command.page=="down"?GamepadButton.DpadDown:command.page=="up"?GamepadButton.DpadUp:command.page=="back"?GamepadButton.East:GamepadButton.South;InputSystem.QueueStateEvent(testPad,new GamepadState().WithButton(padButton));releasePadAt=Time.unscaledTime+.15f;break;
                                case "page": FindFirstObjectByType<RecoveryHud>()?.Show(command.page);break;
                                case "contract": session.SelectContract(command.contract);break;
                                case "vehicle": session.SelectVehicle(command.vehicle);break;
                                case "host": session.Host(false); break;
                                case "solo": session.Host(true); break;
                                case "join": session.Join(command.address); break;
                                case "ready": session.ToggleReady(); break;
                                case "start": session.Deploy(); break;
                                case "hook": if (session.Connected) session.World.ToggleCable(); break;
                                case "lobby": session.Replay(); break;
                                case "leave": session.Leave(); break;
                                case "quit": Application.Quit(); break;
                            }
                            if (!string.IsNullOrEmpty(command.screenshot)) ScreenCapture.CaptureScreenshot(command.screenshot);
                        }
                    }
                }
                session.ValidationInputActive = true;
                session.ValidationInput = new Vector2(command.steer, command.throttle);
                session.ValidationBrake = command.brake;
                session.ValidationWinch = command.winch;
                var world = session.World;
                var output = new State { frame = Time.frameCount, slot = session.LocalSlot, occupied = world.Occupied, connected = session.Connected,
                    selected=UnityEngine.EventSystems.EventSystem.current!=null&&UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject!=null?UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name:"",overflowing=FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None).Where(t=>t.isTextOverflowing&&t.gameObject.activeInHierarchy).Select(t=>t.text).Take(12).ToArray(),
                    host = session.IsHost, contract=world.ContractIndex,vehicle=session.Profile.vehicle,resets=world.Resets(0)+world.Resets(1),page=session.View, phase = world.Phase.ToString(), status = session.Status, ready0 = world.Ready(0), ready1 = world.Ready(1),
                    cable0 = world.Attached(0), cable1 = world.Attached(1), tension0 = world.Tension(0), tension1 = world.Tension(1),
                    driver0 = world.Drivers[0].transform.position, driver1 = world.Drivers[1].transform.position, target = world.Casualty.transform.position,
                    speed0 = world.Drivers[0].Speed, speed1 = world.Drivers[1].Speed, targetSpeed = world.Casualty.Speed, elapsed = world.Elapsed, snaps = world.Snaps };
                File.WriteAllText(file + ".state.json", JsonUtility.ToJson(output, true));
            }
            catch (IOException) { /* Another process may be atomically replacing a command file. */ }
        }

        private void OnDestroy()
        {
            if (session != null) session.ValidationInputActive = false;
            if(testPad!=null)InputSystem.RemoveDevice(testPad);
            if(hadProfile)PlayerPrefs.SetString("TowTrouble.Profile.v1",originalProfile);else PlayerPrefs.DeleteKey("TowTrouble.Profile.v1");PlayerPrefs.Save();
        }
    }
}
#endif
