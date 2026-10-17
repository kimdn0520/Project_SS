using UnityEditor;
using System.Linq;
public static class InspectRuneBuild
{
    public static object Execute()=>"Splash asset GUID: "+AssetDatabase.AssetPathToGUID("Assets/Scenes/Splash.unity")+"; "+string.Join("; ",EditorBuildSettings.scenes.Select(s=>s.path+"="+s.guid));
}
