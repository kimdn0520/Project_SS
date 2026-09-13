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
        public void SetValue(float value,Color color)
        {
            Value=Mathf.Clamp01(value);
            reveal.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,fullHeight*Value);
            fill.enabled=Value>0;fill.color=color;
        }
    }
}
