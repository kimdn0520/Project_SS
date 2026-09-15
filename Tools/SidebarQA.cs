using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class SidebarQA
{
    static readonly List<string> results=new List<string>();
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);results.Add("PASS: "+label);}
    public static void Execute(){Run().Forget();}
    static async UniTaskVoid Run()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
        page.OnWillLeave();var data=page.Model.Data;int oldDay=data.moleSupportDay,oldUsed=data.moleSupportUsed;
        try
        {
            results.Clear();var root=page.transform.Find("SidebarCanvas/SafeArea");
            Check(root.GetComponentsInChildren<Transform>(true).All(t=>!t.name.StartsWith("SidebarSlot_")),"empty sidebar placeholders removed");
            var icons=root.GetComponentsInChildren<BaseSidebarIcon>(true);Check(icons.Length==5,"five typed sidebar icon instances");
            var prefab=PrefabUtility.LoadPrefabContents("Assets/Resources/Prefabs/PlayPage.prefab");
            try { Check(prefab.GetComponentsInChildren<BaseSidebarIcon>(true).All(i=>PrefabUtility.IsPartOfPrefabInstance(i)),"all five icons are nested prefab instances in PlayPage"); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var mole=icons.OfType<MoleSupportIcon>().Single();
            data.moleSupportDay=MoleSupportDaily.Day(DateTime.Now);data.moleSupportUsed=0;mole.RefreshVisibility();
            mole.isAvailable=false;mole.RefreshVisibility();Check(!mole.gameObject.activeSelf,"base availability hides icon");
            mole.isAvailable=true;mole.RefreshVisibility();Check(mole.gameObject.activeSelf,"base availability restores icon");
            mole.GetComponent<Button>().onClick.Invoke();await UniTask.Delay(500,ignoreTimeScale:true);
            var popup=UnityEngine.Object.FindFirstObjectByType<ExpeditionNotice>();Check(popup!=null,"support button opens common notice popup");
            float deadline=Time.realtimeSinceStartup+10;
            while(PopupManager.IsChanging&&Time.realtimeSinceStartup<deadline)await UniTask.Yield();
            popup.Accept();
            while(data.moleSupportUsed==0&&Time.realtimeSinceStartup<deadline)await UniTask.Yield();
            Check(data.moleSupportUsed==1,"editor ad-completion preview consumes one use");
            for(int i=0;i<4;i++)Check(page.CompleteMoleSupportPreview(),"daily completion accepted "+(i+2));
            mole.RefreshVisibility();Check(!mole.gameObject.activeSelf&&!page.CompleteMoleSupportPreview(),"fifth use hides icon and sixth is rejected");
            var loaded=JsonUtility.FromJson<ExpeditionSave>(PlayerPrefs.GetString("ProjectSS.Play.v3"));
            Check(loaded.moleSupportUsed==5&&loaded.moleSupportDay==data.moleSupportDay,"daily limit persists to actual save");
            data.moleSupportDay=MoleSupportDaily.Day(DateTime.Now.AddDays(-1));
            await UniTask.Delay(1200,ignoreTimeScale:true);
            Check(mole.gameObject.activeSelf&&data.moleSupportUsed==0&&mole.remainingText.text=="5/5","hidden icon resets and reappears on new local day");
            var test=new ExpeditionSave{moleSupportDay=20260915,moleSupportUsed=5};
            Check(!MoleSupportDaily.Refresh(test,20260914)&&test.moleSupportUsed==5,"clock rollback does not grant fresh quota");
            Check(MoleSupportDaily.Refresh(test,20260916)&&MoleSupportDaily.Remaining(test)==5,"next date resets daily quota");
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
            await UniTask.WaitForEndOfFrame(page);var image=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes("PrototypeQA/sidebar-support.png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
        }
        catch(Exception e){results.Add("FAIL: "+e);Debug.LogException(e);}
        finally
        {
            PopupManager.Clear();data.moleSupportDay=oldDay;data.moleSupportUsed=oldUsed;page.SendMessage("Persist");page.OnDidEnter();
            File.WriteAllLines("PrototypeQA/sidebar-support.txt",results);
        }
    }
}
