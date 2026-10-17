using System;
using UnityEngine;

namespace ProjectSS.Expedition
{
    public enum GearRarity { Common, Rare, Epic, Legendary, Mythic }
    public enum GearOptionStat { Attack, Health, AttackSpeed, CriticalChance, CriticalDamage, Defense, LifeSteal }
    public enum GearOptionUnit { Flat, Percent }
    public enum GearSkillEffect { None, Freeze, Burn, Damage, Heal, Shield }
    public enum GearSkillTrigger { Cooldown, OnAttack, OnCriticalHit }
    public enum GearSkillTarget { SingleEnemy, EnemyArea, Self, Allies }

    [Serializable]
    public sealed class GearRandomOption
    {
        public GearOptionStat stat;
        public GearOptionUnit unit;
        public float min = 1, max = 5;
        public float weight = 1;
    }

    /// <summary>Authored skill specification. Execution is supplied by the combat system.</summary>
    [Serializable]
    public sealed class GearSkillDefinition
    {
        public bool enabled;
        public string id, title;
        [TextArea] public string description;
        public GearSkillEffect effect;
        public GearSkillTrigger trigger;
        public GearSkillTarget target = GearSkillTarget.EnemyArea;
        public float cooldown = 10, chance = 100, duration = 2, radius = 3, power;
        public GameObject visualPrefab;
    }
}
