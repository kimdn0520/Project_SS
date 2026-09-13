using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InspectChest
{
 public static object Execute(){var t=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Layer Lab/2D Minimal-IconPack/Icons/512/Item_Chest_01_Wood.png");return new {width=t.width,height=t.height,parts=AssetDatabase.LoadAllAssetsAtPath("Assets/Prototype/Art/ChestParts.asset").OfType<Sprite>().Select(s=>s.name+":"+s.rect+":"+s.bounds).ToArray()};}
}
