using System;
using UnityEngine;

namespace RuneBreak
{
    // Presentation only. Every target and resting pose is authored in MainPage.
    public sealed class RuneLobbyHero : MonoBehaviour
    {
        [Serializable] public struct FloatingPart
        {
            public RectTransform rect;
            public Vector2 position,amplitude;
            public float angle,rotation,period,phase;
        }
        public RuneLobbyPage page;
        public FloatingPart[] parts=Array.Empty<FloatingPart>();
        private double elapsed;
        public double Elapsed=>elapsed;
        private void OnEnable(){elapsed=0;Present(0,RunePreferences.Reduced);}
        private void OnDisable()=>ResetPose();
        private void Update()=>AdvancePresentation(Time.unscaledDeltaTime);
        public void AdvancePresentation(float dt)
        {
            if(!isActiveAndEnabled||page==null||page.IsModalBlocked)return;
            elapsed+=Mathf.Max(0,dt);Present(elapsed,RunePreferences.Reduced);
        }
        public void ResetPose()=>Present(0,true);
        private void Present(double time,bool reduced)
        {
            foreach(var part in parts)
            {
                if(part.rect==null)continue;
                double t=time*Math.PI*2/Math.Max(1,part.period)+part.phase;
                float x=reduced?0:(float)Math.Sin(t*.73)*part.amplitude.x;
                float y=reduced?0:(float)Math.Sin(t)*part.amplitude.y;
                float angle=part.angle+(reduced?0:(float)Math.Sin(t*.83+.4)*part.rotation);
                part.rect.anchoredPosition=part.position+new Vector2(x,y);
                part.rect.localRotation=Quaternion.Euler(0,0,angle);
            }
        }
    }
}
