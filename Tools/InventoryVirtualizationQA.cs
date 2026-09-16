using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;

public static class InventoryVirtualizationQA
{
    public static void Execute(){Run().Forget();}
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
    static async UniTaskVoid Run()
    {
        var live=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
        GameObject host=null;ExpeditionCatalog catalog=null;
        var checks=new List<string>();
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
            live.pausePolicy.ReleasePause("AppFocusLoss");live.OpenMenu(2);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayPage.prefab");
            var source=prefab.GetComponent<PlayPage>();
            catalog=UnityEngine.Object.Instantiate(source.catalog);
            var weapon=catalog.gear[0];
            catalog.gear=Enumerable.Range(0,1000).Select(i=>new GearDefinition{title="테스트 검 "+i,description="스크롤 재사용 검증 "+i,equipSlot=0,hero=-1,damage=i+1,interval=1,icon=weapon.icon}).ToArray();
            catalog.materials=new[]{new MaterialDefinition{id="qa_material",title="테스트 재료",description="재료 셀 재사용",icon=weapon.icon}};
            var data=ExpeditionSave.Fresh(1000);for(int i=0;i<1000;i++)data.inventory[i]=1;
            var model=new ExpeditionModel(catalog,data,1);model.AddMaterial(0,5);
            host=new GameObject("VirtualInventoryQA");host.SetActive(false);
            var testPage=host.AddComponent<PlayPage>();testPage.catalog=catalog;testPage.pausePolicy=live.pausePolicy;
            typeof(PlayPage).GetField("<Model>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(testPage,model);
            var cloned=UnityEngine.Object.Instantiate(source.inventoryView.gameObject,live.inventoryView.transform.parent);
            cloned.name="VirtualInventoryQAList";
            var bag=cloned.GetComponent<ExpeditionInventory>();bag.page=testPage;
            live.inventoryView.gameObject.SetActive(false);
            cloned.SetActive(true);bag.Refresh();Canvas.ForceUpdateCanvases();
            await UniTask.Yield();bag.Refresh();
            int budget=Mathf.CeilToInt((bag.scroll.viewport!=null?bag.scroll.viewport.rect.height:((RectTransform)bag.scroll.transform).rect.height)/136f)+1+bag.OverscanRows*2;
            Check(bag.FilteredItemIndices.Count==1000,"All data items must remain in list");
            Check(bag.CreatedCellCount<=budget,"Cell count scales with catalog");
            Check(bag.content.rect.height==1000*136-12,"Virtual content height wrong");
            checks.Add("1000 items, "+bag.CreatedCellCount+" reusable cells; overscan "+bag.OverscanRows+" per side");
            int created=bag.CreatedCellCount;
            for(int step=0;step<=30;step++)
            {
                bag.scroll.verticalNormalizedPosition=1-step/30f;
                bag.scroll.onValueChanged.Invoke(bag.scroll.normalizedPosition);
                await UniTask.Yield();
                Check(bag.ActiveCellCount<=budget&&bag.CreatedCellCount==created,"Scrolling allocated new cells");
                foreach(var row in bag.ActiveRows)
                {
                    int i=Mathf.RoundToInt(-row.root.anchoredPosition.y/136f);
                    Check(row.title.text=="테스트 검 "+i+" ×1"&&row.detail.text.EndsWith(i.ToString()),"Recycled binding mismatch");
                }
            }
            Check(bag.TryGetActiveRow(999,false,out var last),"Last item missing at scroll end");
            last.button.onClick.Invoke();await UniTask.WaitUntil(()=>PopupManager.CurrentPopup is ItemDetailsPopup&&!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));
            Check(((ItemDetailsPopup)PopupManager.CurrentPopup).itemName.text=="테스트 검 999","Recycled click opened old item");
            PopupManager.CurrentPopup.OnEscape();await UniTask.WaitUntil(()=>!PopupManager.IsOpenAny&&!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));
            checks.Add("Fast scrolling through 1000 items reuses same cells; recycled click opens item 999");
            bag.scroll.verticalNormalizedPosition=.5f;bag.scroll.onValueChanged.Invoke(bag.scroll.normalizedPosition);await UniTask.Yield();
            float offset=bag.content.anchoredPosition.y;
            int visibleFirst=Mathf.FloorToInt(offset/136f);
            Check(bag.TryGetActiveRow(visibleFirst-2,false,out _)&&bag.TryGetActiveRow(visibleFirst-1,false,out _),"Top overscan missing");
            await UniTask.WaitForEndOfFrame(live);var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/inventory-virtualized.png",shot.EncodeToPNG());UnityEngine.Object.Destroy(shot);
            bag.SelectFilter(1);Check(bag.ActiveCellCount==0&&bag.empty.gameObject.activeSelf,"Empty category retained cells");
            bag.SelectFilter(3);Check(bag.TryGetActiveRow(0,true,out var material)&&material.title.text=="테스트 재료","Material reused wrong data");
            bag.SelectFilter(0);Check(bag.TryGetActiveRow(0,false,out _),"Filter switch did not reset scroll");
            bag.scroll.verticalNormalizedPosition=0;bag.scroll.onValueChanged.Invoke(bag.scroll.normalizedPosition);
            for(int i=1;i<1000;i++)data.inventory[i]=0;bag.Refresh();
            Check(bag.ActiveCellCount==1&&bag.TryGetActiveRow(0,false,out _)&&bag.content.anchoredPosition.y==0,"Inventory shrink stranded viewport");
            cloned.SetActive(false);Check(bag.ActiveCellCount==0&&bag.PooledCellCount==created,"Closing bag failed to return cells");
            cloned.SetActive(true);bag.Refresh();Check(bag.CreatedCellCount==created&&bag.ActiveCellCount==1,"Reopen did not reuse pool");
            checks.Add("Two-row buffers, empty/category/material changes, shrinking data, disable/reopen pooling passed");
            UnityEngine.Object.Destroy(cloned);
            File.WriteAllText("PrototypeQA/inventory-virtualized.txt","PASS\n"+string.Join("\n",checks));
        }
        catch(Exception e){File.WriteAllText("PrototypeQA/inventory-virtualized.txt","FAIL: "+e);Debug.LogException(e);}
        finally
        {
            PopupManager.Clear();if(host!=null)UnityEngine.Object.Destroy(host);if(catalog!=null)UnityEngine.Object.Destroy(catalog);
            var test=live.inventoryView.transform.parent.Find("VirtualInventoryQAList");if(test!=null)UnityEngine.Object.Destroy(test.gameObject);
            live.inventoryView.gameObject.SetActive(true);live.OpenMenu(2);
        }
    }
}
