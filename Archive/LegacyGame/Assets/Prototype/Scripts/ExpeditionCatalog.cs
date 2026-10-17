using UnityEngine;

namespace ProjectSS.Expedition
{
    [CreateAssetMenu(menuName = "ProjectSS/Expedition Catalog")]
    public sealed class ExpeditionCatalog : ScriptableObject
    {
        [Min(1)] public int weaponCapacity = 100, armorCapacity = 100, accessoryCapacity = 100;
        [Range(0,1)] public float dismantleRefundRate = .25f;
        public HeroDefinition[] heroes = { new HeroDefinition{id="rowen"},new HeroDefinition{id="rin"},new HeroDefinition{id="mira"} };
        public int[] heroStarCosts = {5,10,20,40};
        public GearDefinition[] gear;
        public MaterialDefinition[] materials = System.Array.Empty<MaterialDefinition>();
    }
}
