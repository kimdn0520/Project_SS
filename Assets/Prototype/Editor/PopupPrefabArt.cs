using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition.Editor
{
    public static class PopupPrefabArt
    {
        static readonly Color Ink=new Color(.13f,.20f,.23f), Secondary=new Color(.28f,.38f,.36f), Accent=new Color(.24f,.43f,.40f);
        static void Ref(Object target,string field,Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        public static void Configure(PlayPage page)
        {
            const string texture="Assets/Textures/UI/Common/Frames/popup-bg-minimal-578x765.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(texture);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spriteBorder=new Vector4(64,64,64,64);importer.spritePixelsPerUnit=100;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(texture);
            page.popupManager=page.GetComponentInChildren<PopupManager>(true);
            var notice=Author(page.notice,sprite);var equipment=Author(page.equipmentPopup,sprite);
            Ref(notice,"pausePolicy",null);equipment.page=null;equipment.pausePolicy=null;
            Directory.CreateDirectory("Assets/Resources/Prefabs/Popups");
            var noticeAsset=PrefabUtility.SaveAsPrefabAsset(notice.gameObject,"Assets/Resources/Prefabs/Popups/ExpeditionNoticePopup.prefab").GetComponent<ExpeditionNoticePopup>();
            var equipAsset=PrefabUtility.SaveAsPrefabAsset(equipment.gameObject,"Assets/Resources/Prefabs/Popups/EquipmentSelectionPopup.prefab").GetComponent<EquipmentSelectionPopup>();
            if(!EditorUtility.IsPersistent(page.notice))Object.DestroyImmediate(page.notice.gameObject);
            if(!EditorUtility.IsPersistent(page.equipmentPopup))Object.DestroyImmediate(page.equipmentPopup.gameObject);
            Object.DestroyImmediate(notice.gameObject);Object.DestroyImmediate(equipment.gameObject);
            page.notice=noticeAsset;page.equipmentPopup=equipAsset;
            var so=new SerializedObject(page.popupManager);so.FindProperty("renderCamera").objectReferenceValue=null;so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(page);
        }
        static T Author<T>(T source,Sprite sprite) where T:BasePopupHandler
        {
            var popup=Object.Instantiate(source);popup.name=source.name;popup.gameObject.SetActive(false);popup.Canvas.worldCamera=null;popup.Canvas.renderMode=RenderMode.ScreenSpaceCamera;
            foreach(var image in popup.GetComponentsInChildren<Image>(true))
            {
                if(image.name=="Panel"||image.name=="Card") {image.sprite=sprite;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=1;image.color=Color.white;}
                else if(image.name=="Dim"||image.name=="Curtain")image.color=new Color(.025f,.055f,.075f,.78f);
                else if(image.name=="EquipmentList")image.color=new Color(.70f,.77f,.69f,.55f);
                else if(image.name.StartsWith("SelectEquipment"))image.color=new Color(.94f,.96f,.89f);
                else if(image.GetComponent<Button>()!=null)image.color=Accent;
            }
            foreach(var text in popup.GetComponentsInChildren<TMP_Text>(true))
            {
                text.color=text.name=="Stats"||text.name=="Count"||text.name=="Empty"?Secondary:Ink;
                if(text.transform.parent.GetComponent<Button>()!=null && !text.transform.parent.name.StartsWith("SelectEquipment"))text.color=new Color(.97f,.98f,.91f);
            }
            foreach(var button in popup.GetComponentsInChildren<Button>(true))
            {
                var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.93f,.97f,.91f);colors.pressedColor=new Color(.73f,.84f,.76f);colors.selectedColor=colors.highlightedColor;colors.disabledColor=new Color(.73f,.77f,.71f,.75f);button.colors=colors;
            }
            if(popup is EquipmentSelectionPopup equipment)
            {
                SetRect(equipment.title.rectTransform,80,280,460,48);
                SetRect((RectTransform)popup.transform.Find("CloseEquipment"),554,280,86,44);
                SetRect(equipment.scroll.GetComponent<RectTransform>(),70,338,580,594);
                equipment.content.sizeDelta=new Vector2(580,equipment.content.sizeDelta.y);
                foreach(var row in equipment.rows){row.root.sizeDelta=new Vector2(580,118);foreach(var t in new[]{row.title,row.detail,row.count})t.rectTransform.sizeDelta=new Vector2(460,t.rectTransform.sizeDelta.y);}
            }
            else
            {
                popup.transform.Find("NoticeKicker").gameObject.SetActive(false);
                SetRect((RectTransform)popup.transform.Find("NoticeTitle"),86,326,460,70);
                SetRect((RectTransform)popup.transform.Find("NoticeBody"),86,424,548,406);
                SetRect((RectTransform)popup.transform.Find("Close"),548,304,86,44);
                SetRect((RectTransform)popup.transform.Find("Accept"),86,880,548,64);
            }
            return popup;
        }
        static void SetRect(RectTransform r,float x,float y,float w,float h){r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    }
}
