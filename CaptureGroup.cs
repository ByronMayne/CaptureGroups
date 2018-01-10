using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


public static class CaptureGroup
{
    public delegate void RepaintDelegate();
    public class CaptureState
    {
        public Rect Bounds;
        public string SavePath;
    }

    private static Stack<CaptureState> _captureStack;
    private static int _isPreformingCapture;
    private static RepaintDelegate _onRepaint;
    private static float _viewHeight;
    private static float _viewYOffset;
    private static EventType _lastEvent;
    private static bool _isDebugEnabled;
    private static GUIStyle _debugStyle;
    private static GUIContent _cachedContent;
    private static RectOffset _padding;

    static CaptureGroup()
    {
        _captureStack = new Stack<CaptureState>();
        _cachedContent = new GUIContent();
        _padding = new RectOffset(4, 4, 4, 4);
    }

    /// <summary>
    /// How much bigger that screen shots should be beyond the group. Useful
    /// to make sure images don't get clipped by the edge.
    /// </summary>
    public static RectOffset CapturePadding
    {
        get { return _padding; }
        set { _padding = value; }
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
                        _onRepaint = null;
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        _onRepaint.Invoke();
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
        bounds.y = _viewHeight - bounds.y - bounds.height + _viewYOffset;

        bounds.y -= _padding.top;
        bounds.height += _padding.top + _padding.bottom;
        bounds.x -= _padding.left;
        bounds.width += _padding.left + _padding.right;
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
        _viewHeight = editor.position.y;
        _onRepaint = editor.Repaint;
        _isPreformingCapture = 2;
        _viewYOffset = 0f;
    }


    public static void PreformCapture(Editor editor)
    {
        _viewHeight = Screen.height;
        _onRepaint = editor.Repaint;
        _isPreformingCapture = 2;
        // Inspectors have an offset 
        _viewYOffset -= EditorGUIUtility.singleLineHeight;
    }

    public static bool ShowDebug
    {
        set { _isDebugEnabled = value; }
        get { return _isDebugEnabled; }
    }
}
