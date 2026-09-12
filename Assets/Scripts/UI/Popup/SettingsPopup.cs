using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 환경설정 팝업: BGM/SFX 음량 슬라이더 및 게임 옵션.
/// </summary>
public class SettingsPopup : BasePopupHandler
{
    [Header("[Audio Sliders]")]
    [SerializeField] private Slider sliderBgm;
    [SerializeField] private Slider sliderSfx;

    public override void OnWillEnter(object param)
    {
        base.OnWillEnter(param);
        SessionPausePolicy.Instance?.RequestPause("SettingsPopup");

        if (sliderBgm != null) sliderBgm.value = AudioListener.volume;
        if (sliderSfx != null) sliderSfx.value = 1.0f;

        if (sliderBgm != null)
        {
            sliderBgm.onValueChanged.RemoveAllListeners();
            sliderBgm.onValueChanged.AddListener(val => AudioListener.volume = val);
        }
    }

    public override void OnDidLeave()
    {
        base.OnDidLeave();
        SessionPausePolicy.Instance?.ReleasePause("SettingsPopup");
    }
}
