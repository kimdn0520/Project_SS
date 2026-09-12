using UnityEngine;
using UnityEditor;

public static class ScrollerInspector
{
    public static void Inspect()
    {
        GameObject ppPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayPage.prefab");
        var scroller = ppPrefab.GetComponentInChildren<TopMapScroller>(true);
        if (scroller == null)
        {
            Debug.LogError("TopMapScroller not found!");
            return;
        }

        SerializedObject so = new SerializedObject(scroller);
        SerializedProperty layersProp = so.FindProperty("layers");
        Debug.Log($"Scroller Layers Count: {layersProp.arraySize}");
        for (int i = 0; i < layersProp.arraySize; i++)
        {
            var l = layersProp.GetArrayElementAtIndex(i);
            string name = l.FindPropertyRelative("layerName").stringValue;
            float spd = l.FindPropertyRelative("scrollSpeed").floatValue;
            float tw = l.FindPropertyRelative("tileWidth").floatValue;
            var tilesProp = l.FindPropertyRelative("tileTransforms");
            Debug.Log($"Layer[{i}] name: {name}, speed: {spd}, tileWidth: {tw}, tilesCount: {tilesProp.arraySize}");
            for (int j = 0; j < tilesProp.arraySize; j++)
            {
                var t = tilesProp.GetArrayElementAtIndex(j).objectReferenceValue as Transform;
                Debug.Log($"   Tile[{j}]: {(t != null ? t.name : "null")}, pos: {(t != null ? t.localPosition.ToString() : "")}");
            }
        }
    }
}
