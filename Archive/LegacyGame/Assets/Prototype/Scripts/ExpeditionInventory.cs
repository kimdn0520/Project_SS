using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
            public Button button, dismantle;
        }
        public PlayPage page;
        public RectTransform content;
        public ScrollRect scroll;
        public Row[] gearRows, materialRows;
        public Image[] tabs;
        public TMP_Text empty, capacityLabel;
        [SerializeField, Range(1, 4)] private int overscanRows = 2;
        [SerializeField, Min(0)] private float rowSpacing = 12;
        private int filter;
        public static readonly string[] CategoryNames = { "무기", "방어구", "장신구", "재료" };
        private bool initialized;
        private Row template;
        private RectTransform poolRoot;
        private float rowHeight, lastViewportHeight = -1, lastViewportWidth = -1;
        private bool rendering, confirming;
        private readonly List<string> itemUids = new List<string>();
        private readonly List<string> nextUids = new List<string>();
        public IReadOnlyList<string> FilteredInstanceIds => itemUids;
        private readonly List<int> items = new List<int>();
        private readonly List<int> nextItems = new List<int>();
        private readonly List<int> releases = new List<int>();
        private readonly Dictionary<int, Cell> active = new Dictionary<int, Cell>();
        private readonly Stack<Cell> pool = new Stack<Cell>();
        private sealed class Cell
        {
            public Row row;
            public int itemIndex = -1;
            public bool material;
            public string uid;
        }
        public int ActiveCellCount => active.Count;
        public int PooledCellCount => pool.Count;
        public int CreatedCellCount { get; private set; }
        public int OverscanRows => overscanRows;
        public int SelectedFilter => filter;
        public IReadOnlyList<int> FilteredItemIndices => items;
        public IEnumerable<Row> ActiveRows { get { foreach (var cell in active.Values) yield return cell.row; } }
        public bool TryGetActiveRow(int itemIndex, bool material, out Row row)
        {
            foreach (var cell in active.Values)
                if (cell.itemIndex == itemIndex && cell.material == material) { row = cell.row; return true; }
            row = null; return false;
        }
        public bool TryGetInstanceRow(string uid, out Row row)
        { foreach(var cell in active.Values)if(cell.uid==uid){row=cell.row;return true;}row=null;return false; }
        private float Stride => rowHeight + rowSpacing;
        private RectTransform Viewport => scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        public static Row CloneRow(Row template, Transform parent)
        {
            var root = Instantiate(template.root.gameObject, parent).GetComponent<RectTransform>();
            var originals = template.root.GetComponentsInChildren<TMP_Text>(true);
            var copies = root.GetComponentsInChildren<TMP_Text>(true);
            var images = template.root.GetComponentsInChildren<Image>(true);
            var clonedImages = root.GetComponentsInChildren<Image>(true);
            return new Row { root = root, title = copies[Array.IndexOf(originals, template.title)],
                detail = copies[Array.IndexOf(originals, template.detail)], count = copies[Array.IndexOf(originals, template.count)],
                icon = clonedImages[Array.IndexOf(images, template.icon)], button = root.GetComponent<Button>(), dismantle = root.Find("Dismantle")?.GetComponent<Button>() };
        }
        public static void ResizeRows(ref Row[] rows, int count, Transform parent)
        {
            if (rows == null || rows.Length == 0) throw new InvalidOperationException("Inventory row template is missing.");
            int originalCount = rows.Length;
            for (int i = count; i < originalCount; i++) Destroy(rows[i].root.gameObject);
            var template = rows[0];
            Array.Resize(ref rows, count);
            for (int i = originalCount; i < count; i++) rows[i] = CloneRow(template, parent);
        }
        void Initialize()
        {
            // The authored rows remain compatible with existing editor styling tools. At runtime
            // keep one hidden template; the catalog never determines the number of UI objects.
            template = gearRows[0];
            rowHeight = Mathf.Max(1, template.root.rect.height);
            poolRoot = new GameObject("InventoryCellPool", typeof(RectTransform)).GetComponent<RectTransform>();
            poolRoot.SetParent(transform, false);
            poolRoot.gameObject.SetActive(false);
            foreach (var row in gearRows) RetireAuthoredRow(row);
            foreach (var row in materialRows) RetireAuthoredRow(row);
            gearRows = new[] { template };
            materialRows = Array.Empty<Row>();
            scroll.onValueChanged.AddListener(OnScroll);
            initialized = true;
        }
        void RetireAuthoredRow(Row row)
        {
            row.root.gameObject.SetActive(false);
            row.root.SetParent(poolRoot, false);
            if (row != template)
            {
                if (Application.isPlaying) Destroy(row.root.gameObject);
                else DestroyImmediate(row.root.gameObject);
            }
        }
        Cell CreateCell()
        {
            var row = CloneRow(template, poolRoot);
            row.root.name = "InventoryCell";
            row.button = row.root.GetComponent<Button>() ?? row.root.gameObject.AddComponent<Button>();
            row.button.targetGraphic = row.root.GetComponent<Image>();
            row.button.targetGraphic.raycastTarget = true;
            row.button.interactable = true;
            row.button.onClick = new Button.ButtonClickedEvent();
            var cell = new Cell { row = row };
            // Subscribe once. A recycled cell always reads its current binding when clicked.
            row.button.onClick.AddListener(() => { if (cell.itemIndex >= 0) OpenDetails(cell.itemIndex, cell.material, cell.uid); });
            row.title.raycastTarget = row.detail.raycastTarget = row.icon.raycastTarget = false;
            if(row.dismantle!=null){row.dismantle.onClick=new Button.ButtonClickedEvent();row.dismantle.onClick.AddListener(()=>ConfirmDismantle(cell.uid));}
            row.count.raycastTarget=false;
            CreatedCellCount++;
            return cell;
        }
        void Bind(Cell cell, int listIndex)
        {
            cell.itemIndex = items[listIndex];
            cell.material = filter == 3;
            cell.uid = itemUids[listIndex];
            var row = cell.row;
            string title, description; Sprite icon;
            if (cell.material)
            {
                var item = page.catalog.materials[cell.itemIndex];
                title = item.title; description = item.description; icon = item.icon;
            }
            else
            {
                var item = page.catalog.gear[cell.itemIndex];
                title = item.title; description = item.description; icon = item.icon;
            }
            bool equipped=!cell.material&&page.Model.IsInstanceEquipped(cell.uid);
            bool battle=!cell.material&&page.Model.IsBattleInstance(cell.uid);
            row.count.text=cell.material?$"×{page.Model.MaterialCount(cell.itemIndex)}":equipped?"장비 중":battle?"전투 중":"";
            row.count.gameObject.SetActive(cell.material||equipped||battle);
            if(row.dismantle!=null){row.dismantle.gameObject.SetActive(!cell.material&&!equipped&&!battle);row.dismantle.interactable=page.Model.CanDismantle(cell.uid);}
            row.title.text = title; row.detail.text = description;
            row.icon.sprite = icon; row.icon.enabled = icon != null;
            row.root.anchoredPosition = new Vector2(0, -listIndex * Stride);
            row.root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, content.rect.width);
        }
        void Release(Cell cell)
        {
            var row = cell.row;
            cell.itemIndex = -1;cell.uid=null;
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null && events.currentSelectedGameObject == row.button.gameObject) events.SetSelectedGameObject(null);
            row.root.gameObject.SetActive(false);
            row.root.SetParent(poolRoot, false);
            pool.Push(cell);
        }
        void ReleaseAll()
        {
            foreach (var cell in active.Values) Release(cell);
            active.Clear();
        }
        void OnScroll(Vector2 _) { UpdateVisible(false); }
        void OnEnable() { if (initialized && page != null && page.Model != null) Refresh(); }
        void OnDisable() { if (initialized) ReleaseAll(); }
        void OnDestroy() { if (scroll != null) scroll.onValueChanged.RemoveListener(OnScroll); }
        void LateUpdate()
        {
            if (initialized && (Viewport.rect.height != lastViewportHeight || Viewport.rect.width != lastViewportWidth)) UpdateVisible(true);
        }
        void UpdateVisible(bool rebind)
        {
            if (!initialized || !isActiveAndEnabled || rendering) return;
            rendering = true;
            try
            {
                lastViewportHeight = Mathf.Max(0, Viewport.rect.height);
                lastViewportWidth = Viewport.rect.width;
                float maxOffset = Mathf.Max(0, content.rect.height - lastViewportHeight);
                float offset = Mathf.Clamp(content.anchoredPosition.y, 0, maxOffset);
                int first = Mathf.Max(0, Mathf.FloorToInt(offset / Stride) - overscanRows);
                int last = Mathf.Min(items.Count - 1, Mathf.CeilToInt((offset + lastViewportHeight) / Stride) - 1 + overscanRows);
                releases.Clear();
                foreach (var pair in active) if (pair.Key < first || pair.Key > last) releases.Add(pair.Key);
                foreach (int index in releases) { Release(active[index]); active.Remove(index); }
                // Prewarm enough for a middle-of-list view, including both buffer bands.
                // Normal scrolling then performs no Instantiate/Destroy or event allocation.
                int budget = Mathf.Min(items.Count, Mathf.CeilToInt(lastViewportHeight / Stride) + 1 + overscanRows * 2);
                while (active.Count + pool.Count < budget) pool.Push(CreateCell());
                for (int index = first; index <= last; index++)
                {
                    if (!active.TryGetValue(index, out var cell))
                    {
                        cell = pool.Pop(); cell.row.root.SetParent(content, false);
                        Bind(cell, index); active.Add(index, cell);
                        cell.row.root.gameObject.SetActive(true);
                    }
                    else if (rebind) Bind(cell, index);
                }
            }
            finally { rendering = false; }
        }
        public void OpenDetails(int index, bool material, string uid = null)
        {
            if (PopupManager.IsChanging || index < 0 || index >= (material ? page.catalog.materials.Length : page.catalog.gear.Length)) return;
            PopupManager.Show("ItemDetailsPopup", new ItemDetailsPopup.Selection { page = page, index = index, material = material, uid = uid });
        }
        public async UniTaskVoid ConfirmDismantle(string uid)
        {
            if(confirming||PopupManager.IsChanging||!page.Model.CanDismantle(uid))return;
            var model=page.Model;var instance=model.Instance(uid);confirming=true;
            try{
                bool accepted=await PopupManager.ShowAsync<bool>("DismantleConfirmationPopup",new ExpeditionNoticePopup.Content{
                    pausePolicy=page.pausePolicy,title="장비 분해",body=page.catalog.gear[instance.definition].title+"을(를) 분해하시겠습니까?",action="분해",confirmation=true}).AttachExternalCancellation(destroyCancellationToken);
                if(accepted){if(model.Dismantle(uid)){page.SaveGearChanges();Refresh();}else ToastPopup.Show("사용 중인 장비는 분해할 수 없습니다.");}
            }catch(OperationCanceledException){}finally{confirming=false;}
        }
        public void SelectFilter(int index)
        {
            if (index < 0 || index >= CategoryNames.Length) return;
            if (initialized && filter != index) ReleaseAll();
            filter = index;
            scroll.StopMovement();
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0);
            Refresh();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1;
            UpdateVisible(false);
        }
        public void Refresh()
        {
            if (page.Model == null) return;
            if (!initialized) Initialize();
            if(capacityLabel!=null)capacityLabel.text=filter==3?"보유 재료":CategoryNames[filter]+$" {page.Model.StorageUsed(filter)}/{page.Model.StorageLimit(filter)}";
            nextItems.Clear();
            nextUids.Clear();
            if(filter!=3)foreach(var instance in page.Model.Data.gearInstances)
            {
                if(page.Model.GearCategory(instance.definition)!=filter)continue;
                nextItems.Add(instance.definition);nextUids.Add(instance.uid);
            }
            if(filter==3)for(int i=0;i<page.catalog.materials.Length;i++)
                if(page.Model.MaterialCount(i)>0){nextItems.Add(i);nextUids.Add(null);}
            bool changed = items.Count != nextItems.Count;
            for (int i = 0; !changed && i < items.Count; i++) changed = items[i] != nextItems[i] || itemUids[i] != nextUids[i];
            if (changed) { ReleaseAll(); items.Clear(); items.AddRange(nextItems);itemUids.Clear();itemUids.AddRange(nextUids); }
            float height = Mathf.Max(0, items.Count * Stride - rowSpacing);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (changed)
            {
                scroll.StopMovement();
                content.anchoredPosition = new Vector2(content.anchoredPosition.x,
                    Mathf.Clamp(content.anchoredPosition.y, 0, Mathf.Max(0, height - Viewport.rect.height)));
            }
            empty.gameObject.SetActive(items.Count==0);
            empty.text = "보유한 " + CategoryNames[filter] + " 아이템이 없습니다.";
            for(int i=0;i<tabs.Length;i++)
            {
                bool selected=i==filter;
                tabs[i].color=new Color(.078f,.137f,.173f);
                var surface=tabs[i].transform.Find("Surface")?.GetComponent<Image>();
                if(surface!=null)surface.color=selected?new Color(.21f,.43f,.45f):new Color(.95f,.96f,.91f);
                var label=tabs[i].GetComponentInChildren<TMP_Text>();if(label!=null)label.color=selected?Color.white:new Color(.14f,.24f,.29f);
            }
            UpdateVisible(true);
        }
    }
}
