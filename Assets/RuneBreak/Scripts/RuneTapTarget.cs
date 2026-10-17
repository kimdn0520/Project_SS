using UnityEngine;
using UnityEngine.EventSystems;

namespace RuneBreak
{
    public sealed class RuneTapTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RunePlayPage page;
        public bool slot;
        public int index;
        public void Tap()=>page.Tap(index,slot);
        public void OnPointerEnter(PointerEventData e)=>page.Hover(index,slot);
        public void OnPointerExit(PointerEventData e)=>page.ClearHover();
        public void OnPointerDown(PointerEventData e)=>page.Press(index,slot,true);
        public void OnPointerUp(PointerEventData e)=>page.Press(index,slot,false);
    }
}
