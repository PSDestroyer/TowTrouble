using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Switch;



namespace UnityEngine.InputSystem.Switch.Samples.SixAxis
{
    public partial class Sensor : MonoBehaviour
    {
        public enum SensorIndex
        {
            Primary = 0,
            Secondary = 1
        }

        public SensorIndex index;

        [SerializeField]
        TMP_Text outputText;

        void Update()
        {
            if (Gamepad.current is NPad pad)
            {
#if UNITY_INPUTSYSTEM_SWITCH_2SIXAXIS
                var attitude = index == SensorIndex.Primary ? pad.primaryAttitude.value : pad.secondaryAttitude.value;
                var acceleration = index == SensorIndex.Primary ? pad.primaryAcceleration.value : pad.secondaryAcceleration.value;
                var angularVelocity = index == SensorIndex.Primary ? pad.primaryAngularVelocity.value : pad.secondaryAngularVelocity.value;
#else
                var attitude = index == SensorIndex.Primary ? pad.primaryAttitude.value : Quaternion.identity;
                var acceleration = index == SensorIndex.Primary ? pad.primaryAcceleration.value : Vector3.zero;
                var angularVelocity = index == SensorIndex.Primary ? pad.primaryAngularVelocity.value : Vector3.zero;
#endif

                //Adjust the Y and Z axis to match the camera rotation
                transform.rotation = QuaternionUtils.SwapYAndZ(attitude);

                outputText.text = $"Attitude {attitude}\nAcceleration {acceleration}\nAngular Velocity{angularVelocity}";

                if (pad.buttonNorth.wasPressedThisFrame)
                {
                    pad.StartSixAxisSensor();
                }

                if (pad.buttonSouth.wasPressedThisFrame)
                {
                    pad.StopSixAxisSensor();
                }
            }

        }
    }
}