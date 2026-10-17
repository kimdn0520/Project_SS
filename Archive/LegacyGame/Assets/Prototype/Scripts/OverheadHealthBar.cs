using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition
{
    public sealed class OverheadHealthBar : MonoBehaviour
    {
        [SerializeField] private RectTransform reveal;
        [SerializeField] private Image fill;
        [SerializeField] private float fullWidth=94;
        [SerializeField] private float minimumCapWidth=8;
        [SerializeField] private float transitionDuration=.14f;
        private Tween transition;
        private bool initialized;
        private float target=1;
        public float Value { get; private set; } = 1;
        public Image Fill => fill;
        public void SetValue(float value,bool immediate=false)
        {
            value=Mathf.Clamp01(value);
            if(initialized&&!immediate&&Mathf.Approximately(value,target))return;
            transition?.Kill();target=value;
            bool snap=immediate||!initialized||!isActiveAndEnabled;initialized=true;
            if(snap){Render(value);return;}
            transition=DOTween.To(()=>Value,Render,value,transitionDuration).SetEase(Ease.OutQuad);
        }
        private void Render(float value)
        {
            Value=value;float width=fullWidth*value;
            // Keep sliced caps at their authored radius; clip only the final few pixels near zero.
            fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Max(minimumCapWidth,width));
            reveal.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
            fill.enabled=value>0;
        }
        private void OnEnable(){initialized=false;}
        private void OnDisable(){transition?.Kill();if(fill!=null&&reveal!=null)Render(target);initialized=false;}
    }
}
