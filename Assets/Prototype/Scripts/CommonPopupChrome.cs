using UnityEngine;
namespace ProjectSS.Expedition
{
    // PopupCommon owns the relationship between its frame and close control.
    // Consumers may resize the frame; they never author separate close coordinates.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class CommonPopupChrome : MonoBehaviour
    {
        public RectTransform frame, close;
        [SerializeField] private Vector2 inset = new Vector2(1, .2f);
        void OnEnable() { Apply(); }
        void LateUpdate() { Apply(); }
        public void Apply()
        {
            if(frame==null||close==null||close.parent==null)return;
            var corner=frame.TransformPoint(new Vector3(frame.rect.xMax,frame.rect.yMax,0));
            var parent=(RectTransform)close.parent;
            var local=(Vector2)parent.InverseTransformPoint(corner);
            close.anchorMin=close.anchorMax=close.pivot=Vector2.one;
            var desired=local-new Vector2(parent.rect.xMax,parent.rect.yMax)+new Vector2(-inset.x,-inset.y);
            if((close.anchoredPosition-desired).sqrMagnitude>.0001f)close.anchoredPosition=desired;
        }
    }
}
