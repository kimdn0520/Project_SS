using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSS.Expedition
{
    public sealed class ExpeditionInventory : MonoBehaviour
    {
        [Serializable] public sealed class Row
        {
            public RectTransform root;
            public TMP_Text title, detail, count;
            public Image icon;
            public Button button;
        }
        public PlayPage page;
        public RectTransform content;
        public ScrollRect scroll;
        public Row[] gearRows, materialRows;
        public Image[] tabs;
        public TMP_Text empty;
        private int filter;
        public void SelectFilter(int index) { filter = index; scroll.verticalNormalizedPosition = 1; Refresh(); }
        public void Refresh()
        {
            if (page.Model == null) return;
            float y = 0;
            for(int i=0;i<gearRows.Length;i++)
            {
                var row = gearRows[i]; var gear = page.catalog.gear[i];
                bool show = filter != 2 && (filter == 0 || gear.equipSlot == 0) && page.Model.Data.inventory[i] > 0;
                row.root.gameObject.SetActive(show); if(!show) continue;
                row.root.anchoredPosition = new Vector2(0,-y); y += 128;
                row.count.text = $"보유 {page.Model.Data.inventory[i]} · 사용 중 {page.Model.Data.inventory[i]-page.Model.Available(i)}";
            }
            int[] counts = {page.Model.Data.iron,page.Model.Data.crystal,page.Model.Data.relic};
            for(int i=0;i<materialRows.Length;i++)
            {
                var row=materialRows[i]; bool show=filter!=1 && counts[i]>0;
                row.root.gameObject.SetActive(show); if(!show)continue;
                row.root.anchoredPosition=new Vector2(0,-y);y+=128;row.count.text=$"보유 {counts[i]}";
            }
            content.sizeDelta=new Vector2(content.sizeDelta.x,y);
            empty.gameObject.SetActive(y==0);
            for(int i=0;i<tabs.Length;i++)tabs[i].color=i==filter?new Color(.24f,.48f,.46f):new Color(.12f,.2f,.23f);
        }
    }
}
