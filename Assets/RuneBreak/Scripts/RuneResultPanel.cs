using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    public enum RuneResultAction { Home, Next, Retry }
    public sealed class RuneResultData
    {
        public int level;
        public bool won,demo,hasNext;
        public RuneCoinReward reward;
    }
    public sealed class RuneResultPanel : BasePopupHandler
    {
        public const string PrefabName="ResultPopup";
        public override string PopupName=>PrefabName;
        public RectTransform panel,primaryRect,coinRoot,coinTarget,walletRect,heroRect,raysRect;
        public TMP_Text title,description,primaryLabel,balanceLabel;
        public Button primaryButton,homeButton,claimButton;
        public Image primaryFace,hero,rays;
        public Sprite nextSprite,homeSprite;
        public PoolManager coinPool;
        public RuneCoinFlight[] coinInstances;
        public RuneRewardPile coinPile;
        public RuneFxGraphic rewardEffects;
        public RectTransform titleGroup;
        public TMP_Text titleDepth;
        public AudioSource rewardAudio;
        public AudioClip coinSound,swooshSound;
        [Header("English copy")]
        public string clearTitle="Clear!",failureTitle="Try Again";
        [TextArea] public string clearMessage="Level {0} complete!\nReady for the next challenge?";
        [TextArea] public string finalMessage="All levels complete!\nThanks for playing.";
        [TextArea] public string failureMessage="Storage is full.\nGive it another try!";
        public string nextLabel="Next",homeLabel="Home",retryLabel="Try Again",nextDemoLabel="Next";
        private RuneResultAction primaryAction;
        private RuneCoinReward reward;
        private bool won,collecting,claimed;
        private float clock,impactAge=10,navAt=-1;
        private int initialBalance,targetBalance,arrived,completed;
        private readonly RuneCoinFlight[] active=new RuneCoinFlight[16];
        public bool IsCollecting=>collecting;
        public bool RewardReady=>claimed&&!collecting;
        protected override object DefaultCloseParam=>RuneResultAction.Home;
        public override void OnWillEnter(object param)
        {
            base.OnWillEnter(param);var data=(RuneResultData)param;won=data.won;clock=0;impactAge=10;navAt=-1;arrived=completed=0;collecting=false;
            reward=data.reward??new RuneCoinReward(data.won&&!data.demo?40:0);claimed=won&&reward.Claimed;
            initialBalance=targetBalance=RuneCoinReward.Balance;balanceLabel.text=initialBalance.ToString();walletRect.localScale=Vector3.one;
            title.text=titleDepth.text=won?clearTitle:failureTitle;title.fontSize=titleDepth.fontSize=won?102:64;
            titleGroup.anchoredPosition=new Vector2(270,won?-225:-310);titleGroup.localScale=Vector3.one;titleGroup.localRotation=Quaternion.Euler(0,0,won?-4:0);
            title.enableVertexGradient=won;title.color=Color.white;titleDepth.color=RuneArtLibrary.Hex(won?"bc7613":"695276");
            coinTarget.localScale=balanceLabel.rectTransform.localScale=Vector3.one;rewardEffects.Clear();
            description.text=won?"+"+reward.amount:failureMessage;description.fontSize=won?54:24;
            description.rectTransform.anchoredPosition=new Vector2(40,won?-548:-390);
            hero.gameObject.SetActive(won);rays.gameObject.SetActive(won);hero.color=Color.white;heroRect.localScale=Vector3.one;
            hero.enabled=false;coinPile.ResetPose(RunePreferences.Reduced);raysRect.localRotation=Quaternion.identity;
            primaryAction=won?RuneResultAction.Next:data.demo?RuneResultAction.Retry:RuneResultAction.Home;
            primaryLabel.text=won?nextLabel:primaryAction==RuneResultAction.Retry?retryLabel:homeLabel;
            primaryFace.sprite=won?nextSprite:homeSprite;
            primaryRect.anchoredPosition=new Vector2(won?282:165,won?-690:-590);
            primaryButton.gameObject.SetActive(!won||claimed);homeButton.gameObject.SetActive(won&&claimed);claimButton.gameObject.SetActive(won&&!claimed);claimButton.interactable=true;
            primaryButton.transform.localScale=homeButton.transform.localScale=Vector3.one;coinPool.Initialize();
        }
        public void ClaimReward()
        {
            if(!CanEscape()||!won||claimed||collecting)return;
            reward.Claim();claimed=true;collecting=true;targetBalance=RuneCoinReward.Balance;claimButton.interactable=false;claimButton.gameObject.SetActive(false);arrived=completed=0;
            if(RunePreferences.Audio&&rewardAudio!=null&&swooshSound!=null)rewardAudio.PlayOneShot(swooshSound,RunePreferences.Volume*.45f);
            coinPile.Claim();var to=(Vector2)coinRoot.InverseTransformPoint(coinTarget.position);
            for(int i=0;i<coinInstances.Length;i++){var from=(Vector2)coinRoot.InverseTransformPoint(coinPile.launchPoints[i%coinPile.launchPoints.Length].position);var f=coinPool.RentCached("RewardCoins") as RuneCoinFlight;active[i]=f;if(f!=null)f.Begin(from,to,i,RunePreferences.Reduced);}
        }
        private void Update()=>AdvancePresentation(Mathf.Min(.05f,Time.unscaledDeltaTime));
        public void AdvancePresentation(float dt)
        {
            if(!IsOpen)return;clock+=dt;impactAge+=dt;rewardEffects.Tick(dt);
            float punch=RunePreferences.Reduced?0:Mathf.Exp(-impactAge*14)*Mathf.Cos(impactAge*32);
            coinTarget.localScale=Vector3.one*(1+punch*.22f);balanceLabel.rectTransform.localScale=Vector3.one*(1+Mathf.Max(0,punch)*.065f);
            if(won){float t=Mathf.Clamp01(clock/.34f),scale=RunePreferences.Reduced?1:1+1.8f*Mathf.Pow(t-1,3)+.8f*Mathf.Pow(t-1,2);titleGroup.localScale=Vector3.one*scale;}
            if(won){coinPile.Tick(dt,RunePreferences.Reduced);heroRect.localScale=Vector3.one;rays.color=new Color(1,1,1,.26f);if(!RunePreferences.Reduced)raysRect.localRotation=Quaternion.Euler(0,0,clock*1.5f);}
            if(collecting)
            {
                var to=(Vector2)coinRoot.InverseTransformPoint(coinTarget.position);
                for(int i=0;i<active.Length;i++)if(active[i]!=null)
                {
                    var flight=active[i];flight.SetDestination(to);bool finished=flight.Tick(dt);
                    if(flight.TakeArrival(out var point))
                    {
                        arrived++;impactAge=0;rewardEffects.CoinArrival(point,RunePreferences.Reduced);
                        balanceLabel.text=Mathf.RoundToInt(Mathf.Lerp(initialBalance,targetBalance,(float)arrived/coinInstances.Length)).ToString();
                        if(RunePreferences.Audio&&rewardAudio!=null&&coinSound!=null)rewardAudio.PlayOneShot(coinSound,RunePreferences.Volume*.55f);
                    }
                    if(finished){coinPool.ReturnCached(flight);active[i]=null;completed++;}
                }
                if(completed>=coinInstances.Length){collecting=false;balanceLabel.text=targetBalance.ToString();navAt=clock;primaryButton.gameObject.SetActive(true);homeButton.gameObject.SetActive(true);}
            }
            if(navAt>=0){float t=Mathf.Clamp01((clock-navAt)/.25f),s=RunePreferences.Reduced?1:1+2.70158f*Mathf.Pow(t-1,3)+1.70158f*Mathf.Pow(t-1,2);primaryButton.transform.localScale=homeButton.transform.localScale=Vector3.one*s;}
        }
        public void ChoosePrimary(){if(CanEscape()&&(!won||RewardReady))Close(primaryAction);}
        public void ChooseHome(){if(CanEscape()&&(!won||RewardReady))Close(RuneResultAction.Home);}
        public override void OnEscape(){if(!won||RewardReady)base.OnEscape();}
        public override void OnWillLeave(){base.OnWillLeave();for(int i=0;i<active.Length;i++)if(active[i]!=null){coinPool.ReturnCached(active[i]);active[i]=null;}rewardEffects.Clear();}
    }
}
