using System.IO;
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class ChestArtCheck
{
    public static void Execute(){Run().Forget();}
    static async UniTaskVoid Run()
    {
        var page=Object.FindFirstObjectByType<PlayPage>();
        EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");
        page.holdDig.HardCancel();page.OpenMenu(0);await UniTask.Delay(600);
        page.miningView.SetChest(true);page.miningView.OpenChest();await UniTask.Delay(460);
        ExpeditionValidation.Capture();File.Copy("PrototypeQA/latest.png","PrototypeQA/art-chest-hinged.png",true);
        page.miningView.SetChest(page.Model.Data.chest);
    }
}
