using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    public sealed class RunePlayPage : SceneBase, IPageBackHandler
    {
        public RuneLevelCatalog catalog;
        public RuneArtLibrary art;
        public PageManager pages;
        public PoolManager pool;
        public RectTransform cabinet;
        public Image background;
        public Image[] gates,blocks,gateFlashes;
        public RectTransform[] blockRects;
        public RuneSlotView[] slots;
        public RuneFxGraphic effects;
        public RuneMechanicGraphic mechanicLines;
        public Image[] chainWraps,stackMounts,stackPreviews,mirrors,mirrorGlints;
        public RectTransform[] chainRects,stackRects,previewRects,mirrorRects;
        public TMP_Text mechanicHint;
        public TMP_Text occupancy,stageNumber;
        public RectTransform highlightRect,pathRect;
        public Image highlight,pathImage;
        public TMP_Text resultTitle,resultDescription,resultAction;
        public GameObject settings,result,resultHome;
        public AudioSource audioSource;
        public AudioClip[] sounds;
        public RuneState State {get;private set;}
        public RuneRules Rules {get;private set;}
        public int LevelIndex {get;private set;}
        public int ActiveFlights=>flights.Count;
        private RuneState visual;
        private readonly Stack<RuneState> history=new Stack<RuneState>();
        private readonly List<RuneFlight> flights=new List<RuneFlight>(48);
        private readonly bool[] pending=new bool[28];
        private readonly int[] holes=new int[2];
        private readonly float[] flashUntil=new float[2];
        private readonly float[] releaseAt=new float[28],revealAt=new float[28],mirrorFlashUntil=new float[8];
        private float clock,shake,lastImpact=-99,finishAt=-1,hintUntil,bumpAt=-99;
        private int chain,bestChain,hintId=-1,hoverId=-1,bumpId=-1,bumpBlocker=-1,pressedId=-1;
        private bool hintSlot,hoverSlot,shownResult;
        private readonly Vector2 cabinetOrigin=new Vector2(30,-165);

        public override void OnWillEnter(object param){LoadLevel(param is int i?i:Mathf.Min(PlayerPrefs.GetInt("RuneBreak.Next",0),catalog.CampaignCount-1));}
        public override void OnDidEnter(){}
        public override void OnWillLeave(){settings.SetActive(false);CancelFlights();effects.Clear();}
        public override void OnDidLeave(){}
        public bool HandleBack(){if(settings.activeSelf){settings.SetActive(false);return true;}Home();return true;}
        public void Home()=>pages.Change(UIPageType.MainPage);
        public void OpenSettings()=>settings.SetActive(true);
        public void Restart()=>LoadLevel(LevelIndex);
        public void Previous()=>LoadLevel(Mathf.Max(0,LevelIndex-1));
        public void Next()=>LoadLevel(Rules.Level.demo?(LevelIndex+1<catalog.levels.Length?LevelIndex+1:catalog.CampaignCount):(LevelIndex+1)%catalog.CampaignCount);
        public void ResultPrimary(){if(State.phase==RunePhase.Won){if(LevelIndex+1==(Rules.Level.demo?catalog.levels.Length:catalog.CampaignCount))Home();else Next();}else if(Rules.Level.demo)Restart();else Home();}
        public void LoadLevel(int index)
        {
            CancelFlights();LevelIndex=Mathf.Clamp(index,0,catalog.levels.Length-1);Rules=new RuneRules(catalog.levels[LevelIndex]);State=Rules.Initial();history.Clear();clock=0;chain=bestChain=0;
            ResetPresentation();stageNumber.text=$"STAGE {LevelIndex+1:00}";
            if(mechanicHint!=null){mechanicHint.gameObject.SetActive(Rules.Level.demo);mechanicHint.text=Rules.Level.desc;}
            if(mirrors!=null)for(int i=0;i<mirrors.Length;i++)
            {
                bool valid=Rules.Level.mirrors!=null&&i<Rules.Level.mirrors.Length;mirrors[i].gameObject.SetActive(valid);mirrorGlints[i].gameObject.SetActive(valid);if(!valid)continue;
                var m=Rules.Level.mirrors[i];mirrors[i].sprite=m.slash?art.mirrorSlash:art.mirrorBackslash;var p=RuneFlight.Turn(new RuneCell(m.x,m.y));mirrorRects[i].anchoredPosition=new Vector2(p.x,-p.y-2);((RectTransform)mirrorGlints[i].transform).anchoredPosition=new Vector2(p.x,-p.y-2);
            }
            for(int i=0;i<blocks.Length;i++)
            {
                bool valid=i<Rules.Level.blocks.Length;blocks[i].gameObject.SetActive(valid);if(!valid)continue;var b=Rules.Level.blocks[i];blocks[i].sprite=art.Rune(b.color,b.dir);
            }Refresh();
        }
        private void CancelFlights(){foreach(var f in flights)pool.ReturnCached(f);flights.Clear();Array.Clear(pending,0,pending.Length);}
        private void ResetPresentation()
        {
            CancelFlights();visual=State.Copy();effects.Clear();shake=0;finishAt=-1;shownResult=false;result.SetActive(false);hintId=hoverId=pressedId=-1;chain=0;lastImpact=-99;cabinet.anchoredPosition=cabinetOrigin;
            // LoadLevel resets the clock. No animation may retain a timestamp or target from the old board.
            bumpId=bumpBlocker=-1;bumpAt=-99;hintUntil=0;hintSlot=hoverSlot=false;
            for(int i=0;i<releaseAt.Length;i++){releaseAt[i]=0;revealAt[i]=-100;}Array.Clear(mirrorFlashUntil,0,mirrorFlashUntil.Length);
            highlight.enabled=pathImage.enabled=false;
            for(int i=0;i<blockRects.Length;i++)
            {
                blockRects[i].localScale=Vector3.one;blockRects[i].localRotation=Quaternion.identity;blockRects[i].sizeDelta=new Vector2(48,48);
                if(i<Rules.Level.blocks.Length){var p=RuneFlight.Center(Rules.Level.blocks[i]);if(Rules.IsVisible(State,i)&&Rules.Underneath(State,i)>=0)p-=new Vector2(4,5);blockRects[i].anchoredPosition=new Vector2(p.x,-p.y-2);}
            }
            for(int side=0;side<2;side++){holes[side]=0;var g=Rules.Gate(visual,side);for(int i=0;i<g.damage;i++)holes[side]|=1<<i;flashUntil[side]=0;gateFlashes[side].color=Color.clear;}RenderGates();
            RenderMechanics();
        }
        public void Tap(int index,bool slot)
        {
            if(State==null||settings.activeSelf||shownResult)return;
            int id=slot?State.slots[index]:index;if(id<0)return;
            if(slot&&pending[id])return;
            var move=new RuneMove(slot?RuneMoveKind.Slot:RuneMoveKind.Board,id);
            if(!Rules.Apply(State,move,out var next,out var shot,out string reason))
            {
                Sound(5);
                if(reason=="blocked"||reason=="chained"){bumpId=id;bumpAt=clock;bumpBlocker=-1;uint blockers=Rules.Blockers[id]&State.board;for(int j=0;j<Rules.Level.blocks.Length;j++)if((blockers&(1u<<j))!=0){bumpBlocker=j;break;}}
                if(reason=="chained")
                {
                    int side=Rules.ChainSides[id];flashUntil[side]=clock+.3f;var b=Rules.Level.blocks[id];effects.AddWave(RuneFlight.Center(b),Rules.Level.Layers(side)[b.chainLayer].color,false);
                }
                return;
            }
            history.Push(State);State=next;hintId=-1;hoverId=-1;pressedId=-1;
            var flight=pool.RentCached("RuneFlights") as RuneFlight;
            if(flight==null)throw new InvalidOperationException("RuneFlights must contain at least 48 inspector-baked instances.");
            flight.Begin(art,Rules.Level.blocks[id],shot,slot?clock:Mathf.Max(clock,releaseAt[id]),RunePreferences.Reduced,Rules.Routes[id]);
            if(shot.side>=0)
            {
                for(int i=flights.Count-1;i>=0;i--){var old=flights[i];if(old.shot.side!=shot.side||old.shot.store||old.shot.overflow)continue;flight.duration=Mathf.Max(flight.duration,old.start+old.duration+(old.shot.layer==shot.layer?.055f:.13f)-flight.start);break;}
            }
            for(int i=0;i<Rules.Level.blocks.Length;i++)
            {
                if((shot.unlocked&(1u<<i))!=0)releaseAt[i]=flight.start+flight.duration;
                if((shot.revealed&(1u<<i))!=0){revealAt[i]=clock+.04f;effects.Reveal(RuneFlight.Center(Rules.Level.blocks[i]),Rules.Level.blocks[i].color,RunePreferences.Reduced);Sound(10);}
            }
            flights.Add(flight);if(shot.store)pending[id]=true;
            Sound(shot.relaunch?1:0);Refresh();
        }
        public void Undo()
        {
            if(history.Count==0)return;State=history.Pop();ResetPresentation();Refresh();
        }
        public void Hint()
        {
            if(State.phase!=RunePhase.Playing)return;
            var solution=RuneSolver.Solve(Rules,State);
            if(solution.status==RuneSolveStatus.Dead){hintId=-1;return;}
            if(solution.status==RuneSolveStatus.Limit)return;
            if(solution.route.Count==0)return;var m=solution.route[0];hintId=m.id;hintSlot=m.kind==RuneMoveKind.Slot;hintUntil=clock+5;
        }
        public void Hover(int index,bool slot){if(State==null)return;hoverId=slot?State.slots[index]:index;hoverSlot=slot;}
        public void ClearHover()=>hoverId=-1;
        public void Press(int index,bool slot,bool pressed){if(!slot)pressedId=pressed?index:-1;}
        private void Refresh()
        {
            for(int i=0;i<Rules.Level.blocks.Length;i++)blocks[i].gameObject.SetActive(Rules.IsVisible(State,i));
            for(int i=0;i<slots.Length;i++)slots[i].Render(art,Rules,State,i,State.slots[i]>=0&&pending[State.slots[i]]);
            occupancy.text=$"{State.Filled}/5";occupancy.color=RuneArtLibrary.Hex(State.Filled==5?"ffb38f":"d9dfc8");
            RenderMechanics();
        }
        private void RenderMechanics()
        {
            if(chainWraps==null||State==null)return;
            for(int i=0;i<chainWraps.Length;i++)
            {
                bool valid=i<Rules.Level.blocks.Length;bool visible=valid&&Rules.IsVisible(State,i);
                chainWraps[i].enabled=valid&&Rules.IsChained(visual,i)&&(visible||releaseAt[i]>clock);
                int below=visible?Rules.Underneath(State,i):-1;stackMounts[i].enabled=stackPreviews[i].enabled=below>=0;
                if(!valid)continue;var pos=RuneFlight.Center(Rules.Level.blocks[i]);chainRects[i].anchoredPosition=new Vector2(pos.x,-pos.y-2);
                stackRects[i].anchoredPosition=new Vector2(pos.x,-pos.y-2);
                previewRects[i].anchoredPosition=new Vector2(pos.x+10,-pos.y-12);
                if(below>=0){var b=Rules.Level.blocks[below];stackPreviews[i].sprite=art.Rune(b.color,b.dir);}
            }
            if(mirrorGlints!=null)for(int i=0;i<mirrorGlints.Length;i++)
            {
                float flash=Mathf.Clamp01((mirrorFlashUntil[i]-clock)/.23f);mirrorGlints[i].color=new Color(1,1,1,flash*.85f);mirrorGlints[i].rectTransform.localScale=Vector3.one*(1+flash*.7f);
            }
            if(mechanicLines!=null)mechanicLines.Render(Rules,State,visual,clock,hoverSlot?-1:hoverId);
        }
        private void RenderGates(){for(int i=0;i<2;i++){var g=Rules.Gate(visual,i);gates[i].sprite=art.Gate(LevelIndex,i,g.layer,holes[i]);}}
        private void Complete(RuneFlight f)
        {
            var s=f.shot;if(s.store){pending[s.id]=false;effects.AddWave(f.to,f.color,false);Sound(2);Refresh();return;}
            if(s.overflow){effects.Emit(f.to,f.color,true,RunePreferences.Reduced);effects.Overflow();Sound(6);return;}
            var gate=Rules.Gate(visual,s.side);float coord=s.side==0?f.to.x-96:f.to.y-84;int candidate=Mathf.Clamp(Mathf.FloorToInt(coord/288*gate.max),0,gate.max-1),segment=-1,distance=int.MaxValue;
            for(int i=0;i<gate.max;i++)if((holes[s.side]&(1<<i))==0&&Mathf.Abs(i-candidate)<distance){segment=i;distance=Mathf.Abs(i-candidate);}if(segment>=0)holes[s.side]|=1<<segment;
            visual.spent[s.side]=s.spent;if(s.layerBreak)holes[s.side]=0;flashUntil[s.side]=clock+.2f;
            if(s.unlocked!=0)
            {
                for(int i=0;i<Rules.Level.blocks.Length;i++)if((s.unlocked&(1u<<i))!=0)
                {
                    var point=RuneFlight.Center(Rules.Level.blocks[i]);var anchor=s.side==0?new Vector2(point.x,72):new Vector2(393,point.y);
                    effects.ReleaseChain(point,anchor,f.color,RunePreferences.Reduced);
                }
                Sound(8);shake=Mathf.Max(shake,6);
            }
            chain=clock-lastImpact<1.15f?chain+1:1;lastImpact=clock;bestChain=Mathf.Max(bestChain,chain);shake=Mathf.Max(shake,s.layerBreak?5:2.4f);
            effects.Emit(f.to,f.color,s.layerBreak,RunePreferences.Reduced);
            effects.Chain(chain);effects.Label(s.unlocked!=0?"CHAIN BREAK!":s.layerBreak?(s.sealBreak?"SEAL BREAK!":"LAYER BREAK!"):chain>1?"HIT "+chain:"CRACK!",new Vector2(Mathf.Clamp(f.to.x,115,352),Mathf.Clamp(f.to.y,40,415)-13),f.color,s.layerBreak,s.layerBreak?.78f:.42f);
            if(s.layerBreak)
            {
                var next=Rules.Gate(visual,s.side);if(next.color!=null)
                {
                    string title=next.color=="red"?"EMBER":next.color=="blue"?"TIDE":next.color=="gold"?"SPARK":"GROVE";
                    effects.Label(title+" OPEN",s.side==0?new Vector2(240,94):new Vector2(330,360),next.color,false,1.1f,.05f);
                    for(int i=0;i<5;i++)if(State.slots[i]>=0&&Rules.Level.blocks[State.slots[i]].color==next.color)effects.AddWave(RuneFlight.Slot(i),next.color,true);
                }
            }
            Sound(s.layerBreak?4:3);RenderGates();Refresh();
        }
        private void Sound(int i){if(RunePreferences.Audio&&audioSource!=null&&i<sounds.Length)audioSource.PlayOneShot(sounds[i],RunePreferences.Volume);}
        private void Update()=>AdvancePresentation(Mathf.Min(.035f,Time.unscaledDeltaTime));
        // Explicit delta supports deterministic visual QA; rule state changes only in Tap.
        public void AdvancePresentation(float dt)
        {
            if(State==null)return;
            if(settings.activeSelf)return;
            clock+=dt;bool reduced=RunePreferences.Reduced;
            foreach(var f in flights)while(f.TakeMirrorImpact(clock,out var point))
            {
                effects.Reflect(point,f.color,reduced);Sound(9);shake=Mathf.Max(shake,1.5f);
                for(int i=0;i<(Rules.Level.mirrors?.Length??0);i++){var m=Rules.Level.mirrors[i];if(Vector2.Distance(point,RuneFlight.Turn(new RuneCell(m.x,m.y)))<1)mirrorFlashUntil[i]=clock+.23f;}
            }
            // Completion order is due-time order, independent of render frame rate.
            while(true){RuneFlight earliest=null;foreach(var f in flights)if(clock>=f.start+f.duration&&(earliest==null||f.start+f.duration<earliest.start+earliest.duration))earliest=f;if(earliest==null)break;flights.Remove(earliest);Complete(earliest);pool.ReturnCached(earliest);}
            foreach(var f in flights)f.Render(clock,reduced);
            background.sprite=art.cabinets[reduced?0:Mathf.FloorToInt(clock*9)%3];
            for(int i=0;i<slots.Length;i++)slots[i].Tick(clock,reduced);
            float bumpAge=clock-bumpAt;
            bool bumping=!reduced&&bumpId>=0&&bumpId<Rules.Level.blocks.Length&&bumpAge>=0&&bumpAge<.3f;
            for(int i=0;i<Rules.Level.blocks.Length;i++)
            {
                var b=Rules.Level.blocks[i];var p=RuneFlight.Center(b);float squash=pressedId==i?.09f:0;
                if(Rules.IsVisible(State,i)&&Rules.Underneath(State,i)>=0)p-=new Vector2(4,5);
                if(bumping&&(i==bumpId||i==bumpBlocker)){float t=bumpAge/.3f;RuneRules.Direction(Rules.Level.blocks[bumpId].dir,out int dx,out int dy);p+=new Vector2(dx,dy)*Mathf.Sin(t*Mathf.PI*3)*(1-t)*4;squash+=Mathf.Sin(t*Mathf.PI)*(i==bumpId?.1f:-.06f);}
                float reveal=Mathf.Clamp01((clock-revealAt[i])/.28f),pop=1;
                if(!reduced&&reveal<1){float ease=1-Mathf.Pow(1-reveal,3);p+=new Vector2(10,12)*(1-ease);pop=Mathf.Lerp(.55f,1,ease)+Mathf.Sin(reveal*Mathf.PI)*.1f;}
                blockRects[i].anchoredPosition=new Vector2(p.x,-p.y-2);blockRects[i].localScale=new Vector3((1+squash)*pop,(1-squash)*pop,1);
            }
            for(int i=0;i<2;i++){var c=RuneArtLibrary.Light(Rules.Gate(visual,i).color??"green");c.a=Mathf.Max(0,(flashUntil[i]-clock)/.2f)*.38f;gateFlashes[i].color=c;}
            effects.Tick(dt);shake=Mathf.Max(0,shake-dt*25);cabinet.anchoredPosition=cabinetOrigin+(reduced?Vector2.zero:new Vector2(Mathf.Round((UnityEngine.Random.value-.5f)*shake*2),Mathf.Round((UnityEngine.Random.value-.5f)*shake*2)));
            RenderHighlight();
            RenderMechanics();
            if(State.phase!=RunePhase.Playing&&flights.Count==0&&!shownResult){if(finishAt<0)finishAt=clock;if(clock-finishAt>.48f)ShowResult();}
        }
        private void RenderHighlight()
        {
            int id=hintId>=0&&clock<hintUntil?hintId:RunePreferences.Paths?hoverId:-1;bool slot=id==hintId?hintSlot:hoverSlot;
            highlight.enabled=false;pathImage.enabled=false;if(id<0||State.phase!=RunePhase.Playing)return;
            Vector2 p;
            if(slot){int s=Array.IndexOf(State.slots,id);if(s<0)return;p=RuneFlight.Slot(s);}
            else {if((State.board&(1u<<id))==0)return;p=RuneFlight.Center(Rules.Level.blocks[id]);}
            highlight.enabled=true;highlightRect.anchoredPosition=new Vector2(p.x,-p.y);highlightRect.sizeDelta=Vector2.one*(slot?62:44);highlight.color=new Color(1,.94f,.71f,.18f+.12f*Mathf.Sin(clock*7));
            if(!slot&&Rules.Routes[id].turns.Length==0){var end=RuneFlight.Exit(Rules.Level.blocks[id]);for(int j=0;j<Rules.Level.blocks.Length;j++)if((Rules.Blockers[id]&State.board&(1u<<j))!=0){var q=RuneFlight.Center(Rules.Level.blocks[j]);if(Vector2.Distance(p,q)<Vector2.Distance(p,end))end=q;}
                var delta=end-p;pathImage.enabled=true;pathRect.anchoredPosition=new Vector2((p.x+end.x)/2,-(p.y+end.y)/2);pathRect.sizeDelta=new Vector2(delta.magnitude,2);pathRect.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);pathImage.color=Rules.IsFree(State,id)?new Color(.84f,.81f,.62f,.45f):new Color(.93f,.57f,.45f,.65f);}
        }
        private void ShowResult()
        {
            shownResult=true;bool won=State.phase==RunePhase.Won;result.SetActive(true);resultTitle.text=won?"결계를 돌파했어요!":"보관칸이 넘쳤어요";resultDescription.text=won?$"직접 타격 {State.direct}  ·  재발사 {State.relaunch}\n최고 연속 타격 {bestChain}":"다섯 칸을 채운 상태에서\n추가 보관이 발생했어요.";resultAction.text=won?"다음 방으로":"홈으로";
            resultHome.SetActive(won);
            if(Rules.Level.demo)resultAction.text=won?(LevelIndex+1<catalog.levels.Length?"다음 기믹 체험":"홈으로"):"다시 체험";
            else if(won&&LevelIndex+1>=catalog.CampaignCount)resultAction.text="홈으로";
            if(won&&!Rules.Level.demo){int clears=PlayerPrefs.GetInt("RuneBreak.Clears",0)|(1<<LevelIndex);PlayerPrefs.SetInt("RuneBreak.Clears",clears);PlayerPrefs.SetInt("RuneBreak.Next",Mathf.Min(LevelIndex+1,catalog.CampaignCount-1));PlayerPrefs.Save();}Sound(won?7:6);
        }
    }
}
