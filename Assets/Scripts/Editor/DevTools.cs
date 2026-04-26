#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Developer utilities accessible from the Unity menu bar.
/// Menu: ColoringApp → Reset All Progress  (shortcut: Ctrl+Shift+R)
/// </summary>
public static class DevTools
{
    [MenuItem("ColoringApp/Reset All Progress %#r")]
    private static void ResetAllProgress()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            title:   "Reset All Progress",
            message: "This permanently deletes all level-completion flags, canvas saves, " +
                     "and colour preferences from this device.\n\nContinue?",
            ok:      "Reset Everything",
            cancel:  "Cancel");

        if (!confirmed) return;

        new ProgressService().ResetAll();

        EditorUtility.DisplayDialog(
            "Done",
            "All progress has been wiped. Hit Play to start fresh.",
            "OK");

        Debug.Log("[ColoringApp] DevTools — all progress reset.");
    }

    [MenuItem("ColoringApp/Open Persistent Data Folder")]
    private static void OpenPersistentDataFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
#endif
