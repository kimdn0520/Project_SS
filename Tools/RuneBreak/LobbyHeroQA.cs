using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RuneBreak;

public static class LobbyHeroQA
{
    public static object Inspect(){var p=UnityEngine.Object.FindFirstObjectByType<PageManager>().GetPage<RuneLobbyPage>(UIPageType.MainPage);return string.Join("\n",p.hero.GetComponentsInChildren<TMP_Text>().Select(t=>t.name+" rect="+t.rectTransform.rect.size+" preferred="+t.preferredWidth+"x"+t.preferredHeight+" overflow="+t.isTextOverflowing));}
    static void Check(bool ok,string message){if(!ok)throw new Exception("LOBBY HERO: "+message);}
    public static void Execute()=>Run().Forget();
    static async UniTaskVoid Run()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var p=pages.GetPage<RuneLobbyPage>(UIPageType.MainPage);
        bool reduced=RunePreferences.Reduced;string wallet=PlayerPrefs.GetString(RuneCoinReward.SaveKey,"");int next=PlayerPrefs.GetInt("RuneBreak.Next",0),clears=PlayerPrefs.GetInt("RuneBreak.Clears",0);
        try
        {
            PopupManager.Clear();pages.ChangeImmediate(UIPageType.MainPage);await UniTask.Delay(350,ignoreTimeScale:true);p.CanvasGroup.alpha=1;RunePreferences.Reduced=false;var h=p.hero;
            Check(h!=null&&h.parts.Length==5,"one title and four floating blocks");Check(p.showcase==null&&p.GetComponentsInChildren<RuneLobbyShowcase>(true).Length==0,"showcase removed");
            Check(p.GetComponentsInChildren<RuneFlight>(true).Length==0&&p.GetComponentsInChildren<RuneFxGraphic>(true).Length==0&&p.GetComponentsInChildren<RuneLobbyFlightPool>(true).Length==0,"old flights/particles/pool removed");
            Check(h.GetComponentsInChildren<Image>(true).Length==4&&h.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"four non-interactive blocks");
            var art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(RuneBreak.Editor.RunePunchAuthoring.ArtPath);
            Check(h.GetComponentsInChildren<Image>(true).All(i=>art.storedRunes.Contains(i.sprite)),"original shared sprites");
            int count=p.GetComponentsInChildren<Transform>(true).Length;Vector2[] expected=null;
            foreach(int fps in new[]{30,60,120})
            {
                h.enabled=false;h.enabled=true;for(int i=0;i<fps*6;i++)h.AdvancePresentation(1f/fps);var pose=h.parts.Select(part=>part.rect.anchoredPosition).ToArray();
                if(expected==null)expected=pose;else for(int i=0;i<pose.Length;i++)Check(Vector2.Distance(expected[i],pose[i])<.003f,"frame-independent float");
            }
            RunePreferences.Reduced=true;h.AdvancePresentation(.1f);foreach(var part in h.parts){Check(part.rect.anchoredPosition==part.position,"reduced motion resting position");Check(Mathf.Abs(Mathf.DeltaAngle(part.angle,part.rect.localEulerAngles.z))<.001f,"reduced motion resting rotation");}RunePreferences.Reduced=false;
            h.AdvancePresentation(.5f);p.OpenSettings();await UniTask.WaitUntil(()=>!PopupManager.IsChanging);double t=h.Elapsed;var paused=h.parts.Select(part=>part.rect.anchoredPosition).ToArray();h.AdvancePresentation(2);Check(h.Elapsed==t,"settings pauses clock");for(int i=0;i<paused.Length;i++)Check(paused[i]==h.parts[i].rect.anchoredPosition,"settings pauses pose");
            ((RuneSettingsPanel)PopupManager.CurrentPopup).Dismiss();await UniTask.WaitUntil(()=>!PopupManager.IsChanging);await UniTask.Yield();h.AdvancePresentation(.1f);Check(h.Elapsed>t,"resume after settings");
            foreach(var tab in Enumerable.Range(0,p.tabs.Length)){p.SelectTab(tab);Check(p.tabs.Count(x=>x.content.activeSelf)==1,"one visible tab");Check(h.gameObject.activeInHierarchy==(tab==p.homeIndex),"hero belongs to Home");}p.SelectTab(p.homeIndex);
            foreach(var size in new[]{new Vector2Int(540,960),new Vector2Int(540,1200),new Vector2Int(768,1024),new Vector2Int(1280,720)})
            {
                SetSize(size.x,size.y);await UniTask.Delay(250,ignoreTimeScale:true);await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);Canvas.ForceUpdateCanvases();
                h.enabled=false;h.enabled=true;
                for(int frame=0;frame<200;frame++)
                {
                    h.AdvancePresentation(.05f);foreach(var part in h.parts)Within(part.rect,p.Canvas);
                    if(frame%20==0)foreach(var text in h.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"title overflow: "+text.name);}
                }
                foreach(var rect in p.GetComponentsInChildren<RectTransform>().Where(r=>r.name=="Play"||r.name=="Settings"||r.name=="NavigationDock"))Within(rect,p.Canvas);
                Capture(p.Canvas,"Tools/RuneBreak/QA/LobbyHero-"+size.x+"x"+size.y+".png");
            }
            Check(p.GetComponentsInChildren<Transform>(true).Length==count,"no runtime hierarchy growth");
            var play=p.playLabel.GetComponentInParent<Button>();Check(play.onClick.GetPersistentEventCount()==1,"existing Play callback");play.onClick.Invoke();await UniTask.WaitUntil(()=>!pages.IsChanging);Check(pages.CurrentPageType==UIPageType.PlayPage,"Play navigation");pages.ChangeImmediate(UIPageType.MainPage);
            Check(PlayerPrefs.GetInt("RuneBreak.Next",0)==next&&PlayerPrefs.GetInt("RuneBreak.Clears",0)==clears&&PlayerPrefs.GetString(RuneCoinReward.SaveKey,"")==wallet,"progress and wallet unchanged");
            File.WriteAllText("Tools/RuneBreak/QA/lobby-hero.json","{\"status\":\"passed\",\"blocks\":4,\"showcaseRemoved\":true,\"frameRates\":[30,60,120],\"settingsPause\":true,\"reducedMotion\":true,\"noTextOverflow\":true,\"aspects\":4,\"navigation\":true,\"progressUnchanged\":true}");Debug.Log("LOBBY HERO PASS: motion, pause, reduced motion, four aspects, navigation and progress.");
        }
        catch(Exception e){File.WriteAllText("Tools/RuneBreak/QA/lobby-hero.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString()}));Debug.LogException(e);}
        finally{PopupManager.Clear();RunePreferences.Reduced=reduced;SetSize(540,960);pages.ChangeImmediate(UIPageType.MainPage);}
    }
    public static void Film()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();pages.ChangeImmediate(UIPageType.MainPage);var p=pages.GetPage<RuneLobbyPage>(UIPageType.MainPage);p.CanvasGroup.alpha=1;bool reduced=RunePreferences.Reduced;
        try{PopupManager.Clear();RunePreferences.Reduced=false;p.hero.enabled=false;p.hero.enabled=true;Directory.CreateDirectory("Temp/LobbyHeroFilm");for(int i=0;i<120;i++){Capture(p.Canvas,"Temp/LobbyHeroFilm/"+i.ToString("D3")+".png");p.hero.AdvancePresentation(.05f);}Capture(p.Canvas,"Tools/RuneBreak/QA/LobbyHero-Preview.png");}
        finally{RunePreferences.Reduced=reduced;}
    }
    static void Within(RectTransform rect,Canvas canvas)
    {var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var c in corners){var p=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,c);Check(p.x>=-1&&p.y>=-1&&p.x<=Screen.width+1&&p.y<=Screen.height+1,"clipped "+rect.name+" at "+Screen.width+"x"+Screen.height);}}
    static void SetSize(int width,int height)
    {
        var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameViewSizes");var singleton=type.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);var group=type.GetMethod("GetGroup").Invoke(singleton,new[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null),index=-1;
        for(int i=0;i<count;i++){var item=group.GetType().GetMethod("GetGameViewSize").Invoke(group,new object[]{i});if((int)item.GetType().GetProperty("width").GetValue(item)==width&&(int)item.GetType().GetProperty("height").GetValue(item)==height){index=i;break;}}
        if(index<0){var item=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,height,$"Lobby QA {width}x{height}"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{item});index=count;}
        var vt=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(vt);vt.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,index);view.Repaint();
    }
    static void Capture(Canvas canvas,string path)
    {
        Canvas.ForceUpdateCanvases();var camera=canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24);var tex=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
