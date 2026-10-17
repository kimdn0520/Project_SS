using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProjectSS.Expedition.Editor
{
    public static class ImportedUIArt
    {
        const string Folder = "Assets/Resources/Prefabs/Popups/";
        static Sprite popup, panel, menu, close, fill, track;
        static void Ref(Object target, string field, Object value)
        { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        static Sprite Import(string path, Vector4 border)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
            imp.spriteBorder = border; imp.spritePixelsPerUnit = 100; imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true; imp.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; imp.SetTextureSettings(settings); imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static RectTransform Rect(string name, Transform parent, float x=0, float y=0, float w=720, float h=1280)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0,1); r.anchoredPosition = new Vector2(x,-y); r.sizeDelta = new Vector2(w,h); return r;
        }
        static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
        static Image Image(RectTransform r, Sprite sprite, Color color, float ppu=1)
        { var im=r.gameObject.AddComponent<Image>(); Style(im,sprite,color,ppu); return im; }
        static void Style(Image im,Sprite sprite,Color color,float ppu=1)
        { im.sprite=sprite;im.type=UnityEngine.UI.Image.Type.Sliced;im.color=color;im.pixelsPerUnitMultiplier=ppu; }
        static TMP_Text Text(RectTransform r, TMP_FontAsset font, string value, float size=26)
        { var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.10f,.19f,.25f);t.raycastTarget=false;return t; }
        static void CloseStyle(Button b)
        {
            Style(b.GetComponent<Image>(),close,Color.white); b.GetComponent<Image>().type=UnityEngine.UI.Image.Type.Simple;
            b.GetComponent<Image>().preserveAspect=true;
            foreach(var text in b.GetComponentsInChildren<TMP_Text>(true))text.gameObject.SetActive(false);
            ((RectTransform)b.transform).sizeDelta=new Vector2(58,58);
        }
        static GameObject Common()
        {
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"CommonPopup.prefab");
            if(existing!=null)return existing;
            var root=Rect("CommonPopup",null); Stretch(root);root.gameObject.AddComponent<PopupSafeLayout>();
            var window=Rect("Window",root);window.anchorMin=window.anchorMax=window.pivot=new Vector2(.5f,.5f);window.anchoredPosition=Vector2.zero;
            Image(Rect("bg",window,30,230,660,830),popup,new Color(.70f,.85f,.83f));
            Stretch(Rect("Contents",window)); Stretch(Rect("Title",window));
            var b=Image(Rect("Close",window,620,260,58,58),close,Color.white).gameObject.AddComponent<Button>();CloseStyle(b);
            var asset=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Folder+"CommonPopup.prefab");Object.DestroyImmediate(root.gameObject);return asset;
        }
        static Transform AttachCommon(BasePopupHandler handler,GameObject common)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(common,handler.transform);
            return root.transform.Find("Window");
        }
        static void UpgradePopup(string path,GameObject common)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(root.transform.Find("CommonPopup")!=null)
                {
                    foreach(var child in root.transform.Cast<Transform>())if(child.name=="Dim"||child.name=="Curtain")Stretch((RectTransform)child);
                    PrefabUtility.SaveAsPrefabAsset(root,path);return;
                }
                var handler=root.GetComponent<BasePopupHandler>();var old=root.transform.Cast<Transform>().ToArray();
                var window=AttachCommon(handler,common);var contents=window.Find("Contents");
                var bg=window.Find("bg").GetComponent<RectTransform>();
                foreach(var t in old)
                {
                    if(t.name=="Panel"||t.name=="Card") {var r=(RectTransform)t;bg.anchoredPosition=r.anchoredPosition;bg.sizeDelta=r.sizeDelta;Object.DestroyImmediate(t.gameObject);}
                    else if(t.name=="Close"||t.name=="CloseEquipment")Object.DestroyImmediate(t.gameObject);
                    else if(t.name=="Dim"||t.name=="Curtain") {Stretch((RectTransform)t);t.SetAsFirstSibling();}
                    else t.SetParent(t.name=="Title"||t.name=="NoticeTitle"?window.Find("Title"):contents,false);
                }
                Ref(handler,"closeButton",window.Find("Close").GetComponent<Button>());
                var dim=root.transform.Find("Dim");
                if(dim!=null){var b=dim.GetComponent<Button>()??dim.gameObject.AddComponent<Button>();Ref(handler,"curtainButton",b);}
                foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
                    if(text.transform.parent.GetComponent<Button>()==null)text.color=new Color(.10f,.19f,.25f);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        static VeinSelectionPopup Veins(PlayPage page,GameObject common)
        {
            var root=Rect("VeinSelectionPopup",null);var canvas=root.gameObject.AddComponent<Canvas>();
            EditorUtility.CopySerialized(page.notice.Canvas,canvas);canvas.worldCamera=null;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();EditorUtility.CopySerialized(page.notice.Canvas.GetComponent<CanvasScaler>(),scaler);
            root.gameObject.AddComponent<GraphicRaycaster>();var group=root.gameObject.AddComponent<CanvasGroup>();
            var handler=root.gameObject.AddComponent<VeinSelectionPopup>();Ref(handler,"canvas",canvas);Ref(handler,"canvasGroup",group);
            var dim=Image(Rect("Curtain",root),null,new Color(.02f,.05f,.07f,.82f));Stretch(dim.rectTransform);
            Ref(handler,"curtainButton",dim.gameObject.AddComponent<Button>());
            var window=AttachCommon(handler,common);Ref(handler,"closeButton",window.Find("Close").GetComponent<Button>());
            Text(Rect("Heading",window.Find("Title"),80,280,490,48),page.depthLabel.font,"광맥 선택",32);
            Text(Rect("Hint",window.Find("Contents"),80,348,550,50),page.depthLabel.font,"채굴할 광맥을 선택하세요",23);
            handler.choices=new Button[3];handler.labels=new TMP_Text[3];
            for(int i=0;i<3;i++)
            {
                var row=Image(Rect("Vein"+i,window.Find("Contents"),75,420+i*155,570,130),panel,new Color(.86f,.94f,.9f),2);
                var b=row.gameObject.AddComponent<Button>();handler.choices[i]=b;UnityEventTools.AddIntPersistentListener(b.onClick,handler.Choose,i);
                var icon=Image(Rect("Ore",row.transform,18,22,80,80),page.depositSprites[i],Color.white);icon.type=UnityEngine.UI.Image.Type.Simple;icon.preserveAspect=true;icon.raycastTarget=false;
                handler.labels[i]=Text(Rect("Label",row.transform,115,38,435,70),page.depthLabel.font,new[]{"철 광맥","서리 광맥","유적 광맥"}[i],25);
            }
            root.gameObject.SetActive(false);var asset=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Folder+"VeinSelectionPopup.prefab");Object.DestroyImmediate(root.gameObject);return asset.GetComponent<VeinSelectionPopup>();
        }
        public static void Configure(PlayPage page)
        {
            popup=Import("Assets/ETC/bg_popup.png",new Vector4(49,49,49,49));
            panel=Import("Assets/Textures/UI/Common/Frames/popup-bg-minimal-578x765.png",new Vector4(64,64,64,64));
            menu=Import("Assets/Textures/UI/Common/Cells/menu_bg.png",new Vector4(22,22,22,22));
            close=Import("Assets/ETC/btn_close.png",Vector4.zero);
            fill=Import("Assets/Textures/UI/Common/Bars/progressbar_green.png",new Vector4(20,18,20,18));
            track=Import("Assets/Textures/UI/Common/Bars/bg_progressbar_navy.png",new Vector4(23,20,23,20));
            var common=Common();UpgradePopup(Folder+"ExpeditionNoticePopup.prefab",common);UpgradePopup(Folder+"EquipmentSelectionPopup.prefab",common);
            page.veinPopup=Veins(page,common);
            foreach(var b in page.routeButtons)if(b!=null)Object.DestroyImmediate(b.gameObject);
            page.routeButtons=Array.Empty<Button>();page.routePanels=Array.Empty<Image>();page.routeLabels=Array.Empty<TMP_Text>();
            foreach(var p in page.menuPanels)
            {
                foreach(var b in p.GetComponentsInChildren<Button>(true))if(b.name.StartsWith("BackToMine")){CloseStyle(b);((RectTransform)b.transform).anchoredPosition=new Vector2(632,-114);}
                foreach(var im in p.GetComponentsInChildren<Image>(true))
                    if(im.name=="Backdrop"||im.name=="BagBackground")Style(im,panel,new Color(.24f,.34f,.38f),1);
            }
            foreach(var im in page.menuButtons)Style(im,menu,Color.white);
            foreach(var bar in page.heroHealthBars.Concat(new[]{page.enemyHealthBar}))
            {
                foreach(var im in bar.GetComponentsInChildren<Image>(true))
                {Style(im,im==bar.Fill?fill:track,Color.white,5);if(im.name=="Rim"||im.name=="Track")im.enabled=false;}
                var data=new SerializedObject(bar);data.FindProperty("minimumCapWidth").floatValue=8;data.ApplyModifiedPropertiesWithoutUndo();
            }
            RestoreDigGauge(page);
            Sidebar(page);
            MenuPolishArt.Configure(page);
            BattleHeaderArt.Configure(page);
            if(System.IO.File.Exists(WindowSkinArt.FramePath)&&System.IO.File.Exists(WindowSkinArt.ClosePath))WindowSkinArt.Configure(page);
            if(System.IO.File.Exists("Assets/Prototype/Art/GoldBlueChest-v2.png"))GoldChestArt.Configure(page);
            if(System.IO.File.Exists("Assets/Textures/UI/Common/Frames/WarmWindow-v3.png"))WarmUIArt.Configure(page);
            FormationArt.Configure(page);
            SidebarIconArt.Configure(page);
            EditorUtility.SetDirty(page);
        }
        static void RestoreDigGauge(PlayPage page)
        {
            var extra=page.heatGauge.transform.Find("ImportedTrack");if(extra!=null)Object.DestroyImmediate(extra.gameObject);
            foreach(var im in page.heatGauge.GetComponentsInChildren<Image>(true))
            {
                string hex=im.name=="BurstGaugeFrame"?"101C28":im.name=="GoldTrim"?"D9A44A":im.name=="Steel"?"496979":im.name=="Well"?"101F2A":im.name=="Charge"?"F6BD51":im.name=="CapBottom"?"866237":im.name.StartsWith("Tick")?"F8D68B":"FFF0AE";
                ColorUtility.TryParseHtmlString("#"+hex,out var color);
                Style(im,ExpeditionUIArt.Panel(),color,4);im.enabled=true;im.raycastTarget=false;
            }
            page.heatBar.rectTransform.localRotation=Quaternion.identity;
            GaugeArt.Configure(page);
        }
        static void Sidebar(PlayPage page)
        {
            var existing=page.transform.Find("SidebarCanvas");
            if(existing!=null)
            {
                foreach(var child in existing.Find("SafeArea").Cast<Transform>().ToArray())child.SetParent(page.minePanel.transform,false);
                Object.DestroyImmediate(existing.gameObject);
            }
            var root=Rect("SidebarCanvas",page.transform);var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.planeDistance=10f;canvas.sortingOrder=200;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();var safe=Rect("SafeArea",root);Stretch(safe);
            var layout=root.gameObject.AddComponent<ExpeditionSidebarLayout>();layout.page=page;layout.safeArea=safe;
            var source=page.minePanel.transform.Find("SidebarChallenge");
            var oldVein=page.minePanel.transform.Find("SidebarVeins");if(oldVein!=null)Object.DestroyImmediate(oldVein.gameObject);
            var vein=Object.Instantiate(source.gameObject,page.minePanel.transform);vein.name="SidebarVeins";
            vein.GetComponentInChildren<TMP_Text>(true).text="광맥";vein.transform.Find("Visual/Icon").GetComponent<Image>().sprite=page.depositSprites[0];
            var button=vein.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,page.OpenVeins);
            foreach(var t in page.minePanel.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Sidebar")).ToArray())
            {
                bool right=t.name=="SidebarChallenge"||t.name=="SidebarVeins"||t.name.StartsWith("SidebarSlot_1");
                int row=t.name=="SidebarFirstCharge"||t.name=="SidebarVeins"?1:t.name.EndsWith("_2")?2:t.name.EndsWith("_1")?1:0;
                if(t.name=="SidebarSlot_1_1"){Object.DestroyImmediate(t.gameObject);continue;}
                var r=(RectTransform)t;t.SetParent(safe,false);r.anchorMin=r.anchorMax=new Vector2(right?1:0,.674f);r.pivot=new Vector2(right?1:0,1);r.anchoredPosition=new Vector2(right?-16:16,-row*99);
            }
        }
    }
}
