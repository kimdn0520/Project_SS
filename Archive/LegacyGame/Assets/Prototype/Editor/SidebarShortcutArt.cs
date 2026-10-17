using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition.Editor
{
    public static class SidebarShortcutArt
    {
        const string Icons="Assets/Layer Lab/2D Minimal-IconPack/Icons/256/";
        static Color Hex(string s){ColorUtility.TryParseHtmlString("#"+s,out var c);return c;}
        static Image Image(Transform parent,string name,float x,float y,float w,float h,Sprite sprite,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var im=go.GetComponent<Image>();im.sprite=sprite;im.color=color;im.raycastTarget=false;
            var r=im.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return im;
        }
        static void Ref(Object target,string name,Object value){var so=new SerializedObject(target);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        public static void Configure(PlayPage page)
        {
            Directory.CreateDirectory("Assets/Resources/Prefabs/UI");
            for(int i=0;i<3;i++)
            {
                string name=i==0?"SidebarStore":i==1?"SidebarFirstCharge":"SidebarChallenge";var parent=page.minePanel.transform;
                var old=parent.Find(i==2?"SidebarSlot_1_0":"SidebarSlot_0_"+i);if(old!=null)Object.DestroyImmediate(old.gameObject);
                old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);
                var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button),typeof(SidebarShortcutFeedback));
                var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(84,84);
                var input=go.GetComponent<Image>();input.color=Color.clear;input.raycastTarget=true;
                var button=go.GetComponent<Button>();button.targetGraphic=input;button.transition=Selectable.Transition.None;var navigation=button.navigation;navigation.mode=Navigation.Mode.None;button.navigation=navigation;
                var visual=new GameObject("Visual",typeof(RectTransform));visual.transform.SetParent(go.transform,false);var vr=(RectTransform)visual.transform;vr.anchorMin=vr.anchorMax=vr.pivot=new Vector2(.5f,.5f);vr.sizeDelta=new Vector2(84,84);
                var panel=ExpeditionUIArt.Panel();
                Image(vr,"Outline",0,0,84,84,panel,Hex("101922")).type=UnityEngine.UI.Image.Type.Sliced;
                Image(vr,"Rim",3,3,78,78,panel,Hex(i==0?"E4EBDD":"F5D878")).type=UnityEngine.UI.Image.Type.Sliced;
                Image(vr,"Inset",6,6,72,72,panel,Hex("18242D")).type=UnityEngine.UI.Image.Type.Sliced;
                Image(vr,"ColorField",8,8,68,55,panel,Hex(i==0?"8070A6":"6C969F")).type=UnityEngine.UI.Image.Type.Sliced;
                var icon=Image(vr,"Icon",7,-3,70,70,AssetDatabase.LoadAssetAtPath<Sprite>(Icons+(i==0?"UI_Shop_05_Purple.png":i==1?"UI_Rewards_Gift_02_Yellow.png":"UI_Rewards_Trophy_01_Gold.png")),Color.white);icon.preserveAspect=true;
                Image(vr,"NamePlate",5,57,74,23,panel,Hex("17212A")).type=UnityEngine.UI.Image.Type.Sliced;
                var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(vr,false);var text=label.GetComponent<TextMeshProUGUI>();text.font=page.depthLabel.font;text.fontSize=20;text.fontStyle=FontStyles.Bold;text.text=i==0?"스토어":i==1?"첫 충전":"도전";text.alignment=TextAlignmentOptions.Center;text.color=Hex("FFF5DC");text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.NoWrap;
                var tr=text.rectTransform;tr.anchorMin=tr.anchorMax=tr.pivot=new Vector2(0,1);tr.anchoredPosition=new Vector2(5,-56);tr.sizeDelta=new Vector2(74,25);
                var feedback=go.GetComponent<SidebarShortcutFeedback>();Ref(feedback,"visual",vr);Ref(feedback,"button",button);
                var prefab=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Resources/Prefabs/UI/"+name+".prefab");Object.DestroyImmediate(go);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);((RectTransform)instance.transform).anchoredPosition=new Vector2(i==2?620:16,-(417+(i==2?0:i)*99));
                if(i==2)UnityEventTools.AddIntPersistentListener(instance.GetComponent<Button>().onClick,page.OpenMenu,3);
            }
        }
    }
}
