using HalvaStudio.Save;
using UnityEditor;
using UnityEngine;

public sealed class SaveManagerEditor : EditorWindow
{
    [MenuItem("Tow Trouble/Save Data")]
    public static void ShowWindow() => GetWindow<SaveManagerEditor>("Save Data");
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Local save", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(SaveManager.SavePath, GUILayout.Height(42));
        EditorGUILayout.HelpBox("Career, settings and co-op address belong to this local user. Use Reset Progress in Settings for a fresh career. This window never deletes unrelated application data.", MessageType.Info);
        if (GUILayout.Button("Show Save Folder")) EditorUtility.RevealInFinder(System.IO.Path.GetDirectoryName(SaveManager.SavePath));
    }
}
