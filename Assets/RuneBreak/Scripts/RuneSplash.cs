using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace RuneBreak
{
    public sealed class RuneSplash : MonoBehaviour
    {
        public Image progress;
        public TMP_Text status;
        [SerializeField] private string playScene="Play";
        private IEnumerator Start()
        {
            Application.targetFrameRate=60;
            var load=SceneManager.LoadSceneAsync(playScene);load.allowSceneActivation=false;
            float elapsed=0;while(load.progress<.9f||elapsed<.65f){elapsed+=Time.unscaledDeltaTime;progress.fillAmount=Mathf.Min(load.progress/.9f,elapsed/.65f);yield return null;}
            progress.fillAmount=1;status.text="READY";load.allowSceneActivation=true;
        }
    }
}
