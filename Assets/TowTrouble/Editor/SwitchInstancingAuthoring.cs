using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static class SwitchInstancingAuthoring
    {
        [MenuItem("Tow Trouble/Platform/Refresh Switch Rock Instancing")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play before authoring Switch instancing materials.");
            const string folder = "Assets/TowTrouble/Platform/SwitchInstancing";
            EnsureFolder(folder);
            EnsureFolder("Assets/TowTrouble/Resources");
            const string path = "Assets/TowTrouble/Resources/SwitchRockInstancing.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<RecoverySwitchInstancingCatalog>(path);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<RecoverySwitchInstancingCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
            var sources = new HashSet<Material>();
            // Preserve previous entries when refreshing a different scene.
            foreach (var entry in catalog.entries) if (entry.source != null) sources.Add(entry.source);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials)
                    if (material != null && material.shader != null && material.shader.name == "Shader Graphs/S_Rock" &&
                        AssetDatabase.Contains(material) && SupportsInstancing(material.shader)) sources.Add(material);
            var entries = new List<RecoverySwitchInstancingCatalog.Entry>();
            foreach (var source in sources)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
                string copyPath = folder + "/Rock_" + guid + ".mat";
                var copy = AssetDatabase.LoadAssetAtPath<Material>(copyPath);
                if (copy == null) { copy = new Material(source); AssetDatabase.CreateAsset(copy, copyPath); }
                else { copy.shader = source.shader; copy.CopyPropertiesFromMaterial(source); }
                copy.name = source.name + " (Switch Instanced)";
                copy.enableInstancing = true;
                EditorUtility.SetDirty(copy); AssetDatabase.SaveAssetIfDirty(copy);
                entries.Add(new RecoverySwitchInstancingCatalog.Entry { source = source, instanced = copy });
            }
            entries.Sort((a, b) => string.CompareOrdinal(a.source.name, b.source.name));
            catalog.entries = entries.ToArray();
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog);
        }
        private static bool SupportsInstancing(Shader shader)
        {
            var method = typeof(ShaderUtil).GetMethod("HasInstancing", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            return method != null && (bool)method.Invoke(null, new object[] { shader });
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
    }
}
