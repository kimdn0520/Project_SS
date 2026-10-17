using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition
{
    public sealed class VerticalGauge : MonoBehaviour
    {
        [SerializeField] private RectTransform reveal;
        [SerializeField] private Image fill;
        [SerializeField] private float fullHeight=134;
        public float Value { get; private set; }
        private float displayed;
        private bool initialized;
        public void SetValue(float value,Color color)
        {
            Value=Mathf.Clamp01(value);
            if(!initialized || Value==0 || !isActiveAndEnabled)displayed=Value;
            initialized=true;
            reveal.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,fullHeight*displayed);
            fill.enabled=Value>0;fill.color=color;
        }
        private void LateUpdate()
        {
            displayed=Mathf.MoveTowards(displayed,Value,Time.unscaledDeltaTime*5f);
            reveal.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,fullHeight*displayed);
        }
        private void OnDisable(){initialized=false;}
    }
}
