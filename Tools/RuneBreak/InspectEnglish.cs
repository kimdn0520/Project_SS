using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;
public static class InspectEnglish
{
    public static void Execute()
    {
        var lines=new System.Collections.Generic.List<string>();
        foreach(var path in new[]{"Assets/Resources/Prefabs/MainPage.prefab","Assets/Resources/Prefabs/PlayPage.prefab","Assets/Resources/Prefabs/Popups/SettingsPopup.prefab"})
        {var root=PrefabUtility.LoadPrefabContents(path);try{foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))if(text.text.Any(c=>c>=0xAC00&&c<=0xD7A3))lines.Add(path+" | "+text.name+" | "+text.text);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        File.WriteAllLines("Temp/english-labels.txt",lines);
    }
}
