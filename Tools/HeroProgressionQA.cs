using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using ProjectSS.Expedition;
public static class HeroProgressionQA {
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 public static string Execute(){checks=0;var catalog=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset"));try{
 catalog.heroStarCosts=new[]{5,10,20,40};var data=ExpeditionSave.Fresh(catalog.gear.Length);var m=new ExpeditionModel(catalog,data,1);
 Check(m.HeroProgression(0).stars==1&&m.HeroProgression(0).duplicates==0,"Legacy starts at 1 star");Check(data.IsValid(catalog.gear.Length),"Migrated save valid");
 Check(m.GrantHeroCopies(0,4)&&m.HeroProgression(0).stars==1&&m.HeroProgression(0).duplicates==4,"Before threshold");Check(m.GrantHeroCopies(0)&&m.HeroProgression(0).stars==2&&m.HeroProgression(0).duplicates==0,"Exact threshold");
 Check(m.GrantHeroCopies(0,73)&&m.HeroProgression(0).stars==5&&m.HeroProgression(0).duplicates==3,"Multiple promotions and remainder");m.GrantHeroCopies(0,2);Check(m.HeroProgression(0).stars==5&&m.HeroProgression(0).duplicates==5,"Max star preserves excess");Check(m.HeroProgression(1).stars==1,"Other hero unaffected");
 var loaded=JsonUtility.FromJson<ExpeditionSave>(JsonUtility.ToJson(data));var restored=new ExpeditionModel(catalog,loaded);Check(loaded.IsValid(catalog.gear.Length)&&restored.HeroProgression(0).stars==5&&restored.HeroProgression(0).duplicates==5,"Save reload");
 var limited=ExpeditionSave.Fresh(catalog.gear.Length);limited.ownedHeroes=new[]{"rowen"};limited.formation=new[]{"rowen",null,null};var unlock=new ExpeditionModel(catalog,limited);Check(unlock.GrantHeroCopies(1)&&unlock.IsOwned(1)&&unlock.HeroProgression(1).stars==1&&unlock.HeroProgression(1).duplicates==0,"First copy unlocks, not duplicate");
 Check(!unlock.GrantHeroCopies(-1)&&!unlock.GrantHeroCopies(1,0),"Invalid input rejected");foreach(HeroGrade grade in Enum.GetValues(typeof(HeroGrade))){catalog.heroes[0].grade=grade;Check(restored.Grade(0)==grade,"Grade independent of stars");}
 loaded.heroProgress[0].stars=6;Check(!loaded.IsValid(catalog.gear.Length),"Invalid stars rejected");
 string result="PASS: "+checks+" hero progression checks (migration, thresholds, 5-star cap, overflow retention, unlock, save/reload, E-S grades).";File.WriteAllText("PrototypeQA/hero-progression.txt",result);return result;
 }finally{UnityEngine.Object.DestroyImmediate(catalog);}}
}
