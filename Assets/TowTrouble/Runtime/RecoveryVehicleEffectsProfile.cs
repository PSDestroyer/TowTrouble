using System;
using UnityEngine;
namespace TowTrouble
{
    [Serializable] public sealed class VehicleEmissionAtlas
    {
        public Texture Source;
        public Texture Running;
        public Texture Braking;
    }
    [Serializable] public sealed class VehicleExhaustPorts
    {
        public Mesh Mesh;
        public Vector3[] Positions;
        public Vector3[] Directions;
    }
    public sealed class RecoveryVehicleEffectsProfile : ScriptableObject
    {
        public VehicleEmissionAtlas[] Atlases=Array.Empty<VehicleEmissionAtlas>();
        public VehicleExhaustPorts[] Exhausts=Array.Empty<VehicleExhaustPorts>();
        public Material Smoke;
    }
}
