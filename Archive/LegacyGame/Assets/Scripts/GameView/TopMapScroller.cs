using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상단 지상 전장의 2D 무한 패럴랙스 맵 스크롤러.
/// 직교 카메라(Orthographic) 환경에서 카메라를 고정하고,
/// 각 배경 레이어(원경 산, 호수, 소나무, 울타리, 잔디 지면)를
/// 각자의 원근 속도로 좌측(-X)으로 부드럽게 무한 루프 스크롤링합니다.
/// </summary>
[DisallowMultipleComponent]
public class TopMapScroller : MonoBehaviour
{
    [Serializable]
    public class ParallaxLayer
    {
        public string layerName;
        public float scrollSpeed = 1f;
        public List<Transform> tileTransforms = new List<Transform>();
        public float tileWidth = 10f;
    }

    [Header("스크롤 기본 설정")]
    [SerializeField] private float baseSpeed = 1.0f;
    [SerializeField] private bool isScrolling = true;

    [Header("패럴랙스 레이어 목록")]
    [SerializeField] private List<ParallaxLayer> layers = new List<ParallaxLayer>();

    public float BaseSpeed
    {
        get => baseSpeed;
        set => baseSpeed = value;
    }

    public bool IsScrolling
    {
        get => isScrolling;
        set => isScrolling = value;
    }

    public void AddLayer(string name, float speed, List<Transform> tiles, float width)
    {
        layers.Add(new ParallaxLayer
        {
            layerName = name,
            scrollSpeed = speed,
            tileTransforms = tiles,
            tileWidth = width
        });
    }

    private void Update()
    {
        if (!isScrolling) return;

        float dt = Time.deltaTime;
        for (int i = 0; i < layers.Count; i++)
        {
            UpdateLayer(layers[i], dt);
        }
    }

    private void UpdateLayer(ParallaxLayer layer, float dt)
    {
        if (layer.tileTransforms == null || layer.tileTransforms.Count < 2) return;

        float moveAmount = layer.scrollSpeed * baseSpeed * dt;

        // 1. 모든 타일을 좌측으로 이동
        for (int i = 0; i < layer.tileTransforms.Count; i++)
        {
            Transform t = layer.tileTransforms[i];
            if (t != null)
            {
                t.localPosition += Vector3.left * moveAmount;
            }
        }

        // 2. 가장 왼쪽과 가장 오른쪽 타일 찾기
        Transform leftmost = null;
        Transform rightmost = null;
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        for (int i = 0; i < layer.tileTransforms.Count; i++)
        {
            Transform t = layer.tileTransforms[i];
            if (t == null) continue;

            float x = t.localPosition.x;
            if (x < minX)
            {
                minX = x;
                leftmost = t;
            }
            if (x > maxX)
            {
                maxX = x;
                rightmost = t;
            }
        }

        // 3. 가장 왼쪽 타일이 화면 좌측(-4.5f)을 완전히 벗어나면 가장 오른쪽 타일 뒤로 재배치
        if (leftmost != null && rightmost != null && leftmost != rightmost)
        {
            float halfWidth = layer.tileWidth * 0.5f;
            if (minX + halfWidth < -4.5f)
            {
                Vector3 newPos = leftmost.localPosition;
                newPos.x = rightmost.localPosition.x + layer.tileWidth - 0.02f;
                leftmost.localPosition = newPos;
            }
        }
    }
}
