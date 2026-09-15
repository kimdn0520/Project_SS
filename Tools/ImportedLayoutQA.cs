using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class ImportedLayoutQA
{
    static BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    public static void Wide(){Size(1920,1080);}
    public static void Tall(){Size(1080,2400);}
    public static void Portrait(){Size(720,1280);}
    static void Size(int w,int h)
    {
        var asm=typeof(Editor).Assembly;var sizesType=asm.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=singleton.GetProperty("instance").GetValue(null);
        var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{Enum.ToObject(asm.GetType("UnityEditor.GameViewSizeGroupType"),0)});
        var sizeType=asm.GetType("UnityEditor.GameViewSize");var size=Activator.CreateInstance(sizeType,flags,null,new object[]{Enum.ToObject(asm.GetType("UnityEditor.GameViewSizeType"),1),w,h,"Imported UI QA"},null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        var window=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));window.GetType().GetProperty("selectedSizeIndex",flags).SetValue(window,count-1);window.Focus();
    }
    public static void Capture(){Run().Forget();}
    public static void Equipment(){EquipmentShot().Forget();}
    public static void Gauge(){GaugeShot().Forget();}
    public static void Miner(){MinerShot().Forget();}
    static async UniTaskVoid MinerShot()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();page.OpenMenu(0);
        await UniTask.Delay(250);await Shot("miner-armored");
        page.miner.MineStrike(.6f);await UniTask.Delay(220);await Shot("miner-armored-swing");
        var so=new SerializedObject(page.miner);var chest=(SpriteRenderer)so.FindProperty("chest").objectReferenceValue;var helmet=(SpriteRenderer)so.FindProperty("helmet").objectReferenceValue;
        if(!helmet.gameObject.activeInHierarchy||chest.sprite.name!="FA_Chest_045_Gray"||helmet.sprite.name!="FA_Helmet_041_Gray")throw new Exception("Miner armor missing in play");
        File.WriteAllText("PrototypeQA/miner-armor.txt","PASS: compatible CharacterMaker chest 045 / helmet 041; armor retained in play; mining swing captured.");
    }
    static async UniTaskVoid GaugeShot()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();page.OpenMenu(0);page.OnWillLeave();
        try
        {
            page.heatGauge.SetValue(.65f,new Color(1,.77f,.36f));await UniTask.Delay(250);await Shot("dig-gauge-refined");
            var so=new SerializedObject(page.heatGauge);var reveal=(RectTransform)so.FindProperty("reveal").objectReferenceValue;
            if(Mathf.Abs(reveal.rect.height-134*.65f)>.1f)throw new Exception("Gauge did not settle");
            page.heatGauge.SetValue(0,Color.white);if(page.heatBar.enabled)throw new Exception("Zero gauge remains visible");
            File.WriteAllText("PrototypeQA/dig-gauge-refined.txt","PASS: original gold/steel frame and ticks restored; smooth charge settles at 65%; zero fill hidden.");
        }
        finally{page.Refresh();page.OnDidEnter();}
    }
    static async UniTaskVoid EquipmentShot()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();page.OpenMenu(1);page.OpenHero(0);page.SelectSlot(0);await UniTask.Delay(700);
        var t=PopupManager.CurrentPopup.Canvas.transform.Find("PopupCommon/Window/Close");var im=t.GetComponent<Image>();
        Debug.Log("CLOSE QA active="+t.gameObject.activeInHierarchy+" culled="+im.canvasRenderer.cull+" alpha="+im.canvasRenderer.GetAlpha()+" world="+t.position+" canvas="+im.canvas.name+" size="+((RectTransform)t).rect);
        await Shot("equipment-close");
    }
    static async UniTaskVoid Run()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();page.pausePolicy.ReleasePause("AppFocusLoss");page.OpenMenu(0);
        await UniTask.Delay(400);await Shot("layout-"+Screen.width+"x"+Screen.height);
        page.OpenVeins();await UniTask.Delay(400);await Shot("layout-popup-"+Screen.width+"x"+Screen.height);PopupManager.CurrentPopup.OnEscape();
    }
    static async UniTask Shot(string name)
    {await UniTask.WaitForEndOfFrame(UnityEngine.Object.FindFirstObjectByType<PlayPage>());var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    public static string Inspect()
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Popups/EquipmentSelection.prefab");
        var r=asset.transform.Find("PopupCommon/Window/Close");var im=r.GetComponent<Image>();var b=r.GetComponent<Button>();
        return "Close="+r.gameObject.activeSelf+" image="+im.enabled+" color="+im.color+" pos="+((RectTransform)r).anchoredPosition+" scale="+r.localScale+" sprite="+im.sprite+" children="+r.childCount+" button="+b.enabled;
    }
}
