using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상단 전투 전장의 횡스크롤 패럴랙스 배경 컨트롤러.
/// Travel 상태에서는 배경이 왼쪽으로 스크롤하여 용사가 전진하는 느낌을 주며,
/// Encounter 및 Combat 상태에서는 부드럽게 감속하여 정지합니다.
/// </summary>
public class ParallaxScroller : MonoBehaviour
{
    [System.Serializable]
    public class ScrollLayer
    {
        public string name;
        public float speedMultiplier = 1.0f;
        public List<Transform> segments = new List<Transform>();
        public float segmentWidth = 14.0f;
    }

    [SerializeField] private float baseSpeed = 1.2f;
    [SerializeField] private List<ScrollLayer> layers = new List<ScrollLayer>();

    private float currentSpeedMultiplier = 1.0f;
    private float targetSpeedMultiplier = 1.0f;
    private bool isPaused = false;

    public void SetTargetSpeed(float multiplier, bool immediate = false)
    {
        targetSpeedMultiplier = multiplier;
        if (immediate)
        {
            currentSpeedMultiplier = multiplier;
        }
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    private void Update()
    {
        if (isPaused) return;

        // 부드러운 속도 감속/가속
        currentSpeedMultiplier = Mathf.MoveTowards(currentSpeedMultiplier, targetSpeedMultiplier, Time.deltaTime * 3.0f);

        if (currentSpeedMultiplier <= 0.001f) return;

        float moveAmount = baseSpeed * currentSpeedMultiplier * Time.deltaTime;

        foreach (var layer in layers)
        {
            if (layer.segments == null || layer.segments.Count == 0) continue;

            float layerMove = moveAmount * layer.speedMultiplier;

            foreach (var seg in layer.segments)
            {
                if (seg == null) continue;
                seg.localPosition += Vector3.left * layerMove;

                // 래핑 판정: 세그먼트가 왼쪽으로 벗어나면 오른쪽 끝으로 이동
                if (seg.localPosition.x <= -layer.segmentWidth)
                {
                    seg.localPosition += Vector3.right * (layer.segmentWidth * layer.segments.Count);
                }
            }
        }
    }

    public void AddLayer(string layerName, float speedMultiplier, List<Transform> segs, float width)
    {
        layers.Add(new ScrollLayer
        {
            name = layerName,
            speedMultiplier = speedMultiplier,
            segments = segs,
            segmentWidth = width
        });
    }

    public void ClearLayers()
    {
        layers.Clear();
    }
}
