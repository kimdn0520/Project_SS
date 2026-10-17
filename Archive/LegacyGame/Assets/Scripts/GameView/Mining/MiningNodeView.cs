using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 채굴 지점의 단일 블록/광석 비주얼 뷰.
/// 흙, 암석, 광석의 외형과 균열(Crack) 단계별 시각 효과, 타격 흔들림을 표현합니다.
/// </summary>
public class MiningNodeView : MonoBehaviour
{
    [Header("[Renderers]")]
    [SerializeField] private SpriteRenderer blockRenderer;
    [SerializeField] private SpriteRenderer crackRenderer;
    [SerializeField] private Transform visualRoot;

    [Header("[Sprites]")]
    [SerializeField] private Sprite soilSprite;
    [SerializeField] private Sprite rockSprite;
    [SerializeField] private Sprite oreSprite;
    [SerializeField] private Sprite crack1Sprite;
    [SerializeField] private Sprite crack2Sprite;

    private MiningNodeData currentData;
    private Vector3 originScale;
    private CancellationTokenSource punchCts;

    public MiningNodeData CurrentData => currentData;
    public bool IsCompleted => currentData == null || currentData.isCompleted;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        originScale = visualRoot.localScale;

        LoadSpritesIfNull();
    }

    private void LoadSpritesIfNull()
    {
        if (SpriteManager.Instance != null)
        {
            if (soilSprite == null) soilSprite = SpriteManager.Instance.Get("Block_Soil");
            if (rockSprite == null) rockSprite = SpriteManager.Instance.Get("Block_Rock");
            if (oreSprite == null) oreSprite = SpriteManager.Instance.Get("Material_Ore_01");
            if (crack1Sprite == null) crack1Sprite = SpriteManager.Instance.Get("Block_Crack_1");
            if (crack2Sprite == null) crack2Sprite = SpriteManager.Instance.Get("Block_Crack_2");
        }
    }

    public void Setup(MiningNodeData data)
    {
        currentData = data;
        LoadSpritesIfNull();

        gameObject.SetActive(true);
        if (visualRoot != null) visualRoot.localScale = originScale;

        // 소재별 스프라이트 설정 (오직 정사각 네모 블록만 사용)
        if (blockRenderer != null)
        {
            blockRenderer.color = Color.white;
            switch (data.nodeType)
            {
                case MiningNodeType.Soil:
                    blockRenderer.sprite = soilSprite != null ? soilSprite : rockSprite;
                    break;
                case MiningNodeType.Rock:
                    blockRenderer.sprite = rockSprite;
                    break;
                case MiningNodeType.Ore:
                    // 광석도 네모 암석 블록을 기본으로 하고 빛나는 골드 틴트 적용
                    blockRenderer.sprite = rockSprite;
                    blockRenderer.color = new Color(1.0f, 0.88f, 0.45f, 1f);
                    break;
            }
        }

        UpdateCrackVisual();
    }

    public void ApplyDamage(int damage)
    {
        if (currentData == null || currentData.isCompleted) return;

        currentData.TakeDamage(damage);
        UpdateCrackVisual();
        PunchScaleAsync().Forget();

        if (currentData.isCompleted)
        {
            PlayBreakEffectAsync().Forget();
        }
    }

    private void UpdateCrackVisual()
    {
        if (crackRenderer == null) return;

        if (currentData == null || currentData.nodeType == MiningNodeType.Soil || currentData.isCompleted)
        {
            crackRenderer.gameObject.SetActive(false);
            return;
        }

        if (currentData.visualStage == 1)
        {
            crackRenderer.gameObject.SetActive(true);
            crackRenderer.sprite = crack1Sprite;
        }
        else if (currentData.visualStage == 2)
        {
            crackRenderer.gameObject.SetActive(true);
            crackRenderer.sprite = crack2Sprite;
        }
        else
        {
            crackRenderer.gameObject.SetActive(false);
        }
    }

    private async UniTaskVoid PunchScaleAsync()
    {
        if (visualRoot == null) return;
        punchCts?.Cancel();
        punchCts?.Dispose();
        punchCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        CancellationToken ct = punchCts.Token;

        try
        {
            visualRoot.localScale = originScale * 1.14f;
            await UniTask.Delay(45, cancellationToken: ct);
            if (visualRoot != null) visualRoot.localScale = originScale;
        }
        catch { }
    }

    private async UniTaskVoid PlayBreakEffectAsync()
    {
        if (crackRenderer != null) crackRenderer.gameObject.SetActive(false);

        // 파괴 연출: 빠르게 축소 후 비활성화
        if (visualRoot != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.12f;
                visualRoot.localScale = Vector3.Lerp(originScale, Vector3.zero, t);
                await UniTask.Yield();
            }
        }

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        punchCts?.Cancel();
        punchCts?.Dispose();
    }
}
