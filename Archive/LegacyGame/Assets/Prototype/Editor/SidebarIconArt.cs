using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace ProjectSS.Expedition.Editor
{
    public static class SidebarIconArt
    {
        public static void Configure(PlayPage page)
        {
            var parent=page.transform.Find("SidebarCanvas/SafeArea");
            if(parent==null)return;
            foreach(var t in parent.Cast<Transform>().Where(t=>t.name.StartsWith("SidebarSlot_")).ToArray())Object.DestroyImmediate(t.gameObject);
            Directory.CreateDirectory("Assets/Resources/Prefabs/UI/Sidebar");
            string[] names={"SidebarStore","SidebarFirstCharge","SidebarChallenge","SidebarVeins","SidebarMoleSupport"};
            Type[] types={typeof(StoreIcon),typeof(FirstChargeIcon),typeof(ChallengeIcon),typeof(LodeIcon),typeof(MoleSupportIcon)};
            for(int i=0;i<names.Length;i++)
            {
                var old=parent.Find(names[i]);
                var source=old!=null?old:parent.Find("SidebarFirstCharge");
                var go=Object.Instantiate(source.gameObject);go.name=names[i];go.SetActive(true);
                foreach(var script in go.GetComponents<BaseSidebarIcon>())Object.DestroyImmediate(script);
                go.GetComponent<Button>().onClick=new Button.ButtonClickedEvent();
                var icon=(BaseSidebarIcon)go.AddComponent(types[i]);
                var label=go.transform.Find("Visual/Label").GetComponent<TMP_Text>();
                if(i==4)
                {
                    label.text="지원품";
                    var badge=go.transform.Find("Visual/Remaining");
                    if(badge==null){badge=Object.Instantiate(label.gameObject,label.transform.parent).transform;badge.name="Remaining";}
                    var text=badge.GetComponent<TMP_Text>();text.text="5/5";text.fontSize=17;text.color=new Color(1,.92f,.65f);
                    text.outlineWidth=.2f;text.outlineColor=Color.black;
                    var r=text.rectTransform;r.anchoredPosition=new Vector2(43,-4);r.sizeDelta=new Vector2(39,24);
                    ((MoleSupportIcon)icon).remainingText=text;
                    const string art="Assets/Textures/UI/Icons/Navigation/MoleSupport.png";
                    var importer=AssetImporter.GetAtPath(art) as TextureImporter;
                    if(importer!=null)
                    {
                        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=256;
                        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
                        importer.SaveAndReimport();
                        var image=go.transform.Find("Visual/Icon").GetComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(art);
                        image.rectTransform.anchoredPosition=new Vector2(7,4);image.rectTransform.sizeDelta=new Vector2(70,64);
                    }
                    var backing=go.transform.Find("Visual/CountBacking");
                    if(backing==null)
                    {
                        var b=new GameObject("CountBacking",typeof(RectTransform),typeof(Image));backing=b.transform;backing.SetParent(label.transform.parent,false);
                        var im=b.GetComponent<Image>();im.sprite=go.transform.Find("Visual/Inset").GetComponent<Image>().sprite;
                        im.type=Image.Type.Sliced;im.color=new Color(.08f,.13f,.16f);im.raycastTarget=false;
                    }
                    var br=(RectTransform)backing;br.anchorMin=br.anchorMax=br.pivot=new Vector2(0,1);br.anchoredPosition=new Vector2(48,4);br.sizeDelta=new Vector2(34,22);
                    text.rectTransform.anchoredPosition=new Vector2(48,4);text.rectTransform.sizeDelta=new Vector2(34,22);text.fontSize=15;
                    badge.SetAsLastSibling();
                }
                var prefab=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Resources/Prefabs/UI/Sidebar/"+types[i].Name+".prefab");
                Object.DestroyImmediate(go);if(old!=null)Object.DestroyImmediate(old.gameObject);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);instance.name=names[i];
                bool right=i==2||i==3;int row=i==1||i==3?1:i==4?2:0;
                var rect=(RectTransform)instance.transform;rect.anchorMin=rect.anchorMax=new Vector2(right?1:0,.674f);
                rect.pivot=new Vector2(right?1:0,1);rect.anchoredPosition=new Vector2(right?-16:16,-99*row);
            }
            EditorUtility.SetDirty(page);
        }
    }
}
