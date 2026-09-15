using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class CaptureSidebar
{
    public static void Execute(){Capture().Forget();}
    static async UniTaskVoid Capture()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
        var icon=page.GetComponentInChildren<MoleSupportIcon>(true);
        if(icon.transform.Find("Visual/Icon").GetComponent<Image>().sprite.name!="MoleSupport")throw new Exception("Mole sprite missing");
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
        await UniTask.WaitForEndOfFrame(page);
        var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/sidebar-support.png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
    }
}
