using UnityEngine;

namespace TowTrouble
{
    /// <summary>Switch-owned material copies also retain instancing shader variants in players.</summary>
    public sealed class RecoverySwitchInstancingCatalog : ScriptableObject
    {
        [System.Serializable] public struct Entry
        {
            public Material source;
            public Material instanced;
        }
        public Entry[] entries = System.Array.Empty<Entry>();
        public Material Find(Material source)
        {
            if (source == null) return null;
            foreach (var entry in entries)
                if (entry.source == source && entry.instanced != null && entry.instanced.enableInstancing &&
                    entry.instanced.shader == source.shader && entry.instanced.shader.isSupported)
                    return entry.instanced;
            return null;
        }
    }
}
