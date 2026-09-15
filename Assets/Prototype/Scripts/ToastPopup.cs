using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ProjectSS.Expedition
{
    /// <summary>Non-modal, unscaled toast. A new message replaces the current one.</summary>
    public sealed class ToastPopup : MonoBehaviour
    {
        [SerializeField] private RectTransform bubble;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private CanvasGroup group;
        private static ToastPopup instance;
        private Sequence sequence;

        public static void Show(string message, Vector2? screenPos = null, float duration = 2f)
        {
            if (!Application.isPlaying || string.IsNullOrWhiteSpace(message)) return;
            if (instance == null)
            {
                var prefab = Resources.Load<ToastPopup>("Prefabs/UI/ToastPopup");
                if (prefab == null) { Debug.LogError("ToastPopup prefab is missing."); return; }
                instance = Instantiate(prefab);
                DontDestroyOnLoad(instance.gameObject);
            }
            instance.Display(message, screenPos, duration);
        }

        private void Display(string message, Vector2? screenPos, float duration)
        {
            sequence?.Kill();
            var canvas = GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=PopupManager.Instance!=null?PopupManager.Instance.RenderCamera:Camera.main;
            canvas.planeDistance=10f;
            gameObject.SetActive(true);
            messageText.text = message;
            group.alpha = 0;
            group.interactable = group.blocksRaycasts = false;
            Canvas.ForceUpdateCanvases();
            var safe = Screen.safeArea;
            float scale = Mathf.Max(.001f, canvas.scaleFactor);
            float width = Mathf.Min(580, (safe.width - 32) / scale);
            bubble.sizeDelta = new Vector2(width, Mathf.Min((safe.height - 48) / scale,
                Mathf.Max(76, messageText.GetPreferredValues(message, width - 48, Mathf.Infinity).y + 36)));
            var point = screenPos ?? new Vector2(safe.center.x, safe.yMin + safe.height * .23f);
            var half = bubble.sizeDelta * scale * .5f;
            point.x = Mathf.Clamp(point.x, safe.xMin + half.x + 8, safe.xMax - half.x - 8);
            point.y = Mathf.Clamp(point.y, safe.yMin + half.y + 8, safe.yMax - half.y - 32 * scale);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)bubble.parent, point, canvas.worldCamera, out var local);
            bubble.anchoredPosition = local;
            sequence = DOTween.Sequence().SetUpdate(true)
                .Append(group.DOFade(1, .16f))
                .AppendInterval(Mathf.Max(.3f, duration))
                .Append(group.DOFade(0, .22f))
                .Join(bubble.DOAnchorPosY(local.y + 24, .22f).SetEase(Ease.OutQuad))
                .OnComplete(() => gameObject.SetActive(false));
        }
        public void Hide() { sequence?.Kill(); gameObject.SetActive(false); }
        public static void HideIfActive() { if (instance != null) instance.Hide(); }
        private void OnDisable() { sequence?.Kill(); }
        private void OnDestroy() { sequence?.Kill(); if (instance == this) instance = null; }
    }
}
