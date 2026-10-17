using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace ProjectSS.Expedition.Editor
{
    public static class MenuPolishArt
    {
        static readonly Color Ink=Hex("243E49"),Muted=Hex("52727B"),Paper=Hex("F2F6E9"),Rim=Hex("14232C"),Accent=Hex("356D72");
        static Sprite cell;
        static Color Hex(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
        static void Rect(RectTransform r,float x,float y,float w,float h)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static Image Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var t=parent.Find(name);if(t==null){t=new GameObject(name,typeof(RectTransform),typeof(Image)).transform;t.SetParent(parent,false);}
            var im=t.GetComponent<Image>();Rect((RectTransform)t,x,y,w,h);Skin(im,color);im.raycastTarget=false;return im;
        }
        static void Skin(Image im,Color color)
        {im.sprite=cell;im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=1;im.color=color;}
        static void ButtonColors(Button b)
        {var c=b.colors;c.normalColor=Color.white;c.highlightedColor=Hex("E6F3EC");c.pressedColor=Hex("BCD8D0");c.selectedColor=Color.white;c.disabledColor=Color.white;b.colors=c;}
        static void Surface(Image im,Color color)
        {
            Skin(im,Rim);var r=im.rectTransform;var inside=Box(r,"Surface",4,4,r.rect.width-8,r.rect.height-8,color);
            inside.transform.SetAsFirstSibling();
        }
        static void Label(Transform root,string name,float x,float y,float w,float h,float size,Color color)
        {var t=root.Find(name)?.GetComponent<TMP_Text>();if(t==null)return;Rect(t.rectTransform,x,y,w,h);t.fontSize=size;t.color=color;}
        static void Row(ExpeditionInventory.Row row,float width)
        {
            row.root.sizeDelta=new Vector2(width,124);Surface(row.root.GetComponent<Image>(),Paper);
            var well=Box(row.root,"IconWell",14,16,90,90,Hex("D7E8E5"));well.transform.SetSiblingIndex(1);
            Rect(row.icon.rectTransform,21,23,76,76);row.icon.transform.SetAsLastSibling();
            Rect(row.title.rectTransform,120,14,width-138,32);row.title.fontSize=24;row.title.fontStyle=FontStyles.Bold;row.title.color=Ink;
            Rect(row.detail.rectTransform,120,50,width-138,32);row.detail.fontSize=18;row.detail.color=Muted;
            Rect(row.count.rectTransform,120,88,width-138,27);row.count.fontSize=17;row.count.color=Accent;
            if(row.button!=null)ButtonColors(row.button);
        }
        public static void Configure(PlayPage page)
        {
            cell=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Common/Cells/Panel9Slice.png");
            foreach(var panel in page.menuPanels)
            {
                foreach(var t in panel.GetComponentsInChildren<TMP_Text>(true))t.color=t.name=="SaveHint"||t.name=="ComingSoon"?Muted:Ink;
                foreach(var im in panel.GetComponentsInChildren<Image>(true))
                {
                    if(im.name=="Backdrop"||im.name=="BagBackground"){im.color=Color.white;Rect(im.rectTransform,0,100,720,1073);}
                    else if(im.GetComponent<Button>()!=null&&!im.name.StartsWith("BackToMine"))
                    {Surface(im,Paper);ButtonColors(im.GetComponent<Button>());}
                }
                var title=panel.transform.Find("Title")?.GetComponent<TMP_Text>();
                if(title!=null){Rect(title.rectTransform,54,128,520,50);title.fontSize=34;title.fontStyle=FontStyles.Bold;}
                foreach(var b in panel.GetComponentsInChildren<Button>(true))if(b.name.StartsWith("BackToMine"))Rect((RectTransform)b.transform,610,120,58,58);
            }
            Heroes(page);
            AlignHeroMenuIcon(page);
            var bag=page.inventoryView;
            Rect(page.bagTitle.rectTransform,54,190,612,35);
            for(int i=0;i<bag.tabs.Length;i++)
            {Rect(bag.tabs[i].rectTransform,54+i*210,242,192,50);Surface(bag.tabs[i],Paper);}
            AlignBagTabs(bag);
            Rect((RectTransform)bag.scroll.transform,54,318,612,786);bag.scroll.GetComponent<Image>().color=Color.clear;
            bag.content.sizeDelta=new Vector2(612,bag.content.sizeDelta.y);
            foreach(var row in bag.gearRows.Concat(bag.materialRows))Row(row,612);
            foreach(var p in page.heroEquipmentPanels)
            {
                foreach(var im in p.GetComponentsInChildren<Image>(true))if(im.name.StartsWith("Slot"))Surface(im,Paper);
            }
            foreach(var im in page.menuPanels[4].GetComponentsInChildren<Image>(true))
                if(im.name.EndsWith("Preview")){Rect(im.rectTransform,54,im.name.StartsWith("Pickaxe")?260:458,612,166);Surface(im,Paper);Label(im.transform,"Title",136,24,320,38,26,Ink);Label(im.transform,"Description",136,76,448,60,20,Muted);Label(im.transform,"ComingSoon",468,28,116,32,18,Muted);}
            Popup("Assets/Resources/Prefabs/Popups/EquipmentSelectionPopup.prefab");
            Popup("Assets/Resources/Prefabs/Popups/VeinSelectionPopup.prefab");
            Popup("Assets/Resources/Prefabs/Popups/ExpeditionNoticePopup.prefab");
            Toast(page.depthLabel.font);
            EditorUtility.SetDirty(page);
        }
        public static void AlignHeroMenuIcon(PlayPage page)
        {
            var image=page.menuIcons[0].GetComponent<Image>();
            var r=image.rectTransform;
            image.preserveAspect=true;
            r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,.5f);
            r.sizeDelta=new Vector2(112,112);r.localScale=Vector3.one;
            // Compensate for the portrait's transparent padding so the visible hero is centered.
            r.anchoredPosition=new Vector2(2.275f,-1.625f);
            page.menuIconRest[0]=r.localPosition;
        }
        public static void AlignBagTabs(ExpeditionInventory bag)
        {
            float width = (612 - 12 * (bag.tabs.Length - 1)) / bag.tabs.Length;
            for(int i = 0; i < bag.tabs.Length; i++)
            {
                var tab = bag.tabs[i];
                Rect(tab.rectTransform,54+i*(width+12),242,width,50);
                var surface=tab.transform.Find("Surface") as RectTransform;
                if(surface!=null)
                {
                    surface.anchorMin=Vector2.zero;surface.anchorMax=Vector2.one;
                    surface.pivot=new Vector2(.5f,.5f);
                    surface.offsetMin=new Vector2(4,4);surface.offsetMax=new Vector2(-4,-4);
                }
                foreach(var label in tab.GetComponentsInChildren<TMP_Text>(true))
                {
                    var r=label.rectTransform;
                    r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);
                    r.offsetMin=new Vector2(8,4);r.offsetMax=new Vector2(-8,-4);
                    label.margin=Vector4.zero;label.alignment=TextAlignmentOptions.Center;
                }
            }
        }
        static void Heroes(PlayPage page)
        {
            var grid=page.heroGrid.transform;
            Label(grid,"CollectionHint",54,190,612,36,19,Muted);
            for(int i=0;i<8;i++)
            {
                var card=grid.Find("HeroCard"+i);Rect((RectTransform)card,54+(i%3)*210,260+(i/3)*268,192,244);
                Surface(card.GetComponent<Image>(),i<3?Paper:Hex("D6E3DC"));ButtonColors(card.GetComponent<Button>());
                if(i<3)
                {
                    var well=card.Find("PortraitWell").GetComponent<Image>();Rect(well.rectTransform,10,10,172,157);Skin(well,Hex(new[]{"D8DDD0","CDE4DC","DED8E8"}[i]));
                    Rect((RectTransform)card.Find("Portrait"),23,35,146,132);
                    Label(card,"Role",22,17,148,28,18,Muted);
                    Label(card,"HeroName",10,164,172,44,22,Ink);
                    var name=card.Find("HeroName").GetComponent<TMP_Text>();name.overflowMode=TextOverflowModes.Overflow;name.textWrappingMode=TextWrappingModes.NoWrap;
                    var strip=grid.Find("ActiveStrip"+i)??card.Find("ActiveStrip"+i);strip.SetParent(card,false);Rect((RectTransform)strip,34,209,124,25);Skin(strip.GetComponent<Image>(),Accent);
                    var active=grid.Find("ActiveLabel"+i)??card.Find("ActiveLabel"+i);active.SetParent(card,false);Rect((RectTransform)active,34,207,124,28);active.GetComponent<TMP_Text>().color=Paper;active.GetComponent<TMP_Text>().fontSize=15;
                }
                else {Label(card,"Unknown",16,52,160,100,52,Hex("8AA6A4"));Label(card,"Locked",16,174,160,38,20,Muted);}
            }
        }
        static void Popup(string path)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var equip=root.GetComponent<EquipmentSelectionPopup>();
                if(equip!=null)
                {
                    Rect((RectTransform)equip.scroll.transform,70,350,580,586);equip.scroll.GetComponent<Image>().color=Color.clear;
                    foreach(var row in equip.rows)Row(row,580);
                    Surface(equip.unequip.GetComponent<Image>(),Paper);ButtonColors(equip.unequip);
                    foreach(var t in equip.unequip.GetComponentsInChildren<TMP_Text>())t.color=Ink;
                }
                var vein=root.GetComponent<VeinSelectionPopup>();
                if(vein!=null)foreach(var b in vein.choices){Surface(b.GetComponent<Image>(),Paper);ButtonColors(b);}
                var accept=root.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name=="Accept");
                if(accept!=null){Surface(accept.GetComponent<Image>(),Paper);ButtonColors(accept);foreach(var t in accept.GetComponentsInChildren<TMP_Text>())t.color=Ink;}
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void Toast(TMP_FontAsset font)
        {
            var root=new GameObject("ToastPopup",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(ToastPopup));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.planeDistance=10f;canvas.sortingOrder=5000;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var bubble=Box(root.transform,"Bubble",0,0,580,90,Accent);bubble.rectTransform.anchorMin=bubble.rectTransform.anchorMax=bubble.rectTransform.pivot=new Vector2(.5f,.5f);bubble.rectTransform.anchoredPosition=Vector2.zero;
            var group=bubble.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=group.interactable=false;
            var go=new GameObject("Message",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(bubble.transform,false);
            var t=go.GetComponent<TextMeshProUGUI>();t.font=font;t.fontSize=24;t.color=Paper;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.overflowMode=TextOverflowModes.Ellipsis;
            var r=t.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(24,16);r.offsetMax=new Vector2(-24,-16);
            var so=new SerializedObject(root.GetComponent<ToastPopup>());so.FindProperty("bubble").objectReferenceValue=bubble.rectTransform;so.FindProperty("messageText").objectReferenceValue=t;so.FindProperty("group").objectReferenceValue=group;so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Prefabs/UI/ToastPopup.prefab");Object.DestroyImmediate(root);
        }
    }
}
