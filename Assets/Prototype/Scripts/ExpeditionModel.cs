using System;
using UnityEngine;
namespace ProjectSS.Expedition
{
    public enum GearEffect { Normal, BreakArmor, Frost, Burn, Leech, Armor }
    [Serializable] public sealed class GearDefinition
    {
        public string title, description, spriteKey, slot;
        public int hero, iron, crystal, relic, unlock, equipSlot;
        public float damage, interval, health;
        public GearEffect effect;
        public Sprite icon;
        public WeaponItem prefab;
        // Authoring metadata is additive: existing catalog indices and combat fields stay valid.
        public string id;
        public GearRarity rarity;
        public int maxRandomOptions = 1;
        public GearRandomOption[] randomOptions = Array.Empty<GearRandomOption>();
        public GearSkillDefinition skill = new GearSkillDefinition();
        public SkillDefinition skillAsset;
        // Retain inline data for existing catalogs until explicitly migrated in the editor.
        public GearSkillDefinition ResolvedSkill => rarity < GearRarity.Legendary ? null :
            skillAsset != null ? skillAsset.settings : skill != null && skill.enabled ? skill : null;
    }
    [Serializable] public sealed class ExpeditionSave
    {
        public int version = 4, iron, crystal, relic, cleared, depth, excavations, route, chestPity;
        public string[] ownedHeroes, formation;
        public int moleSupportDay, moleSupportUsed;
        public bool autoMine, autoBattle = true, chest;
        public int[] inventory;
        public int[] equipment = { 0,-1,-1,-1, 1,-1,-1,-1, 2,-1,-1,-1 };
        public static ExpeditionSave Fresh(int count = 11)
        {
            var d = new ExpeditionSave { inventory = new int[count] };
            d.inventory[0] = d.inventory[1] = d.inventory[2] = 1; HeroRoster.Migrate(d);return d;
        }
        public bool IsValid(int count)
        {
            if ((version != 3&&version!=4) || inventory == null || inventory.Length != count || equipment == null || equipment.Length != 12) return false;
            if (iron < 0 || crystal < 0 || relic < 0 || cleared < 0 || cleared > 9999 || depth < 0 || excavations < 0 || route < 0 || route > 2 || chestPity < 0) return false;
            foreach (int n in inventory) if (n < 0) return false;
            foreach (int id in equipment) if (id < -1 || id >= count || (id >= 0 && inventory[id] == 0)) return false;
            return true;
        }
        // The authoring tool appends gear without moving existing indices. Preserve old saves
        // when new catalog entries are added; shrinking or invalid saves still fail validation.
        public bool TryExpandInventory(int count)
        {
            if (inventory == null || inventory.Length < 3 || inventory.Length > count || !IsValid(inventory.Length)) return false;
            if (inventory.Length < count) Array.Resize(ref inventory, count);
            return true;
        }
    }
    public sealed partial class ExpeditionModel
    {
        public const double ChestChance = 0.04;
        public const int ChestMinimumVeins = 8;
        public const int ChestGuaranteedVeins = 40;
        public readonly ExpeditionCatalog Catalog;
        public ExpeditionSave Data { get; }
        readonly System.Random random;
        readonly float[] hp = new float[3], timers = new float[3];
        public int Region => Data.cleared / 10 + 1;
        public int Wave => Data.cleared % 10 + 1;
        public bool IsBoss => Wave == 10;
        public int EnemyKind => IsBoss ? 2 : (Data.cleared % 3 == 2 ? 1 : 0);
        public int BattleStage { get; private set; }
        public bool Fighting { get; private set; }
        public float EnemyHp { get; private set; }
        public float EnemyMaxHp { get; private set; }
        public float EnemyTimer { get; private set; }
        public int LastTargetHero { get; private set; }
        public bool LastVictory { get; private set; }
        public bool LastFirstClear { get; private set; }
        public int BlockHp { get; private set; }
        public const int VeinDurability = 16;
        public int BlockMaxHp => Data.chest ? 3 : VeinDurability;
        public int MiningPower => 1 + Mathf.Min(2, Data.cleared / 10);
        public int LastGear { get; private set; } = -1;
        public int LastAmount { get; private set; }
        public string LastLoot { get; private set; } = "꾹 눌러 광맥을 부숴보세요";
        public float TeamHp => hp[0] + hp[1] + hp[2];
        public float HeroHp(int i) => hp[i];
        public int Equipped(int hero, int slot) => Data.equipment[hero * 4 + slot];
        public float HeroMaxHp(int i)
        {
            float total = i == 0 ? 160 : i == 1 ? 90 : 75;
            for (int slot = 1; slot < 4; slot++) { int id = Equipped(i, slot); if (id >= 0) total += Catalog.gear[id].health; }
            return total;
        }
        public float HeroDamage(int i)
        {
            float n = Catalog.gear[Equipped(i, 0)].damage;
            for (int s = 1; s < 4; s++) { int id = Equipped(i, s); if (id >= 0) n += Catalog.gear[id].damage; }
            return n;
        }
        public event Action<int,float,GearEffect> HeroHit;
        public event Action<float,bool> EnemyHit;
        public event Action<bool> BattleEnded, Mined;
        public ExpeditionModel(ExpeditionCatalog catalog, ExpeditionSave data, int seed = -1)
        {
            Catalog = catalog; Data = data;HeroRoster.Migrate(Data); random = seed < 0 ? new System.Random() : new System.Random(seed);
            BlockHp = BlockMaxHp; ResetHealth();
        }
        public bool SelectRoute(int route)
        {
            if (route < 0 || route > 2 || (route == 2 && Data.cleared < 10)) return false;
            Data.route = route; return true;
        }
        public void ClaimChest()
        {
            if (Data.chest) Dig(BlockHp);
        }
        public void Dig(int bonusDamage = 0)
        {
            BlockHp -= MiningPower + Math.Max(0, bonusDamage);
            bool broken = BlockHp <= 0; LastGear = -1;
            if (broken)
            {
                Data.excavations++; Data.depth += 2;
                if (Data.chest)
                {
                    int eligible = 0;
                    for (int i = 3; i < Catalog.gear.Length; i++) if (Catalog.gear[i].unlock <= Data.cleared) eligible++;
                    int roll = random.Next(eligible);
                    for (int i = 3; i < Catalog.gear.Length; i++)
                        if (Catalog.gear[i].unlock <= Data.cleared && roll-- == 0) { LastGear = i; break; }
                    Data.inventory[LastGear]++; LastAmount = 1;
                    LastLoot = Catalog.gear[LastGear].title + " 발견!"; Data.chestPity = 0;
                }
                else
                {
                    LastAmount = Data.route == 2 ? 1 : 3 + Data.cleared / 10;
                    if (Data.route == 0) Data.iron += LastAmount;
                    else if (Data.route == 1) Data.crystal += LastAmount;
                    else Data.relic += LastAmount;
                    LastLoot = new[] { "철광석", "서리 결정", "유적 파편" }[Data.route] + " +" + LastAmount;
                    Data.chestPity++;
                }
                Data.chest = Data.excavations == 3 || Data.chestPity >= ChestGuaranteedVeins ||
                    (Data.chestPity >= ChestMinimumVeins && random.NextDouble() < ChestChance);
                BlockHp = BlockMaxHp;
            }
            Mined?.Invoke(broken);
        }
        public int Available(int id)
        {
            int n = Data.inventory[id]; foreach (int used in Data.equipment) if (used == id) n--; return n;
        }
        public bool CanCraft(int id)
        {
            if (id < 3 || id >= Catalog.gear.Length) return false;
            var g = Catalog.gear[id]; return Data.cleared >= g.unlock && Data.iron >= g.iron && Data.crystal >= g.crystal && Data.relic >= g.relic;
        }
        public bool Craft(int id)
        {
            if (!CanCraft(id)) return false; var g = Catalog.gear[id];
            Data.iron -= g.iron; Data.crystal -= g.crystal; Data.relic -= g.relic; Data.inventory[id]++; return true;
        }
        public bool Equip(int id, int hero)
        {
            if (id < 0 || id >= Catalog.gear.Length || !IsOwned(hero) || Available(id) <= 0) return false;
            var g = Catalog.gear[id]; if (g.hero >= 0 && g.hero != hero) return false;
            Data.equipment[hero * 4 + g.equipSlot] = id;
            if (!Fighting) ResetHealth();
            return true;
        }
        public bool Unequip(int hero, int slot)
        {
            if (hero < 0 || hero > 2 || slot <= 0 || slot > 3) return false;
            Data.equipment[hero * 4 + slot] = -1;
            if (!Fighting) ResetHealth();
            return true;
        }
    }
}
