using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace ProjectSS.Expedition
{
    public sealed class SidebarShortcutFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform visual;
        [SerializeField] private Button button;
        private Tween press;
        public void OnPointerDown(PointerEventData data)
        {
            if(data.button!=PointerEventData.InputButton.Left||!button.IsInteractable())return;
            press?.Kill();press=visual.DOScale(.91f,.07f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
        public void OnPointerUp(PointerEventData data){Release();}
        public void OnPointerExit(PointerEventData data){Release();}
        void Release(){press?.Kill();press=visual.DOScale(1,.12f).SetEase(Ease.OutBack).SetUpdate(true);}
        void OnDisable(){press?.Kill();if(visual!=null)visual.localScale=Vector3.one;}
    }
}
