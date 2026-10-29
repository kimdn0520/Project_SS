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
        private bool reducedMotion,arrivalPending,contacted;
        public bool TrailVisible=>gameObject.activeSelf&&image.enabled&&!reducedMotion&&elapsed-delay>duration*.20f&&elapsed-delay<duration;
        public Vector2 TrailPoint(float secondsAgo)=>Position(Mathf.Clamp01((elapsed-delay-secondsAgo)/duration));
        public void SetDestination(Vector2 destination){to=destination;control=to+new Vector2(68,-92);}
        public void Begin(Vector2 from,Vector2 to,int index,bool reduced)
        {
            this.from=from;reducedMotion=reduced;elapsed=0;delay=index*(reduced?.012f:.05f);duration=reduced?.28f:.48f;arrivalPending=contacted=false;
            burst=from+new Vector2(reduced?0:(index%3-1)*22,reduced?0:35+index%3*8);SetDestination(to);
            rect.anchoredPosition=from;rect.sizeDelta=Vector2.one*38;rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;image.color=Color.white;image.enabled=false;
        }
        public bool Tick(float dt)
        {
            elapsed+=dt;float t=elapsed-delay;image.enabled=t>=0;if(t<0)return false;
            float u=Mathf.Clamp01(t/duration),q=Mathf.Pow(u,1.9f);
            rect.anchoredPosition=Position(u);rect.localScale=Vector3.one;rect.localRotation=Quaternion.Euler(0,0,reducedMotion?0:Mathf.Sin(u*Mathf.PI)*10);
            if(u>=1&&!contacted){contacted=true;arrivalPending=true;}
            float absorb=Mathf.Clamp01((t-duration)/(reducedMotion?.035f:.085f));
            rect.sizeDelta=Vector2.one*(contacted?Mathf.Lerp(30,4,absorb):Mathf.Lerp(38,30,q));
            image.color=new Color(1,1,1,1-absorb*absorb);return absorb>=1;
        }
        public bool TakeArrival(out Vector2 point){point=to;if(!arrivalPending)return false;arrivalPending=false;return true;}
        private Vector2 Position(float u){float q=Mathf.Pow(u,1.9f),v=1-q;return reducedMotion?Vector2.Lerp(from,to,u):v*v*v*from+3*v*v*q*burst+3*v*q*q*control+q*q*q*to;}
    }
}
