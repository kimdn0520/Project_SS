using System;
using UnityEngine;

namespace RuneBreak
{
    [Serializable] public class RuneBlock { public string id, dir, color; public int x, y; }
    [Serializable] public class RuneLayer { public string color; public int hp; }
    [Serializable] public class RuneSeals { public RuneLayer[] N, E; }
    [Serializable] public class RuneLevel
    {
        public string id, name, tag, desc, tip;
        public int size = 6, capacity = 5;
        public RuneSeals seals;
        public RuneBlock[] blocks;
        public RuneLayer[] Layers(int side) => side == 0 ? seals.N : seals.E;
    }
    [CreateAssetMenu(menuName = "Rune Break/Level Catalog")]
    public sealed class RuneLevelCatalog : ScriptableObject { public RuneLevel[] levels; }
    [Serializable] public class RuneLevelJson { public RuneLevel[] levels; }
}
