using UnityEngine;
using UnityEditor;

/// <summary>
/// LiderKamera görüntüsünü bağımsız bir Editor penceresinde gösterir.
/// Play başlayınca otomatik açılır. K tuşu ile aç/kapa.
/// </summary>
[InitializeOnLoad]
public class LiderCamWindow : EditorWindow
{
    private static LiderCamWindow _instance;

    static LiderCamWindow()
    {
        // Play moduna geçince otomatik aç
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Play başladı → pencereyi aç
            LiderCameraController.ShowWindow = true;
            Open();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            // Play durdu → pencereyi kapat
            if (_instance != null) _instance.Close();
        }
    }

    [MenuItem("Window/LiderKamera")]
    public static void Open()
    {
        _instance = GetWindow<LiderCamWindow>("LiderKamera");
        _instance.minSize = new Vector2(320, 180);
        LiderCameraController.ShowWindow = true;
    }

    void OnEnable()
    {
        _instance = this;
        EditorApplication.update += OnEditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        if (_instance == this) _instance = null;
    }

    void OnEditorUpdate()
    {
        if (!EditorApplication.isPlaying) return;

        bool show = LiderCameraController.ShowWindow;

        if (show && _instance == null)
            Open();
        else if (!show && _instance != null)
            _instance.Close();

        if (_instance != null)
            _instance.Repaint();
    }

    void OnGUI()
    {
        var rt = LiderCameraController.EditorRT;

        if (rt == null)
        {
            GUIStyle style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 12 };
            GUILayout.FlexibleSpace();
            GUILayout.Label("▶ Oyunu başlat → ROV kamerası buraya yansır.", style);
            GUILayout.FlexibleSpace();
            return;
        }

        GUI.DrawTexture(new Rect(0, 0, position.width, position.height), rt, ScaleMode.ScaleToFit);
    }
}
