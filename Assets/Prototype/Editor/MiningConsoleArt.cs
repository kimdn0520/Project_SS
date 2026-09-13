using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition.Editor
{
    public static class MiningConsoleArt
    {
        static Color Hex(string s){ColorUtility.TryParseHtmlString("#"+s,out var c);return c;}
        static Image Box(Transform parent,string name,float x,float y,float w,float h,string color)
        {
            var old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var im=go.GetComponent<Image>();im.sprite=ExpeditionUIArt.Panel();im.type=Image.Type.Sliced;im.color=Hex(color);im.raycastTarget=false;
            var r=im.rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return im;
        }
        public static void Configure(PlayPage page)
        {
            var navigation=page.menuButtons[0].transform.parent;var deck=navigation.Find("NavigationDeck");if(deck!=null)Object.DestroyImmediate(deck.gameObject);
            var parent=page.digButton.transform.parent;
            var dock=Box(parent,"MiningConsole",221,1170,278,110,"0C1721");dock.transform.SetAsFirstSibling();
            Box(dock.transform,"SteelRim",4,3,270,107,"456D81");Box(dock.transform,"Housing",8,7,262,103,"19384B");
            for(int i=0;i<4;i++)Box(dock.transform,"Vent"+i,76+i*33,73,20,4,"0D2130");
            ((RectTransform)page.digButton.transform).anchoredPosition=new Vector2(360,-1143);
            parent.Find("DigBase").GetComponent<RectTransform>().anchoredPosition=new Vector2(360,-1182);
            page.digLabel.gameObject.SetActive(false);
            var oldFill=page.heatBar;if(oldFill!=null)Object.DestroyImmediate(oldFill.gameObject);
            var frame=Box(parent,"BurstGaugeFrame",539,968,42,160,"101C28");
            Box(frame.transform,"GoldTrim",2,2,38,156,"D9A44A");Box(frame.transform,"Steel",5,5,32,150,"496979");Box(frame.transform,"Well",8,9,26,142,"101F2A");
            var fill=Box(frame.transform,"Charge",11,13,20,134,"F6BD51");fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Vertical;fill.fillOrigin=0;page.heatBar=fill;
            Box(fill.transform,"GlassHighlight",3,1,3,132,"FFF0AF").color=new Color(1,1,1,.16f);
            // Glass highlight sits on the well, so it does not imply a charge while empty.
            fill.transform.Find("GlassHighlight").gameObject.SetActive(false);
            for(int i=1;i<6;i++)Box(frame.transform,"Tick"+i,27,13+i*134/6f,5,2,"F8D68B");
            Box(frame.transform,"CapTop",12,3,18,3,"FFF0AE");Box(frame.transform,"CapBottom",12,154,18,3,"866237");
            GaugeArt.Configure(page);
            DigFeedbackArt.Configure(page);
        }
    }
}
