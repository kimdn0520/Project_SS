using UnityEditor;

public static class PlaySceneSetup
{
    [MenuItem("ProjectSS/Setup Play Scene (PagePrefabBuilder)")]
    public static void SetupScene()
    {
        PagePrefabBuilder.Execute();
    }
}
