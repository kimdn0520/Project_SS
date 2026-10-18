using UnityEngine;
using UnityEngine.UI;

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
        public GameObject settings;
        public AudioSource audioSource;
        public AudioClip launchSound,breakSound;
        private readonly RuneBlock[] blocks=new RuneBlock[6];
        private readonly RuneFlight[] flights=new RuneFlight[6];
        private float clock,nextLaunch,resetAt,shake,touchUntil;
        private int launched,landed,holes;
        private Vector2 stageRest;
        private bool ready;
        public int Launched=>launched;
        public int Landed=>landed;
        private void Awake()
        {
            stageRest=stage.anchoredPosition;
            for(int i=0;i<6;i++)blocks[i]=new RuneBlock{id="lobby"+i,x=i%2==0?1:4,y=2+i/2*2,dir="N",color=i<2?"green":"gold"};
            ready=true;
        }
        private void OnEnable(){if(ready)RestartPreview();}
        private void OnDisable(){if(ready){CancelFlights();effects.Clear();stage.anchoredPosition=stageRest;}}
        private void CancelFlights(){for(int i=0;i<flights.Length;i++){if(flights[i]!=null)pool.ReturnCached(flights[i]);flights[i]=null;}}
        public void RestartPreview()
        {
            CancelFlights();clock=0;nextLaunch=1.3f;resetAt=0;launched=landed=holes=0;shake=touchUntil=0;effects.Clear();
            stage.anchoredPosition=stageRest;gate.enabled=true;gate.sprite=art.Gate(1,0,0,0);
            for(int i=0;i<6;i++){runes[i].enabled=true;runes[i].sprite=art.Rune(blocks[i].color,"N");runeRects[i].localScale=Vector3.one;Place(i,0);}
        }
        public void Interact()
        {
            if(settings.activeSelf)return;
            if(landed==6){RestartPreview();}
            touchUntil=clock+2.4f;nextLaunch=Mathf.Min(nextLaunch,clock+.04f);
            effects.AddWave(new Vector2(240,312),"gold",true);
            if(RunePreferences.Audio)audioSource.PlayOneShot(launchSound,RunePreferences.Volume*.45f);
        }
        private void Update()=>AdvancePresentation(Mathf.Min(.05f,Time.unscaledDeltaTime));
        public void AdvancePresentation(float dt)
        {
            if(!ready||settings.activeSelf)return;
            clock+=dt;bool reduced=RunePreferences.Reduced;
            // Wait for the green layer to visibly clear before launching the gold layer.
            if(launched<6&&clock>=nextLaunch&&(launched<2||landed>=2))Launch(reduced);
            for(int i=0;i<flights.Length;i++)
            {
                var f=flights[i];if(f==null)continue;f.Render(clock,reduced);
                if(clock<f.start+f.duration)continue;
                landed++;bool layerBreak=landed==2||landed==6;holes|=1<<(landed<=2?landed-1:landed-3);
                if(landed==2)holes=0;
                gate.enabled=landed<6;gate.sprite=art.Gate(1,0,landed<2?0:landed<6?1:2,landed==6?0:holes);
                effects.Emit(f.to,f.color,layerBreak,reduced);shake=layerBreak?4:2;
                if(layerBreak){effects.AddWave(new Vector2(240,59),f.color,true);if(clock<touchUntil&&RunePreferences.Audio)audioSource.PlayOneShot(breakSound,RunePreferences.Volume*.45f);}
                if(landed==6){resetAt=clock+2.2f;effects.Emit(new Vector2(240,245),"gold",true,reduced);}
                pool.ReturnCached(f);flights[i]=null;
            }
            for(int i=launched;i<6;i++)Place(i,reduced?0:Mathf.Sin(clock*2+i*.5f)*2);
            shake=Mathf.Max(0,shake-dt*18);float bob=reduced?0:Mathf.Round(Mathf.Sin(clock*1.5f)*3);
            stage.anchoredPosition=stageRest+new Vector2(reduced?0:Mathf.Round(Mathf.Sin(clock*110)*shake),bob);
            effects.Tick(dt);
            if(resetAt>0&&clock>=resetAt)RestartPreview();
        }
        private void Place(int i,float bob){var p=RuneFlight.Center(blocks[i]);runeRects[i].anchoredPosition=new Vector2(p.x,-p.y+Mathf.Round(bob));}
        private void Launch(bool reduced)
        {
            int i=launched;var f=pool.RentCached("LobbyFlights") as RuneFlight;if(f==null)return;
            int layer=i<2?0:1,index=i<2?i:i-2,max=i<2?2:4;
            var shot=new RuneShot{id=i,side=0,layer=layer,damageIndex=index,max=max,layerBreak=i==1||i==5,sealBreak=i==5};
            f.Begin(art,blocks[i],shot,clock,reduced);f.to=new Vector2(96+(index+.5f)*288/max,59);
            flights[i]=f;runes[i].enabled=false;launched++;nextLaunch=clock+(clock<touchUntil?.22f:.8f);
            effects.AddWave(RuneFlight.Center(blocks[i]),blocks[i].color,false);
        }
    }
}
