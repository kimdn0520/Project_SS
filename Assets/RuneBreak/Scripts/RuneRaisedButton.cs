using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    // Face and all its labels/icons are authored together in the prefab.
    public sealed class RuneRaisedButton : Button
    {
        public RectTransform face;
        public Vector2 restingPosition;
        public float pressDepth=4;

        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            base.DoStateTransition(state,instant);
            if(face!=null)face.anchoredPosition=restingPosition+Vector2.down*(state==SelectionState.Pressed?pressDepth:0);
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            if(face!=null)face.anchoredPosition=restingPosition;
        }
    }
}
