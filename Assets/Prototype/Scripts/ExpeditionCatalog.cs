using UnityEngine;

namespace ProjectSS.Expedition
{
    [CreateAssetMenu(menuName = "ProjectSS/Expedition Catalog")]
    public sealed class ExpeditionCatalog : ScriptableObject
    {
        public GearDefinition[] gear;
    }
}
