using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    // The original Aseprite layers, authored as individual pieces in ResultPopup.
    public sealed class RuneRewardPile : MonoBehaviour
    {
        [Serializable] public struct Piece {public RectTransform rect;public Image image;public Vector2 position;public float delay;}
        public Piece[] pieces;
        public Image shadow;
        public Image[] glints;
        public RectTransform[] glintRects,launchPoints;
        private float age,claimAt=-100;
        public void ResetPose(bool reduced){age=0;claimAt=-100;Present(reduced);}
        public void Claim(){claimAt=age;}
        public void Tick(float dt,bool reduced){age+=dt;Present(reduced);}
        private void Present(bool reduced)
        {
            foreach(var p in pieces)
            {
                float t=age-p.delay,y=0,a=reduced?Mathf.Clamp01(age/.15f):Mathf.Clamp01(t/.07f);
                if(!reduced&&t<.22f)y=32*(1-Mathf.Pow(Mathf.Clamp01(t/.22f),2));
                else if(!reduced&&t<.42f)y=5*Mathf.Sin(Mathf.Clamp01((t-.22f)/.20f)*Mathf.PI);
                float claim=age-claimAt-p.delay*.5f;
                if(!reduced&&claim>=0&&claim<.24f)y+=3*Mathf.Sin(claim/.24f*Mathf.PI);
                p.rect.anchoredPosition=p.position+new Vector2(0,y);p.rect.localScale=Vector3.one;p.rect.localRotation=Quaternion.identity;p.image.color=new Color(1,1,1,a);
            }
            shadow.color=new Color(1,1,1,Mathf.Clamp01(age/.4f));
            for(int i=0;i<glints.Length;i++)
            {
                float t=(age-.55f-i*.63f)%2.8f;float a=!reduced&&age>.55f+i*.63f&&t>=0&&t<.36f?Mathf.Sin(t/.36f*Mathf.PI):0;
                glints[i].color=new Color(1,1,1,a*.85f);glintRects[i].localScale=Vector3.one*(.65f+.35f*a);glintRects[i].localRotation=Quaternion.Euler(0,0,t*24);
            }
        }
    }
}
