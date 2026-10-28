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
        public AudioSource rewardAudio;
        public AudioClip coinSound;
        [Header("English copy")]
        public string clearTitle="Clear!",failureTitle="Try Again";
        [TextArea] public string clearMessage="Level {0} complete!\nReady for the next challenge?";
        [TextArea] public string finalMessage="All levels complete!\nThanks for playing.";
        [TextArea] public string failureMessage="Storage is full.\nGive it another try!";
        public string nextLabel="Next",homeLabel="Home",retryLabel="Try Again",nextDemoLabel="Next";
        private RuneResultAction primaryAction;
        private RuneCoinReward reward;
        private bool won,collecting,claimed;
        private float clock,pulse,navAt=-1;
        private int initialBalance,targetBalance,arrived;
        private readonly RuneCoinFlight[] active=new RuneCoinFlight[16];
        public bool IsCollecting=>collecting;
        public bool RewardReady=>claimed&&!collecting;
        protected override object DefaultCloseParam=>RuneResultAction.Home;
        public override void OnWillEnter(object param)
        {
            base.OnWillEnter(param);var data=(RuneResultData)param;won=data.won;clock=0;pulse=0;navAt=-1;arrived=0;collecting=false;
            reward=data.reward??new RuneCoinReward(data.won&&!data.demo?40:0);claimed=won&&reward.Claimed;
            initialBalance=targetBalance=RuneCoinReward.Balance;balanceLabel.text=initialBalance.ToString();walletRect.localScale=Vector3.one;
            title.text=won?clearTitle:failureTitle;title.fontSize=won?94:64;
            title.rectTransform.anchoredPosition=new Vector2(25,won?-145:-230);
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
            reward.Claim();claimed=true;collecting=true;targetBalance=RuneCoinReward.Balance;claimButton.interactable=false;claimButton.gameObject.SetActive(false);arrived=0;
            coinPile.Claim();var to=(Vector2)coinRoot.InverseTransformPoint(coinTarget.position);
            for(int i=0;i<coinInstances.Length;i++){var from=(Vector2)coinRoot.InverseTransformPoint(coinPile.launchPoints[i%coinPile.launchPoints.Length].position);var f=coinPool.RentCached("RewardCoins") as RuneCoinFlight;active[i]=f;if(f!=null)f.Begin(from,to,i,RunePreferences.Reduced);}
        }
        private void Update()=>AdvancePresentation(Mathf.Min(.05f,Time.unscaledDeltaTime));
        public void AdvancePresentation(float dt)
        {
            if(!IsOpen)return;clock+=dt;pulse=Mathf.Max(0,pulse-dt*5);walletRect.localScale=Vector3.one*(1+pulse*.10f);
            if(won){coinPile.Tick(dt,RunePreferences.Reduced);heroRect.localScale=Vector3.one;rays.color=new Color(1,1,1,.55f);if(!RunePreferences.Reduced)raysRect.localRotation=Quaternion.Euler(0,0,clock*1.5f);}
            if(collecting)
            {
                for(int i=0;i<active.Length;i++)if(active[i]!=null&&active[i].Tick(dt))
                {
                    coinPool.ReturnCached(active[i]);active[i]=null;arrived++;pulse=1;
                    balanceLabel.text=Mathf.RoundToInt(Mathf.Lerp(initialBalance,targetBalance,(float)arrived/coinInstances.Length)).ToString();
                    if(RunePreferences.Audio&&rewardAudio!=null&&coinSound!=null)rewardAudio.PlayOneShot(coinSound,RunePreferences.Volume*.45f);
                }
                if(arrived>=coinInstances.Length){collecting=false;balanceLabel.text=targetBalance.ToString();navAt=clock;primaryButton.gameObject.SetActive(true);homeButton.gameObject.SetActive(true);}
            }
            if(navAt>=0){float t=Mathf.Clamp01((clock-navAt)/.25f),s=RunePreferences.Reduced?1:1+2.70158f*Mathf.Pow(t-1,3)+1.70158f*Mathf.Pow(t-1,2);primaryButton.transform.localScale=homeButton.transform.localScale=Vector3.one*s;}
        }
        public void ChoosePrimary(){if(CanEscape()&&(!won||RewardReady))Close(primaryAction);}
        public void ChooseHome(){if(CanEscape()&&(!won||RewardReady))Close(RuneResultAction.Home);}
        public override void OnEscape(){if(!won||RewardReady)base.OnEscape();}
        public override void OnWillLeave(){base.OnWillLeave();for(int i=0;i<active.Length;i++)if(active[i]!=null){coinPool.ReturnCached(active[i]);active[i]=null;}}
    }
}
