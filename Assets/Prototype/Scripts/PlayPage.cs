using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition
{
    public sealed class PlayPage : SceneBase, IPageBackHandler
    {
        [Serializable] public sealed class GearCard
        {
            public TMP_Text name, detail, cost, action;
            public Image icon, panel;
            public Button button, craft;
        }
        public ExpeditionCatalog catalog;
        public SpriteManager sprites;
        public PoolManager pool;
        public SessionPausePolicy pausePolicy;
        public ExpeditionNotice notice;
        public ExpeditionInventory inventoryView;
        public EquipmentSelectionPopup equipmentPopup;
        public ExpeditionActor[] heroes, enemies;
        public ExpeditionActor miner;
        public Transform miningWorld;
        public SpriteRenderer[] blocks, deposits;
        public Sprite[] depositSprites;
        public ExpeditionFx[] effects;
        public WeaponItem[] itemEffects;
        public ExpeditionMiningView miningView;
        public HoldDigButton holdDig;
        public Image heatBar;

        public TMP_Text digLabel, soundLabel;
        public GameObject minePanel;
        public GameObject[] menuPanels;
        public GameObject heroGrid;
        public GameObject[] heroEquipmentPanels;
        public Image[] menuButtons;
        public Transform[] menuIcons;
        public Vector3[] menuIconRest;
        public TMP_Text resources, stageLabel, enemyStatus, depthLabel;
        public TMP_Text[] routeLabels, heroDetails, slotLabels;
        public Image[] slotIcons, routePanels;
        public Button[] routeButtons;
        public Button digButton;
        public Image enemyBar;
        public Image[] heroHpBars;
        public RectTransform[] heroHpRoots;
        public RectTransform enemyHpRoot;
        public Toggle autoBattleToggle;
        public TMP_Text bagTitle;
        public GearCard[] gearCards;
        public Transform[] scrolling;
        public Vector3[] enemyRest;
        public ExpeditionModel Model { get; private set; }
        public MiningRhythm Rhythm { get; } = new MiningRhythm();
        public int ActiveTab { get; private set; }
        public enum Journey { Waiting, Walking, Fighting, Recovering }
        public Journey State { get; private set; }
        const string SaveKey = "ProjectSS.Play.v3";
        CancellationTokenSource lifetime;
        float walkClock, miningClock, manualReady, refreshClock, saveClock;
        bool running, wired, muted, dirty, oneShot;
        int selectedHero, selectedSlot = -1, visibleEnemy;
        private float chestClock;
        private bool chestRewarded;
        public bool ChestOpening { get; private set; }
        static readonly Color Gold = new Color(1, .77f, .36f), Mint = new Color(.4f, .93f, .8f);
        public override void SetupRenderCamera(Camera camera)
        {
            base.SetupRenderCamera(camera);
            if (notice != null) notice.SetupRenderCamera(camera);
            if (equipmentPopup != null) equipmentPopup.SetupRenderCamera(camera);
        }
        public override void OnWillEnter(object param)
        {
            if (Model == null)
            {
                DOTween.SetTweensCapacity(640,192);
                var d = ExpeditionSave.Fresh(catalog.gear.Length);
                try
                {
                    var saved = JsonUtility.FromJson<ExpeditionSave>(PlayerPrefs.GetString(SaveKey,""));
                    if (saved != null && saved.IsValid(catalog.gear.Length) && ValidLoadout(saved)) d = saved;
                }
                catch (ArgumentException) { }
                Model = new ExpeditionModel(catalog,d);
            }
            if (!wired)
            {
                Model.Mined += OnMined; Model.HeroHit += OnHeroHit; Model.EnemyHit += OnEnemyHit; Model.BattleEnded += OnBattleEnded;
                holdDig.OnDig += Dig; holdDig.OnPressedChanged += OnHeld; wired = true;
            }
            PopupManager.RegisterPopup(notice.PopupName, notice);
            PopupManager.RegisterPopup(equipmentPopup.PopupName, equipmentPopup);
            foreach (var actor in heroes) actor.InitializeActor();
            foreach (var actor in enemies) actor.InitializeActor();
            miner.InitializeActor(); ApplyEquipment();
            Model.Data.autoMine=false;
            miningView.SetRoute(Model.Data.route); miningView.SetChest(Model.Data.chest);
            State = Journey.Waiting;ChestOpening=false; SetEnemyVisible(false); OpenMenu(0); Refresh();
            canvasGroup.alpha = 1; canvasGroup.interactable = canvasGroup.blocksRaycasts = true;
        }
        bool ValidLoadout(ExpeditionSave d)
        {
            var counts = new int[catalog.gear.Length];
            for (int h=0; h<3; h++) for(int s=0;s<4;s++)
            {
                int id=d.equipment[h*4+s]; if(id<0) { if(s==0)return false; continue; }
                var g=catalog.gear[id]; if(g.equipSlot!=s || (g.hero>=0 && g.hero!=h) || ++counts[id]>d.inventory[id])return false;
            }
            return true;
        }
        public override void OnDidEnter()
        {
            running=true; lifetime?.Cancel(); lifetime?.Dispose();
            lifetime=CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken); RunAsync(lifetime.Token).Forget();
        }
        public override void OnWillLeave() { running=false; lifetime?.Cancel(); holdDig.HardCancel(); Persist(); foreach(var fx in effects)fx.ReturnToPool();foreach(var item in itemEffects)item.Return(); }
        public override void OnDidLeave() { }
        async UniTaskVoid RunAsync(CancellationToken ct)
        {
            try
            {
                while(running)
                {
                    ct.ThrowIfCancellationRequested();
                    if(!pausePolicy.IsPaused && ActiveTab==0)
                    {
                        float dt=Mathf.Min(Time.deltaTime,.1f); Rhythm.Tick(dt); TickJourney(dt);
                        if(ChestOpening)
                        {
                            chestClock+=dt;
                            if(chestClock>=.5f&&!chestRewarded){chestRewarded=true;Model.Dig(Model.BlockMaxHp);}
                            if(chestClock>=1.15f)FinishChest();
                        }
                        if(Model.Data.autoMine && !Rhythm.Holding && !ChestOpening) { miningClock+=dt; if(miningClock>=1.35f){miningClock=0;MineOnce(0);} }
                    }
                    refreshClock+=Time.deltaTime; saveClock+=Time.deltaTime;
                    if(refreshClock>=.1f){refreshClock=0;Refresh();}
                    if(saveClock>=4){saveClock=0;if(dirty)Persist();}
                    await UniTask.Yield(PlayerLoopTiming.Update,ct);
                }
            }
            catch(OperationCanceledException) { }
        }
        public void TickJourney(float dt)
        {
            if(State==Journey.Waiting)
            {
                if(!Model.Data.autoBattle && !oneShot)return;
                oneShot=false; State=Journey.Walking;walkClock=0;Model.ResetHealth();
                visibleEnemy=Model.EnemyKind;enemies[visibleEnemy].InitializeActor();SetEnemyVisible(true);
                foreach(var h in heroes){h.SetAlive(true);h.SetWalking(true);}
            }
            else if(State==Journey.Walking)
            {
                walkClock+=dt;
                foreach(var layer in scrolling){var p=layer.localPosition;p.x-=dt*.85f;if(p.x < -7.6f)p.x+=15.2f;layer.localPosition=p;}
                enemies[visibleEnemy].transform.position=enemyRest[visibleEnemy]+Vector3.right*Mathf.Lerp(5,0,Mathf.Clamp01(walkClock/2.2f));
                if(walkClock>=2.2f){foreach(var h in heroes)h.SetWalking(false);Model.StartBattle();State=Journey.Fighting;}
            }
            else if(State==Journey.Fighting)Model.Tick(dt);
            else
            {
                walkClock-=dt;
                if(walkClock<=0){State=Journey.Waiting;SetEnemyVisible(false);}
            }
        }
        public void OpenMenu(int menu)
        {
            holdDig.HardCancel(); ActiveTab=menu;
            minePanel.SetActive(menu==0); miningWorld.gameObject.SetActive(menu==0);
            for(int i=0;i<menuPanels.Length;i++)menuPanels[i].SetActive(menu==i+1);
            for(int i=0;i<menuIcons.Length;i++)
            {
                menuIcons[i].DOKill();menuIcons[i].DOLocalMove(menuIconRest[i]+Vector3.up*(menu==i+1?10:0),.16f).SetEase(Ease.OutQuad).SetUpdate(true);
                menuButtons[i].color=menu==i+1?new Color(.38f,.35f,.56f):new Color(.17f,.21f,.30f);
            }
            if(menu==1)ShowHeroGrid();
            if(Model!=null)Refresh();
        }
        public void SelectTab(int tab) { OpenMenu(tab); }
        public bool HandleBack()
        {
            if(ActiveTab==1&&!heroGrid.activeSelf){ShowHeroGrid();return true;}
            if(ActiveTab!=0){OpenMenu(0);return true;}
            return false;
        }
        public void SelectHero(int hero){selectedHero=hero;selectedSlot=-1;Refresh();}
        public void ShowHeroGrid(){heroGrid.SetActive(true);foreach(var panel in heroEquipmentPanels)panel.SetActive(false);}
        public void OpenHero(int hero)
        {
            selectedHero=hero;selectedSlot=-1;heroGrid.SetActive(false);
            for(int i=0;i<heroEquipmentPanels.Length;i++)heroEquipmentPanels[i].SetActive(i==hero);
            Refresh();
        }
        public void SelectSlot(int index){selectedHero=index/4;selectedSlot=index%4;PopupManager.Show(equipmentPopup.PopupName,new EquipmentSelectionPopup.Selection{hero=selectedHero,slot=selectedSlot});}
        public void SelectRoute(int route){if(pausePolicy.IsPaused||!Model.SelectRoute(route))return;miningView.SetRoute(route);dirty=true;Refresh();}
        public void Dig(){if(!running||pausePolicy.IsPaused||ActiveTab!=0||Time.unscaledTime<manualReady||ChestOpening||miningView.IsDescending)return;manualReady=Time.unscaledTime+Rhythm.Interval;MineOnce(Rhythm.BonusDamage);}
        void MineOnce(int bonus)
        {
            if(Model.Data.chest){ChestOpening=true;chestClock=0;chestRewarded=false;miningView.OpenChest();}
            else Model.Dig(bonus);
        }
        void FinishChest()
        {
            // Resolve reward only once, after the lid opens; no rock hit, cracks or break audio.
            ChestOpening=false;miningView.Descend();miningView.SetChest(Model.Data.chest);
        }
        void OnHeld(bool held){Rhythm.SetHeld(held);if(!held)miningClock=0;}
        public void ToggleAuto(){Model.Data.autoMine=!Model.Data.autoMine;dirty=true;Refresh();}
        public void SetAutoBattle(bool enabled){Model.Data.autoBattle=enabled;dirty=true;}
        public void StartOrRetreat(){if(State==Journey.Fighting){Model.Data.autoBattle=false;Model.Retreat();}else oneShot=true;Refresh();}
        public void ToggleSound(){muted=!muted;miningView.SetMuted(muted);soundLabel.text=muted?"소리 OFF":"소리 ON";}
        public void Craft(int id){if(!Model.Craft(id))return;dirty=true;Refresh();}
        public void Equip(int id)
        {
            EquipForHero(id,selectedHero);
        }
        public bool EquipForHero(int id,int hero){if(!Model.Equip(id,hero))return false;ApplyEquipment();dirty=true;Refresh();return true;}
        public bool UnequipForHero(int hero,int slot){if(!Model.Unequip(hero,slot))return false;ApplyEquipment();dirty=true;Refresh();return true;}
        public void ClearSlot(){if(selectedSlot>0&&Model.Unequip(selectedHero,selectedSlot)){ApplyEquipment();dirty=true;Refresh();}}
        void ApplyEquipment()
        {
            for(int i=0;i<3;i++)
            {
                var g=catalog.gear[Model.Equipped(i,0)];heroes[i].Equip(g.slot,sprites.Get(g.spriteKey),false);
                int armor=Model.Equipped(i,2),helmet=Model.Equipped(i,1);
                heroes[i].EquipArmor(armor<0?null:sprites.Get(catalog.gear[armor].spriteKey),helmet<0?null:sprites.Get(catalog.gear[helmet].spriteKey));
            }
        }
        void SetEnemyVisible(bool visible){for(int i=0;i<enemies.Length;i++)enemies[i].gameObject.SetActive(visible&&i==visibleEnemy);enemyHpRoot.gameObject.SetActive(visible);}
        void OnMined(bool broken)
        {
            dirty=true;if(ActiveTab!=0)return;
            if(broken)Rhythm.BreakRock();
            if(!ChestOpening){holdDig.Pulse(Rhythm.Interval);miner.MineStrike(Rhythm.Interval);miningView.Strike(broken,Rhythm.BurstRemaining>0,Rhythm.Heat,1f-(float)Model.BlockHp/Model.BlockMaxHp);}
            if(broken)
            {
                if(Model.LastGear>=0)
                {
                    var item=pool.RentCached("Weapon_"+Model.LastGear) as WeaponItem;
                    if(item!=null)item.Reveal(miningView.ChestPosition+Vector3.up*.22f,Model.LastLoot);
                }
                else
                {
                    var fx=pool.RentCached("ExpeditionFx") as ExpeditionFx;
                    if(fx!=null)fx.Loot(miningView.HitPosition+Vector3.up*.2f,depositSprites[Model.Data.route],Model.LastLoot,false);
                }
                if(!ChestOpening)miningView.SetChest(Model.Data.chest);
            }
        }
        void OnHeroHit(int hero,float damage,GearEffect effect){heroes[hero].Attack(hero==2);enemies[visibleEnemy].Hit(false);Float(enemies[visibleEnemy].HitPosition,Mathf.CeilToInt(damage).ToString(),Gold);}
        void OnEnemyHit(float damage,bool shielded){enemies[visibleEnemy].Attack();heroes[Model.LastTargetHero].Hit(false);Float(heroes[Model.LastTargetHero].HitPosition,"-"+Mathf.CeilToInt(damage),Color.red);}
        void OnBattleEnded(bool win)
        {
            State=Journey.Recovering;walkClock=win?1.1f:2.4f;dirty=true;
            if(win){enemies[visibleEnemy].SetAlive(false);foreach(var h in heroes)if(h.gameObject.activeInHierarchy)h.Celebrate();}
            
            Persist();
        }
        void Float(Vector3 p,string text,Color color){var fx=pool.RentCached("ExpeditionFx") as ExpeditionFx;if(fx!=null)fx.Show(p,text,color);}
        public void Refresh()
        {
            if(Model==null)return;var d=Model.Data;
            resources.text=$"철  {d.iron}     결정  {d.crystal}     파편  {d.relic}";
            stageLabel.text=$"{Model.Region}-{Model.Wave}  ·  "+(Model.Region%2==1?"초원 전선":"잊힌 요새")+(Model.IsBoss?"  /  BOSS":"");
            enemyStatus.text=State==Journey.Walking?"다음 적을 찾아 이동 중":State==Journey.Fighting?"교전 중":State==Journey.Recovering?"원정대 재정비":"원정 대기";
            autoBattleToggle.SetIsOnWithoutNotify(d.autoBattle);
            heatBar.fillAmount=Rhythm.BurstRemaining>0?Rhythm.BurstRemaining/2.6f:Rhythm.Charge/6f;
            heatBar.color=Rhythm.BurstRemaining>0?Mint:Gold;
            enemyBar.fillAmount=Model.Fighting?Model.EnemyHp/Model.EnemyMaxHp:State==Journey.Recovering?0:1;
            enemyHpRoot.position=enemies[visibleEnemy].HpPosition;
            for(int i=0;i<3;i++){heroHpBars[i].fillAmount=Model.HeroHp(i)/Model.HeroMaxHp(i);heroHpRoots[i].position=heroes[i].HpPosition;heroes[i].SetAlive(Model.HeroHp(i)>0);}
            depthLabel.text=$"갱도 {d.depth}m";

            
            digLabel.text=Rhythm.Holding?"DIG!":"DIG";
            for(int i=0;i<3;i++){routeButtons[i].interactable=i<2||d.cleared>=10;routePanels[i].color=d.route==i?new Color(.24f,.48f,.46f):new Color(.12f,.2f,.23f);}
            if(ActiveTab!=1 && ActiveTab!=2)return;
            string[] names={"로웬 · 전사","린 · 도적","미라 · 마법사"};string[] slots={"무기","투구","갑옷","장신구"};
            for(int h=0;h<3;h++)
            {
                heroDetails[h].text=names[h]+$"\n공격 {Model.HeroDamage(h):0} · HP {Model.HeroMaxHp(h):0}";
                for(int s=0;s<4;s++){int index=h*4+s,id=Model.Equipped(h,s);slotIcons[index].enabled=id>=0;if(id>=0)slotIcons[index].sprite=catalog.gear[id].icon;slotLabels[index].text=slots[s]+"\n"+(id>=0?catalog.gear[id].title:"비어 있음");}
            }
            if(ActiveTab!=2)return;
            inventoryView.Refresh();
        }
        public void Help(){PopupManager.Show(notice.PopupName,new ExpeditionNotice.Content{title="플레이 안내",body="DIG를 꾹 누르면 채굴이 빨라집니다.\n광맥 속 상자에서 장비를 발견하세요.\n\n용사 관리에서 각 용사의 장비를 바꾸고, 가방에서 재료로 장비를 제작할 수 있습니다.\n\n자동 원정은 패배한 구간에 재도전합니다. 10번째 구간의 보스를 처치하면 다음 지역으로 이동합니다."});}
        public void ResetProgress(){ResetAsync().Forget();}
        async UniTaskVoid ResetAsync()
        {
            try{bool ok=await PopupManager.ShowAsync<bool>(notice.PopupName,new ExpeditionNotice.Content{title="진행 초기화",body="현재 Play 진행과 획득 장비를 초기화할까요?",action="초기화",confirmation=true}).AttachExternalCancellation(destroyCancellationToken);if(!ok)return;Unwire();Model=new ExpeditionModel(catalog,ExpeditionSave.Fresh(catalog.gear.Length));OnWillEnter(null);dirty=true;Persist();}
            catch(OperationCanceledException){}
        }
        void Persist(){if(Model==null)return;PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(Model.Data));PlayerPrefs.Save();dirty=false;}
        void Unwire(){if(!wired)return;Model.Mined-=OnMined;Model.HeroHit-=OnHeroHit;Model.EnemyHit-=OnEnemyHit;Model.BattleEnded-=OnBattleEnded;holdDig.OnDig-=Dig;holdDig.OnPressedChanged-=OnHeld;wired=false;}
        void OnApplicationPause(bool pause){if(pause)Persist();}
        void OnApplicationQuit(){Persist();}
        void OnDestroy(){running=false;lifetime?.Cancel();lifetime?.Dispose();Unwire();}
    }
}
