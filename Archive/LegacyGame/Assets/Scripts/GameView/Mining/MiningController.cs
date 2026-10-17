using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 하단 지하 채굴 시스템의 코어 컨트롤러.
/// 은퇴용사 땅만파의 스윙 모션, 도구 자동 전환(삽/곡괭이), 노드 파괴 및 보상, 지층 진행(Scroll Up)을 총괄합니다.
/// </summary>
public class MiningController : MonoBehaviour
{
    [Header("[Actor & View]")]
    [SerializeField] private RetiredHeroActor retiredHero;
    [SerializeField] private Transform digSiteRoot;
    [SerializeField] private Transform excavationChunksRoot;
    [SerializeField] private GameObject miningVfxPrefab;

    [Header("[Vertical Column Settings]")]
    [SerializeField] private List<MiningNodeView> activeNodeViews = new List<MiningNodeView>();
    [SerializeField] private float blockStepY = 1.6f;
    [SerializeField] private float scrollDuration = 0.22f;

    [Header("[Mining Balances]")]
    [SerializeField] private int depthMeters = 1;
    [SerializeField] private int pickaxePower = 20;
    [SerializeField] private int shovelPower = 25;

    public int DepthMeters => depthMeters;
    public int PickaxePower => pickaxePower;
    public int ShovelPower => shovelPower;
    public bool IsSwinging { get; private set; }
    public bool IsTransitioning { get; private set; }

    /// <summary>
    /// 캐릭터가 완전히 블록에 딱 떨어져야만 Dig 버튼 발동 가능
    /// </summary>
    public bool CanDig => !IsSwinging && !IsTransitioning && (retiredHero == null || !retiredHero.IsLanding);

    public event Action<int> OnDepthChanged;
    public event Action<string> OnMiningLog;

    private CancellationTokenSource swingLoopCts;
    private CancellationTokenSource columnTransitionCts;
    private Vector3 initialDigSitePos;

    private void Awake()
    {
        if (digSiteRoot != null)
        {
            initialDigSitePos = digSiteRoot.localPosition;
        }
    }

    private void Start()
    {
        if (activeNodeViews.Count == 0 && digSiteRoot != null)
        {
            activeNodeViews.AddRange(digSiteRoot.GetComponentsInChildren<MiningNodeView>(true));
        }

        SpawnInitialStrata();
        UpdateToolForCurrentTarget();
    }

    /// <summary>
    /// 세로 1열 초기 블록 배치 및 데이터 생성
    /// </summary>
    private void SpawnInitialStrata()
    {
        if (activeNodeViews.Count == 0) return;

        for (int i = 0; i < activeNodeViews.Count; i++)
        {
            if (activeNodeViews[i] == null) continue;

            activeNodeViews[i].transform.localPosition = new Vector3(0f, -i * blockStepY, 0f);

            // 첫 번째 블록은 암석/돌, 이후 돌/광석/흙 혼합
            MiningNodeType nodeType = (i == 0) ? MiningNodeType.Rock : GetNextNodeType(depthMeters + i);
            int hp = 35 + (i * 10);
            long gold = 30 + (i * 15);
            int gems = (i == 2) ? 1 : 0;
            string loot = (i == 3) ? "황금 원석" : null;

            activeNodeViews[i].Setup(new MiningNodeData($"node_{depthMeters}_{i}", nodeType, hp, gold, gems, loot));
        }

        if (digSiteRoot != null)
        {
            digSiteRoot.localPosition = initialDigSitePos;
        }

        UpdateToolForCurrentTarget();
        OnDepthChanged?.Invoke(depthMeters);
    }

    private MiningNodeType GetNextNodeType(int depth)
    {
        int mod = depth % 6;
        if (mod == 0) return MiningNodeType.Ore;
        if (mod == 2) return MiningNodeType.Soil;
        return MiningNodeType.Rock;
    }

    private MiningNodeView GetCurrentTargetNode()
    {
        if (activeNodeViews.Count > 0 && activeNodeViews[0] != null)
        {
            return activeNodeViews[0];
        }
        return null;
    }

    private void UpdateToolForCurrentTarget()
    {
        MiningNodeView target = GetCurrentTargetNode();
        if (target != null && target.CurrentData != null && retiredHero != null)
        {
            retiredHero.SetTool(target.CurrentData.requiredTool);
        }
    }

    /// <summary>
    /// DIG 버튼 탭/홀드 시 호출되는 스윙 진입점.
    /// 완전히 착지 완료된 상태(CanDig == true)에서만 발동!
    /// </summary>
    public void TriggerDig()
    {
        if (!CanDig) return;

        // 전역 정지 상태이면 무시
        if (SessionPausePolicy.Instance != null && SessionPausePolicy.Instance.IsPaused)
        {
            return;
        }

        ExecuteSwingAsync().Forget();
    }

    private async UniTaskVoid ExecuteSwingAsync()
    {
        IsSwinging = true;

        swingLoopCts?.Cancel();
        swingLoopCts?.Dispose();
        swingLoopCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        CancellationToken ct = swingLoopCts.Token;

        try
        {
            UpdateToolForCurrentTarget();

            if (retiredHero != null)
            {
                await retiredHero.PlaySwingMotionAsync(OnSwingImpact, ct);
            }
            else
            {
                OnSwingImpact();
                await UniTask.Delay(150, cancellationToken: ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsSwinging = false;
        }
    }

    private void OnSwingImpact()
    {
        MiningNodeView target = GetCurrentTargetNode();
        if (target == null) return;

        int damage = (target.CurrentData.nodeType == MiningNodeType.Soil) ? shovelPower : pickaxePower;
        target.ApplyDamage(damage);

        // 타격 VFX 생성
        Vector3 impactPos = retiredHero != null ? retiredHero.DigImpactAnchor.position : target.transform.position;
        SpawnMiningVfx(impactPos);

        if (target.IsCompleted)
        {
            HandleNodeCompleted(target);
        }
    }

    private void HandleNodeCompleted(MiningNodeView completedNode)
    {
        depthMeters++;
        OnDepthChanged?.Invoke(depthMeters);

        MiningNodeData nodeData = completedNode.CurrentData;
        if (nodeData != null)
        {
            if (nodeData.rewardGold > 0)
            {
                PlayerInventoryModel.Instance?.AddGold(nodeData.rewardGold);
            }

            if (nodeData.rewardGems > 0)
            {
                PlayerInventoryModel.Instance?.AddGems(nodeData.rewardGems);
            }

            if (!string.IsNullOrEmpty(nodeData.dropLoot))
            {
                PlayerInventoryModel.Instance?.AddEquipment(nodeData.dropLoot);
            }
        }

        // 새 블록 상승 및 캐릭터 착지 바운스 연출 시작 (완전히 착지할 때까지 Dig 잠금)
        AdvanceColumnSequenceAsync(completedNode).Forget();
    }

    /// <summary>
    /// 블록 파괴 후 새 블록이 아래에서 위로 올라오며 캐릭터가 착지하는 연출.
    /// 캐릭터가 완전히 블록에 딱 떨어질 때까지 IsTransitioning = true 유지.
    /// </summary>
    private async UniTaskVoid AdvanceColumnSequenceAsync(MiningNodeView brokenNode)
    {
        IsTransitioning = true;

        columnTransitionCts?.Cancel();
        columnTransitionCts?.Dispose();
        columnTransitionCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        CancellationToken ct = columnTransitionCts.Token;

        try
        {
            // 1. 캐릭터 떨어지는 느낌 및 착지 바운스 비동기 태스크 시작
            UniTask heroBounceTask = UniTask.CompletedTask;
            if (retiredHero != null)
            {
                heroBounceTask = retiredHero.PlayFallAndLandBounceAsync(scrollDuration, ct);
            }

            // 2. digSiteRoot를 위로 blockStepY 만큼 스무스하게 이동
            if (digSiteRoot != null)
            {
                Vector3 startPos = digSiteRoot.localPosition;
                Vector3 targetPos = initialDigSitePos + new Vector3(0f, blockStepY, 0f);

                float elapsed = 0f;
                while (elapsed < scrollDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / scrollDuration);
                    float easeT = Mathf.SmoothStep(0f, 1f, t);
                    digSiteRoot.localPosition = Vector3.Lerp(startPos, targetPos, easeT);
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                digSiteRoot.localPosition = targetPos;
            }

            // 캐릭터 착지가 완전히 끝날 때까지 대기
            await heroBounceTask;

            // 3. 인덱스 0이었던 파괴된 블록을 리스트 맨 뒤로 순환
            if (activeNodeViews.Count > 0 && activeNodeViews[0] == brokenNode)
            {
                activeNodeViews.RemoveAt(0);
                activeNodeViews.Add(brokenNode);
            }

            // 4. digSiteRoot 원위치 복귀 및 모든 자식 블록 세로 1열 재정렬
            if (digSiteRoot != null)
            {
                digSiteRoot.localPosition = initialDigSitePos;
            }

            for (int i = 0; i < activeNodeViews.Count; i++)
            {
                if (activeNodeViews[i] != null)
                {
                    activeNodeViews[i].transform.localPosition = new Vector3(0f, -i * blockStepY, 0f);
                }
            }

            // 5. 맨 뒤로 간 블록을 새로운 지층 데이터로 리셋 및 활성화
            MiningNodeType nextType = GetNextNodeType(depthMeters + activeNodeViews.Count);
            int hp = 35 + (depthMeters * 6);
            long gold = 35 + (depthMeters * 12);
            int gems = (UnityEngine.Random.value < 0.25f) ? 1 : 0;
            string loot = (UnityEngine.Random.value < 0.15f) ? "빛나는 원석" : null;

            brokenNode.Setup(new MiningNodeData($"node_{depthMeters}_{activeNodeViews.Count}", nextType, hp, gold, gems, loot));

            UpdateToolForCurrentTarget();
        }
        catch (OperationCanceledException) { }
        finally
        {
            // 완전히 착지 완료된 후에만 Dig 버튼 잠금 해제!
            IsTransitioning = false;
        }
    }

    private void SpawnMiningVfx(Vector3 pos)
    {
        if (miningVfxPrefab != null)
        {
            GameObject vfx = Instantiate(miningVfxPrefab, pos, Quaternion.identity, excavationChunksRoot != null ? excavationChunksRoot : transform);
            Destroy(vfx, 0.5f);
        }
    }

    public void CancelDig()
    {
        swingLoopCts?.Cancel();
        swingLoopCts?.Dispose();
        swingLoopCts = null;

        columnTransitionCts?.Cancel();
        columnTransitionCts?.Dispose();
        columnTransitionCts = null;

        IsSwinging = false;
        IsTransitioning = false;

        if (digSiteRoot != null)
        {
            digSiteRoot.localPosition = initialDigSitePos;
        }

        if (retiredHero != null)
        {
            retiredHero.CancelSwing();
        }
    }

    private void OnDestroy()
    {
        CancelDig();
    }
}
