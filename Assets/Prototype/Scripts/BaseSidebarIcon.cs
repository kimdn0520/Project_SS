using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition
{
    [RequireComponent(typeof(Button))]
    public abstract class BaseSidebarIcon : MonoBehaviour
    {
        [Tooltip("기능이 열려 있을 때만 아이콘을 표시합니다.")]
        public bool isAvailable = true;
        protected PlayPage Page => GetComponentInParent<PlayPage>(true);
        protected virtual bool CanShow => Page != null && Page.Model != null;
        protected virtual void Awake() { GetComponent<Button>().onClick.AddListener(Click); }
        void Click() { if (isAvailable && CanShow) Open(); }
        public void RefreshVisibility() { gameObject.SetActive(isAvailable && CanShow); }
        protected abstract void Open();
    }
}
