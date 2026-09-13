using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class PolishedArtQA
{
    public static void Execute(){Run().Forget();}
    static void Shot(string name){ExpeditionValidation.Capture();File.Copy("PrototypeQA/latest.png","PrototypeQA/"+name+".png",true);}
    static async UniTaskVoid Run()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var backup=JsonUtility.ToJson(page.Model.Data);
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();SessionPausePolicy.Instance.ReleasePause("AppFocusLoss");
            page.OpenMenu(0);page.Model.Data.autoBattle=false;page.Model.Data.chest=false;page.miningView.SetChest(false);
            var so=new SerializedObject(page.miningView);var bank=so.FindProperty("damageSprites");
            if(bank.arraySize!=5)throw new Exception("Missing four fracture stages");
            int count=page.miningWorld.GetComponentsInChildren<Transform>(true).Length;
            Shot("art-intact");
            for(int stage=1;stage<=4;stage++)
            {
                page.miningView.Strike(stage==4,false,0,stage*.25f-.01f);Shot("art-fracture-"+stage);
                if(page.miningWorld.GetComponentsInChildren<LineRenderer>(true).Any(line=>line.enabled))throw new Exception("Legacy line cracks visible");
                if(!page.blocks.Any(r=>r.enabled&&r.sprite==(Sprite)bank.GetArrayElementAtIndex(stage).objectReferenceValue))throw new Exception("Fracture sprite did not appear");
                await UniTask.Delay(500);
            }
            for(int i=0;i<17;i++){page.miningView.Descend();await UniTask.Delay(500);if(i==6||i==14)Shot("art-shaft-scroll-"+i);}
            if(page.miningWorld.GetComponentsInChildren<Transform>(true).Length!=count)throw new Exception("Art creates map objects at runtime");
            page.Model.Data.chest=true;page.miningView.SetChest(true);Shot("art-chest-closed");page.miningView.OpenChest();await UniTask.Delay(420);Shot("art-chest-open");
            File.WriteAllText("PrototypeQA/polished-art.txt","PASS: intact + four distinct fracture sprites; legacy line cracks disabled; 18 descents with fixed object count; closed/open chest captured.\n");Debug.Log("POLISHED ART QA PASS");
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("PrototypeQA/polished-art.txt","FAIL: "+e);}
        finally{page.holdDig.HardCancel();JsonUtility.FromJsonOverwrite(backup,page.Model.Data);page.OpenMenu(2);page.OnWillEnter(null);PlayerPrefs.SetString("ProjectSS.Play.v3",backup);PlayerPrefs.Save();}
    }
}
