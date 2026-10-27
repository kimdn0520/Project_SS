using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    public static class RunePreferences
    {
        // Share the existing sound manager's preference without creating a manager at runtime.
        public static bool Music
        {
            get=>SoundManager.ExistingInstance!=null?SoundManager.ExistingInstance.IsBGMOn:PlayerPrefs.GetInt("Sound_BgmOn",1)!=0;
            set
            {
                PlayerPrefs.SetInt("Sound_BgmOn",value?1:0);
                var sound=SoundManager.ExistingInstance;
                if(sound!=null&&sound.IsBGMOn!=value)sound.IsBGMOn=value;
            }
        }
        public static bool Audio {get=>PlayerPrefs.GetInt("RuneBreak.Audio",0)!=0;set=>PlayerPrefs.SetInt("RuneBreak.Audio",value?1:0);}
        public static bool Reduced {get=>PlayerPrefs.GetInt("RuneBreak.Reduced",0)!=0;set=>PlayerPrefs.SetInt("RuneBreak.Reduced",value?1:0);}
        public static bool Paths {get=>PlayerPrefs.GetInt("RuneBreak.Paths",1)!=0;set=>PlayerPrefs.SetInt("RuneBreak.Paths",value?1:0);}
        public static float Volume {get=>PlayerPrefs.GetFloat("RuneBreak.Volume",.45f);set=>PlayerPrefs.SetFloat("RuneBreak.Volume",value);}
    }
    public sealed class RuneSettingsPanel : BasePopupHandler
    {
        public const string PrefabName="RuneSettingsPopup";
        public override string PopupName=>PrefabName;
        public Toggle musicToggle,audioToggle;
        public RectTransform panel;
        public GameObject lobbyActions,playActions;
        public const string HomeResult="Home";
        public override void OnWillEnter(object param)
        {
            base.OnWillEnter(param);
            musicToggle.SetIsOnWithoutNotify(RunePreferences.Music);
            audioToggle.SetIsOnWithoutNotify(RunePreferences.Audio);
            bool playing=param is RunePlayPage;
            lobbyActions.SetActive(!playing);
            playActions.SetActive(playing);
        }
        public void SetMusic(bool v)=>RunePreferences.Music=v;
        public void SetAudio(bool v)=>RunePreferences.Audio=v;
        public void Dismiss()=>OnEscape();
        public void GoHome(){if(CanEscape())Close(HomeResult);}
        public override void OnWillLeave(){PlayerPrefs.Save();base.OnWillLeave();}
    }
}
