using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RuneBreak;
public static class InspectSurfaceLayout
{
    public static void Verify()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var p=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);
        bool enabled=p.enabled,audio=RunePreferences.Audio;p.enabled=false;RunePreferences.Audio=false;
        try
        {
            PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,0);p.CanvasGroup.alpha=1;p.LoadLevel(0);
            int id=Enumerable.Range(0,p.Rules.Level.blocks.Length).First(i=>p.Rules.Apply(p.State,new RuneMove(RuneMoveKind.Board,i),out _,out var shot,out _)&&shot.store);
            p.Tap(id,false);var flight=p.pool.GetComponentsInChildren<RuneFlight>(true).Single(f=>f.gameObject.activeSelf);int slot=flight.shot.slot;
            flight.Render(flight.start+flight.duration,true);var end=flight.body.position;var endSize=flight.body.sizeDelta;
            p.AdvancePresentation(flight.duration+.01f);p.slots[slot].Tick(10,true);
            float landingError=Vector3.Distance(end,p.slots[slot].gemRect.position);
            if(landingError>.02f||Vector2.Distance(endSize,p.slots[slot].gemRect.sizeDelta)>.01f)throw new Exception("Storage landing position or size changed at handoff.");
            flight.Begin(p.art,p.Rules.Level.blocks[id],new RuneShot{relaunch=true,slot=slot,side=0,max=2},0,true);
            float launchError=Vector3.Distance(flight.body.position,p.slots[slot].gemRect.position);
            if(launchError>.02f||Vector2.Distance(flight.body.sizeDelta,endSize)>.01f)throw new Exception("Storage launch position or size changed at handoff.");
            p.LoadLevel(0);var preview=p.State.Copy();preview.slots=new[]{"blue","gold","red","green","blue"}.Select(c=>Array.FindIndex(p.Rules.Level.blocks,b=>b.color==c)).ToArray();
            for(int i=0;i<5;i++){var s=p.slots[i];s.Render(p.art,p.Rules,preview,i,false);s.Tick(10,true);if(s.gemRect.rect.width>((RectTransform)s.transform).rect.width)throw new Exception("Storage sprite exceeds well.");}
            Capture(p.Canvas,"Surface-StorageAligned");
            p.LoadLevel(0);var solution=RuneAutoRules.Solve(p.Rules,p.State);
            foreach(var move in solution.route)
            {
                p.Tap(move.id,false);
                for(int i=0;i<180;i++){if(p.State.phase!=RunePhase.Playing)typeof(RunePlayPage).GetField("finishAt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(p,float.PositiveInfinity);p.AdvancePresentation(1f/60);}
                if(!p.gates[0].enabled)break;
            }
            if(p.gates[0].enabled||p.gateCounts[0].enabled)throw new Exception("Broken gate remained visible.");Capture(p.Canvas,"Surface-GateBroken");
            var playButton=p.cabinet.parent.Find("Settings").GetComponent<Button>();
            pages.ChangeImmediate(UIPageType.MainPage);var lobby=pages.GetPage<RuneLobbyPage>(UIPageType.MainPage);lobby.CanvasGroup.alpha=1;
            var lobbyButton=lobby.transform.Find("SafeContent/Settings").GetComponent<Button>();
            if(((Image)playButton.targetGraphic).sprite!=((Image)lobbyButton.targetGraphic).sprite)throw new Exception("Settings images differ.");
            Capture(lobby.Canvas,"Surface-SettingsIcon-Lobby");
            File.WriteAllText("Tools/RuneBreak/QA/surface-refinement.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="passed",landingPositionError=landingError,launchPositionError=launchError,identicalStorageSize=true,fiveStorageSlots=true,brokenGateHidden=true,sharedSettingsImage=true}));
            Debug.Log("SURFACE REFINEMENT PASS: no storage handoff jump; five pockets; real gate break; shared settings image.");
        }
        catch(Exception e){File.WriteAllText("Tools/RuneBreak/QA/surface-refinement.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString()}));throw;}
        finally{RunePreferences.Audio=audio;p.enabled=enabled;pages.ChangeImmediate(UIPageType.PlayPage,0);p.LoadLevel(0);}
    }
    public static void Live()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var p=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);
        File.WriteAllText("Temp/surface-live.json",Newtonsoft.Json.JsonConvert.SerializeObject(p.slots.Select(s=>new{slot=s.name,sprite=s.gem.sprite.name,position=s.gemRect.anchoredPosition.ToString(),size=s.gemRect.sizeDelta.ToString(),scale=s.gemRect.localScale.ToString(),color=s.gem.color.ToString(),enabled=s.gem.enabled,rect=s.gem.sprite.rect.ToString(),atlas=s.gem.sprite.texture.name,packed=s.gem.sprite.packed}),Newtonsoft.Json.Formatting.Indented));
        Capture(p.Canvas,"Surface-BeforeGateRevision");
    }
    public static void Capture(Canvas canvas,string name)
    {
        Canvas.ForceUpdateCanvases();var cam=canvas.worldCamera;var prior=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24);var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name+".png",texture.EncodeToPNG());}
        finally{cam.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    public static void Execute()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/Resources/Prefabs/PlayPage.prefab");
        try
        {
            var p=root.GetComponent<RunePlayPage>();
            var report=p.slots.Select(s=>new{slot=s.name,parent=s.transform.parent.name,position=((RectTransform)s.transform).anchoredPosition.ToString(),gemParent=s.gem.transform.parent.name,gemAnchor=s.gemRect.anchorMin.ToString(),gemPivot=s.gemRect.pivot.ToString(),gemPosition=s.gemRect.anchoredPosition.ToString(),size=s.gemRect.sizeDelta.ToString(),scale=s.gemRect.localScale.ToString(),rotation=s.gemRect.localEulerAngles.ToString(),preserveAspect=s.gem.preserveAspect,sprite=s.gem.sprite.name,rect=s.gem.sprite.rect.ToString(),type=s.gem.type.ToString(),fill=s.gem.fillAmount});
            File.WriteAllText("Temp/surface-inspection.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
