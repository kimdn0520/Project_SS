using UnityEngine;
using TMPro;
using DG.Tweening;

namespace ProjectSS.Expedition
{
    /// <summary>Scene-prewarmed effect: no Instantiate, component lookup, or coroutine on rent.</summary>
    public sealed class ExpeditionFx : PoolObject
    {
        [SerializeField] private TextMeshPro label;
        [SerializeField] private SpriteRenderer spark;
        [SerializeField] private PoolManager pool;
        private Sequence sequence;
        public bool IsBattleFeedback {get;private set;}
        [SerializeField] private Sprite defaultSpark;
        [SerializeField] private Material battleMaterial, miningMaterial;
        public void Loot(Vector3 position, Sprite icon, string text, bool equipment)
        {
            IsBattleFeedback=false;
            sequence?.Kill(); transform.position = position; transform.localScale = Vector3.one; transform.localRotation = Quaternion.identity;
            spark.sharedMaterial = miningMaterial; spark.sprite = icon; spark.color = Color.white;
            spark.transform.localScale = Vector3.one * (equipment ? .54f : .36f) / Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
            label.text = "\n\n" + text; label.color = equipment ? new Color(1,.8f,.4f) : Color.white;
            float duration = equipment ? 1.25f : .75f;
            sequence = DOTween.Sequence().Append(transform.DOMoveY(position.y+.85f,duration).SetEase(Ease.OutQuad))
                .Insert(duration*.45f,spark.DOFade(0,duration*.55f)).Join(label.DOFade(0,duration*.55f)).OnComplete(ReturnToPool);
        }

        public void Show(Vector3 position, string text, Color color)
        {
            sequence?.Kill();
            bool mining = position.y < 0;
            IsBattleFeedback=!mining;
            position.x = Mathf.Clamp(position.x, -2.4f, 2.4f);
            position.y = Mathf.Clamp(position.y, mining ? -2f : 3f, mining ? -.3f : 5.2f);
            spark.sharedMaterial = mining ? miningMaterial : battleMaterial;
            transform.position = position;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            label.text = text; label.color = color;
            spark.sprite = defaultSpark;
            spark.color = color; spark.transform.localScale = Vector3.one * 0.07f;
            sequence = DOTween.Sequence()
                .Append(transform.DOMoveY(position.y + 0.6f, 0.65f).SetEase(Ease.OutQuad))
                .Join(spark.transform.DOScale(0.2f, 0.3f))
                .Join(spark.DOFade(0, 0.3f))
                .Insert(0.3f, label.DOFade(0, 0.35f))
                .OnComplete(ReturnToPool);
        }
        public void Scatter(Vector3 position, Sprite sprite, Color color, int index)
        {
            IsBattleFeedback=false;
            sequence?.Kill();
            transform.position = position;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            label.text = "";
            spark.sharedMaterial = miningMaterial;
            spark.sprite = sprite; spark.color = color;
            spark.transform.localScale = Vector3.one * (0.10f + (index % 4) * 0.045f) / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            float direction = index % 2 == 0 ? -1 : 1;
            var destination = position + new Vector3(direction * Random.Range(.4f, 1.3f), Random.Range(-.9f, -.35f), 0);
            sequence = DOTween.Sequence()
                .Append(transform.DOJump(destination, Random.Range(.3f, .85f), 1, 0.62f))
                .Join(transform.DORotate(new Vector3(0, 0, direction * Random.Range(150f, 360f)), 0.62f))
                .Insert(0.32f, spark.DOFade(0, 0.30f))
                .OnComplete(ReturnToPool);
        }
        public void ReturnToPool() { IsBattleFeedback=false;sequence?.Kill(); pool.ReturnCached(this); }
        private void OnDisable() { sequence?.Kill(); }
    }
}
