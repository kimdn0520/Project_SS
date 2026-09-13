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
            animation?.Kill(); transform.position=position;transform.localRotation=Quaternion.identity;transform.localScale=Vector3.one*.35f;
            visual.color=Color.white;label.color=new Color(1,.82f,.42f);label.text="\n\n"+title;
            label.alpha=0;
            animation=DOTween.Sequence().Append(transform.DOMoveY(position.y+.8f,.42f).SetEase(Ease.OutCubic))
                .Join(transform.DOScale(1,.38f).SetEase(Ease.OutBack,1.25f))
                .Insert(.2f,label.DOFade(1,.2f)).AppendInterval(.45f)
                .Append(transform.DOMoveY(position.y+1.05f,.40f).SetEase(Ease.OutSine))
                .Join(visual.DOFade(0,.4f)).Join(label.DOFade(0,.4f)).OnComplete(Return);
        }
        public void Return(){animation?.Kill();if(pool!=null)pool.ReturnCached(this);}
        private void OnDisable(){animation?.Kill();}
    }
}
