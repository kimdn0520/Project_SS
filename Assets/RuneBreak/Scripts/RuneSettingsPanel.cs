using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    public static class RunePreferences
    {
        public static bool Audio {get=>PlayerPrefs.GetInt("RuneBreak.Audio",0)!=0;set=>PlayerPrefs.SetInt("RuneBreak.Audio",value?1:0);}
        public static bool Reduced {get=>PlayerPrefs.GetInt("RuneBreak.Reduced",0)!=0;set=>PlayerPrefs.SetInt("RuneBreak.Reduced",value?1:0);}
        public static bool Paths {get=>PlayerPrefs.GetInt("RuneBreak.Paths",1)!=0;set=>PlayerPrefs.SetInt("RuneBreak.Paths",value?1:0);}
        public static float Volume {get=>PlayerPrefs.GetFloat("RuneBreak.Volume",.45f);set=>PlayerPrefs.SetFloat("RuneBreak.Volume",value);}
    }
    public sealed class RuneSettingsPanel : MonoBehaviour
    {
        public Toggle audioToggle,reducedToggle,pathToggle;
        public Slider volume;
        private void OnEnable(){audioToggle.SetIsOnWithoutNotify(RunePreferences.Audio);reducedToggle.SetIsOnWithoutNotify(RunePreferences.Reduced);pathToggle.SetIsOnWithoutNotify(RunePreferences.Paths);volume.SetValueWithoutNotify(RunePreferences.Volume);}
        public void SetAudio(bool v)=>RunePreferences.Audio=v;
        public void SetReduced(bool v)=>RunePreferences.Reduced=v;
        public void SetPaths(bool v)=>RunePreferences.Paths=v;
        public void SetVolume(float v)=>RunePreferences.Volume=v;
        public void Close(){PlayerPrefs.Save();gameObject.SetActive(false);}
    }
}
