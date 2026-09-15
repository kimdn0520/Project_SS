using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;
using ProjectSS.Expedition;
public static class InspectPolish
{
    public static string Execute()
    {
        var page=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayPage.prefab").GetComponent<PlayPage>();
        return string.Join("\n",page.heroGrid.transform.Find("HeroCard0").GetComponentsInChildren<TMP_Text>(true).Select(t=>t.name+" text="+t.text+" color="+t.color+" pos="+t.rectTransform.anchoredPosition+" size="+t.rectTransform.sizeDelta+" active="+t.gameObject.activeSelf+" enabled="+t.enabled));
    }
}
