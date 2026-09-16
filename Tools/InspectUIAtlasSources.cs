using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class InspectUIAtlasSources
{
    public static string Execute()
    {
        var lines=new SortedDictionary<string,string>();
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/Prefabs"}))
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            foreach(var image in prefab.GetComponentsInChildren<Image>(true))
            {
                if(image.sprite==null)continue;
                string path=AssetDatabase.GetAssetPath(image.sprite);
                if(path.StartsWith("Packages/")||path=="")continue;
                string material=image.material==null?"default":AssetDatabase.GetAssetPath(image.material);
                lines[path]=$"{path} | texture={AssetDatabase.GetAssetPath(image.sprite.texture)} | sprite={image.sprite.name} | size={image.sprite.rect.width}x{image.sprite.rect.height} | material={material} | image={image.name}";
            }
        }
        File.WriteAllLines("PrototypeQA/ui-atlas-sources.txt",lines.Values);
        return string.Join("\n",lines.Values);
    }
}
