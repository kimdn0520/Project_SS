using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;
public static class AutoBattlePlayQAFinal
{
 static readonly List<string> results=new List<string>();
 static void Check(bool yes,string message){if(!yes)throw new Exception(message);results.Add("PASS: "+message);}
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void SetModel(PlayPage page,ExpeditionCatalog catalog,ExpeditionSave save)
 {
  typeof(PlayPage).GetMethod("Unwire",Private).Invoke(page,null);page.catalog=catalog;
  typeof(PlayPage).GetProperty("Model").SetValue(page,new ExpeditionModel(catalog,save,5));page.OnWillEnter(null);
 }
 static void Begin(PlayPage page){page.TickJourney(.01f);page.TickJourney(2.21f);Check(page.State==PlayPage.Journey.Fighting,"automatic encounter starts");}
 static async UniTask Shot(PlayPage page,string name)
 {
  page.Refresh();await UniTask.WaitForEndOfFrame(page);var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/auto-"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
 }
 public static void Execute(){Run().Forget();}
 static async UniTaskVoid Run()
 {
  results.Clear();EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
  var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();page.OnWillLeave();PopupManager.Clear();
  const string key="ProjectSS.Play.v3";var backup=PlayerPrefs.GetString(key);var originalCatalog=page.catalog;
  var originalSave=JsonUtility.ToJson(page.Model.Data);
  var catalog=ScriptableObject.CreateInstance<ExpeditionCatalog>();catalog.gear=originalCatalog.gear.Select(g=>JsonUtility.FromJson<GearDefinition>(JsonUtility.ToJson(g))).ToArray();
  try
  {
   Check(page.GetComponentsInChildren<UnityEngine.UI.Toggle>(true).All(t=>t.name!="AutoBattle"),"legacy battle toggle removed from live prefab");
   foreach(var formation in new[]{new[]{null,null,"mira"},new[]{"rin",null,"rowen"},new[]{"mira","rowen","rin"}})
   {
    var d=ExpeditionSave.Fresh(catalog.gear.Length);d.formation=formation;SetModel(page,catalog,d);page.Model.Data.autoBattle=false;Begin(page);
    for(int slot=0;slot<3;slot++)
    {
     int hero=page.Model.BattleHero(slot);if(hero<0)continue;
     Check(page.heroes[hero].gameObject.activeSelf&&Vector3.Distance(page.heroes[hero].transform.localPosition,page.formationPositions[slot])<.001f,"actor follows formation slot "+slot);
     Check(page.heroHpRoots[hero].gameObject.activeSelf,"matching health bar active for hero "+hero);
    }
    for(int h=0;h<3;h++)Check(page.heroes[h].gameObject.activeSelf==(Array.IndexOf(formation,HeroRoster.Ids[h])>=0),"undeployed actor hidden "+h);
    page.TickJourney(.4f);await Shot(page,"party-"+formation.Count(id=>id!=null));
   }
   var config=ExpeditionSave.Fresh(catalog.gear.Length);config.inventory[3]=2;
   SetModel(page,catalog,config);Begin(page);float hp=page.Model.HeroHp(0),timer=page.Model.AttackRemaining(0);var oldPosition=page.heroes[0].transform.localPosition;
   Check(page.EquipForHero(3,0)&&page.AssignFormation(2,0),"live equipment and formation edits accepted");
   Check(page.Model.HeroHp(0)==hp&&page.Model.AttackRemaining(0)==timer&&page.Model.BattleEquipped(0,0)==0&&page.heroes[0].transform.localPosition==oldPosition,"live battle retains old gear/position/HP/timer");
   page.OpenMenu(1);await Shot(page,"formation-pending");Check(page.pendingBattleLabel.gameObject.activeSelf,"pending changes message visible");
   page.OpenHero(0);await Shot(page,"hero-equipment");
   page.OpenMenu(0);page.Model.Retreat();Check(page.RetryRemaining==3,"retry default is 3 seconds");
   page.TickJourney(2.99f);Check(page.State==PlayPage.Journey.Recovering,"retry waits full duration");await Shot(page,"retry");
   page.TickJourney(.02f);Check(page.State==PlayPage.Journey.Fighting&&page.Model.BattleHero(2)==0&&page.Model.BattleEquipped(0,0)==3,"retry starts new snapshot after 3 seconds");
   Check(Vector3.Distance(page.heroes[0].transform.localPosition,page.formationPositions[2])<.001f,"retry visuals follow updated formation");
   foreach(var g in catalog.gear)if(g.equipSlot==0)g.damage=0;
   config=ExpeditionSave.Fresh(catalog.gear.Length);config.cleared=14;config.formation=new[]{null,"rin",null};SetModel(page,catalog,config);Begin(page);
   for(int i=0;i<1000&&page.Model.Fighting;i++)page.TickJourney(.05f);
   Check(page.State==PlayPage.Journey.Recovering&&!page.Model.LastVictory&&page.Model.Data.cleared==14,"actual 2-5 defeat preserves wave");
   page.TickJourney(3.01f);Check(page.Model.Fighting&&page.Model.Wave==5&&page.Model.Region==2&&page.Model.HeroHp(1)==page.Model.BattleMaxHp(1),"2-5 retry restores HP on same wave");
   var frost=AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Prototype/Skills/frost_nova.asset");
   catalog.gear[0].rarity=GearRarity.Legendary;catalog.gear[0].skillAsset=null;catalog.gear[0].skill=JsonUtility.FromJson<GearSkillDefinition>(JsonUtility.ToJson(frost.settings));catalog.gear[0].skill.cooldown=.5f;
   SetModel(page,catalog,ExpeditionSave.Fresh(catalog.gear.Length));Begin(page);int skillHero=-1;page.Model.SkillCast+=(h,s)=>skillHero=h;
   page.TickJourney(.6f);Check(skillHero==0&&page.Model.EnemyFrozenFor>0,"live skill cast and freeze event attached to correct hero");await Shot(page,"skill");
   foreach(var g in catalog.gear)if(g.equipSlot==0)g.damage=10000;
   config=ExpeditionSave.Fresh(catalog.gear.Length);config.cleared=8;SetModel(page,catalog,config);Begin(page);page.TickJourney(2);
   Check(page.Model.Data.cleared==9,"live 1-9 clear");page.TickJourney(1.2f);Begin(page);Check(page.Model.IsBoss,"live 1-10 boss");await Shot(page,"boss");page.TickJourney(2);
   Check(page.Model.Region==2&&page.Model.Wave==1,"live boss clear to 2-1");
   page.OpenMenu(1);page.formationPanel.SelectPosition(0);page.formationPanel.SelectHero(2);Check(page.Model.ConfiguredHero(0)==2,"formation UI selection flow");await Shot(page,"formation");
   // Exercise the actual PlayerPrefs loading path, not just serialization in isolation.
   var legacy=ExpeditionSave.Fresh(catalog.gear.Length);legacy.version=3;legacy.formation=null;legacy.ownedHeroes=null;legacy.autoBattle=false;legacy.cleared=14;legacy.iron=77;
   typeof(PlayPage).GetMethod("Unwire",Private).Invoke(page,null);typeof(PlayPage).GetProperty("Model").SetValue(page,null);
   PlayerPrefs.SetString(key,JsonUtility.ToJson(legacy));page.OnWillEnter(null);
   Check(page.Model.Data.version==4&&page.Model.Data.cleared==14&&page.Model.Data.iron==77&&page.Model.Data.formation.SequenceEqual(HeroRoster.Ids),"actual legacy PlayerPrefs load migrates without resetting progress");
   page.AssignFormation(2,0);string saved=PlayerPrefs.GetString(key);
   typeof(PlayPage).GetMethod("Unwire",Private).Invoke(page,null);typeof(PlayPage).GetProperty("Model").SetValue(page,null);page.OnWillEnter(null);
   Check(page.Model.ConfiguredHero(2)==0&&page.Model.Data.cleared==14&&!page.Model.Fighting&&page.State==PlayPage.Journey.Waiting,"actual reconnect restores formation and restarts wave without reward");
   // Run the production asynchronous game loop and chest animation using real frame time.
   foreach(var g in catalog.gear)if(g.equipSlot==0)g.damage=100;
   SetModel(page,catalog,ExpeditionSave.Fresh(catalog.gear.Length));page.Model.Data.autoBattle=false;
   page.Model.Data.chest=true;page.miningView.SetChest(true);int gearCount=page.Model.Data.inventory.Sum();
   page.pausePolicy.ReleasePause("AppFocusLoss");page.OnDidEnter();await UniTask.Delay(200,ignoreTimeScale:true);page.Dig();
   Check(page.ChestOpening,"real DIG opens chest during automatic journey");
   float deadline=Time.realtimeSinceStartup+30;
   while((page.Model.Data.cleared==0||page.ChestOpening)&&Time.realtimeSinceStartup<deadline)await UniTask.Delay(100,ignoreTimeScale:true);
   Check(page.Model.Data.cleared>0,"production async loop automatically moves, attacks and rewards; state="+page.State+", paused="+page.pausePolicy.IsPaused);
   Check(!page.ChestOpening&&page.Model.Data.inventory.Sum()==gearCount+1,"real chest animation awards gear once and finishes alongside auto combat");
   page.OnWillLeave();
   File.WriteAllLines("PrototypeQA/auto-battle-play.txt",results);
  }
  catch(Exception e){results.Add("FAIL: "+e);File.WriteAllLines("PrototypeQA/auto-battle-play.txt",results);Debug.LogException(e);}
  finally
  {
   page.OnWillLeave();SetModel(page,originalCatalog,JsonUtility.FromJson<ExpeditionSave>(originalSave));PlayerPrefs.SetString(key,backup);PlayerPrefs.Save();page.OnDidEnter();UnityEngine.Object.Destroy(catalog);
  }
 }
}
