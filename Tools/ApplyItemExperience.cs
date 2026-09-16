using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;

public static class ApplyItemExperience
{
    static TMP_FontAsset font;
    static Material outline;
    static void Rect(RectTransform r, float x, float y, float w, float h)
    { r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); }
    static TMP_Text Label(Transform parent, string name, float x, float y, float w, float h, float size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
        var t=go.GetComponent<TMP_Text>(); t.font=font; t.fontSharedMaterial=outline; t.fontSize=size;
        t.color=new Color(.14f,.24f,.29f); t.raycastTarget=false; t.textWrappingMode=TextWrappingModes.Normal;
        Rect(t.rectTransform,x,y,w,h); return t;
    }
    public static string Execute()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play first");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Arial Rounded Bold SDF.asset");
        outline=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Arial Rounded Bold SDF Outline.mat");
        var small=AssetDatabase.LoadAllAssetsAtPath("Assets/Textures/UI/Common/Buttons/JadeAction-v3.png").OfType<Sprite>().First(s=>s.name=="Action");
        var jadeMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Art/JadeActionUI.mat");
        const string pagePath="Assets/Resources/Prefabs/PlayPage.prefab";
        var root=PrefabUtility.LoadPrefabContents(pagePath);
        try
        {
            var page=root.GetComponent<PlayPage>();
            foreach(var b in page.heroGrid.GetComponentsInChildren<Button>(true).Where(b=>b.name=="ManageGear"))
            {
                var im=b.GetComponent<Image>(); im.sprite=small; im.overrideSprite=null; im.material=jadeMaterial;
                im.type=Image.Type.Sliced; im.pixelsPerUnitMultiplier=8f; im.color=Color.white;
                Rect(im.rectTransform,28,174,136,34);
                var label=b.GetComponentInChildren<TMP_Text>(); label.color=new Color(1,.96f,.82f);
                label.fontSizeMax=22; label.fontSizeMin=14;
                label.rectTransform.offsetMin=new Vector2(14,8); label.rectTransform.offsetMax=new Vector2(-14,-5);
            }
            const string headerPath="Assets/Fonts/Arial Rounded Bold SDF Battle Header.mat";
            var header=AssetDatabase.LoadAssetAtPath<Material>(headerPath);
            if(header==null){header=new Material(font.material); AssetDatabase.CreateAsset(header,headerPath);}
            header.SetColor(ShaderUtilities.ID_FaceColor,Color.white);
            header.SetColor(ShaderUtilities.ID_OutlineColor,Color.black);
            header.SetFloat(ShaderUtilities.ID_FaceDilate,.18f);
            header.SetFloat(ShaderUtilities.ID_OutlineWidth,.23f); header.EnableKeyword("OUTLINE_ON");
            EditorUtility.SetDirty(header);
            page.stageLabel.font=font; page.stageLabel.fontSharedMaterial=header;
            page.stageLabel.color=Color.white; page.stageLabel.fontStyle=FontStyles.Bold;
            page.stageLabel.fontWeight=FontWeight.Bold; page.stageLabel.fontSize=30;
            page.stageLabel.enableAutoSizing=false; page.stageLabel.UpdateMeshPadding();
            foreach(var row in page.inventoryView.gearRows.Concat(page.inventoryView.materialRows))
            {
                Rect(row.title.rectTransform,120,16,row.root.rect.width-140,34);
                Rect(row.detail.rectTransform,120,55,row.root.rect.width-140,52);
                row.title.fontSize=24; row.detail.fontSize=19;
                row.detail.textWrappingMode=TextWrappingModes.Normal;
                row.detail.overflowMode=TextOverflowModes.Ellipsis;
                row.title.textWrappingMode=TextWrappingModes.NoWrap; row.title.overflowMode=TextOverflowModes.Ellipsis;
                row.count.gameObject.SetActive(false);
            }
            SeedCatalog(page);
            PrefabUtility.SaveAsPrefabAsset(root,pagePath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        // Restore action buttons, not the informational vein cards.
        var action=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/ActionButton.prefab").GetComponent<Image>();
        const string veinPath="Assets/Resources/Prefabs/Popups/VeinSelection.prefab";
        root=PrefabUtility.LoadPrefabContents(veinPath);
        try
        {
            foreach(var b in root.GetComponent<VeinSelectionPopup>().choices)
            {
                var im=b.GetComponent<Image>(); im.sprite=action.sprite; im.overrideSprite=null; im.material=action.material;
                im.type=action.type; im.pixelsPerUnitMultiplier=action.pixelsPerUnitMultiplier; im.color=Color.white;
            }
            PrefabUtility.SaveAsPrefabAsset(root,veinPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        BuildPopup(); AssetDatabase.SaveAssets();
        return "Compact ManageGear, restored vein actions, bold white battle header, inventory descriptions and ItemDetails popup saved.";
    }
    static void SeedCatalog(PlayPage page)
    {
        var catalog=page.catalog;
        string[] descriptions={"낡았지만 아직 쓸만해 보이는 검이다.","정찰병이 가볍게 휴대하는 날렵한 단검이다.","마법을 배우는 견습생을 위한 소박한 완드다.","철기사가 정성껏 손질한 튼튼한 검이다.","단단한 바위도 부술 것 같은 묵직한 망치다.","손잡이에서 차가운 기운이 흐르는 지팡이다.","날 끝에 붉은 빛이 맴도는 수상한 단검이다.","작은 불씨를 품고 있는 따뜻한 지팡이다.","오랜 원정에도 몸을 든든하게 감싸주는 갑옷이다.","거친 탐험길에서 머리를 보호해 주는 투구다.","차가운 결정이 매달린 작은 행운의 부적이다."};
        // Migrate only the original authored descriptions, preserving later custom prose.
        for(int i=0;i<Math.Min(descriptions.Length,catalog.gear.Length);i++)
        {
            var g=catalog.gear[i];
            if(g.description=="전사의 기본 무기"||g.description=="빠른 연속 공격"||g.description=="마법 피해"||(g.description??"").StartsWith("기본 공격 ")||(g.description??"").StartsWith("공용 · "))g.description=descriptions[i];
            if(g.equipSlot!=0 && g.criticalDamage==150)g.criticalDamage=0;
        }
        if(catalog.materials==null||catalog.materials.Length==0)
        {
            catalog.materials=new MaterialDefinition[3];
            string[] names={"철광석","서리 결정","유적 파편"};
            string[] desc={"여러 장비를 만드는 데 쓰이는 단단한 광석이다.","은은한 냉기를 머금은 투명한 결정이다.","오래된 유적에서 발견한 신비로운 파편이다."};
            for(int i=0;i<3;i++)catalog.materials[i]=new MaterialDefinition{id=new[]{"iron","crystal","relic"}[i],title=names[i],description=desc[i],icon=page.inventoryView.materialRows[i].icon.sprite,storage=(MaterialStorage)(i+1)};
        }
        EditorUtility.SetDirty(catalog);
        foreach(var row in page.inventoryView.gearRows.Select((r,i)=>new{r,i}))
        {row.r.title.text=catalog.gear[row.i].title; row.r.detail.text=catalog.gear[row.i].description;}
        foreach(var row in page.inventoryView.materialRows.Select((r,i)=>new{r,i}))
        {row.r.title.text=catalog.materials[row.i].title; row.r.detail.text=catalog.materials[row.i].description;}
    }
    static void BuildPopup()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/Resources/Prefabs/Popups/ExpeditionNotice.prefab");
        try
        {
            root.name="ItemDetails";
            var old=root.GetComponent<ExpeditionNotice>(); var oldSo=new SerializedObject(old);
            var popup=root.AddComponent<ItemDetailsPopup>(); var so=new SerializedObject(popup);
            foreach(var field in new[]{"canvas","canvasGroup","curtainButton","closeButton"})
                so.FindProperty(field).objectReferenceValue=oldSo.FindProperty(field).objectReferenceValue;
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.title=(TMP_Text)oldSo.FindProperty("titleText").objectReferenceValue;
            var body=(TMP_Text)oldSo.FindProperty("bodyText").objectReferenceValue; body.gameObject.SetActive(false);
            var actionLabel=(TMP_Text)oldSo.FindProperty("actionText").objectReferenceValue;
            var button=actionLabel.GetComponentInParent<Button>(true); button.onClick=new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick,popup.OnClickClose); actionLabel.text="닫기";
            UnityEngine.Object.DestroyImmediate(old);
            var window=root.transform.Find("PopupCommon/Window");
            var frame=(RectTransform)window.Find("bg");
            // Fit all content inside the existing 660x830 common window.
            var contents=window.Find("Contents");
            foreach(Transform child in contents)if(child!=button.transform)child.gameObject.SetActive(false);
            popup.itemName=Label(contents,"ItemName",214,345,394,42,29);
            popup.description=Label(contents,"ItemDescription",214,397,394,95,22);
            popup.description.enableAutoSizing=true; popup.description.fontSizeMin=17; popup.description.fontSizeMax=22;
            var iconGo=new GameObject("ItemIcon",typeof(RectTransform),typeof(Image));iconGo.transform.SetParent(contents,false);
            popup.icon=iconGo.GetComponent<Image>();popup.icon.preserveAspect=true;popup.icon.raycastTarget=false;
            Rect(popup.icon.rectTransform,92,342,100,126);
            // Stats scroll independently, so long accessory/description data remains reachable.
            var scrollGo=new GameObject("StatsScroll",typeof(RectTransform),typeof(ScrollRect),typeof(Image),typeof(RectMask2D));scrollGo.transform.SetParent(contents,false);
            var scroll=scrollGo.GetComponent<ScrollRect>();Rect((RectTransform)scrollGo.transform,92,495,532,330);
            var bg=scrollGo.GetComponent<Image>();bg.color=new Color(1,1,1,.06f);
            popup.stats=Label(scrollGo.transform,"Stats",0,0,510,500,24);
            popup.stats.lineSpacing=10;
            scroll.content=popup.stats.rectTransform;scroll.viewport=(RectTransform)scrollGo.transform;
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            popup.ownership=Label(contents,"Ownership",92,840,532,36,21);
            Rect((RectTransform)button.transform,230,900,260,64);
            popup.title.text="아이템 정보";
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Prefabs/Popups/ItemDetails.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
