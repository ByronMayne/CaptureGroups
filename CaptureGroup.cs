using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


public static class CaptureGroup
{
    public class CaptureState
    {
        public Rect Bounds;
        public string SavePath;
    }

    private static Stack<CaptureState> _captureStack;
    private static int _isPreformingCapture;
    private static EditorWindow _activeEditor;
    private static EventType _lastEvent;
    private static bool _isDebugEnabled;
    private static GUIStyle _debugStyle;
    private static GUIContent _cachedContent;

    static CaptureGroup()
    {
        _captureStack = new Stack<CaptureState>();
        _cachedContent = new GUIContent();
    }

    private static bool IsLayout
    {
        get { return Event.current.type == EventType.Layout; }
    }

    private static bool IsRepaint
    {
        get { return Event.current.type == EventType.Repaint; }
    }

    public static void Begin(string savePath)
    {
        Rect verticle = EditorGUILayout.BeginVertical();
        if (_isDebugEnabled || _isPreformingCapture > 0)
        {

            if (_debugStyle == null)
            {
                _debugStyle = new GUIStyle("U2D.createRect");
                _debugStyle.fontStyle = FontStyle.Bold;
                _debugStyle.normal.textColor = new Color(1, 0, 1);
                _debugStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (IsRepaint)
            {
                CaptureState state = new CaptureState();
                state.SavePath = savePath;
                state.Bounds = verticle;
                _captureStack.Push(state);
            }
        }
    }

    public static void End()
    {
        EditorGUILayout.EndVertical();

        if (_isDebugEnabled || _isPreformingCapture > 0)
        {
            EventType currentEvent = Event.current.type;

            if (_isPreformingCapture > 0)
            {
                if ((currentEvent == EventType.Repaint || currentEvent == EventType.Layout) && currentEvent != _lastEvent)
                {
                    _lastEvent = currentEvent;

                    if (currentEvent == EventType.Repaint)
                    {
                        _isPreformingCapture--;
                    }

                    if (_isPreformingCapture == 0)
                    {
                        _activeEditor = null;
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        _activeEditor.Repaint();
                    }
                }
            }

            switch (currentEvent)
            {
                case EventType.Repaint:
                    CaptureState state = _captureStack.Pop();
                    if (_isDebugEnabled)
                    {
                        _cachedContent.text = state.SavePath;
                        _debugStyle.Draw(state.Bounds, _cachedContent, true, false, false, false);
                    }
                    if (_isPreformingCapture == 1)
                    {
                        Capture(state);
                    }
                    break;
            }
        }
    }

    private static void Capture(CaptureState state)
    {
        // Get our bounds
        Rect bounds = state.Bounds;
        // Inverse the position of y because the layout system top left is zero and screen position is bottom left.
        bounds.y = _activeEditor.position.height - bounds.y - bounds.height;
        // Create our next texture 
        Texture2D texture = new Texture2D(Mathf.CeilToInt(bounds.width), Mathf.CeilToInt(bounds.height), TextureFormat.RGBA32, false);
        // Read the pixels from the screen
        texture.ReadPixels(bounds, 0, 0, false);
        // Apply the pixels
        texture.Apply(false, false);
        // Convert to png
        byte[] bytes = texture.EncodeToJPG();
        // Get the current directory 
        string currentDirectory = Directory.GetCurrentDirectory();
        // Create our output path
        string outputPath = Path.Combine(currentDirectory, state.SavePath);
        // Change the extension
        outputPath = Path.ChangeExtension(outputPath, ".png");
        // Get the output directory
        string outputDirectory = Path.GetDirectoryName(outputPath);
        // If it does not exist make it
        Directory.CreateDirectory(outputDirectory);
        // Write the image to disk
        File.WriteAllBytes(outputPath, bytes);
    }

    public static void PreformCapture(EditorWindow editor)
    {
        _activeEditor = editor;
        _isPreformingCapture = 2;
        _activeEditor.Repaint();
    }

    public static bool ShowDebug
    {
        set { _isDebugEnabled = value; }
        get { return _isDebugEnabled; }
    }
}
