using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RuneBreak
{
    // Face and all its labels/icons are authored together in the prefab.
    public sealed class RuneRaisedButton : Button
    {
        public RectTransform face;
        public Vector2 restingPosition;
        public float pressDepth=4;
        [Header("Single block interaction")]
        public bool scalePress;
        [Range(.9f,1f)] public float pressedScale=.96f;
        public float pressDuration=.07f,releaseDuration=.14f;
        private float fromScale=1,toScale=1,elapsed,duration;
        private bool animating;

        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            base.DoStateTransition(state,instant);
            if(face==null)return;
            if(!scalePress){face.anchoredPosition=restingPosition+Vector2.down*(state==SelectionState.Pressed?pressDepth:0);return;}
            face.anchoredPosition=restingPosition;
            float target=state==SelectionState.Pressed?pressedScale:1;
            if(instant||!Application.isPlaying||RunePreferences.Reduced||state==SelectionState.Disabled)
            {
                animating=false;face.localScale=Vector3.one*target;toScale=target;return;
            }
            // Highlight/selection changes during a release must not restart the tween.
            if(animating&&Mathf.Approximately(target,toScale))return;
            fromScale=face.localScale.x;toScale=target;elapsed=0;
            duration=Mathf.Max(.001f,target<fromScale?pressDuration:releaseDuration);animating=true;
        }
        private void Update()
        {
            if(!animating||face==null)return;
            elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/duration);
            face.localScale=Vector3.one*Mathf.Lerp(fromScale,toScale,t*t*(3-2*t));
            if(t>=1)animating=false;
        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if(scalePress)DoStateTransition(IsInteractable()?SelectionState.Normal:SelectionState.Disabled,false);
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            animating=false;fromScale=toScale=1;
            if(face!=null){face.anchoredPosition=restingPosition;if(scalePress)face.localScale=Vector3.one;}
        }
    }
}
