using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class ConsoleChestQA
{
    public static void Execute(){Run().Forget();}
    public static void Finish()
    {
        Camera.main.targetTexture=null;RenderTexture.active=null;
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();page.holdDig.HardCancel();page.OpenMenu(0);
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");Shot("final");
    }
    static void Shot(string name)
    {
        var camera=Camera.main;var rt=new RenderTexture(720,1280,24);var target=camera.targetTexture;var rect=camera.rect;var active=RenderTexture.active;Texture2D tex=null;
        try { camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;tex=new Texture2D(720,1280,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();File.WriteAllBytes("PrototypeQA/console2-"+name+".png",tex.EncodeToPNG()); }
        finally {RenderTexture.active=active;camera.targetTexture=target;camera.rect=rect;if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
    }
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);}
    static async UniTaskVoid Run()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();string backup=JsonUtility.ToJson(page.Model.Data);
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");
            page.holdDig.HardCancel();page.OpenMenu(0);page.Model.Data.autoBattle=false;
            Check(page.blocks.Length>=7,"Insufficient preplaced vein rows");
            Check(!page.digLabel.gameObject.activeSelf,"DIG text still visible");
            Check(!page.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="NavigationDeck"),"NavigationDeck remains");
            int objectCount=page.miningWorld.GetComponentsInChildren<Transform>(true).Length;
            page.Model.Data.chest=false;page.miningView.SetChest(false);
            for(int cycle=0;cycle<10;cycle++)
            {
                page.miningView.Descend();
                for(int frame=0;frame<24;frame++)
                {
                    await UniTask.Delay(20);
                    Check(page.blocks.Where(r=>r.enabled).Min(r=>r.bounds.min.y)<-6.4f,"Vein column ends above bottom of viewport");
                }
            }
            Check(page.miningWorld.GetComponentsInChildren<Transform>(true).Length==objectCount,"Mining instantiated map objects");
            page.heatBar.fillAmount=.65f;Shot("console-gauge");
            page.Model.Data.chest=true;page.miningView.SetChest(true);int count=page.Model.Data.excavations;
            Shot("chest-sequence-closed");page.Dig();Check(page.ChestOpening,"Chest did not start");
            await UniTask.Delay(220);Shot("chest-sequence-unlock");Check(page.Model.Data.excavations==count,"Chest awarded before opening");
            await UniTask.Delay(330);Shot("chest-sequence-lid");
            for(int wait=0;wait<60&&page.Model.Data.excavations==count;wait++)await UniTask.Delay(50);await UniTask.Delay(350);Shot("chest-sequence-reward");Check(page.Model.Data.excavations==count+1,$"Chest reward count failed paused={page.pausePolicy.IsPaused}");
            for(int wait=0;wait<80&&(page.ChestOpening||page.miningView.IsDescending||page.itemEffects.Any(item=>item.gameObject.activeSelf));wait++)await UniTask.Delay(50);Check(!page.ChestOpening&&!page.miningView.IsDescending,"Chest did not finish descending");
            Check(page.Model.Data.excavations==count+1,"Chest awarded twice");
            Check(page.itemEffects.All(item=>!item.gameObject.activeSelf),"Reward item was not returned to PoolManager");
            File.WriteAllText("PrototypeQA/console-chest.txt","PASS: seven authored veins cover below viewport during ten descents; no runtime map creation.\nPASS: no NavigationDeck/DIG text, gauge preview captured.\nPASS: unlock -> inside lid -> one reward -> descend; item returned to PoolManager.\n");Debug.Log("CONSOLE CHEST QA PASS");
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("PrototypeQA/console-chest.txt","FAIL: "+e);}
        finally{page.holdDig.HardCancel();JsonUtility.FromJsonOverwrite(backup,page.Model.Data);page.OpenMenu(2);page.OnWillEnter(null);PlayerPrefs.SetString("ProjectSS.Play.v3",backup);PlayerPrefs.Save();}
    }
}
