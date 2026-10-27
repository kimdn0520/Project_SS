using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RuneBreak
{
    // A self-contained attract sequence. It never changes campaign state or progress.
    public sealed class RuneLobbyShowcase : MonoBehaviour
    {
        public RuneArtLibrary art;
        public RuneLobbyFlightPool pool;
        public RuneFxGraphic effects;
        public Image gate;
        public Image[] runes;
        public RectTransform[] runeRects;
        public RectTransform stage;
        public RuneLobbyPage page;
        public TMP_Text gateCount;
        public Image[] layerPreviews;
        public float flightScale=1;
        private readonly RuneBlock[] blocks=new RuneBlock[6];
        private readonly RuneFlight[] flights=new RuneFlight[6];
        private float clock,nextLaunch,resetAt,shake;
        private int launched,landed,holes;
        private Vector2 stageRest;
        private bool ready;
        public int Launched=>launched;
        public int Landed=>landed;
        private void Awake()
        {
            stageRest=stage.anchoredPosition;
            for(int i=0;i<6;i++)blocks[i]=new RuneBlock{id="lobby"+i,x=i%2==0?1:4,y=(art.punchStyle?1:2)+i/2*2,dir="N",color=art.punchStyle?i<2?"green":i<4?"blue":"red":i<2?"green":"gold"};
            ready=true;
        }
        private void OnEnable(){if(ready)RestartPreview();}
        private void OnDisable(){if(ready){CancelFlights();effects.Clear();stage.anchoredPosition=stageRest;}}
        private void CancelFlights(){for(int i=0;i<flights.Length;i++){if(flights[i]!=null)pool.ReturnCached(flights[i]);flights[i]=null;}}
        public void RestartPreview()
        {
            CancelFlights();clock=0;nextLaunch=1.3f;resetAt=0;launched=landed=holes=0;shake=0;effects.Clear();
            stage.anchoredPosition=stageRest;gate.enabled=true;RenderGate();
            for(int i=0;i<6;i++){runes[i].enabled=true;runes[i].sprite=art.Rune(blocks[i].color,"N");runeRects[i].localScale=Vector3.one;Place(i,0);}
        }
        private void Update()=>AdvancePresentation(Mathf.Min(.05f,Time.unscaledDeltaTime));
        public void AdvancePresentation(float dt)
        {
            if(!ready||page.IsModalBlocked)return;
            clock+=dt;bool reduced=RunePreferences.Reduced;
            // Each color pair clears automatically before the next layer launches.
            if(launched<6&&clock>=nextLaunch&&(art.punchStyle?landed>=launched/2*2:launched<2||landed>=2))Launch(reduced);
            for(int i=0;i<flights.Length;i++)
            {
                var f=flights[i];if(f==null)continue;f.Render(clock,reduced);f.body.sizeDelta*=flightScale;
                if(clock<f.start+f.duration)continue;
                landed++;bool layerBreak=art.punchStyle?landed%2==0:landed==2||landed==6;holes|=1<<(landed<=2?landed-1:landed-3);
                if(landed==2)holes=0;
                gate.enabled=landed<6;RenderGate();
                if(art.punchStyle)effects.ShatterBlock(f.to,f.color,layerBreak,reduced);
                else effects.Emit(f.to,f.color,layerBreak,reduced);
                shake=layerBreak?4:2;
                if(layerBreak&&!art.punchStyle)effects.AddWave(new Vector2(240,59),f.color,true);
                if(landed==6)resetAt=clock+2.2f;
                pool.ReturnCached(f);flights[i]=null;
            }
            for(int i=launched;i<6;i++)Place(i,reduced?0:Mathf.Sin(clock*2+i*.5f)*2);
            shake=Mathf.Max(0,shake-dt*18);float bob=reduced?0:Mathf.Round(Mathf.Sin(clock*1.5f)*3);
            stage.anchoredPosition=stageRest+new Vector2(reduced?0:Mathf.Round(Mathf.Sin(clock*110)*shake),bob);
            effects.Tick(dt);
            if(resetAt>0&&clock>=resetAt)RestartPreview();
        }
        private void Place(int i,float bob){var p=RuneFlight.Center(blocks[i]);runeRects[i].anchoredPosition=new Vector2(p.x,-p.y+Mathf.Round(bob));}
        private void RenderGate()
        {
            if(!art.punchStyle){gate.sprite=art.Gate(1,0,landed<2?0:landed<6?1:2,landed==6?0:holes);return;}
            string color=landed<2?"green":landed<4?"blue":"red";
            gate.sprite=art.longGates[RuneArtLibrary.ColorIndex(color)*2];gate.enabled=landed<6;
            gateCount.enabled=landed<6;gateCount.text=(2-landed%2).ToString();
            for(int i=0;i<layerPreviews.Length;i++){int layer=landed/2+i+1;layerPreviews[i].enabled=layer<3;if(layer<3)layerPreviews[i].color=art.MidTone(layer==1?"blue":"red");}
        }
        private void Launch(bool reduced)
        {
            int i=launched;var f=pool.RentCached("LobbyFlights") as RuneFlight;if(f==null)return;
            int layer=i<2?0:1,index=i<2?i:i-2,max=i<2?2:4;
            if(art.punchStyle){layer=i/2;index=i%2;max=2;}
            var shot=new RuneShot{id=i,side=0,layer=layer,damageIndex=index,max=max,layerBreak=art.punchStyle?i%2==1:i==1||i==5,sealBreak=i==5};
            f.Begin(art,blocks[i],shot,clock,reduced);f.to=new Vector2(96+(index+.5f)*288/max,59);
            f.body.sizeDelta*=flightScale;
            flights[i]=f;runes[i].enabled=false;launched++;nextLaunch=clock+.8f;
            effects.AddWave(RuneFlight.Center(blocks[i]),blocks[i].color,false);
        }
    }
}
