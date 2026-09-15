using UnityEditor;
using UnityEngine;
namespace ProjectSS.Expedition.Editor
{
    public static class MinerArmorArt
    {
        public static void Configure(PlayPage page)
        {
            const string pack="Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Vol.1/Parts/";
            var chest=AssetDatabase.LoadAssetAtPath<Sprite>(pack+"Chest/FA_Chest_045_Gray.png");
            var helmet=AssetDatabase.LoadAssetAtPath<Sprite>(pack+"Helmet/FA_Helmet_041_Gray.png");
            if(chest==null||helmet==null)throw new System.Exception("Miner armor sprites missing");
            page.miner.EquipArmor(chest,helmet);
            var so=new SerializedObject(page.miner);
            so.FindProperty("defaultChest").objectReferenceValue=chest;
            so.FindProperty("armorChest").objectReferenceValue=chest;
            so.FindProperty("armorHelmet").objectReferenceValue=helmet;so.ApplyModifiedPropertiesWithoutUndo();
            foreach(var sr in page.miner.GetComponentsInChildren<SpriteRenderer>(true))
                if(sr.name=="Hair"||sr.name=="Hair_Helmet"||sr.name=="Beard")sr.gameObject.SetActive(false);
            EditorUtility.SetDirty(page.miner);
        }
    }
}
