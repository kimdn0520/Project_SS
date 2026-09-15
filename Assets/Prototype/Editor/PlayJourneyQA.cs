using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace ProjectSS.Expedition.Editor
{
    public static class PlayJourneyQA
    {
        public static void Start(){Run().Forget();}
        static async UniTaskVoid Run()
        {
            var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
            string backup=JsonUtility.ToJson(page.Model.Data);float timeScale=Time.timeScale;
            var results=new System.Collections.Generic.List<string>();
            try
            {
                page.OpenMenu(0);page.Model.Data.autoMine=false;page.Model.Data.autoBattle=false;
                page.Model.Data.chest=true;page.miningView.SetChest(true);
                await UniTask.Delay(300,cancellationToken:page.destroyCancellationToken);
                int count=page.Model.Data.inventory.Sum();page.Dig();
                if(!page.ChestOpening)throw new Exception("DIG did not open chest");
                await UniTask.Delay(650,cancellationToken:page.destroyCancellationToken);
                ExpeditionValidation.Capture();File.Copy("PrototypeQA/latest.png","PrototypeQA/chest-open.png",true);
                if(page.Model.Data.inventory.Sum()!=count+1)throw new Exception("Chest reward not granted after opening");
                if(page.miningView.GetComponentsInChildren<LineRenderer>(true).Any(l=>l.enabled))throw new Exception("Chest used rock cracks");
                await UniTask.Delay(650,cancellationToken:page.destroyCancellationToken);
                if(page.ChestOpening||page.Model.Data.inventory.Sum()!=count+1)throw new Exception("Chest completion duplicated reward");
                results.Add("PASS: chest lid opens, one item emerges, no cracks or duplicate reward");
                page.Model.Retreat();page.Model.Data.cleared=0;
                page.Model.Data.iron=page.Model.Data.crystal=page.Model.Data.relic=500;
                for(int i=3;i<page.catalog.gear.Length;i++)page.Model.Data.inventory[i]=5;
                page.Model.Equip(4,0);page.Model.Equip(6,1);page.Model.Equip(7,2);
                for(int hero=0;hero<3;hero++){page.Model.Equip(8,hero);page.Model.Equip(9,hero);page.Model.Equip(10,hero);}
                page.OnWillEnter(null);page.Model.Data.autoBattle=true;Time.timeScale=3;
                float deadline=Time.realtimeSinceStartup+180;int last=-1;bool walk=false,fight=false;
                while(page.Model.Data.cleared<10)
                {
                    if(Time.realtimeSinceStartup>deadline)throw new Exception("Live chapter did not finish");
                    if(page.State==PlayPage.Journey.Walking)walk=true;
                    if(page.State==PlayPage.Journey.Fighting && page.Model.Data.cleared!=last)
                    {
                        fight=true;last=page.Model.Data.cleared;
                        if(last==0||last==8||last==9){ExpeditionValidation.Capture();File.Copy("PrototypeQA/latest.png",$"PrototypeQA/stage-1-{last+1}.png",true);}
                        File.WriteAllText("PrototypeQA/journey-progress.txt",$"Live stage 1-{last+1}, {page.State}");
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update,page.destroyCancellationToken);
                }
                if(!walk||!fight||last!=9||page.Model.Region!=2||page.Model.Wave!=1)throw new Exception("Chapter transition skipped a stage");
                results.Add("PASS: live walking/encounters/basic attacks advance 1-1 through 1-10 boss into 2-1");
                page.Model.Data.autoBattle=false;
                await UniTask.Delay(1600,cancellationToken:page.destroyCancellationToken);
                if(page.State==PlayPage.Journey.Waiting)throw new Exception("Legacy auto OFF stopped automatic encounters");
                results.Add("PASS: legacy auto OFF does not stop automatic encounters");
                Time.timeScale=1;page.Refresh();ExpeditionValidation.Capture();File.Copy("PrototypeQA/latest.png","PrototypeQA/stage-2-1.png",true);
                // Sample a full tile cycle, including the exact seam, while preserving every baked tile.
                var positions=page.scrolling.Select(t=>t.localPosition).ToArray();
                for(int sample=0;sample<5;sample++)
                {
                    float offset=sample*1.9f;
                    for(int i=0;i<page.scrolling.Length;i++){var p=positions[i];p.x=(i%2)*7.6f-offset;if(p.x < -7.6f)p.x+=15.2f;page.scrolling[i].localPosition=p;}
                    ExpeditionValidation.Capture();File.Copy("PrototypeQA/latest.png",$"PrototypeQA/scroll-{sample}.png",true);
                    var sky=page.scrolling.Where(t=>t.name.StartsWith("Sky")).Select(t=>t.GetComponentInChildren<SpriteRenderer>().bounds).OrderBy(b=>b.min.x).ToArray();
                    if(sky[0].max.x<sky[1].min.x-.001f||sky[0].min.x> -3.6f||sky[1].max.x<3.6f)throw new Exception("Sky gap during repeat");
                }
                for(int i=0;i<page.scrolling.Length;i++)page.scrolling[i].localPosition=positions[i];
                results.Add("PASS: five scrolling offsets including wrap seam cover the viewport without sky gaps");
                File.WriteAllLines("PrototypeQA/live-journey.txt",results);Debug.Log(string.Join("\n",results));
            }
            catch(Exception e){File.WriteAllText("PrototypeQA/live-journey.txt","FAIL: "+e);Debug.LogException(e);}
            finally
            {
                Time.timeScale=timeScale;
                if(page!=null){page.Model.Retreat();JsonUtility.FromJsonOverwrite(backup,page.Model.Data);page.OnWillEnter(null);page.SendMessage("Persist");}
            }
        }
    }
}
