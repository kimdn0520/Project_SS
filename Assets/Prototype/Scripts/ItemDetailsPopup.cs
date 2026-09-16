using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSS.Expedition
{
    public sealed class ItemDetailsPopup : BasePopupHandler
    {
        public sealed class Selection { public PlayPage page; public int index; public bool material; public string uid; }
        public TMP_Text title, itemName, description, stats, ownership;
        public Image icon;
        SessionPausePolicy pause;
        public string SelectedUid { get; private set; }
        public override string PopupName => "ItemDetails";
        public static readonly string[] RarityNames = { "일반", "희귀", "영웅", "전설", "신화" };
        public override void OnWillEnter(object param)
        {
            base.OnWillEnter(param);
            var selection=(Selection)param;var page=selection.page;var model=page.Model;
            pause=page.pausePolicy;pause.RequestPause(PopupName);title.text="아이템 정보";
            SelectedUid=selection.uid;
            if(selection.material)
            {
                var item=page.catalog.materials[selection.index];itemName.text=item.title;description.text=item.description;icon.sprite=item.icon;
                stats.text=RarityNames[(int)item.rarity]+" · 재료";ownership.text=$"보유 {model.MaterialCount(selection.index)}";
            }
            else
            {
                var instance=model.Instance(selection.uid);
                var item=page.catalog.gear[selection.index];itemName.text=item.title;description.text=item.description;icon.sprite=item.icon;
                stats.text=Describe(instance==null?item:model.EffectiveGear(instance));
                if(instance!=null){stats.text+="\n\n추가 옵션";
                    if(instance.options==null||instance.options.Length==0)stats.text+="\n없음";
                    else foreach(var option in instance.options)stats.text+="\n"+OptionText(option);
                }
                ownership.text=instance==null?"":model.IsInstanceEquipped(instance.uid)?"장비 중":model.IsBattleInstance(instance.uid)?"전투 사용 중":"";
            }
            icon.enabled=icon.sprite!=null;
            stats.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(425,stats.GetPreferredValues(stats.text,510,0).y+20));
            stats.rectTransform.anchoredPosition=Vector2.zero;
        }
        public static string OptionText(GearRoll option)
        {
            string[] labels={"공격력","체력","공격속도","치명타 확률","치명타 피해","방어력","흡혈"};
            return labels[(int)option.stat]+$" +{option.value:0.##}"+(option.unit==GearOptionUnit.Percent?"%":"");
        }
        public static string Describe(GearDefinition item)
        {
            string heading = RarityNames[(int)item.rarity] + " · " + new[] { "무기", "투구", "갑옷", "장신구" }[item.equipSlot];
            if (item.equipSlot == 0)
                return heading + $"\n\n공격력  {item.damage:0.##}\n공격속도  {item.AttacksPerSecond:0.##}회/초\n치명타 확률  {item.criticalChance:0.##}%\n치명타 피해  {item.criticalDamage:0.##}%\n스킬 증폭  {item.skillAmplification:0.##}%";
            if (item.equipSlot == 1 || item.equipSlot == 2)
                return heading + $"\n\n방어력  {item.defense:0.##}\n체력  {item.health:0.##}\n회피율  {item.evasion:0.##}%";
            // Accessories retain the existing bonuses without imposing a new accessory stat policy.
            return heading + $"\n\n공격력  {item.damage:0.##}\n체력  {item.health:0.##}\n방어력  {item.defense:0.##}\n치명타 확률  {item.criticalChance:0.##}%\n치명타 피해  +{item.criticalDamage:0.##}%\n회피율  {item.evasion:0.##}%\n스킬 증폭  {item.skillAmplification:0.##}%";
        }
        public override void OnDidLeave() { Release(); }
        void Release() { if (pause != null) pause.ReleasePause(PopupName); pause = null; }
        protected override void OnDestroy() { Release(); base.OnDestroy(); }
    }
}
