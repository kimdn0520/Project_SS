using System;
using UnityEngine;

namespace ProjectSS.Expedition
{
    public enum MaterialStorage { Inventory, Iron, Crystal, Relic }

    [Serializable] public sealed class MaterialDefinition
    {
        public string id, title;
        [TextArea] public string description;
        public Sprite icon;
        public GearRarity rarity;
        public MaterialStorage storage;
    }

    [Serializable] public sealed class MaterialStack
    {
        public string id;
        public int count;
    }

    public sealed partial class ExpeditionModel
    {
        public int MaterialCount(int index)
        {
            if (index < 0 || index >= Catalog.materials.Length) return 0;
            var item = Catalog.materials[index];
            switch (item.storage)
            {
                case MaterialStorage.Iron: return Data.iron;
                case MaterialStorage.Crystal: return Data.crystal;
                case MaterialStorage.Relic: return Data.relic;
            }
            foreach (var stack in Data.materials ?? Array.Empty<MaterialStack>())
                if (stack != null && stack.id == item.id) return stack.count;
            return 0;
        }

        // Custom material rewards can use this without changing the legacy currency save fields.
        public bool AddMaterial(int index, int amount)
        {
            if (index < 0 || index >= Catalog.materials.Length || amount <= 0) return false;
            var item = Catalog.materials[index];
            if (MaterialCount(index) > int.MaxValue - amount) return false;
            switch (item.storage)
            {
                case MaterialStorage.Iron: Data.iron += amount; return true;
                case MaterialStorage.Crystal: Data.crystal += amount; return true;
                case MaterialStorage.Relic: Data.relic += amount; return true;
            }
            if (string.IsNullOrWhiteSpace(item.id)) return false;
            if (Data.materials == null) Data.materials = Array.Empty<MaterialStack>();
            foreach (var stack in Data.materials)
                if (stack != null && stack.id == item.id) { stack.count += amount; return true; }
            Array.Resize(ref Data.materials, Data.materials.Length + 1);
            Data.materials[Data.materials.Length - 1] = new MaterialStack { id = item.id, count = amount };
            return true;
        }
    }
}
