using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEditor;
using RuneBreak;
using RuneBreak.Editor;

public static class FontQA
{
    private static void Check(bool value,string message){if(!value)throw new Exception("FONT QA: "+message);}
    public static void Assets()
    {
        RuneValidation.Prefabs();int count=0;
        foreach(string name in new[]{"MainPage","PlayPage"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/"+name+".prefab");
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)){ValidateText(text);count++;}
        }
        foreach(var font in new[]{RuneFontAuthoring.Regular,RuneFontAuthoring.Bold})
        {
            Check(font.atlasPopulationMode==AtlasPopulationMode.Static,"Font not pre-baked");
            Check(font.atlasTextures.All(a=>a.filterMode==FilterMode.Point),"Font texture is filtered");
            for(int c=0xAC00;c<=0xD7A3;c++)Check(font.HasCharacter((char)c),"Missing Hangul syllable "+c);
        }
        Debug.Log("GALMURI ASSET PASS: "+count+" TMP labels; regular/bold include all 11,172 Hangul syllables; static point-filtered atlases; prefab references valid.");
    }
    private static void ValidateText(TMP_Text text)
    {
        Check(text.font==RuneFontAuthoring.Regular||text.font==RuneFontAuthoring.Bold,"Old font at "+text.name);
        foreach(char c in text.text)if(!char.IsWhiteSpace(c))Check(text.font.HasCharacter(c),"Missing character "+c+" at "+text.name);
    }
    public static void Runtime()
    {
        var m=UnityEngine.Object.FindFirstObjectByType<PageManager>();Check(m!=null&&Application.isPlaying,"Enter Play mode");
        m.ChangeImmediate(UIPageType.PlayPage,0);var p=m.GetPage<RunePlayPage>(UIPageType.PlayPage);p.CanvasGroup.alpha=1;
        int count=p.GetComponentsInChildren<Transform>(true).Length;
        p.LoadLevel(0);p.AdvancePresentation(.01f);Capture(p.Canvas.worldCamera,"Galmuri-play.png");
        p.Tap(2,false);for(int i=0;i<50;i++)p.AdvancePresentation(.02f);
        Check(p.slots.Any(s=>s.label.text=="WAIT"),"WAIT state not rendered through font");Capture(p.Canvas.worldCamera,"Galmuri-wait.png");
        var fx=p.effects;fx.Clear();fx.Label("CHAIN BREAK!",new Vector2(240,170),"gold",true,.8f,.1f);fx.Chain(3);fx.Tick(.05f);
        Check(fx.captionLabels.All(t=>!t.enabled),"Delayed caption visible early");fx.Tick(.1f);
        Check(fx.captionLabels.Count(t=>t.enabled)==1&&fx.chainLabel.enabled,"Effect caption or chain missing");
        Capture(p.Canvas.worldCamera,"Galmuri-effects.png");fx.Tick(1);
        Check(fx.captionLabels.All(t=>!t.enabled)&&!fx.chainLabel.enabled,"Expired captions not hidden");
        for(int i=0;i<100;i++){fx.Label("HIT "+i,new Vector2(240,170),"red",false,.4f);fx.Tick(.01f);}fx.Clear();
        Check(count==p.GetComponentsInChildren<Transform>(true).Length,"Effects created hierarchy objects");
        var animation=PopupManager.Instance.GlobalAnimation;PopupManager.Instance.GlobalAnimation=null;
        try
        {
            p.OpenSettings();Capture(p.Canvas.worldCamera,"Galmuri-settings.png");
            var popup=(RuneSettingsPanel)PopupManager.CurrentPopup;
            foreach(var text in popup.GetComponentsInChildren<TMP_Text>(true)){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"Settings overflow: "+text.name);}
        }
        finally{PopupManager.Clear();PopupManager.Instance.GlobalAnimation=animation;}
        p.LoadLevel(0);m.ChangeImmediate(UIPageType.MainPage);var lobby=m.GetPage<RuneLobbyPage>(UIPageType.MainPage);lobby.CanvasGroup.alpha=1;
        foreach(var text in lobby.GetComponentsInChildren<TMP_Text>(true))ValidateText(text);
        Debug.Log("GALMURI RUNTIME PASS: board/WAIT, delayed captions, chain label, 100 caption reuses, cleanup, constant hierarchy, settings overflow check.");
    }
    private static void Capture(Camera camera,string name)
    {
        Canvas.ForceUpdateCanvases();var target=new RenderTexture(540,960,24);var prev=camera.targetTexture;var active=RenderTexture.active;var image=new Texture2D(540,960,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name,image.EncodeToPNG());}
        finally{camera.targetTexture=prev;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
    }
}
