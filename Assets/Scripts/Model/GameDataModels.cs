using System;
using System.Collections.Generic;
using UnityEngine;

public enum MiningNodeType
{
    Soil,   // 흙층 (삽 사용)
    Rock,   // 암석층 (곡괭이 사용)
    Ore     // 희귀 광석 (곡괭이 사용)
}

public enum MiningToolType
{
    Shovel,  // 삽
    Pickaxe  // 곡괭이
}

[Serializable]
public class MiningNodeData
{
    public string nodeId;
    public MiningNodeType nodeType;
    public MiningToolType requiredTool;
    public int curHp;
    public int maxHp;
    public int visualStage; // 0: 온전함, 1: 1차 균열, 2: 2차 대균열
    public long rewardGold;
    public int rewardGems;
    public string dropLoot;
    public bool isCompleted;

    public MiningNodeData(string id, MiningNodeType type, int hp, long gold, int gems = 0, string loot = null)
    {
        nodeId = id;
        nodeType = type;
        requiredTool = (type == MiningNodeType.Soil) ? MiningToolType.Shovel : MiningToolType.Pickaxe;
        maxHp = hp;
        curHp = hp;
        visualStage = 0;
        rewardGold = gold;
        rewardGems = gems;
        dropLoot = loot;
        isCompleted = false;
    }

    public void TakeDamage(int damage)
    {
        if (isCompleted) return;

        curHp = Mathf.Max(0, curHp - damage);

        // 균열 시각 단계 갱신 (암석/광석일 경우)
        if (nodeType != MiningNodeType.Soil)
        {
            float ratio = (float)curHp / Mathf.Max(1, maxHp);
            if (ratio <= 0.35f) visualStage = 2;
            else if (ratio <= 0.70f) visualStage = 1;
            else visualStage = 0;
        }

        if (curHp <= 0)
        {
            isCompleted = true;
        }
    }
}

/// <summary>
/// 재화 및 획득 전리품(장비, 상자)을 소유하는 런타임 인벤토리 모델.
/// UI 텍스트에서 값을 읽지 않으며, 모든 변경 사항을 Action 이벤트로 통지합니다.
/// </summary>
public class PlayerInventoryModel : SingletonMonoBehaviour<PlayerInventoryModel>
{
    [SerializeField] private long gold = 0;
    [SerializeField] private int gems = 10;
    [SerializeField] private List<string> equipments = new List<string>();
    [SerializeField] private List<string> chests = new List<string>();

    public long Gold => gold;
    public int Gems => gems;
    public IReadOnlyList<string> Equipments => equipments;
    public IReadOnlyList<string> Chests => chests;

    public event Action<long> OnGoldChanged;
    public event Action<int> OnGemsChanged;
    public event Action<string> OnEquipmentAdded;
    public event Action<string> OnChestAdded;

    public void AddGold(long amount)
    {
        if (amount <= 0) return;
        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }

    public bool SpendGold(long amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        return true;
    }

    public void AddGems(int amount)
    {
        if (amount <= 0) return;
        gems += amount;
        OnGemsChanged?.Invoke(gems);
    }

    public bool SpendGems(int amount)
    {
        if (gems < amount) return false;
        gems -= amount;
        OnGemsChanged?.Invoke(gems);
        return true;
    }

    public void AddEquipment(string eqName)
    {
        if (string.IsNullOrEmpty(eqName)) return;
        equipments.Add(eqName);
        OnEquipmentAdded?.Invoke(eqName);
    }

    public void AddChest(string chestName)
    {
        if (string.IsNullOrEmpty(chestName)) return;
        chests.Add(chestName);
        OnChestAdded?.Invoke(chestName);
    }
}

/// <summary>
/// 현역 용사 및 은퇴용사의 능력치 런타임 모델
/// </summary>
public class HeroStatsModel : SingletonMonoBehaviour<HeroStatsModel>
{
    [Header("[Active Hero (Battle)]")]
    [SerializeField] private int activeHeroHp = 100;
    [SerializeField] private int activeHeroMaxHp = 100;
    [SerializeField] private int activeHeroAtk = 25;
    [SerializeField] private float activeHeroAtkInterval = 1.0f;
    [SerializeField] private string equippedWeapon = "Iron Sword";

    [Header("[Retired Hero (Mining)]")]
    [SerializeField] private int pickaxePower = 15;
    [SerializeField] private int shovelPower = 20;

    public int ActiveHeroHp => activeHeroHp;
    public int ActiveHeroMaxHp => activeHeroMaxHp;
    public int ActiveHeroAtk => activeHeroAtk;
    public float ActiveHeroAtkInterval => activeHeroAtkInterval;
    public string EquippedWeapon => equippedWeapon;

    public int PickaxePower => pickaxePower;
    public int ShovelPower => shovelPower;

    public event Action<int, int> OnActiveHeroHpChanged;
    public event Action<int> OnActiveHeroAtkChanged;
    public event Action<int> OnPickaxePowerChanged;

    public void UpgradeHeroAtk(int delta)
    {
        activeHeroAtk += delta;
        OnActiveHeroAtkChanged?.Invoke(activeHeroAtk);
    }

    public void UpgradePickaxePower(int delta)
    {
        pickaxePower += delta;
        shovelPower += delta;
        OnPickaxePowerChanged?.Invoke(pickaxePower);
    }

    public void SetHeroHp(int cur, int max)
    {
        activeHeroHp = cur;
        activeHeroMaxHp = max;
        OnActiveHeroHpChanged?.Invoke(activeHeroHp, activeHeroMaxHp);
    }

    public void EquipWeapon(string weaponName, int atkBonus)
    {
        equippedWeapon = weaponName;
        activeHeroAtk += atkBonus;
        OnActiveHeroAtkChanged?.Invoke(activeHeroAtk);
    }
}
