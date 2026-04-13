using UnityEngine;
using UnityEngine.UI;

public class WorldCharacterUI : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 Offset = new Vector3(0, 1.2f, 0); // 캐릭터 머리 위 오프셋
    public float SmoothSpeed = 10f;

    [Header("References")]
    public Slider StaminaBar;
    public Image StaminaFill;

    [Header("Colors")]
    public Color NormalStaminaColor = new Color(0.9f, 0.8f, 0.2f); // Yellowish
    public Color ExhaustedStaminaColor = new Color(1.0f, 0.2f, 0.2f); // Reddish

    private PlayerController player;
    private Canvas canvas;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        canvas = GetComponent<Canvas>();
        
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().localScale = new Vector3(0.01f, 0.01f, 0.01f);
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        transform.position = player.transform.position + Offset;
        
        Vector3 localScale = transform.localScale;
        localScale.x = Mathf.Abs(localScale.x); 
        transform.localScale = localScale;

        if (StaminaBar != null)
        {
            StaminaBar.maxValue = player.MaxStamina;
            StaminaBar.value = player.CurrentStamina;

            if (StaminaFill != null)
            {
                StaminaFill.color = (player.CurrentStamina <= 0) ? ExhaustedStaminaColor : NormalStaminaColor;
            }
            
            // 스태미너가 가득 찼을 때는 UI를 숨겨서 화면을 깔끔하게 유지 (선택 사항)
            // gameObject.SetActive(player.CurrentStamina < player.MaxStamina);
        }
    }
}
