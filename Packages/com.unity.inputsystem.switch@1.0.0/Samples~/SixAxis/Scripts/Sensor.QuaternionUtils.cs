using System;
using UnityEngine;

namespace UnityEngine.InputSystem.Switch.Samples.SixAxis
{
    public partial class Sensor
    {
        public static class QuaternionUtils
        {
            // Matrix to Swap Y/Z Axis
            private static readonly Matrix4x4 SwapYZ = new Matrix4x4(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(0, 0, 0, 1)
            );

            public static Quaternion SwapYAndZ(Quaternion q)
            {
                Matrix4x4 rot = Matrix4x4.Rotate(q);
                Matrix4x4 swapped = SwapYZ * rot * SwapYZ;
                return swapped.rotation;
            }
        }
    }
}