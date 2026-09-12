using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 하단 지하 채굴의 주인공 은퇴용사 '땅만파' 액터.
/// 흙에서는 삽(Shovel), 암석/광석에서는 곡괭이(Pickaxe)를 자동으로 장착하며,
/// 정확한 타격점(DigImpactAnchor)에 도구가 닿도록 정밀한 스윙/굴착 모션을 재생합니다.
/// </summary>
public class RetiredHeroActor : MonoBehaviour
{
    [Header("[Visual Transforms]")]
    [SerializeField] private Transform characterVisual;
    [SerializeField] private Transform toolSocket;
    [SerializeField] private SpriteRenderer toolRenderer;
    [SerializeField] private Transform digImpactAnchor;

    [Header("[Tool Sprites]")]
    [SerializeField] private Sprite pickaxeSprite;
    [SerializeField] private Sprite shovelSprite;

    [Header("[Pickaxe Motion Angles]")]
    [SerializeField] private Vector3 pickaxeIdleEuler = new Vector3(0, 0, 10f);
    [SerializeField] private Vector3 pickaxeStrikeEuler = new Vector3(0, 0, -50f);

    [Header("[Shovel Motion Angles]")]
    [SerializeField] private Vector3 shovelIdleEuler = new Vector3(0, 0, 15f);
    [SerializeField] private Vector3 shovelStrikeEuler = new Vector3(0, 0, -50f);

    public Transform DigImpactAnchor => digImpactAnchor != null ? digImpactAnchor : transform;
    public MiningToolType CurrentTool { get; private set; } = MiningToolType.Pickaxe;
    public bool IsLanding { get; private set; }

    private Vector3 originBodyPos;
    private Vector3 originScale = Vector3.one;
    private CancellationTokenSource swingCts;
    private CancellationTokenSource landingCts;

    private void Awake()
    {
        if (characterVisual == null)
            characterVisual = transform;

        originBodyPos = characterVisual.localPosition;
        originScale = characterVisual.localScale;

        if (toolRenderer == null && toolSocket != null)
            toolRenderer = toolSocket.GetComponent<SpriteRenderer>();

        // 용사 그림자는 블록 위에서 부자연스러우므로 확실하게 비활성화
        Transform shadow = transform.Find("Character_20260617_193302/Shadow");
        if (shadow != null) shadow.gameObject.SetActive(false);

        // SpriteManager 또는 사전 등록에서 스프라이트 로드
        LoadToolSpritesIfNull();

        SetTool(MiningToolType.Pickaxe);
    }

    private void LoadToolSpritesIfNull()
    {
        if (pickaxeSprite == null && SpriteManager.Instance != null)
        {
            pickaxeSprite = SpriteManager.Instance.Get("pickaxe-256_0");
            if (pickaxeSprite == null)
                pickaxeSprite = SpriteManager.Instance.Get("Gear_Weapons_Pickaxe_01");
        }
        if (shovelSprite == null && SpriteManager.Instance != null)
        {
            shovelSprite = SpriteManager.Instance.Get("Gear_Weapons_Shovel_01");
        }
    }

    /// <summary>
    /// 대상 소재(흙 vs 암석)에 따라 삽/곡괭이 자동 전환
    /// </summary>
    public void SetTool(MiningToolType toolType)
    {
        CurrentTool = toolType;
        LoadToolSpritesIfNull();

        if (toolRenderer != null)
        {
            toolRenderer.sprite = (toolType == MiningToolType.Shovel) ? shovelSprite : pickaxeSprite;
        }

        if (toolSocket != null)
        {
            toolSocket.localEulerAngles = (toolType == MiningToolType.Shovel) ? shovelIdleEuler : pickaxeIdleEuler;
        }
    }

    /// <summary>
    /// 도구 휘두르기 모션 (BeginSwing -> Windup -> Impact -> Recovery)
    /// </summary>
    public async UniTask PlaySwingMotionAsync(Action onImpactCallback, CancellationToken ct)
    {
        swingCts?.Cancel();
        swingCts?.Dispose();
        swingCts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());
        CancellationToken localCt = swingCts.Token;

        Vector3 idleRot = (CurrentTool == MiningToolType.Shovel) ? shovelIdleEuler : pickaxeIdleEuler;
        Vector3 strikeRot = (CurrentTool == MiningToolType.Shovel) ? shovelStrikeEuler : pickaxeStrikeEuler;

        try
        {
            // 1. Windup (도구 뒤로/위로 치켜들기)
            if (toolSocket != null)
                toolSocket.localEulerAngles = idleRot + new Vector3(0, 0, 25f);

            if (characterVisual != null)
                characterVisual.localPosition = originBodyPos + new Vector3(0f, 0.03f, 0f);

            await UniTask.Delay(40, cancellationToken: localCt);

            // 2. Strike & Impact (빠르게 발 밑을 향해 내리찍으며 몸체 아래로 가압)
            if (toolSocket != null)
                toolSocket.localEulerAngles = strikeRot;

            if (characterVisual != null)
                characterVisual.localPosition = originBodyPos + new Vector3(0.02f, -0.07f, 0f);

            // 타격 시점 콜백 실행
            onImpactCallback?.Invoke();

            await UniTask.Delay(60, cancellationToken: localCt);

            // 3. Recovery (원위치 복귀)
            if (toolSocket != null)
                toolSocket.localEulerAngles = idleRot;

            if (characterVisual != null)
                characterVisual.localPosition = originBodyPos;

            await UniTask.Delay(35, cancellationToken: localCt);
        }
        catch (OperationCanceledException)
        {
            if (toolSocket != null) toolSocket.localEulerAngles = idleRot;
            if (characterVisual != null) characterVisual.localPosition = originBodyPos;
        }
    }

    /// <summary>
    /// 블록 파괴 시 캐릭터가 살짝 떨어졌다가 새 블록에 착지하는 물리적 연출.
    /// 1. Fall: 발 밑 블록이 사라지며 아래로 살짝 훅 낙하 (Y: -0.16f)
    /// 2. Land: 새 블록이 위로 올라와 닿는 순간 쿵! 착지 충격 (스쿼시 바운스)
    /// 3. Recover: 원래 자세와 위치로 복귀
    /// </summary>
    public async UniTask PlayFallAndLandBounceAsync(float duration, CancellationToken ct)
    {
        landingCts?.Cancel();
        landingCts?.Dispose();
        landingCts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());
        CancellationToken localCt = landingCts.Token;

        IsLanding = true;
        try
        {
            // 1단계: Fall (자유낙하 느낌) - 약 40% 시간
            float fallDuration = duration * 0.40f;
            float elapsed = 0f;
            Vector3 fallPos = originBodyPos + new Vector3(0f, -0.16f, 0f);

            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fallDuration);
                float easeT = t * t; // 빠른 낙하 가속도
                if (characterVisual != null)
                {
                    characterVisual.localPosition = Vector3.Lerp(originBodyPos, fallPos, easeT);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, localCt);
            }

            // 2단계: Land Impact & Squash (블록 충돌 순간 살짝 찌그러짐) - 약 25% 시간
            float impactDuration = duration * 0.25f;
            elapsed = 0f;
            Vector3 squashScale = new Vector3(originScale.x * 1.08f, originScale.y * 0.88f, 1f);

            while (elapsed < impactDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / impactDuration);
                if (characterVisual != null)
                {
                    characterVisual.localScale = Vector3.Lerp(originScale, squashScale, t);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, localCt);
            }

            // 3단계: Recovery Bounce (제자리 복귀 및 탄성 펴짐) - 남은 시간
            float recoverDuration = duration * 0.35f;
            elapsed = 0f;

            while (elapsed < recoverDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / recoverDuration);
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
                if (characterVisual != null)
                {
                    characterVisual.localPosition = Vector3.Lerp(fallPos, originBodyPos, easeOut);
                    characterVisual.localScale = Vector3.Lerp(squashScale, originScale, easeOut);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, localCt);
            }

            if (characterVisual != null)
            {
                characterVisual.localPosition = originBodyPos;
                characterVisual.localScale = originScale;
            }
        }
        catch (OperationCanceledException)
        {
            if (characterVisual != null)
            {
                characterVisual.localPosition = originBodyPos;
                characterVisual.localScale = originScale;
            }
        }
        finally
        {
            IsLanding = false;
        }
    }

    public void CancelSwing()
    {
        swingCts?.Cancel();
        swingCts?.Dispose();
        swingCts = null;

        landingCts?.Cancel();
        landingCts?.Dispose();
        landingCts = null;
        IsLanding = false;

        Vector3 idleRot = (CurrentTool == MiningToolType.Shovel) ? shovelIdleEuler : pickaxeIdleEuler;
        if (toolSocket != null) toolSocket.localEulerAngles = idleRot;
        if (characterVisual != null)
        {
            characterVisual.localPosition = originBodyPos;
            characterVisual.localScale = originScale;
        }
    }

    private void OnDestroy()
    {
        CancelSwing();
    }
}
