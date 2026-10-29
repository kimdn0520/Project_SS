using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RuneBreak;

public static class RewardArrivalQA
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string message){if(!value)throw new Exception("REWARD ARRIVAL: "+message);}
    static int Count(RuneResultPanel p,string name)=>(int)typeof(RuneResultPanel).GetField(name,Private).GetValue(p);
    public static void Execute()=>Run(false).Forget();
    public static void Film()=>Run(true).Forget();
    static async UniTaskVoid Run(bool film)
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var play=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);
        bool enabled=play.enabled,audio=RunePreferences.Audio,reduced=RunePreferences.Reduced,exists=PlayerPrefs.HasKey(RuneCoinReward.SaveKey);string wallet=PlayerPrefs.GetString(RuneCoinReward.SaveKey,"");
        try
        {
            RunePreferences.Audio=false;play.enabled=false;PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,0);play.CanvasGroup.alpha=1;play.LoadLevel(0);
            if(film)
            {
                SetSize(540,960);await UniTask.Delay(200,ignoreTimeScale:true);RunePreferences.Reduced=false;
                var p=await Open(40);Directory.CreateDirectory("Temp/RewardArrivalFilm");
                for(int i=0;i<96;i++){if(i==24)p.ClaimReward();p.AdvancePresentation(1f/30);Capture(p.Canvas,"Temp/RewardArrivalFilm/"+i.ToString("D3")+".png");await UniTask.Yield();}
                File.WriteAllText("Temp/RewardArrivalFilm/done.txt","96");
            }
            else
            {
                int scenarios=0;var sizes=new[]{new Vector2Int(540,960),new Vector2Int(540,1200),new Vector2Int(768,1024),new Vector2Int(1280,720)};
                foreach(var size in sizes)
                {
                    SetSize(size.x,size.y);await UniTask.Delay(200,ignoreTimeScale:true);
                    foreach(bool lowMotion in size==sizes[0]?new[]{false,true}:new[]{false})
                    foreach(int fps in size==sizes[0]?new[]{30,60,120}:new[]{60})
                    {
                        RunePreferences.Reduced=lowMotion;var p=await Open(40);p.AdvancePresentation(1);
                        var shade=p.transform.Find("Curtain").GetComponent<Image>();Check(Mathf.Abs(shade.color.a-230f/255)<.0001f,"shade alpha 230");
                        Check(p.titleDepth.text=="Clear!"&&p.title.fontSharedMaterial.name=="Fredoka-ClearTitle","authored wordmark");
                        Check(p.coinInstances.Length==12&&p.rewardEffects.rewardFlights.SequenceEqual(p.coinInstances),"existing twelve-flight pool refs");
                        Check(p.swooshSound!=null&&p.coinSound!=null,"authored audio");
                        Check(p.coinRoot.parent==p.walletRect.parent&&p.rewardEffects.transform.parent==p.coinRoot.parent,"shared coordinate space");
                        Check(p.coinRoot.GetComponentsInParent<Mask>().Length==0&&p.coinRoot.GetComponentsInParent<RectMask2D>().Length==0,"no wallet clipping mask");
                        if(scenarios==0)Capture(p.Canvas,"Tools/RuneBreak/QA/RewardArrival-Clear.png");
                        int children=p.GetComponentsInChildren<Transform>(true).Length,before=RuneCoinReward.Balance;var center=p.coinTarget.position;
                        p.ClaimReward();p.ClaimReward();Check(RuneCoinReward.Balance==before+40,"payment once at claim");p.OnEscape();p.ChoosePrimary();Check(PopupManager.CurrentPopup==p,"navigation blocked during collection");
                        bool contactSeen=false;int previous=0;
                        for(int frame=0;frame<fps*2;frame++)
                        {
                            p.AdvancePresentation(1f/fps);Canvas.ForceUpdateCanvases();
                            Check(Vector3.Distance(center,p.coinTarget.position)<.001f&&p.walletRect.localScale==Vector3.one,"stationary destination during punch");
                            foreach(var f in p.coinInstances.Where(c=>c.gameObject.activeSelf&&c.image.enabled))
                            {
                                if(f.image.depth<0)continue; // Unity assigns depth after the newly enabled renderer is submitted.
                                Check(f.image.depth>p.coinTarget.GetComponent<Image>().depth,"coin renders in front of wallet icon: "+f.image.depth+" / "+p.coinTarget.GetComponent<Image>().depth);
                            }
                            int arrivals=Count(p,"arrived");if(arrivals>previous)
                            {
                                contactSeen=true;var target=(Vector2)p.coinRoot.InverseTransformPoint(center);
                                Check(p.coinInstances.Any(c=>c.gameObject.activeSelf&&c.image.enabled&&Vector2.Distance(c.rect.anchoredPosition,target)<.01f),"visible centered absorb at contact");
                                if(previous==0)
                                {
                                    Capture(p.Canvas,$"Tools/RuneBreak/QA/RewardArrival-Contact-{size.x}x{size.y}.png");
                                    var coin=p.coinInstances.First(c=>c.gameObject.activeSelf&&c.image.enabled&&Vector2.Distance(c.rect.anchoredPosition,target)<.01f);
                                    Check(coin.image.canvas==p.coinTarget.GetComponent<Image>().canvas&&coin.image.depth>p.coinTarget.GetComponent<Image>().depth,"rendered contact is in front: "+coin.image.depth+" / "+p.coinTarget.GetComponent<Image>().depth);
                                }
                            }
                            previous=arrivals;
                            if(p.IsCollecting)Check(!p.primaryButton.gameObject.activeSelf&&!p.homeButton.gameObject.activeSelf,"navigation waits for absorption");
                        }
                        Check(contactSeen&&Count(p,"arrived")==12&&Count(p,"completed")==12,"exactly twelve arrivals and returns");
                        Check(p.RewardReady&&p.coinInstances.All(c=>!c.gameObject.activeSelf),"collection complete and pool empty");
                        Check(p.balanceLabel.text==RuneCoinReward.Balance.ToString(),"display matches saved balance");
                        Check(p.GetComponentsInChildren<Transform>(true).Length==children,"no runtime hierarchy growth");
                        p.ChooseHome();await UniTask.WaitUntil(()=>!PopupManager.Instance.IsBlockingInput);Check(PopupManager.Instance.LiveInstanceCount==0,"popup destroyed");scenarios++;
                    }
                }
                File.WriteAllText("Tools/RuneBreak/QA/reward-arrival.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="passed",scenarios,dimAlpha=230,foregroundCoins=true,stableContact=true,oneCredit=true,twelveArrivals=true,poolCleanup=true,noHierarchyGrowth=true}));
                Debug.Log("REWARD ARRIVAL QA PASS: "+scenarios+" motion/layout scenarios.");
            }
        }
        catch(Exception e){File.WriteAllText("Tools/RuneBreak/QA/reward-arrival.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString()}));Debug.LogException(e);}
        finally{PopupManager.Clear();if(exists)PlayerPrefs.SetString(RuneCoinReward.SaveKey,wallet);else PlayerPrefs.DeleteKey(RuneCoinReward.SaveKey);PlayerPrefs.Save();RunePreferences.Audio=audio;RunePreferences.Reduced=reduced;play.enabled=enabled;SetSize(540,960);pages.ChangeImmediate(UIPageType.PlayPage,0);play.LoadLevel(0);}
    }
    static async UniTask<RuneResultPanel> Open(int amount)
    {
        PopupManager.Show(RuneResultPanel.PrefabName,new RuneResultData{level=1,won=true,hasNext=true,reward=new RuneCoinReward(amount)});
        await UniTask.Yield();await UniTask.WaitUntil(()=>!PopupManager.IsChanging);await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        var p=(RuneResultPanel)PopupManager.CurrentPopup;p.enabled=false;Canvas.ForceUpdateCanvases();return p;
    }
    static void SetSize(int width,int height)
    {
        var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameViewSizes");var singleton=type.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        var group=type.GetMethod("GetGroup").Invoke(singleton,new[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null),index=-1;
        for(int i=0;i<count;i++){var item=group.GetType().GetMethod("GetGameViewSize").Invoke(group,new object[]{i});if((int)item.GetType().GetProperty("width").GetValue(item)==width&&(int)item.GetType().GetProperty("height").GetValue(item)==height){index=i;break;}}
        if(index<0){var item=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,height,$"Reward QA {width}x{height}"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{item});index=count;}
        var vt=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(vt);vt.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,index);view.Repaint();
    }
    static void Capture(Canvas canvas,string path)
    {
        Canvas.ForceUpdateCanvases();var camera=canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24);var tex=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
