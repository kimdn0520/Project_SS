using DG.Tweening;
using UnityEngine;
namespace ProjectSS.Expedition
{
    public sealed class BattleDividerShimmer : MonoBehaviour
    {
        [SerializeField] private RectTransform shine;
        [SerializeField] private float startX=-100,endX=820;
        [SerializeField] private float interval=3.5f,duration=1.4f;
        private Sequence loop;
        private void OnEnable()
        {
            shine.anchoredPosition=new Vector2(startX,0);
            loop=DOTween.Sequence().AppendInterval(interval).Append(shine.DOAnchorPosX(endX,duration).SetEase(Ease.InOutSine)).SetLoops(-1,LoopType.Restart).SetUpdate(true);
        }
        private void OnDisable(){loop?.Kill();if(shine!=null)shine.anchoredPosition=new Vector2(startX,0);}
    }
}
