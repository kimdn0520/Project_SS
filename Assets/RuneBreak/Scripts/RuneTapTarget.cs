using UnityEngine;
using UnityEngine.EventSystems;

namespace RuneBreak
{
    public sealed class RuneTapTarget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public RunePlayPage page;
        public bool slot;
        public int index;
        public void Tap()=>page.Tap(index,slot);
        public void OnPointerDown(PointerEventData e)=>page.Press(index,slot,true);
        public void OnPointerUp(PointerEventData e)=>page.Press(index,slot,false);
    }
}
