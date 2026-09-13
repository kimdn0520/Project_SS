using System;
using UnityEngine;
using DG.Tweening;

namespace ProjectSS.Expedition
{
    public sealed class ExpeditionActor : MonoBehaviour
    {
        [Serializable] public struct PartSlot { public string name; public SpriteRenderer renderer; }
        [SerializeField] private Animator animator;
        [SerializeField] private Transform motionRoot;
        [SerializeField] private PartSlot[] weaponSlots;
        [SerializeField] private SpriteRenderer chest;
        [SerializeField] private SpriteRenderer helmet;
        [SerializeField] private Sprite armorChest, armorHelmet;
        [SerializeField] private Sprite defaultChest;
        [SerializeField] private SpriteRenderer[] tintedRenderers = Array.Empty<SpriteRenderer>();
        [SerializeField] private Color[] baseColors = Array.Empty<Color>();
        [SerializeField] private Vector3 restPosition;
        private Tween motion;
        private float idleAt;
        private bool ready;
        private bool alive = true;
        private bool walking;
        [SerializeField] private string walkState = "Walk";
        public void SetWalking(bool value)
        {
            walking = value; idleAt = 0;
            Play(value ? walkState : "Idle");
        }
        public void EquipArmor(Sprite armor, Sprite hat)
        {
            if (chest != null) chest.sprite = armor != null ? armor : defaultChest;
            if (helmet != null) { helmet.gameObject.SetActive(hat != null); if (hat != null) helmet.sprite = hat; }
        }
        [SerializeField] private Transform hpAnchor;
        public Vector3 HpPosition => hpAnchor.position;
        [SerializeField] private Transform miningHand;
        [SerializeField] private Quaternion miningHandRest = Quaternion.identity;
        private Tween miningSwing;

        public Vector3 HitPosition => motionRoot.position + Vector3.up * 0.55f;

        public void InitializeActor()
        {
            ready = true;
            alive = true;
            motionRoot.localPosition = restPosition;
            Play("Idle");
        }

        public void Equip(string slot, Sprite sprite, bool armor)
        {
            foreach (var part in weaponSlots)
            {
                bool visible = part.name == slot;
                part.renderer.gameObject.SetActive(visible);
                if (visible) part.renderer.sprite = sprite;
            }
            if (chest != null) chest.sprite = armor && armorChest != null ? armorChest : defaultChest;
            if (helmet != null) helmet.gameObject.SetActive(armor);
            if (armor && helmet != null && armorHelmet != null)
            {
                helmet.sprite = armorHelmet;
                helmet.gameObject.SetActive(true);
            }
        }

        public void Attack(bool ranged = false)
        {
            Play("Attack");
            idleAt = Time.time + 0.65f;
            motion?.Kill();
            motionRoot.localPosition = restPosition;
            motion = motionRoot.DOPunchPosition(new Vector3(ranged ? 0.06f : 0.22f, 0.025f, 0), 0.3f, 1, 0.2f);
        }

        public void Hit(bool shielded)
        {
            motion?.Kill();
            motionRoot.localPosition = restPosition;
            motion = motionRoot.DOPunchPosition(new Vector3(-0.12f, 0.015f, 0), 0.25f, 2, 0.25f);
            for (int i = 0; i < tintedRenderers.Length; i++)
            {
                var sr = tintedRenderers[i];
                sr.DOKill();
                sr.color = shielded ? new Color(0.4f, 0.95f, 1f) : new Color(1f, 0.58f, 0.5f);
                sr.DOColor(i < baseColors.Length ? baseColors[i] : Color.white, 0.25f);
            }
        }

        public void Celebrate() { Play("Victory"); idleAt = Time.time + 1.6f; }
        public void SetAlive(bool value)
        {
            if (alive == value) return;
            alive = value; idleAt = 0;
            Play(value ? "Idle" : "Defeat");
        }
        public void MineStrike(float interval)
        {
            // Contact coincides with damage/audio; rebound lifts the pick for the next strike.
            if (miningHand == null) { Attack(); return; }
            miningSwing?.Kill();
            miningHand.localRotation = miningHandRest;
            float duration = Mathf.Max(0.065f, interval * 0.94f);
            var contact = miningHandRest.eulerAngles;
            miningSwing = DOTween.Sequence()
                .AppendInterval(duration * 0.12f)
                .Append(miningHand.DOLocalRotate(contact + new Vector3(0, 0, 52), duration * 0.60f).SetEase(Ease.OutQuad))
                .Append(miningHand.DOLocalRotate(contact, duration * 0.28f).SetEase(Ease.InCubic));
            motion?.Kill(); motionRoot.localPosition = restPosition;
            motion = motionRoot.DOPunchPosition(new Vector3(0.045f, -0.018f, 0), duration, 1, 0.15f);
        }
        private void Update()
        {
            if (ready && idleAt > 0 && Time.time >= idleAt) { idleAt = 0; Play(walking ? walkState : "Idle"); }
        }
        private void Play(string state)
        {
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash(state)))
                animator.Play(state, 0, 0);
        }
        private void OnDisable()
        {
            motion?.Kill(); idleAt = 0;
            miningSwing?.Kill();
            if (miningHand != null) miningHand.localRotation = miningHandRest;
            if (motionRoot != null) motionRoot.localPosition = restPosition;
            for (int i = 0; i < tintedRenderers.Length; i++) { tintedRenderers[i].DOKill(); tintedRenderers[i].color = i < baseColors.Length ? baseColors[i] : Color.white; }
        }
    }
}
