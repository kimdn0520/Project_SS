using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using ProjectSS.Expedition;
public static class AutoBattleQA
{
 static readonly List<string> results=new List<string>();
 static void Check(bool yes,string name){if(!yes)throw new Exception(name);results.Add("PASS: "+name);}
 static ExpeditionCatalog Fixture(float? damage=null)
 {
  var source=AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset");
  var clone=ScriptableObject.CreateInstance<ExpeditionCatalog>();clone.gear=source.gear.Select(g=>JsonUtility.FromJson<GearDefinition>(JsonUtility.ToJson(g))).ToArray();
  if(damage.HasValue)foreach(var g in clone.gear)if(g.equipSlot==0)g.damage=damage.Value;
  return clone;
 }
 static ExpeditionModel Model(ExpeditionCatalog c,int cleared=0,params string[] formation)
 {
  var d=ExpeditionSave.Fresh(c.gear.Length);d.cleared=cleared;if(formation.Length>0)d.formation=formation;
  return new ExpeditionModel(c,d,1);
 }
 static void Resolve(ExpeditionModel m){for(int i=0;i<30000&&m.Fighting;i++)m.Tick(.05f);Check(!m.Fighting,"battle resolves");}
 public static string Execute()
 {
  results.Clear();var fixtures=new List<ExpeditionCatalog>();
  try
  {
   var c=Fixture();fixtures.Add(c);
   var old=ExpeditionSave.Fresh(c.gear.Length);old.version=3;old.ownedHeroes=null;old.formation=null;old.autoBattle=false;old.iron=123;old.crystal=45;old.relic=6;old.cleared=14;
   var load=JsonUtility.FromJson<ExpeditionSave>(JsonUtility.ToJson(old));Check(load.TryExpandInventory(c.gear.Length),"v3 inventory compatibility");
   var migrated=new ExpeditionModel(c,load,1);
   Check(load.version==4&&load.autoBattle&&load.formation.SequenceEqual(HeroRoster.Ids)&&load.iron==123&&load.crystal==45&&load.relic==6&&load.cleared==14&&load.equipment.SequenceEqual(old.equipment),"v3 migration preserves resources, gear, progress and defaults formation");
   load.ownedHeroes=new[]{"rin","mira"};load.formation=new[]{"bad","rin","rin"};HeroRoster.Migrate(load);
   Check(load.formation.Count(s=>s!=null)==1&&load.formation[1]=="rin","invalid and duplicate IDs repaired");
   load.formation=new[]{"rowen",null,null};HeroRoster.Migrate(load);Check(load.formation[0]=="rin","unowned/all-empty repaired to owned hero");
   var m=Model(c);var gearBefore=(int[])m.Data.equipment.Clone();
   Check(m.AssignHero(2,0)&&m.ConfiguredHero(0)==2&&m.ConfiguredHero(2)==0,"occupied slot exchange");
   Check(m.RemoveHero(1)&&m.AssignHero(1,2)&&m.ConfiguredHero(0)==-1,"move deployed hero to empty slot");
   Check(m.AssignHero(1,1)&&m.ConfiguredHero(1)==1&&m.FormationSlot(2)<0,"replace with undeployed hero");
   Check(m.RemoveHero(2)&&!m.RemoveHero(1)&&m.Data.equipment.SequenceEqual(gearBefore),"last hero protected and equipment remains hero-owned");
   foreach(var f in new[]{new[]{null,null,"rowen"},new[]{null,"rin","mira"},new[]{"rowen","rin","mira"}})
   {
    m=Model(c,0,f);m.StartBattle();Check(m.FrontAliveHero()==HeroRoster.Index(f.First(s=>s!=null)),"front target skips empty slots");Resolve(m);Check(m.LastVictory,"1/2/3 hero formation can win");
   }
   var zero=Fixture(0);fixtures.Add(zero);m=Model(zero,24,"mira",null,"rowen");m.StartBattle();
   while(m.HeroHp(2)>0)m.Tick(.05f);
   Check(m.FrontAliveHero()==0,"dead front hero skipped");float nextHp=m.HeroHp(0);for(int i=0;i<50;i++)m.Tick(.05f);Check(m.HeroHp(0)<nextHp,"next living hero takes damage");
   var strong=Fixture(10000);fixtures.Add(strong);m=Model(strong,8);int ended=0;m.BattleEnded+=win=>ended++;
   m.StartBattle();Resolve(m);Check(m.Data.cleared==9&&m.Wave==10&&m.IsBoss,"1-9 advances to 1-10 boss");
   int iron=m.Data.iron,crystal=m.Data.crystal,relic=m.Data.relic;m.StartBattle();Resolve(m);
   Check(m.Region==2&&m.Wave==1&&m.Data.iron==iron+20&&m.Data.crystal==crystal+12&&m.Data.relic==relic+3,"boss rewards and 2-1 progression");
   iron=m.Data.iron;m.Tick(50);m.Retreat();Check(ended==2&&m.Data.iron==iron&&m.Data.cleared==10,"result/reward processed once");
   m=Model(zero,14,null,"rin",null);m.StartBattle();Resolve(m);
   Check(!m.LastVictory&&m.Data.cleared==14&&m.Data.iron==0&&m.Data.crystal==0,"2-5 defeat keeps stage and grants no clear reward");
   m.StartBattle();Check(m.HeroHp(1)==m.BattleMaxHp(1)&&m.EnemyHp==m.EnemyMaxHp&&m.EnemyTimer==1.8f,"retry resets all health and enemy timer");
   m=Model(c);m.StartBattle();m.Tick(.2f);float hp=m.HeroHp(0),max=m.BattleMaxHp(0),damage=m.BattleDamage(0),interval=m.BattleInterval(0),timer=m.AttackRemaining(0);
   m.Data.inventory[3]=2;int armor=Array.FindIndex(c.gear,g=>g.equipSlot==2);m.Data.inventory[armor]=1;
   Check(m.Equip(3,0)&&m.Equip(armor,0)&&m.AssignHero(2,0),"configuration editable during battle");
   Check(m.HeroHp(0)==hp&&m.BattleMaxHp(0)==max&&m.BattleDamage(0)==damage&&m.BattleInterval(0)==interval&&m.AttackRemaining(0)==timer&&m.BattleHero(0)==0&&m.BattleEquipped(0,0)==0&&m.PendingBattleChanges,"battle snapshot unaffected by equipment/formation edits");
   m.Retreat();m.StartBattle();Check(m.BattleHero(2)==0&&m.BattleEquipped(0,0)==3&&m.BattleMaxHp(0)==m.HeroMaxHp(0)&&!m.PendingBattleChanges,"next battle applies pending snapshot");
   var skilled=Fixture(0);fixtures.Add(skilled);
   var authored=AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Prototype/Skills/frost_nova.asset");
   var skill=JsonUtility.FromJson<GearSkillDefinition>(JsonUtility.ToJson(authored.settings));skill.cooldown=1;
   skilled.gear[0].rarity=GearRarity.Legendary;skilled.gear[0].skillAsset=null;skilled.gear[0].skill=skill;
   m=Model(skilled);int casts=0;m.SkillCast+=(hero,s)=>{Check(hero==0&&s.effect==GearSkillEffect.Freeze,"skill uses correct hero and authored effect");casts++;};
   m.StartBattle();m.Tick(.9f);Check(casts==0,"skill waits for initial cooldown");m.Tick(.15f);Check(casts==1&&m.EnemyFrozenFor>0,"automatic frost nova targets living enemy");
   float enemyTimer=m.EnemyTimer;m.Tick(.2f);Check(Math.Abs(m.EnemyTimer-enemyTimer)<.001f,"freeze suspends enemy attacks");
   skill.duration=99;Check(m.EnemyFrozenFor<2.1f,"skill definition copied for current battle");
   m.Retreat();int oldCasts=casts;m.Tick(30);Check(casts==oldCasts,"no skill after battle ends");
   m.StartBattle();Check(m.EnemyFrozenFor==0,"previous battle delayed status cleared");
   var save=JsonUtility.FromJson<ExpeditionSave>(JsonUtility.ToJson(m.Data));var restored=new ExpeditionModel(skilled,save,1);
   Check(!restored.Fighting&&restored.Data.cleared==m.Data.cleared&&restored.Data.formation.SequenceEqual(m.Data.formation)&&restored.Data.equipment.SequenceEqual(m.Data.equipment),"reconnect restores configuration/progress without rewarding");
   restored.StartBattle();Check(restored.EnemyHp==restored.EnemyMaxHp,"reconnect starts fresh encounter");
   skill.duration=.1f;skill.cooldown=1;m=Model(skilled,1000,"rowen","rin",null);int deadCasts=0;m.SkillCast+=(hero,s)=>deadCasts++;
   m.StartBattle();while(m.HeroHp(0)>0&&m.Fighting)m.Tick(.05f);int atDeath=deadCasts;for(int i=0;i<100;i++)m.Tick(.05f);Check(deadCasts==atDeath,"dead caster cannot cast");
   Check(!ExpeditionModel.CanExecute(new GearSkillDefinition{effect=GearSkillEffect.Burn,cooldown=1,duration=2}),"undefined burn execution not invented");
   m=Model(c);int startIron=m.Data.iron;for(int i=0;i<16;i++)m.Dig(0);Check(m.Data.excavations==1&&m.Data.depth==2&&m.Data.iron>startIron,"mining durability/material loop unchanged");
   m.Data.chest=true;int inventory=m.Data.inventory.Sum();m.ClaimChest();Check(m.Data.inventory.Sum()==inventory+1,"mining chest awards one gear");
   File.WriteAllLines("PrototypeQA/auto-battle-logic.txt",results);return string.Join("\n",results);
  }
  catch(Exception e){results.Add("FAIL: "+e);File.WriteAllLines("PrototypeQA/auto-battle-logic.txt",results);throw;}
  finally{foreach(var c in fixtures)UnityEngine.Object.DestroyImmediate(c);}
 }
}
