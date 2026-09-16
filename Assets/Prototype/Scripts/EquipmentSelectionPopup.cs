using System.Linq;
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
        private GearInstance[] instances;
        public override string PopupName => "EquipmentSelection";
        public void SetupRenderCamera(Camera camera) { Canvas.worldCamera = camera; }
        public override void OnWillEnter(object param)
        {
            var selection=(Selection)param;page=selection.page;pausePolicy=page.pausePolicy;
            base.OnWillEnter(param);pausePolicy.RequestPause(PopupName);
            instances=page.Model.Data.gearInstances.Where(g=>page.catalog.gear[g.definition].equipSlot==selection.slot&&(page.catalog.gear[g.definition].hero<0||page.catalog.gear[g.definition].hero==selection.hero)).ToArray();
            ExpeditionInventory.ResizeRows(ref rows, System.Math.Max(1,instances.Length), content);
            for(int i=0;i<rows.Length;i++){int index=i;rows[i].button.onClick=new Button.ButtonClickedEvent();rows[i].button.onClick.AddListener(()=>ChooseInstance(index));}
            hero=selection.hero;slot=selection.slot;
            title.text=new[]{"로웬","린","미라"}[hero]+" · "+new[]{"무기","투구","갑옷","장신구"}[slot]+" 선택";
            float y=0;
            for(int i=0;i<rows.Length;i++)
            {
                var row=rows[i]; if(i>=instances.Length){row.root.gameObject.SetActive(false);continue;}
                var instance=instances[i];var g=page.catalog.gear[instance.definition];
                row.title.text=g.title; row.detail.text=g.description;
                row.count.gameObject.SetActive(true); row.icon.sprite=g.icon; row.icon.enabled=g.icon!=null;
                bool show=g.equipSlot==slot&&(g.hero<0||g.hero==hero);
                row.root.gameObject.SetActive(show);if(!show)continue;
                row.root.anchoredPosition=new Vector2(0,-y);y+=row.root.sizeDelta.y+12;
                bool equipped=page.Model.EquippedUid(hero,slot)==instance.uid;
                row.count.text=equipped?"착용 중":!page.Model.IsInstanceEquipped(instance.uid)?$"선택하여 장착":"다른 용사가 사용 중";
                if(instance.options.Length>0)row.detail.text=string.Join(" · ",instance.options.Select(ItemDetailsPopup.OptionText));
                row.button.interactable=!equipped&&!page.Model.IsInstanceEquipped(instance.uid);
                var background=row.root.GetComponent<Image>();
                if(background!=null)background.color=equipped?new Color(.76f,.88f,.81f):Color.white;
            }
            content.sizeDelta=new Vector2(content.sizeDelta.x,y);scroll.verticalNormalizedPosition=1;
            empty.gameObject.SetActive(y==0);unequip.gameObject.SetActive(slot>0);unequip.interactable=page.Model.Equipped(hero,slot)>=0;
        }
        void ChooseInstance(int index){if(!PopupManager.IsChanging&&index>=0&&index<instances.Length&&page.EquipInstanceForHero(instances[index].uid,hero))Close(true);}
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
