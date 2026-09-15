using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSS.Expedition
{
    public sealed class EquipmentSelectionPopup : BasePopupHandler
    {
        public sealed class Selection { public int hero, slot; public PlayPage page; }
        public PlayPage page;
        public SessionPausePolicy pausePolicy;
        public TMP_Text title, empty;
        public RectTransform content;
        public ScrollRect scroll;
        public ExpeditionInventory.Row[] rows;
        public Button unequip;
        private int hero, slot;
        public override string PopupName => "EquipmentSelection";
        public void SetupRenderCamera(Camera camera) { Canvas.worldCamera = camera; }
        public override void OnWillEnter(object param)
        {
            var selection=(Selection)param;page=selection.page;pausePolicy=page.pausePolicy;
            base.OnWillEnter(param);pausePolicy.RequestPause(PopupName);
            hero=selection.hero;slot=selection.slot;
            title.text=new[]{"로웬","린","미라"}[hero]+" · "+new[]{"무기","투구","갑옷","장신구"}[slot]+" 선택";
            float y=0;
            for(int i=0;i<rows.Length;i++)
            {
                var g=page.catalog.gear[i]; var row=rows[i];
                bool show=g.equipSlot==slot&&(g.hero<0||g.hero==hero)&&page.Model.Data.inventory[i]>0;
                row.root.gameObject.SetActive(show);if(!show)continue;
                row.root.anchoredPosition=new Vector2(0,-y);y+=row.root.sizeDelta.y+12;
                bool equipped=page.Model.Equipped(hero,slot)==i;
                row.count.text=equipped?"착용 중":page.Model.Available(i)>0?$"선택하여 장착 · 여유 {page.Model.Available(i)}":"다른 용사가 사용 중";
                row.button.interactable=!equipped&&page.Model.Available(i)>0;
                var surface=row.root.Find("Surface")?.GetComponent<Image>();
                if(surface!=null)surface.color=equipped?new Color(.76f,.88f,.81f):new Color(.95f,.96f,.91f);
            }
            content.sizeDelta=new Vector2(content.sizeDelta.x,y);scroll.verticalNormalizedPosition=1;
            empty.gameObject.SetActive(y==0);unequip.gameObject.SetActive(slot>0);unequip.interactable=page.Model.Equipped(hero,slot)>=0;
        }
        public void Choose(int id)
        {
            if(PopupManager.IsChanging||id<0||id>=page.catalog.gear.Length||page.catalog.gear[id].equipSlot!=slot)return;
            if(page.EquipForHero(id,hero))Close(true);
        }
        public void RemoveEquipment(){if(!PopupManager.IsChanging&&page.UnequipForHero(hero,slot))Close(true);}
        public override void OnDidLeave(){pausePolicy.ReleasePause(PopupName);pausePolicy=null;}
        protected override void OnDestroy(){if(pausePolicy!=null)pausePolicy.ReleasePause(PopupName);base.OnDestroy();}
    }
}
