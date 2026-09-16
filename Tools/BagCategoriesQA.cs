using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;

public static class BagCategoriesQA
{
    public static void Execute(){Run().Forget();}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static async UniTaskVoid Run()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var bag=page.inventoryView;
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
            page.pausePolicy.ReleasePause("AppFocusLoss");page.OpenMenu(2);
            Check(bag.tabs.Length==4,"Four tabs missing");
            for(int tab=0;tab<4;tab++)
            {
                Check(bag.tabs[tab].GetComponentInChildren<TMP_Text>().text==ExpeditionInventory.CategoryNames[tab],"Wrong tab label");
                bag.tabs[tab].GetComponent<Button>().onClick.Invoke();
                await UniTask.Delay(200,ignoreTimeScale:true);
                var expected=new System.Collections.Generic.List<int>();
                for(int i=0;i<page.catalog.gear.Length;i++)
                {
                    int slot=page.catalog.gear[i].equipSlot;
                    bool matches=tab==0&&slot==0||tab==1&&(slot==1||slot==2)||tab==2&&slot==3;
                    if(matches&&page.Model.Data.inventory[i]>0)expected.Add(i);
                }
                if(tab==3)for(int i=0;i<page.catalog.materials.Length;i++)if(page.Model.MaterialCount(i)>0)expected.Add(i);
                Check(bag.FilteredItemIndices.SequenceEqual(expected),"Wrong filtered data in tab "+tab);
                var visible=bag.ActiveRows.ToArray();
                Check(bag.empty.gameObject.activeSelf==(expected.Count==0),"Empty state mismatch");
                await UniTask.WaitForEndOfFrame(page);
                var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/bag-category-"+tab+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
                if(visible.Length>0)
                {
                    visible[0].button.onClick.Invoke();
                    await UniTask.WaitUntil(()=>PopupManager.CurrentPopup is ItemDetailsPopup&&!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));
                    Check(PopupManager.CurrentPopup is ItemDetailsPopup,"Row details failed");
                    PopupManager.CurrentPopup.OnEscape();
                    await UniTask.WaitUntil(()=>!PopupManager.IsOpenAny&&!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));
                    Check(page.ActiveTab==2,"Close did not return to bag, tab="+page.ActiveTab);
                    Check(visible[0].root.gameObject.activeSelf,"Selected tab lost after details");
                }
            }
            File.WriteAllText("PrototypeQA/bag-categories.txt","PASS: four tab labels and actual button listeners; weapons; helmet+armor; accessories; materials; owned-only filtering; empty state; detail popup and return.");
        }
        catch(Exception e){File.WriteAllText("PrototypeQA/bag-categories.txt","FAIL: "+e);Debug.LogException(e);}
        finally{PopupManager.Clear();bag.SelectFilter(0);page.OpenMenu(2);}
    }
}
