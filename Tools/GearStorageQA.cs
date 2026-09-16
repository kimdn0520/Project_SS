using System;using System.Linq;using System.IO;using UnityEditor;using UnityEngine;using ProjectSS.Expedition;
public static class GearStorageQA {
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 public static string Execute(){checks=0;var catalog=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset"));try{
 catalog.weaponCapacity=100;catalog.armorCapacity=100;catalog.accessoryCapacity=100;catalog.dismantleRefundRate=.25f;
 var save=ExpeditionSave.Fresh(catalog.gear.Length);save.inventory[3]=3;var m=new ExpeditionModel(catalog,save,42);
 Check(save.version==5&&save.gearInstances.Count==6,"Legacy quantities migrate");Check(save.gearInstances.Select(g=>g.uid).Distinct().Count()==6,"Unique IDs");Check(save.IsValid(catalog.gear.Length),"Migrated save valid");Check(m.StorageUsed(0)==6,"Equipped items use capacity");
 var copies=m.Copies(3).ToArray();copies[0].options=new[]{new GearRoll{stat=GearOptionStat.Attack,unit=GearOptionUnit.Flat,value=7}};
 Check(m.EffectiveGear(copies[0]).damage==catalog.gear[3].damage+7,"Individual option applied");Check(m.EffectiveGear(copies[1]).damage==catalog.gear[3].damage,"Other copy unchanged");
 Check(m.EquipInstance(copies[0].uid,0)&&m.EquippedUid(0,0)==copies[0].uid,"Equip exact instance");Check(!m.Dismantle(copies[0].uid),"Equipped protection");m.SetGearLocked(copies[1].uid,true);Check(m.CanDismantle(copies[1].uid),"Retired lock flag does not strand items");m.SetGearLocked(copies[1].uid,false);
 var reward=m.DismantleReward(3);int iron=save.iron,crystal=save.crystal,relic=save.relic;
 Check(m.Dismantle(copies[1].uid),"Dismantle allowed");Check(!m.Dismantle(copies[1].uid),"No double reward");Check(save.iron==iron+reward.x&&save.crystal==crystal+reward.y&&save.relic==relic+reward.z&&save.inventory[3]==2,"Atomic material reward and quantity");
 m.StartBattle();Check(m.EquipInstance(copies[2].uid,0),"Change during battle");Check(m.PendingBattleChanges&&!m.Dismantle(copies[0].uid),"Snapshot protected until battle ends");m.CancelBattle();Check(m.Dismantle(copies[0].uid),"Snapshot released");
 while(m.StorageUsed(0)<100)Check(m.TryAddGear(3)!=null,"Fill weapon capacity");Check(m.TryAddGear(3)==null,"101st rejected");
 int armor=Array.FindIndex(catalog.gear,g=>g.equipSlot==2),accessory=Array.FindIndex(catalog.gear,g=>g.equipSlot==3);
 Check(m.TryAddGear(armor)!=null&&m.TryAddGear(accessory)!=null,"Independent category capacities");save.iron=save.crystal=save.relic=10000;save.cleared=100;
 iron=save.iron;Check(!m.Craft(3)&&save.iron==iron,"Full craft charges nothing");
 save.chest=true;save.pendingChestGear=3;int depth=save.depth; m.ClaimChest();Check(save.chest&&save.pendingChestGear==3&&save.depth==depth,"Full chest retains reward and progress");
 var free=m.Copies(3).First(g=>m.CanDismantle(g.uid));Check(m.Dismantle(free.uid),"Free slot");m.ClaimChest();Check(save.pendingChestGear==-1&&m.StorageUsed(0)==100&&m.LastGear==3,"Retry exact chest reward");
 var reload=JsonUtility.FromJson<ExpeditionSave>(JsonUtility.ToJson(save));Check(reload.IsValid(catalog.gear.Length),"Save roundtrip");string equipped=m.EquippedUid(0,0);var loaded=new ExpeditionModel(catalog,reload,50);Check(loaded.EquippedUid(0,0)==equipped&&reload.version==5,"Reload keeps IDs/version");
 var legacy=ExpeditionSave.Fresh(catalog.gear.Length);legacy.inventory[3]=105;var overflow=new ExpeditionModel(catalog,legacy);Check(overflow.StorageUsed(0)==108&&!overflow.HasGearSpace(3),"Over-cap legacy preserved");
 var corrupt=JsonUtility.FromJson<ExpeditionSave>(JsonUtility.ToJson(save));corrupt.gearInstances[1].uid=corrupt.gearInstances[0].uid;Check(!corrupt.IsValid(catalog.gear.Length),"Duplicate IDs rejected");
 string result="PASS: "+checks+" checks; migration, distinct rolls, exact equip, category limits, safe dismantle, battle protection, full chest retention, save/reload.";File.WriteAllText("PrototypeQA/gear-storage.txt",result);return result;
 }finally{UnityEngine.Object.DestroyImmediate(catalog);}}
}
