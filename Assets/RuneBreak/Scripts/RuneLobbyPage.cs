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
        public TMP_Text playLabel,progressLabel;
        public GameObject settings;
        public int homeIndex=2;
        private int selected;
        public override void OnWillEnter(object param){SelectTab(homeIndex);int next=Mathf.Min(PlayerPrefs.GetInt("RuneBreak.Next",0),catalog.levels.Length-1);playLabel.text=$"제 {next+1} 방  ·  플레이";int flags=PlayerPrefs.GetInt("RuneBreak.Clears",0),n=0;for(int i=0;i<catalog.levels.Length;i++)if((flags&(1<<i))!=0)n++;progressLabel.text=$"결계의 기록  {n} / {catalog.levels.Length}";}
        public override void OnDidEnter(){}
        public override void OnWillLeave()=>settings.SetActive(false);
        public override void OnDidLeave(){}
        public void SelectTab(int index)
        {
            if(index<0||index>=tabs.Length)return;selected=index;
            for(int i=0;i<tabs.Length;i++){tabs[i].content.SetActive(i==index);tabs[i].background.color=RuneArtLibrary.Hex(i==index?"5e7964":"253c40");tabs[i].label.color=RuneArtLibrary.Hex(i==index?"fff1af":"a8b8a8");}
        }
        public void Play()=>pages.Change(UIPageType.PlayPage,Mathf.Min(PlayerPrefs.GetInt("RuneBreak.Next",0),catalog.levels.Length-1));
        public void OpenSettings()=>settings.SetActive(true);
        public bool HandleBack(){if(settings.activeSelf){settings.SetActive(false);return true;}if(selected!=homeIndex){SelectTab(homeIndex);return true;}return false;}
    }
}
