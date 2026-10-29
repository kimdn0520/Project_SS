using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    [Serializable] public sealed class RuneLobbyTab
    {
        public string id;
        public GameObject content;
        public Image background;
        public TMP_Text label;
        public RuneLobbyIcon icon;
        public GameObject selectedMarker;
    }
    public sealed class RuneLobbyPage : SceneBase, IPageBackHandler
    {
        public PageManager pages;
        public RuneLevelCatalog catalog;
        public RuneLobbyTab[] tabs;
        public TMP_Text playLabel,comingSoon,coinBalance;
        [HideInInspector] public RuneLobbyShowcase showcase; // Legacy authoring reference; current MainPage has no showcase.
        public RuneLobbyHero hero;
        public PopupManager popups;
        public bool IsModalBlocked=>popups!=null&&popups.IsBlockingInput;
        public Sprite idleTabSkin,selectedTabSkin;
        public bool punchStyle;
        public Color activeTabText=new Color(.318f,.239f,.161f),idleTabText=new Color(.922f,.847f,.667f),activeTabIcon=new Color(.318f,.239f,.161f),idleTabIcon=new Color(.612f,.686f,.612f);
        public int homeIndex=2;
        private int selected;
        private float noticeUntil;
        public override void OnWillEnter(object param){SelectTab(homeIndex);int next=Mathf.Clamp(PlayerPrefs.GetInt("RuneBreak.Next",0),0,catalog.CampaignCount-1);playLabel.text=$"Lv. {next+1}";comingSoon.gameObject.SetActive(false);if(coinBalance!=null)coinBalance.text=RuneCoinReward.Balance.ToString();}
        public override void OnDidEnter(){}
        public override void OnWillLeave()=>PopupManager.Clear();
        public override void OnDidLeave(){}
        public void SelectTab(int index)
        {
            if(index<0||index>=tabs.Length)return;selected=index;
            for(int i=0;i<tabs.Length;i++)
            {
                bool active=i==index;var tab=tabs[i];tab.content.SetActive(active);
                tab.background.sprite=active?selectedTabSkin:idleTabSkin;tab.background.color=Color.white;
                tab.label.color=active?activeTabText:idleTabText;
                tab.icon.color=active?activeTabIcon:idleTabIcon;
                tab.selectedMarker.SetActive(active);
            }
        }
        public void ShowComingSoon(){comingSoon.gameObject.SetActive(true);noticeUntil=Time.unscaledTime+1.7f;}
        private void Update(){if(comingSoon.gameObject.activeSelf&&Time.unscaledTime>=noticeUntil)comingSoon.gameObject.SetActive(false);}
        public void Play()=>pages.Change(UIPageType.PlayPage,Mathf.Min(PlayerPrefs.GetInt("RuneBreak.Next",0),catalog.CampaignCount-1));
        public void PlayDemo(int index)
        {
            int found=0;for(int i=0;i<catalog.levels.Length;i++)if(catalog.levels[i].demo){if(found++==index){pages.Change(UIPageType.PlayPage,i);return;}}
        }
        public void OpenSettings()=>PopupManager.Show(RuneSettingsPanel.PrefabName);
        public bool HandleBack(){if(IsModalBlocked)return PopupManager.HandleBackInput();if(selected!=homeIndex){SelectTab(homeIndex);return true;}return false;}
    }
}
