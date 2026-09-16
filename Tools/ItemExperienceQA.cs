using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;

public static class ItemExperienceQA
{
    public static void Execute(){Run().Forget();}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static async UniTask Shot(PlayPage page,string name)
    {
        await UniTask.Delay(400,ignoreTimeScale:true);await UniTask.WaitForEndOfFrame(page);
        var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/items-"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);
    }
    static async UniTask Close()
    {PopupManager.CurrentPopup.OnEscape();await UniTask.Delay(350,ignoreTimeScale:true);Check(!PopupManager.IsOpenAny,"Popup failed to close");}
    static async UniTaskVoid Run()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");
            page.OpenMenu(0);await Shot(page,"hud");
            Check(page.stageLabel.color==Color.white,"Stage text must be white");
            page.OpenMenu(1);await Shot(page,"heroes");
            var manage=page.heroGrid.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="ManageGear");
            var jade=manage.GetComponent<UnityEngine.UI.Image>().sprite;
            Check(AssetDatabase.GetAssetPath(jade)=="Assets/Textures/UI/Common/Buttons/JadeAction-v3.png" && jade.rect.width<=256,"ManageGear must use compact original JadeAction");
            page.OpenMenu(0);page.OpenVeins();await Shot(page,"veins");
            var veins=(VeinSelectionPopup)PopupManager.CurrentPopup;
            Check(veins.choices.All(b=>b.GetComponent<UnityEngine.UI.Image>().sprite.name!="bg_cell"),"Vein action still uses cell");
            Check(!veins.choices[page.Model.Data.route].interactable,"Selected vein action not disabled");await Close();
            page.OpenMenu(2);await Shot(page,"bag");
            Check(page.inventoryView.TryGetActiveRow(0,false,out var firstRow),"First weapon not bound");
            Check(firstRow.detail.text==page.catalog.gear[0].description,"Bag not using authored description");
            Check(!firstRow.count.gameObject.activeSelf,"Bag still has third text row");
            firstRow.button.onClick.Invoke();await Shot(page,"weapon");
            var detail=(ItemDetailsPopup)PopupManager.CurrentPopup;
            Check(detail.stats.text.Contains("치명타 확률")&&detail.stats.text.Contains("스킬 증폭")&&detail.stats.text.Contains("회/초"),"Weapon stats missing");
            Check(page.pausePolicy.IsPaused,"Details must pause gameplay");await Close();Check(page.ActiveTab==2,"Details lost bag tab");
            page.inventoryView.OpenDetails(8,false);await Shot(page,"armor");detail=(ItemDetailsPopup)PopupManager.CurrentPopup;
            Check(detail.stats.text.Contains("방어력")&&detail.stats.text.Contains("회피율"),"Armor stats missing");await Close();
            page.inventoryView.OpenDetails(10,false);await Shot(page,"accessory");await Close();
            page.inventoryView.OpenDetails(0,true);await Shot(page,"material");await Close();
            File.WriteAllText("PrototypeQA/item-experience.txt","PASS: original JadeAction ManageGear with 256px import; restored vein action/selected state; white battle label; bag name/description; click opens weapon stats; armor/accessory/material details; close returns to bag; pause policy.");
        }
        catch(Exception e){File.WriteAllText("PrototypeQA/item-experience.txt","FAIL: "+e);Debug.LogException(e);}
        finally{PopupManager.Clear();page.OpenMenu(0);}
    }
}
