using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class ApplyEquipmentAtlas
{
    public static void Execute()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset");
        var manager=Object.FindFirstObjectByType<SpriteManager>();
        var sprites=catalog.gear.Select(g=>AssetDatabase.FindAssets(g.spriteKey+" t:Sprite").Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".png")).Select(AssetDatabase.LoadAssetAtPath<Sprite>).First(s=>s!=null&&s.name==g.spriteKey)).ToArray();
        sprites=catalog.gear.Select(g=>AssetDatabase.LoadAssetAtPath<Sprite>(EquipmentAtlasArt.Folder+"/"+g.spriteKey+".png")??sprites[System.Array.IndexOf(catalog.gear,g)]).ToArray();
        EquipmentAtlasArt.Configure(manager,sprites);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);EditorSceneManager.SaveScene(manager.gameObject.scene);
    }
}

