using System;
using System.IO;
using HalvaStudio.Save;
using UnityEditor;
using UnityEngine;

public sealed class SaveManagerEditor : EditorWindow
{
    private string message;
    private MessageType messageType;
    private bool showFiles;
    private Vector2 scroll;

    [MenuItem("Window/Save File Deletion Window")]
    public static void ShowWindow() => GetWindow<SaveManagerEditor>("Save File Deletion");

    [MenuItem("Tow Trouble/Save Data")]
    public static void ShowSaveData() => ShowWindow();

    private void OnEnable()
    {
        minSize = new Vector2(520, 330);
        EditorApplication.playModeStateChanged += PlayModeChanged;
    }
    private void OnDisable() => EditorApplication.playModeStateChanged -= PlayModeChanged;
    private void PlayModeChanged(PlayModeStateChange state) => Repaint();

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Save File Deletion", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("SaveManager file");
        EditorGUILayout.SelectableLabel(SaveManager.SavePath, EditorStyles.wordWrappedLabel, GUILayout.Height(42));
        EditorGUILayout.HelpBox("Deletes this local user's career, settings, first-run setup and co-op address, including recovery backups. To keep settings, use Reset Progress in the game's Settings instead.", MessageType.Info);
        showFiles = EditorGUILayout.Foldout(showFiles, "Files included", true);
        if (showFiles)
            foreach (var path in AssociatedFiles(SaveManager.SavePath))
                EditorGUILayout.LabelField(Path.GetFileName(path), File.Exists(path) ? "Exists" : "Not present");

        GUILayout.Space(8);
        if (GUILayout.Button("Show Save Folder"))
            EditorUtility.RevealInFinder(Path.GetDirectoryName(SaveManager.SavePath));

        bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
        if (playing) EditorGUILayout.HelpBox("Stop Play Mode before deleting the save.", MessageType.Info);
        using (new EditorGUI.DisabledScope(playing))
            if (GUILayout.Button("Delete Save File", GUILayout.Height(30))) ConfirmDeletion();

        if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
        EditorGUILayout.EndScrollView();
    }

    private void ConfirmDeletion()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorUtility.DisplayDialog("Delete local save?",
            "Permanently delete all local progress, settings and first-run setup? The save file, its backup and pending temporary files will be removed.\n\n" + SaveManager.SavePath,
            "Delete Save", "Cancel")) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            int count = DeleteSaveFiles(SaveManager.SavePath);
            message = count == 0 ? "No save files were present. The next Play session starts fresh."
                : "Deleted " + count + " save file(s). The next Play session starts fresh.";
            messageType = MessageType.Info;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException)
        {
            message = "Could not delete all save files: " + error.Message;
            messageType = MessageType.Error;
        }
        finally
        {
            // Also discard cached data after a partial deletion; it must never recreate the old file.
            SaveManager.InvalidateEditorCache();
        }
    }

    private static string[] AssociatedFiles(string path)
    {
        string main = Path.GetFullPath(path);
        return new[] { main, main + ".backup", main + ".tmp", main + ".backup.tmp" };
    }
    private static int DeleteSaveFiles(string path)
    {
        var files = AssociatedFiles(path);
        if (!Directory.Exists(Path.GetDirectoryName(files[0]))) return 0;
        int deleted = 0;
        // Remove recovery files first, leaving the primary save intact if one cannot be removed.
        for (int i = files.Length - 1; i >= 0; i--)
        {
            bool existed = File.Exists(files[i]);
            File.Delete(files[i]);
            if (existed) deleted++;
        }
        return deleted;
    }
}
