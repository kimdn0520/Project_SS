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
    }
    public sealed class RuneLobbyPage : SceneBase, IPageBackHandler
    {
        public PageManager pages;
        public RuneLevelCatalog catalog;
        public RuneLobbyTab[] tabs;
        public TMP_Text playLabel,comingSoon;
        public RuneLobbyShowcase showcase;
        public GameObject settings;
        public int homeIndex=2;
        private int selected;
        private float noticeUntil;
        public override void OnWillEnter(object param){SelectTab(homeIndex);int next=Mathf.Clamp(PlayerPrefs.GetInt("RuneBreak.Next",0),0,catalog.CampaignCount-1);playLabel.text=$"Lv.{next+1}";comingSoon.gameObject.SetActive(false);}
        public override void OnDidEnter(){}
        public override void OnWillLeave()=>settings.SetActive(false);
        public override void OnDidLeave(){}
        public void SelectTab(int index)
        {
            if(index<0||index>=tabs.Length)return;selected=index;
            for(int i=0;i<tabs.Length;i++){tabs[i].content.SetActive(i==index);tabs[i].background.color=RuneArtLibrary.Hex(i==index?"fff1af":"d4e7ac");tabs[i].label.color=RuneArtLibrary.Hex("253c40");}
        }
        public void ShowComingSoon(){comingSoon.gameObject.SetActive(true);noticeUntil=Time.unscaledTime+1.7f;}
        private void Update(){if(comingSoon.gameObject.activeSelf&&Time.unscaledTime>=noticeUntil)comingSoon.gameObject.SetActive(false);}
        public void Play()=>pages.Change(UIPageType.PlayPage,Mathf.Min(PlayerPrefs.GetInt("RuneBreak.Next",0),catalog.CampaignCount-1));
        public void PlayDemo(int index)
        {
            int found=0;for(int i=0;i<catalog.levels.Length;i++)if(catalog.levels[i].demo){if(found++==index){pages.Change(UIPageType.PlayPage,i);return;}}
        }
        public void OpenSettings()=>settings.SetActive(true);
        public bool HandleBack(){if(settings.activeSelf){settings.SetActive(false);return true;}if(selected!=homeIndex){SelectTab(homeIndex);return true;}return false;}
    }
}
