using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;
public static class UpdateButtonPickaxe
{
    public static object Execute()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Stop play before editing");
        const string path="Assets/Resources/Prefabs/PlayPage.prefab";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/2D Minimal-IconPack/Icons/256/Gear_Weapons_Pickaxe_01.png");
        if(sprite==null)throw new System.Exception("Pickaxe sprite missing");
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var page=root.GetComponent<PlayPage>();
            var icon=page.digButton.transform.Find("PressableFace/PickaxeIcon").GetComponent<Image>();
            icon.sprite=sprite;icon.preserveAspect=true;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
        return sprite.name;
    }
}
