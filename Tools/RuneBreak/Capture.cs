using System.IO;
using UnityEditor;
using UnityEngine;
public static class RuneCapture
{
    public static void Execute()
    {
        Directory.CreateDirectory("Tools/RuneBreak/QA");
        var gameView=EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor"));gameView.Focus();
        ScreenCapture.CaptureScreenshot("Tools/RuneBreak/QA/PlayPage.png");
        Debug.Log("RUNE SCREENSHOT requested: "+Screen.width+"x"+Screen.height);
    }
}
