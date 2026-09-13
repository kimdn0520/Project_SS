using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class VerticalPlayQA
{
    public static void Execute(){Run().Forget();}
    static void Check(bool value,string text){if(!value)throw new Exception(text);}
    static void Shot(string name){ExpeditionValidation.Capture();File.Copy("PrototypeQA/latest.png","PrototypeQA/"+name+".png",true);}
    static async UniTaskVoid Run()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();string backup=JsonUtility.ToJson(page.Model.Data);
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();SessionPausePolicy.Instance.ReleasePause("AppFocusLoss");
            page.Model.Data.autoBattle=false;page.OpenMenu(0);page.Model.Data.chest=false;page.miningView.SetChest(false);
            float surfaceY=page.miningView.ActiveRockPosition.y;var rest=page.miner.transform.localPosition;var uiRest=page.digButton.transform.localPosition;int objects=page.miningWorld.GetComponentsInChildren<Transform>(true).Length;
            int before=page.Model.Data.excavations;var pointer=new PointerEventData(EventSystem.current){pointerId=91,button=PointerEventData.InputButton.Left};
            Shot("vertical-idle");ExecuteEvents.Execute(page.digButton.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            float fall=0;bool captured=false;
            for(int i=0;i<300;i++)
            {
                await UniTask.Delay(20);fall=Mathf.Max(fall,rest.y-page.miner.transform.localPosition.y);
                Check(page.digButton.transform.localPosition==uiRest,"Whole UI moved during mining");
                if(!captured&&fall>.2f){Shot("vertical-fall");captured=true;}
            }
            ExecuteEvents.Execute(page.digButton.gameObject,pointer,ExecuteEvents.pointerUpHandler);await UniTask.Delay(1500);
            Check(page.Model.Data.excavations>before+2&&fall>.25f,"Hold did not mine / descend");
            Check(Vector3.Distance(rest,page.miner.transform.localPosition)<.01f,"Miner failed to land");
            Check(page.miningWorld.GetComponentsInChildren<Transform>(true).Length==objects,"Runtime map construction");
            Check(Mathf.Abs(page.miningView.ActiveRockPosition.y-surfaceY)<.01f,"Next rock did not arrive at surface");
            Shot("vertical-landed");
            page.Model.Data.chest=true;page.miningView.SetChest(true);int chestBefore=page.Model.Data.excavations;
            SessionPausePolicy.Instance.ReleasePause("AppFocusLoss");page.Dig();Check(page.ChestOpening,$"Chest failed to start paused={page.pausePolicy.IsPaused} tab={page.ActiveTab} descending={page.miningView.IsDescending} scale={Time.timeScale}");await UniTask.Delay(400);Check(page.ChestOpening,"Chest ended too early");Shot("vertical-chest");
            await UniTask.Delay(1900);Check(!page.ChestOpening&&!page.miningView.IsDescending&&page.Model.Data.excavations==chestBefore+1,$"Chest reward/descent: opening={page.ChestOpening} descending={page.miningView.IsDescending} count={page.Model.Data.excavations-chestBefore} paused={page.pausePolicy.IsPaused}");
            page.OpenMenu(2);page.inventoryView.SelectFilter(0);Shot("bag-all");
            page.inventoryView.SelectFilter(1);
            for(int i=0;i<page.inventoryView.gearRows.Length;i++)if(page.inventoryView.gearRows[i].root.gameObject.activeSelf)Check(page.catalog.gear[i].equipSlot==0,"Weapons filter includes armor");
            Check(page.inventoryView.materialRows.All(r=>!r.root.gameObject.activeSelf),"Weapons filter includes materials");
            page.inventoryView.SelectFilter(2);Shot("bag-materials");Check(page.inventoryView.gearRows.All(r=>!r.root.gameObject.activeSelf),"Materials filter includes gear");
            Check(page.menuPanels[1].GetComponentsInChildren<UnityEngine.UI.Button>(true).Length==4,"Bag still contains equip/craft controls");
            page.menuPanels[1].GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="BackToMine1").onClick.Invoke();Check(page.ActiveTab==0,"Bag return did not resume mining");
            page.OpenMenu(1);page.OpenHero(0);page.SelectSlot(0);await UniTask.Delay(400);Shot("equipment-weapons");
            Check(page.equipmentPopup.IsOpen&&page.ActiveTab==1&&page.pausePolicy.IsPaused,"Slot did not open modal over hero");
            for(int i=0;i<page.equipmentPopup.rows.Length;i++)if(page.equipmentPopup.rows[i].root.gameObject.activeSelf)Check(page.catalog.gear[i].equipSlot==0&&(page.catalog.gear[i].hero<0||page.catalog.gear[i].hero==0),"Equipment list class/slot filter failed");
            page.equipmentPopup.OnClickClose();await UniTask.Delay(350);
            int candidate=Enumerable.Range(0,page.catalog.gear.Length).First(i=>page.catalog.gear[i].equipSlot==2);
            page.Model.Data.inventory[candidate]++;
            page.SelectSlot(2);await UniTask.Delay(350);page.equipmentPopup.rows[candidate].button.onClick.Invoke();await UniTask.Delay(350);
            Check(page.Model.Equipped(0,2)==candidate&&!page.equipmentPopup.IsOpen&&page.ActiveTab==1,"Select equipment failed");Shot("hero-equipped");
            page.SelectSlot(2);await UniTask.Delay(350);page.equipmentPopup.unequip.onClick.Invoke();await UniTask.Delay(350);Check(page.Model.Equipped(0,2)==-1,"Unequip failed");
            page.SelectSlot(3);await UniTask.Delay(350);page.equipmentPopup.OnEscape();await UniTask.Delay(350);Check(!page.equipmentPopup.IsOpen&&!page.pausePolicy.IsPaused,"Popup back left pause locked");
            File.WriteAllText("PrototypeQA/vertical-play.txt",$"PASS: actual hold mined {page.Model.Data.excavations-before} blocks, fall {fall*100:F0}px, lands, input fixed, {objects} authored map objects unchanged.\nPASS: chest opens, rewards once, descends.\nPASS: all/weapon/material filters; bag has 3 filter buttons and mining return.\nPASS: hero slot opens filtered popup; equip, unequip, back and pause release.\n");
            Debug.Log("VERTICAL PLAY QA PASS");
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("PrototypeQA/vertical-play.txt","FAIL: "+e);}
        finally
        {
            page.holdDig.HardCancel();if(page.equipmentPopup.IsOpen)page.equipmentPopup.OnClickClose();
            JsonUtility.FromJsonOverwrite(backup,page.Model.Data);page.OnWillEnter(null);PlayerPrefs.SetString("ProjectSS.Play.v3",backup);PlayerPrefs.Save();
        }
    }
}
