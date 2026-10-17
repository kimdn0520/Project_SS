using System;using System.Collections.Generic;using UnityEngine;
namespace ProjectSS.Expedition
{
 public enum HeroGrade { E, D, C, B, A, S }
 [Serializable] public sealed class HeroDefinition { public string id; public HeroGrade grade; }
 [Serializable] public sealed class HeroProgress { public string id; public int stars=1; public int duplicates; }
 public sealed partial class ExpeditionModel
 {
  public const int MaxHeroStars=5;
  public HeroProgress HeroProgression(int hero) => Data.heroProgress.Find(p=>p.id==HeroRoster.Ids[hero]);
  public HeroGrade Grade(int hero){foreach(var def in Catalog.heroes??Array.Empty<HeroDefinition>())if(def!=null&&def.id==HeroRoster.Ids[hero])return def.grade;return HeroGrade.E;}
  public int RequiredHeroCopies(int stars){if(stars>=MaxHeroStars)return 0;var costs=Catalog.heroStarCosts;return costs!=null&&stars>0&&stars<=costs.Length?Mathf.Max(1,costs[stars-1]):5;}
  void InitializeHeroProgression()
  {
   if(Data.heroProgress==null)Data.heroProgress=new List<HeroProgress>();
   for(int h=0;h<HeroRoster.Ids.Length;h++)if(IsOwned(h)&&HeroProgression(h)==null)Data.heroProgress.Add(new HeroProgress{id=HeroRoster.Ids[h]});
  }
  // A summon/reward supplies the resolved hero ID; this method has no probability or spending policy.
  public bool GrantHeroCopies(int hero,int amount=1)
  {
   if(hero<0||hero>=HeroRoster.Ids.Length||amount<=0)return false;
   var progress=HeroProgression(hero);long added=amount-(IsOwned(hero)?0:1);
   if(progress!=null&&(long)progress.duplicates+added>int.MaxValue)return false;
   if(!IsOwned(hero)){var owned=new List<string>(Data.ownedHeroes);owned.Add(HeroRoster.Ids[hero]);Data.ownedHeroes=owned.ToArray();}
   if(progress==null){progress=new HeroProgress{id=HeroRoster.Ids[hero]};Data.heroProgress.Add(progress);}
   progress.duplicates+=(int)added;
   while(progress.stars<MaxHeroStars&&progress.duplicates>=RequiredHeroCopies(progress.stars))
   {progress.duplicates-=RequiredHeroCopies(progress.stars);progress.stars++;}
   // Keep excess duplicates at five stars for a future exchange policy; never silently discard them.
   return true;
  }
 }
}
