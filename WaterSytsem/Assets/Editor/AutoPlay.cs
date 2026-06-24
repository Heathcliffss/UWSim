#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoPlay
{
    static AutoPlay()
    {
        if (!System.Environment.GetCommandLineArgs().Contains("-autoPlay")) return;

        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                EditorApplication.isPaused = false;
        };

        EditorApplication.update += WaitAndPlay;
    }

    static void WaitAndPlay()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= WaitAndPlay;

        EditorApplication.delayCall += () =>
        {
            EditorApplication.isPaused = false;
            EditorApplication.EnterPlaymode();
        };
    }

    [MenuItem("Tools/Play Basla")]
    public static void EnterFromMenu()
    {
        EditorApplication.isPaused = false;
        EditorApplication.EnterPlaymode();
    }
}
#endif
