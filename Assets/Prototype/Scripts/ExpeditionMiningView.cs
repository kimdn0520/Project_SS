using DG.Tweening;
using UnityEngine;

namespace ProjectSS.Expedition
{
    public sealed class ExpeditionMiningView : MonoBehaviour
    {
        [SerializeField] private Transform shakeRoot;
        [SerializeField] private SpriteRenderer[] rocks, deposits;
        [SerializeField] private SpriteRenderer impactFlash;
        [SerializeField] private Sprite[] oreSprites;
        [SerializeField] private Sprite[] damageSprites;
        [SerializeField] private Sprite fragmentSprite;
        [SerializeField] private float bandHeight = 3.1f, bandCycle = 12.4f;
        [SerializeField] private Vector3[] rockPositions, rockScales;
        [SerializeField] private PoolManager pool;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] hitSounds;
        [SerializeField] private AudioClip breakSound;
        private int nextHitSound;
        [SerializeField] private LineRenderer[] cracks;
        [SerializeField] private LineRenderer[] crackHighlights;
        [SerializeField] private SpriteRenderer chestVisual;
        [SerializeField] private GameObject chestOpenRoot;
        [SerializeField] private Transform chestLid;
        [SerializeField] private SpriteRenderer chestGlow;
        [SerializeField] private Vector3 lidRest;
        [SerializeField] private SpriteRenderer lidFace, lidInside, chestInterior;
        public const float ChestRewardDelay = .78f, ChestDuration = 1.9f;
        private Sequence chestAnimation;
        [SerializeField] private Transform descendingMiner, fractureRoot;
        [SerializeField] private Transform[] shaftBands;
        [SerializeField] private Vector3 minerRest;
        [SerializeField] private Vector3[] bandRest;
        [SerializeField] private Vector3 hitOffset = new Vector3(.55f, .65f, 0);
        private Sequence descent;
        private int activeRock;
        private bool pendingChest;
        public bool IsDescending => descent != null && descent.IsActive();
        public Vector3 ActiveRockPosition => rocks[activeRock].transform.position;
        public Vector3 ChestPosition => chestVisual.transform.position;
        public void OpenChest()
        {
            ClearFractures(); chestVisual.gameObject.SetActive(false); chestOpenRoot.SetActive(true);
            chestAnimation?.Kill(); chestLid.localPosition=lidRest;chestLid.localRotation=Quaternion.identity;chestGlow.color=new Color(1,.8f,.35f,0);
            lidFace.enabled=true;lidInside.enabled=false;chestInterior.enabled=false;
            // Anticipation, hinge turn, inside face, then reward light. The lid never detaches.
            chestAnimation=DOTween.Sequence()
                .Append(chestLid.DOPunchRotation(new Vector3(0,0,3),.16f,2,.2f))
                .Append(chestLid.DOLocalRotate(new Vector3(-86,0,0),.20f).SetEase(Ease.InQuad))
                .AppendCallback(()=>{lidFace.enabled=false;lidInside.enabled=true;chestInterior.enabled=true;})
                .Append(chestLid.DOLocalRotate(Vector3.zero,.32f).SetEase(Ease.OutBack,1.2f))
                .Join(chestLid.DOLocalMove(lidRest+Vector3.up*.03f,.32f))
                .Insert(.40f,chestGlow.DOFade(.9f,.28f))
                .AppendInterval(.35f).Append(chestGlow.DOFade(.25f,.6f));
        }
        public void SetChest(bool chest)
        {
            pendingChest = chest;
            if (IsDescending) return;
            chestAnimation?.Kill();chestOpenRoot.SetActive(false);
            chestVisual.gameObject.SetActive(chest);
            // A chest rests on a ledge; keep support underneath the miner until it opens.
            rocks[activeRock].enabled = true;
            if (chest) ClearFractures();
        }
        private Tween fractureReset;
        private int route = -1;
        public Vector3 HitPosition => transform.TransformPoint(rockPositions[0]) + hitOffset;
        public void Descend()
        {
            if (IsDescending) return;
            int brokenIndex = activeRock;
            activeRock = (activeRock + 1) % rocks.Length;
            float pitch = rockPositions[0].y - rockPositions[1].y;
            descent = DOTween.Sequence().AppendInterval(.08f)
                .AppendCallback(() => { ClearFractures(); rocks[brokenIndex].enabled = false; chestVisual.gameObject.SetActive(false); chestOpenRoot.SetActive(false); });
            for (int slot = 0; slot < rocks.Length - 1; slot++)
            {
                int index = (activeRock + slot) % rocks.Length;
                descent.Join(rocks[index].transform.DOLocalMove(rockPositions[slot], .34f).SetEase(Ease.InOutSine));
            }
            descent.Insert(.08f, descendingMiner.DOLocalMove(minerRest + Vector3.down * .36f, .14f).SetEase(Ease.InQuad));
            descent.Insert(.22f, descendingMiner.DOLocalMove(minerRest, .20f).SetEase(Ease.OutQuad));
            for (int i = 0; i < shaftBands.Length; i++)
            {
                var band = shaftBands[i]; var target = band.localPosition + Vector3.up * pitch;
                descent.Insert(.08f, band.DOLocalMove(target, .34f).SetEase(Ease.InOutSine));
            }
            descent.OnComplete(() =>
            {
                rocks[brokenIndex].transform.localPosition = rockPositions[rocks.Length - 1];
                rocks[brokenIndex].enabled = true;
                for (int i = 0; i < shaftBands.Length; i++)
                    if (shaftBands[i].localPosition.y > bandRest[0].y + bandHeight) shaftBands[i].localPosition -= Vector3.up * bandCycle;
                descent = null; SetChest(pendingChest);
            });
        }
        public void SetRoute(int next)
        {
            if (route == next) return;
            route = next;
            for (int i = 0; i < deposits.Length; i++) deposits[i].enabled = false;
            fractureReset?.Kill();
            ClearFractures();
        }
        public void Strike(bool broken, bool burst, float heat, float fracture)
        {
            if (!gameObject.activeInHierarchy) return;
            fractureReset?.Kill();
            // The fourth, fully split state flashes on the breaking hit before the next vein arrives.
            int stage = broken ? 4 : Mathf.Clamp(Mathf.CeilToInt(fracture * 4), 0, 3);
            if (damageSprites != null && damageSprites.Length == 5) rocks[activeRock].sprite = damageSprites[stage];
            else for (int i = 0; i < cracks.Length; i++)
                cracks[i].enabled = crackHighlights[i].enabled = i < stage;
            if (broken) fractureReset = DOVirtual.DelayedCall(0.10f, ClearFractures, false);
            impactFlash.DOKill(); impactFlash.color = new Color(1f, 0.86f, 0.42f, broken ? 0.8f : 0.45f);
            impactFlash.DOFade(0, 0.1f);
            int count = broken ? (burst ? 16 : 12) : 2;
            for (int i = 0; i < count; i++)
            {
                var fx = pool.RentCached("ExpeditionFx") as ExpeditionFx;
                if (fx != null) fx.Scatter(HitPosition, fragmentSprite != null ? fragmentSprite : rocks[activeRock].sprite, (route == 1 ? new Color(.55f,.85f,1f) : route == 2 ? new Color(1f,.8f,.4f) : new Color(.8f,.87f,.9f)), i);
            }
            audioSource.pitch = 1 + Random.Range(-0.015f, 0.015f);
            var clip = broken ? breakSound : hitSounds[nextHitSound++ % hitSounds.Length];
            audioSource.PlayOneShot(clip, broken ? 0.78f : 0.68f);
            if (broken) Descend();
        }
        private void ClearFractures()
        {
            if(damageSprites != null && damageSprites.Length == 5)foreach(var rock in rocks)rock.sprite=damageSprites[0];
            foreach (var crack in cracks) crack.enabled = false;
            foreach (var highlight in crackHighlights) highlight.enabled = false;
        }
        public void SetMuted(bool muted) { audioSource.mute = muted; }
        private void OnDisable()
        {
            descent?.Kill(); descent = null; activeRock = 0;
            if (descendingMiner != null) descendingMiner.localPosition = minerRest;
            if (shaftBands != null) for (int i = 0; i < shaftBands.Length; i++) shaftBands[i].localPosition = bandRest[i];
            chestAnimation?.Kill();
            fractureReset?.Kill();
            if (impactFlash != null) { impactFlash.DOKill(); impactFlash.color = Color.clear; }
            if (audioSource != null) audioSource.Stop();
            if (rocks == null) return;
            for (int i = 0; i < rocks.Length; i++)
            {
                rocks[i].transform.DOKill();
                rocks[i].transform.localPosition = rockPositions[i]; rocks[i].transform.localScale = rockScales[i];
                rocks[i].enabled = true;
            }
            ClearFractures(); SetChest(pendingChest);
        }
    }
}
