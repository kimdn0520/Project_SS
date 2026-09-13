using UnityEngine;
using TMPro;
using DG.Tweening;
namespace ProjectSS.Expedition
{
    public sealed class WeaponItem : PoolObject
    {
        public int catalogId;
        public SpriteRenderer visual;
        public TextMeshPro label;
        public PoolManager pool;
        private Sequence animation;
        public void Reveal(Vector3 position, string title)
        {
            animation?.Kill(); transform.position=position;transform.localRotation=Quaternion.identity;
            visual.color=Color.white;label.color=new Color(1,.82f,.42f);label.text="\n\n"+title;
            animation=DOTween.Sequence().Append(transform.DOMoveY(position.y+.85f,1.15f).SetEase(Ease.OutQuad))
                .Insert(.5f,visual.DOFade(0,.65f)).Join(label.DOFade(0,.65f)).OnComplete(Return);
        }
        public void Return(){animation?.Kill();if(pool!=null)pool.ReturnCached(this);}
        private void OnDisable(){animation?.Kill();}
    }
}
