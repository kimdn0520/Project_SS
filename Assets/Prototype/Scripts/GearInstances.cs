using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ProjectSS.Expedition
{
    [Serializable] public sealed class GearRoll { public GearOptionStat stat; public GearOptionUnit unit; public float value; }
    [Serializable] public sealed class GearInstance
    {
        public string uid;
        public int definition;
        public bool locked;
        public GearRoll[] options = Array.Empty<GearRoll>();
    }
    public sealed partial class ExpeditionModel
    {
        public int GearCategory(int id) => Catalog.gear[id].equipSlot == 0 ? 0 : Catalog.gear[id].equipSlot == 3 ? 2 : 1;
        public int StorageLimit(int category) => Mathf.Max(1, category == 0 ? Catalog.weaponCapacity : category == 1 ? Catalog.armorCapacity : Catalog.accessoryCapacity);
        public int StorageUsed(int category) => Data.gearInstances.Count(g => GearCategory(g.definition) == category);
        public bool HasGearSpace(int id) => StorageUsed(GearCategory(id)) < StorageLimit(GearCategory(id));
        public GearInstance Instance(string uid) => string.IsNullOrEmpty(uid) ? null : Data.gearInstances.Find(g => g.uid == uid);
        public IEnumerable<GearInstance> Copies(int id) => Data.gearInstances.Where(g => g.definition == id);
        public string EquippedUid(int hero, int slot) => Data.equippedInstances[hero * 4 + slot];
        public bool IsInstanceEquipped(string uid) => !string.IsNullOrEmpty(uid) && Array.IndexOf(Data.equippedInstances, uid) >= 0;
        public bool IsBattleInstance(string uid) => Fighting && combatInstanceIds != null && Array.IndexOf(combatInstanceIds, uid) >= 0;
        string[] combatInstanceIds;
        void MigrateGearInstances()
        {
            if (Data.version >= 5) return;
            Data.gearInstances = new List<GearInstance>();
            for (int id = 0; id < Data.inventory.Length; id++)
                for (int n = 0; n < Data.inventory[id]; n++) Data.gearInstances.Add(NewInstance(id, false));
            Data.equippedInstances = new string[12];
            var used = new HashSet<string>();
            for (int slot = 0; slot < 12; slot++)
            {
                var instance = Data.gearInstances.Find(g => g.definition == Data.equipment[slot] && !used.Contains(g.uid));
                if (instance != null) { Data.equippedInstances[slot] = instance.uid; used.Add(instance.uid); }
            }
            // Preserve all legacy items even above capacity; only new acquisition is blocked.
            Data.version = 5;
        }
        GearInstance NewInstance(int id, bool roll)
        {
            var item = new GearInstance { uid = Guid.NewGuid().ToString("N"), definition = id };
            if (!roll) return item;
            var definition = Catalog.gear[id];
            var candidates = (definition.randomOptions ?? Array.Empty<GearRandomOption>()).Where(o => o != null && o.weight > 0).ToList();
            var rolls = new List<GearRoll>();
            for (int n = 0; n < definition.maxRandomOptions && candidates.Count > 0; n++)
            {
                double draw = random.NextDouble() * candidates.Sum(o => o.weight);
                int index = 0; while (index < candidates.Count - 1 && (draw -= candidates[index].weight) > 0) index++;
                var o = candidates[index]; candidates.RemoveAt(index);
                rolls.Add(new GearRoll { stat = o.stat, unit = o.unit, value = (float)Math.Round(Mathf.Lerp(o.min, o.max, (float)random.NextDouble()), 2) });
            }
            item.options = rolls.ToArray(); return item;
        }
        public GearInstance TryAddGear(int id)
        {
            if (id < 0 || id >= Catalog.gear.Length || !HasGearSpace(id)) return null;
            var instance = NewInstance(id, true); Data.gearInstances.Add(instance); Data.inventory[id]++; return instance;
        }
        public bool EquipInstance(string uid, int hero)
        {
            var item = Instance(uid);
            if (item == null || !IsOwned(hero) || IsInstanceEquipped(uid)) return false;
            var g = Catalog.gear[item.definition]; if (g.hero >= 0 && g.hero != hero) return false;
            int slot = hero * 4 + g.equipSlot;
            Data.equipment[slot] = item.definition; Data.equippedInstances[slot] = uid;
            if (!Fighting) ResetHealth(); return true;
        }
        public bool SetGearLocked(string uid, bool value)
        { var item = Instance(uid); if (item == null) return false; item.locked = value; return true; }
        public bool CanDismantle(string uid)
        { var item = Instance(uid); return item != null && !IsInstanceEquipped(uid) && !IsBattleInstance(uid); }
        public Vector3Int DismantleReward(int definition)
        {
            var g = Catalog.gear[definition]; float rate = Mathf.Clamp01(Catalog.dismantleRefundRate);
            return new Vector3Int(Mathf.Max(1, Mathf.FloorToInt(g.iron * rate)), Mathf.Max(0, Mathf.FloorToInt(g.crystal * rate)), Mathf.Max(0, Mathf.FloorToInt(g.relic * rate)));
        }
        public bool Dismantle(string uid)
        {
            if (!CanDismantle(uid)) return false;
            var item = Instance(uid); var reward = DismantleReward(item.definition);
            if (Data.iron > int.MaxValue-reward.x || Data.crystal > int.MaxValue-reward.y || Data.relic > int.MaxValue-reward.z) return false;
            Data.gearInstances.Remove(item); Data.inventory[item.definition]--;
            Data.iron += reward.x; Data.crystal += reward.y; Data.relic += reward.z; return true;
        }
        public GearDefinition EffectiveGear(GearInstance instance)
        {
            if (instance == null) return null;
            var g = JsonUtility.FromJson<GearDefinition>(JsonUtility.ToJson(Catalog.gear[instance.definition]));
            g.damage = OptionValue(instance, GearOptionStat.Attack, g.damage);
            g.health = OptionValue(instance, GearOptionStat.Health, g.health);
            float speed = OptionValue(instance, GearOptionStat.AttackSpeed, g.AttacksPerSecond);
            if (speed > 0) g.interval = 1 / speed;
            g.criticalChance = OptionValue(instance, GearOptionStat.CriticalChance, g.criticalChance);
            g.criticalDamage = OptionValue(instance, GearOptionStat.CriticalDamage, g.criticalDamage);
            g.defense = OptionValue(instance, GearOptionStat.Defense, g.defense);
            return g;
        }
        public float OptionValue(GearInstance instance, GearOptionStat stat, float value)
        {
            float flat = 0, percent = 0;
            foreach (var o in instance?.options ?? Array.Empty<GearRoll>())
                if (o.stat == stat) { if (o.unit == GearOptionUnit.Percent) percent += o.value; else flat += o.value; }
            return Mathf.Max(0, value * (1 + percent / 100) + flat);
        }
        public float EquippedStat(int hero, int slot, GearOptionStat stat, float value) => OptionValue(Instance(EquippedUid(hero, slot)), stat, value);
        public float HeroAttackInterval(int hero)
        { var g = Catalog.gear[Equipped(hero, 0)]; return 1 / Mathf.Max(.01f, EquippedStat(hero, 0, GearOptionStat.AttackSpeed, g.AttacksPerSecond)); }
        public bool PrepareChestReward()
        {
            if (Data.pendingChestGear < 0)
            {
                var eligible = new List<int>();
                for (int i = 3; i < Catalog.gear.Length; i++) if (Catalog.gear[i].unlock <= Data.cleared) eligible.Add(i);
                if (eligible.Count == 0) { LastLoot = "획득 가능한 장비가 없습니다."; return false; }
                Data.pendingChestGear = eligible[random.Next(eligible.Count)];
            }
            if (HasGearSpace(Data.pendingChestGear)) return true;
            LastLoot = ExpeditionInventory.CategoryNames[GearCategory(Data.pendingChestGear)] + " 보관함이 가득 찼습니다. 장비를 분해한 뒤 상자를 열어주세요.";
            Data.autoMine = false; return false;
        }
    }
}
