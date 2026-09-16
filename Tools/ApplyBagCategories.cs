using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;

public static class ApplyBagCategories
{
    public static string Execute()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play first");
        const string path="Assets/Resources/Prefabs/PlayPage.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var bag=root.GetComponent<PlayPage>().inventoryView;
            if(bag.tabs.Length==3)
            {
                var fourth=UnityEngine.Object.Instantiate(bag.tabs[2].gameObject,bag.tabs[2].transform.parent);
                fourth.name="BagFilter3";
                Array.Resize(ref bag.tabs,4);bag.tabs[3]=fourth.GetComponent<Image>();
            }
            if(bag.tabs.Length!=4)throw new Exception("Expected four bag tabs");
            for(int i=0;i<4;i++)
            {
                var tab=bag.tabs[i];tab.name="BagFilter"+i;
                var button=tab.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();
                UnityEventTools.AddIntPersistentListener(button.onClick,bag.SelectFilter,i);
                var label=tab.GetComponentInChildren<TMP_Text>(true);
                label.text=ExpeditionInventory.CategoryNames[i];label.fontSize=23;
                label.textWrappingMode=TextWrappingModes.NoWrap;
                label.enableAutoSizing=true;label.fontSizeMin=19;label.fontSizeMax=23;
                tab.transform.Find("Surface").GetComponent<Image>().color=i==0?new Color(.21f,.43f,.45f):new Color(.95f,.96f,.91f);
                label.color=i==0?Color.white:new Color(.14f,.24f,.29f);
            }
            MenuPolishArt.AlignBagTabs(bag);
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
            return "Bag tabs saved: 무기 / 방어구 (투구+갑옷) / 장신구 / 재료";
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
