using System;
using UnityEngine;

namespace RuneBreak
{
    [Serializable] public class RuneBlock
    {
        public string id, dir, color;
        public int x, y, depth;
        // A chain is released after this zero-based seal layer is broken.
        public string chainSide;
        public int chainLayer;
    }
    [Serializable] public class RuneMirror { public int x, y; public bool slash = true; }
    [Serializable] public class RuneLayer { public string color; public int hp; }
    [Serializable] public class RuneSeals { public RuneLayer[] N, E; }
    [Serializable] public class RuneLevel
    {
        public string id, name, tag, desc, tip;
        public int size = 6, capacity = 5;
        public RuneSeals seals;
        public RuneBlock[] blocks;
        public RuneMirror[] mirrors = Array.Empty<RuneMirror>();
        public string mechanic;
        public bool demo;
        public RuneLayer[] Layers(int side) => side == 0 ? seals.N : seals.E;
    }
    [CreateAssetMenu(menuName = "Rune Break/Level Catalog")]
    public sealed class RuneLevelCatalog : ScriptableObject
    {
        public RuneLevel[] levels;
        public int CampaignCount { get { int n=0;foreach(var l in levels)if(!l.demo)n++;return n; } }
    }
    [Serializable] public class RuneLevelJson { public RuneLevel[] levels; }
}
