using UnityEngine;
using UnityEditor;
using System.Linq;
public static class InspectGameView
{
    public static object Execute()
    {
        var a=typeof(UnityEditor.Editor).Assembly;
        var names=new[]{"UnityEditor.GameViewSizes","UnityEditor.GameViewSize","UnityEditor.GameViewSizeGroup","UnityEditor.GameView"};
        return string.Join("\n",names.Select(name=>name+"\n"+string.Join("\n",a.GetType(name).GetMembers(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static).Where(m=>m.Name.Contains("Size")||m.Name=="instance"||m.Name=="GetGroup"||m.MemberType==System.Reflection.MemberTypes.Constructor).Select(m=>m.ToString()))));
    }
}
