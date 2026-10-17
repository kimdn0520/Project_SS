using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition.Editor
{
    public static class GaugeArt
    {
        public static void Configure(PlayPage page)
        {
            var fill=page.heatBar;var frame=page.digButton.transform.parent.Find("BurstGaugeFrame");
            var clip=frame.Find("ChargeReveal") as RectTransform;
            if(clip==null){var go=new GameObject("ChargeReveal",typeof(RectTransform),typeof(RectMask2D));go.transform.SetParent(frame,false);clip=(RectTransform)go.transform;}
            clip.anchorMin=clip.anchorMax=clip.pivot=Vector2.zero;clip.anchoredPosition=new Vector2(11,13);clip.sizeDelta=new Vector2(20,134);clip.SetSiblingIndex(3);
            fill.transform.SetParent(clip,false);var rect=fill.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(20,134);
            fill.type=Image.Type.Sliced;fill.pixelsPerUnitMultiplier=4;fill.fillAmount=1;fill.raycastTarget=false;
            var gauge=frame.GetComponent<VerticalGauge>();if(gauge==null)gauge=frame.gameObject.AddComponent<VerticalGauge>();page.heatGauge=gauge;
            var so=new SerializedObject(gauge);so.FindProperty("reveal").objectReferenceValue=clip;so.FindProperty("fill").objectReferenceValue=fill;so.FindProperty("fullHeight").floatValue=134;so.ApplyModifiedPropertiesWithoutUndo();
            gauge.SetValue(0,fill.color);
        }
    }
}
