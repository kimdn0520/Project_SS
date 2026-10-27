using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
public static class CleanBlockUiDrafts
{
    public static void Execute()
    {
        var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/RuneBreak/Art/BlockPunch/BlockPunch.spriteatlas");
        foreach(string name in new[]{"button_gold_base","button_gold_face","button_violet_base","button_violet_face","button_blue_base","button_blue_face","stud_gold","stud_blue","stud_violet","popup_panel"})
        {
            string path="Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/"+name+".png";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite==null)continue;
            atlas.Remove(new Object[]{sprite});AssetDatabase.DeleteAsset(path);
        }
        EditorUtility.SetDirty(atlas);AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
    }
}
