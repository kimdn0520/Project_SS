using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;

public static class ItemAuthoringQA
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> checks=new List<string>();
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add("PASS "+label);}
    public static string Execute()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play first");
        checks.Clear();
        var assembly=typeof(ProjectSS.ContentEditor.ContentManagementWindow).Assembly;
        var wt=typeof(ProjectSS.ContentEditor.ContentManagementWindow);
        var original=AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset");
        string originalJson=EditorJsonUtility.ToJson(original);
        var copy=UnityEngine.Object.Instantiate(original);
        string path="Assets/ItemAuthoringQA_"+Guid.NewGuid().ToString("N")+".asset";
        ProjectSS.ContentEditor.ContentManagementWindow window=null;
        GameObject prefab=null;
        try
        {
            AssetDatabase.CreateAsset(copy,path);
            window=ScriptableObject.CreateInstance<ProjectSS.ContentEditor.ContentManagementWindow>();
            wt.GetMethod("SetSource",Hidden).Invoke(window,new object[]{copy});
            var draft=(ExpeditionCatalog)wt.GetField("draft",Hidden).GetValue(window);
            var pt=assembly.GetType("ProjectSS.ContentEditor.EquipmentManagementPanel");
            var panel=Activator.CreateInstance(pt,true);
            for(int category=1;category<=3;category++)
            {
                pt.GetField("category",Hidden).SetValue(panel,category);
                pt.GetMethod("Add",Hidden).Invoke(panel,new object[]{window,false});
                Check(draft.gear.Last().equipSlot==new[]{0,0,2,3}[category],"category "+category+" creates correct slot");
            }
            var weapon=draft.gear[draft.gear.Length-3];
            weapon.damage=42;weapon.interval=.5f;weapon.criticalChance=12;weapon.criticalDamage=175;weapon.skillAmplification=18;
            var armor=draft.gear[draft.gear.Length-2];armor.defense=34;armor.health=120;armor.evasion=8;
            Check(weapon.AttacksPerSecond==2,"attack speed converts to attacks per second");
            var mt=assembly.GetType("ProjectSS.ContentEditor.MaterialManagementPanel");
            var materialPanel=Activator.CreateInstance(mt,true);
            mt.GetMethod("Add",Hidden).Invoke(materialPanel,new object[]{window,false});
            var material=draft.materials.Last();material.title="QA 목재";material.description="테스트 재료";material.icon=original.gear[0].icon;
            mt.GetMethod("Add",Hidden).Invoke(materialPanel,new object[]{window,true});
            Check(draft.materials.Last().id!=material.id&&draft.materials.Last().storage==MaterialStorage.Inventory,"material clone uses unique stable ID and own storage");
            Check(copy.gear.Length==original.gear.Length&&copy.materials.Length==original.materials.Length,"draft isolates all categories");
            var validation=assembly.GetType("ProjectSS.ContentEditor.GearCatalogValidation").GetMethod("Errors",BindingFlags.Static|BindingFlags.NonPublic);
            Func<int> errors=()=>((List<string>)validation.Invoke(null,new object[]{draft,null})).Count;
            Check(errors()==0,"all item categories validate");
            weapon.criticalChance=101;Check(errors()>0,"reject probability above 100");weapon.criticalChance=12;
            armor.defense=float.NaN;Check(errors()>0,"reject invalid defense");armor.defense=34;
            string id=draft.materials.Last().id;draft.materials.Last().id=material.id;Check(errors()>0,"reject duplicate material IDs");draft.materials.Last().id=id;
            window.SaveChanges();AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var reloaded=AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>(path);
            Check(!window.hasUnsavedChanges&&reloaded.gear.Length==draft.gear.Length&&reloaded.materials.Length==draft.materials.Length,"save all categories and reload");
            Check(reloaded.gear[reloaded.gear.Length-3].criticalDamage==175&&reloaded.gear[reloaded.gear.Length-2].evasion==8,"new gear stats persisted");
            Check(reloaded.materials[3].icon==original.gear[0].icon,"material icon reference persisted");
            var save=ExpeditionSave.Fresh(original.gear.Length);save.iron=31;
            Check(save.TryExpandInventory(reloaded.gear.Length)&&save.iron==31&&save.inventory[0]==1,"new gear preserves existing inventory/currencies");
            var model=new ExpeditionModel(reloaded,save,1);
            Check(model.AddMaterial(3,7)&&model.MaterialCount(3)==7&&model.Data.iron==31,"custom material separate from legacy currency");
            var roundtrip=JsonUtility.FromJson<ExpeditionSave>(JsonUtility.ToJson(save));
            Check(roundtrip.IsValid(reloaded.gear.Length)&&new ExpeditionModel(reloaded,roundtrip,1).MaterialCount(3)==7,"material count save round-trip");
            prefab=PrefabUtility.LoadPrefabContents("Assets/Resources/Prefabs/PlayPage.prefab");
            var page=prefab.GetComponent<PlayPage>();page.catalog=reloaded;
            typeof(PlayPage).GetField("<Model>k__BackingField",Hidden).SetValue(page,model);
            save.inventory[reloaded.gear.Length-3]=1;save.inventory[reloaded.gear.Length-2]=1;save.inventory[reloaded.gear.Length-1]=1;
            page.inventoryView.Refresh();
            Check(page.inventoryView.FilteredItemIndices.Contains(reloaded.gear.Length-3),"new weapon included in virtual list");
            page.inventoryView.SelectFilter(2);
            Check(page.inventoryView.FilteredItemIndices.Contains(reloaded.gear.Length-1),"newly owned accessory included in accessory tab");
            page.inventoryView.SelectFilter(3);
            Check(page.inventoryView.FilteredItemIndices.Contains(3),"newly owned material included in material tab");
            Check(EditorJsonUtility.ToJson(original)==originalJson,"original catalog unchanged by QA");
            File.WriteAllText("PrototypeQA/item-authoring.txt",string.Join("\n",checks));return string.Join("\n",checks);
        }
        finally
        {
            if(prefab!=null)PrefabUtility.UnloadPrefabContents(prefab);
            if(window!=null){window.DiscardChanges();UnityEngine.Object.DestroyImmediate(window);}
            AssetDatabase.DeleteAsset(path);
        }
    }
}
