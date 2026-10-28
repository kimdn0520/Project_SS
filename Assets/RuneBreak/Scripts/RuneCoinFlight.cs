using UnityEngine;
using UnityEngine.UI;
namespace RuneBreak
{
    public sealed class RuneCoinFlight : PoolObject
    {
        public RectTransform rect;
        public Image image;
        private Vector2 from,to,burst,control;
        private float delay,elapsed,duration;
        private bool reducedMotion;
        public void Begin(Vector2 from,Vector2 to,int index,bool reduced)
        {
            this.from=from;this.to=to;reducedMotion=reduced;elapsed=0;delay=index*(reduced?.012f:.035f);duration=reduced?.32f:.64f+index%3*.025f;
            burst=from+new Vector2(reduced?0:(index%3-1)*17,reduced?0:32+index%3*8);control=new Vector2(Mathf.Lerp(burst.x,to.x,.5f)-35,Mathf.Lerp(burst.y,to.y,.42f)+70);
            rect.anchoredPosition=from;rect.sizeDelta=Vector2.one*38;rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;image.color=Color.white;image.enabled=false;
        }
        public bool Tick(float dt)
        {
            elapsed+=dt;float t=elapsed-delay;image.enabled=t>=0;if(t<0)return false;
            float u=Mathf.Clamp01(t/duration),q=Mathf.Pow(u,1.65f),v=1-q;
            rect.anchoredPosition=reducedMotion?Vector2.Lerp(from,to,u):v*v*v*from+3*v*v*q*burst+3*v*q*q*control+q*q*q*to;
            rect.sizeDelta=Vector2.one*Mathf.Lerp(38,26,q);rect.localScale=Vector3.one;rect.localRotation=Quaternion.Euler(0,0,reducedMotion?0:Mathf.Sin(u*Mathf.PI)*12);
            image.color=Color.white;return u>=1;
        }
    }
}
