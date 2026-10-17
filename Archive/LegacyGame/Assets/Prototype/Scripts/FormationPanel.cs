using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition
{
    public sealed class FormationPanel:MonoBehaviour
    {
        public PlayPage page;
        public TMP_Text[] slotLabels,cardStates;
        public Image[] slotBackgrounds;
        public Button[] removeButtons;
        public TMP_Text hint;
        public HeroCollectionCell[] collectionCells;
        int selected=-1;
        public void SelectPosition(int slot){selected=slot;Refresh();}
        public void SelectHero(int hero)
        {
            if(selected<0){page.OpenHero(hero);return;}
            page.AssignFormation(selected,hero);selected=-1;Refresh();
        }
        public void Remove(int slot){page.RemoveFormation(slot);Refresh();}
        public void Refresh()
        {
            if(page.Model==null)return;
            int count=0;for(int s=0;s<3;s++)if(page.Model.ConfiguredHero(s)>=0)count++;
            for(int s=0;s<3;s++)
            {
                int hero=page.Model.ConfiguredHero(s);
                slotLabels[s].text=HeroRoster.Positions[s]+"\n"+(hero<0?"빈 슬롯":HeroRoster.Names[hero]);
                slotBackgrounds[s].color=s==selected?new Color(1,.83f,.48f):Color.white;
                removeButtons[s].interactable=hero>=0&&count>1;
            }
            for(int h=0;h<3;h++)
            {
                int slot=page.Model.FormationSlot(h);
                bool owned=page.Model.IsOwned(h);
                cardStates[h].gameObject.SetActive(false);
                cardStates[h].text=!owned?"미보유":slot<0?"미출전":HeroRoster.Positions[slot]+" · 출전 중";
                foreach(var b in cardStates[h].transform.parent.GetComponentsInChildren<Button>(true))b.interactable=owned;
            }
            foreach(var cell in collectionCells??System.Array.Empty<HeroCollectionCell>())cell.Refresh();
            hint.gameObject.SetActive(false);
        }
        void OnEnable(){selected=-1;Refresh();}
    }
}
