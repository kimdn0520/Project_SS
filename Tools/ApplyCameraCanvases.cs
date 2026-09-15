using UnityEngine;
using UnityEditor;
public static class ApplyCameraCanvases
{
    public static void Execute()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/Prefabs"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var root=PrefabUtility.LoadPrefabContents(path);bool changed=false;
            try
            {
                foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if(canvas.renderMode==RenderMode.WorldSpace)continue;
                    if(canvas.renderMode==RenderMode.ScreenSpaceCamera&&Mathf.Approximately(canvas.planeDistance,10))continue;
                    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.planeDistance=10;changed=true;
                }
                if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
}
