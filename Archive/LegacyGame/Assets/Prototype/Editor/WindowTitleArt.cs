using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSS.Expedition.Editor
{
    public static class WindowTitleArt
    {
        static Sprite gold;
        static void Rect(RectTransform r,Vector2 position,Vector2 size)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size;}
        static Image Box(Transform parent,string name,Vector2 position,Vector2 size,Color color)
        {
            var child=parent.Find(name);
            if(child==null){child=new GameObject(name,typeof(RectTransform),typeof(Image)).transform;child.SetParent(parent,false);}
            var image=child.GetComponent<Image>();Rect(image.rectTransform,position,size);
            image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Common/Cells/Panel9Slice.png");
            image.type=Image.Type.Sliced;image.color=color;image.raycastTarget=false;return image;
        }
        static Color ColorOf(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var color);return color;}
        static void Plate(Transform parent,RectTransform frame,TMP_Text label)
        {
            var position=frame.anchoredPosition+new Vector2((frame.rect.width-360)/2,30);
            var plate=Box(parent,"TitlePlate",position,new Vector2(360,88),Color.white);
            plate.sprite=gold;plate.pixelsPerUnitMultiplier=6;
            foreach(Transform child in plate.transform)child.gameObject.SetActive(false);
            plate.transform.SetAsLastSibling();
            // Keep existing TMP objects so runtime title references continue to work.
            Rect(label.rectTransform,position+new Vector2(24,-16),new Vector2(312,56));
            label.transform.SetAsLastSibling();label.alignment=TextAlignmentOptions.Center;
            label.color=ColorOf("543018");label.fontStyle=FontStyles.Bold;
            label.fontSize=30;label.enableAutoSizing=true;label.fontSizeMin=20;label.fontSizeMax=30;
            label.characterSpacing=3;label.margin=Vector4.zero;label.textWrappingMode=TextWrappingModes.NoWrap;
            label.raycastTarget=false;
        }
        public static void Configure(PlayPage page)
        {
            const string art="Assets/Textures/UI/Common/Frames/GoldTitle-v2.png";
            var imp=(TextureImporter)AssetImporter.GetAtPath(art);imp.textureType=TextureImporterType.Sprite;
            imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=100;
            imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;
            imp.spritesheet=new[]{new SpriteMetaData{name="GoldTitle",rect=new Rect(40,170,1906,460),alignment=0,pivot=new Vector2(.5f,.5f),border=new Vector4(160,130,160,130)}};
            imp.SaveAndReimport();gold=AssetDatabase.LoadAllAssetsAtPath(art).OfType<Sprite>().First();
            foreach(var name in new[]{"CommonPopup","ExpeditionNoticePopup","EquipmentSelectionPopup","VeinSelectionPopup"})
            {
                var path="Assets/Resources/Prefabs/Popups/"+name+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var window=root.transform.Find(name=="CommonPopup"?"Window":"CommonPopup/Window");
                    var title=window.Find("Title");
                    var caption=title.Find("Caption")?.GetComponent<TMP_Text>();
                    if(caption==null)
                    {
                        var go=new GameObject("Caption",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(title,false);
                        caption=go.GetComponent<TMP_Text>();caption.font=page.depthLabel.font;caption.text="제목";
                    }
                    var existing=title.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t!=caption);
                    caption.gameObject.SetActive(existing==null);
                    Plate(title,(RectTransform)window.Find("bg"),existing!=null?existing:caption);
                    var kicker=window.Find("Contents/NoticeKicker");if(kicker!=null)kicker.gameObject.SetActive(false);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(var panel in page.menuPanels)
            {
                var frame=panel.GetComponentsInChildren<Image>(true).FirstOrDefault(i=>i.name=="Backdrop"||i.name=="BagBackground");
                var label=panel.transform.Find("Title")?.GetComponent<TMP_Text>();
                if(frame!=null&&label!=null)Plate(panel.transform,frame.rectTransform,label);
            }
        }
    }
}
