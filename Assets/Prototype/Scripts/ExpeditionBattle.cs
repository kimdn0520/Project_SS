using System;
using System.Collections.Generic;
using UnityEngine;
namespace ProjectSS.Expedition
{
    public sealed partial class ExpeditionModel
    {
        sealed class CombatHero
        {
            public float damage, maxHp, interval;
            public readonly List<CombatSkill> skills=new List<CombatSkill>();
        }
        sealed class CombatSkill
        {
            public GearSkillDefinition definition;
            public float remaining;
        }
        readonly CombatHero[] combatHeroes=new CombatHero[3];
        readonly int[] combatSlots={-1,-1,-1};
        int[] combatEquipment;
        int combatCleared;
        float frozen;
        public int BattleGeneration {get;private set;}
        public float EnemyFrozenFor=>frozen;
        public int BattleRegion=>combatCleared/10+1;
        public int BattleWave=>combatCleared%10+1;
        public bool HasSnapshot=>combatEquipment!=null;
        public event Action<int,GearSkillDefinition> SkillCast;
        public int ConfiguredHero(int slot)=>slot>=0&&slot<3?HeroRoster.Index(Data.formation[slot]):-1;
        public int BattleHero(int slot)=>combatSlots[slot];
        public bool IsOwned(int hero)=>hero>=0&&hero<3&&Array.IndexOf(Data.ownedHeroes,HeroRoster.Ids[hero])>=0;
        public int FormationSlot(int hero)=>hero>=0&&hero<3?Array.IndexOf(Data.formation,HeroRoster.Ids[hero]):-1;
        public bool IsDeployed(int hero)=>Array.IndexOf(combatSlots,hero)>=0;
        public int BattleEquipped(int hero,int slot)=>combatEquipment==null?Equipped(hero,slot):combatEquipment[hero*4+slot];
        public float BattleMaxHp(int hero)=>combatHeroes[hero]?.maxHp??HeroMaxHp(hero);
        public float BattleDamage(int hero)=>combatHeroes[hero]?.damage??HeroDamage(hero);
        public float BattleInterval(int hero)=>combatHeroes[hero]?.interval??Mathf.Max(.1f,HeroAttackInterval(hero));
        public float AttackRemaining(int hero)=>timers[hero];
        public bool PendingBattleChanges
        {
            get
            {
                if(!Fighting||!HasSnapshot)return false;
                for(int s=0;s<3;s++)if(combatSlots[s]!=ConfiguredHero(s))return true;
                for(int i=0;i<12;i++)if(combatEquipment[i]!=Data.equipment[i]||combatInstanceIds[i]!=Data.equippedInstances[i])return true;
                return false;
            }
        }
        public bool AssignHero(int slot,int hero)
        {
            if(slot<0||slot>2||!IsOwned(hero))return false;
            int previous=FormationSlot(hero);
            if(previous==slot)return false;
            string displaced=Data.formation[slot];
            Data.formation[slot]=HeroRoster.Ids[hero];
            if(previous>=0)Data.formation[previous]=displaced;
            if(!Fighting)ResetHealth();return true;
        }
        public bool RemoveHero(int slot)
        {
            if(slot<0||slot>2||ConfiguredHero(slot)<0)return false;
            int count=0;for(int i=0;i<3;i++)if(ConfiguredHero(i)>=0)count++;
            if(count<=1)return false;
            Data.formation[slot]=null;if(!Fighting)ResetHealth();return true;
        }
        public void ResetHealth()
        {
            for(int hero=0;hero<3;hero++)hp[hero]=FormationSlot(hero)>=0?HeroMaxHp(hero):0;
        }
        public void CancelBattle()
        {
            Fighting=false;frozen=0;combatEquipment=null;BattleGeneration++;
            foreach(var hero in combatHeroes)if(hero!=null)hero.skills.Clear();
        }
        public bool StartBattle()
        {
            if(Fighting)return false;
            HeroRoster.Migrate(Data);
            combatCleared=Data.cleared;BattleStage=EnemyKind;
            combatEquipment=(int[])Data.equipment.Clone();combatInstanceIds=(string[])Data.equippedInstances.Clone();frozen=0;BattleGeneration++;
            for(int slot=0;slot<3;slot++)combatSlots[slot]=ConfiguredHero(slot);
            for(int hero=0;hero<3;hero++)
            {
                var snapshot=new CombatHero{damage=HeroDamage(hero),maxHp=HeroMaxHp(hero),interval=Mathf.Max(.1f,HeroAttackInterval(hero))};
                combatHeroes[hero]=snapshot;hp[hero]=IsDeployed(hero)?snapshot.maxHp:0;timers[hero]=snapshot.interval;
                for(int slot=0;slot<4;slot++)
                {
                    int gear=Equipped(hero,slot);if(gear<0)continue;
                    var skill=Catalog.gear[gear].ResolvedSkill;
                    if(!CanExecute(skill))continue;
                    var copy=JsonUtility.FromJson<GearSkillDefinition>(JsonUtility.ToJson(skill));
                    snapshot.skills.Add(new CombatSkill{definition=copy,remaining=copy.cooldown});
                }
            }
            EnemyMaxHp=(IsBoss?610:80+Wave*25)*(1+(Region-1)*.65f);
            EnemyHp=EnemyMaxHp;EnemyTimer=1.8f;LastVictory=LastFirstClear=false;
            Fighting=true;return true;
        }
        // Only Freeze has a complete authored runtime definition in the current project.
        // Power units, burn tick cadence and critical-hit triggers are not defined yet.
        public static bool CanExecute(GearSkillDefinition s)=>s!=null&&s.effect==GearSkillEffect.Freeze
            &&(s.trigger==GearSkillTrigger.Cooldown||s.trigger==GearSkillTrigger.OnAttack)
            &&(s.target==GearSkillTarget.SingleEnemy||s.target==GearSkillTarget.EnemyArea)
            &&s.cooldown>0&&s.duration>0&&s.chance>0
            &&(s.target!=GearSkillTarget.EnemyArea||s.radius>0);
        void TrySkill(int hero,CombatSkill skill,bool attack)
        {
            var s=skill.definition;
            if(!Fighting||hp[hero]<=0||EnemyHp<=0||skill.remaining>0||attack!=(s.trigger==GearSkillTrigger.OnAttack))return;
            // Each encounter currently has one enemy. Area effects are centered on that enemy.
            skill.remaining=s.cooldown;
            if(random.NextDouble()*100>=s.chance)return;
            frozen=Mathf.Max(frozen,s.duration);SkillCast?.Invoke(hero,s);
        }
        public int FrontAliveHero()
        {
            foreach(int hero in combatSlots)if(hero>=0&&hp[hero]>0)return hero;
            return -1;
        }
        public void Retreat(){if(Fighting)Finish(false);}
        public void Tick(float dt)
        {
            if(!Fighting||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            while(dt>0&&Fighting){float step=Mathf.Min(dt,.05f);StepBattle(step);dt-=step;}
        }
        void StepBattle(float dt)
        {
            for(int slot=0;slot<3&&Fighting;slot++)
            {
                int hero=combatSlots[slot];if(hero<0||hp[hero]<=0)continue;
                var actor=combatHeroes[hero];
                foreach(var skill in actor.skills){skill.remaining=Mathf.Max(0,skill.remaining-dt);TrySkill(hero,skill,false);}
                timers[hero]-=dt;
                if(timers[hero]<=0)
                {
                    timers[hero]+=actor.interval;EnemyHp=Mathf.Max(0,EnemyHp-actor.damage);
                    HeroHit?.Invoke(hero,actor.damage,GearEffect.Normal);
                    if(EnemyHp<=0){Finish(true);return;}
                    foreach(var skill in actor.skills)TrySkill(hero,skill,true);
                }
            }
            float frozenTime=Mathf.Min(frozen,dt);frozen-=frozenTime;EnemyTimer-=dt-frozenTime;
            if(EnemyTimer<=0)
            {
                EnemyTimer+=BattleWave==10?1.65f:2.1f;LastTargetHero=FrontAliveHero();
                if(LastTargetHero<0){Finish(false);return;}
                float damage=(BattleWave==10?31:10+BattleWave*1.6f)*(1+(BattleRegion-1)*.45f);
                hp[LastTargetHero]=Mathf.Max(0,hp[LastTargetHero]-damage);EnemyHit?.Invoke(damage,false);
                if(FrontAliveHero()<0)Finish(false);
            }
        }
        void Finish(bool win)
        {
            if(!Fighting)return;
            Fighting=false;frozen=0;LastVictory=LastFirstClear=win;
            if(win)
            {
                bool boss=BattleWave==10;
                Data.iron+=boss?20:3;Data.crystal+=boss?12:1;if(boss)Data.relic+=3;
                Data.cleared=combatCleared+1;
            }
            BattleEnded?.Invoke(win);
        }
    }
}
