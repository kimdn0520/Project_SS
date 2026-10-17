using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuneBreak
{
    [Serializable] public class RuneGlyph { public string key; public string[] rows; }
    [Serializable] public class RuneGlyphJson { public RuneGlyph[] glyphs; }
    [Serializable] public class RuneGateSprite { public string name, side; public int level, layer, mask; public Sprite sprite; }
    [Serializable] public class RuneGateJson { public RuneGateSprite[] gates; }
    [CreateAssetMenu(menuName="Rune Break/Art Library")]
    public sealed class RuneArtLibrary : ScriptableObject
    {
        public Sprite[] cabinets, runes, storedRunes,waitSlots,fullWaitSlots;
        public Sprite white, slot, fullSlot, locked, sparkle;
        public RuneGateSprite[] gates;
        public RuneGlyph[] glyphs;
        private Dictionary<string,Sprite> gateCache;
        private Dictionary<char,string[]> glyphCache;
        public Sprite Rune(string color,string dir="gem") => runes[ColorIndex(color)*5+DirectionIndex(dir)];
        public static int ColorIndex(string c) => c=="red"?0:c=="blue"?1:c=="gold"?2:3;
        public static int DirectionIndex(string d) => d=="N"?0:d=="E"?1:d=="S"?2:d=="W"?3:4;
        private static readonly Color[] light={Hex("ffc49e"),Hex("c0e4e5"),Hex("fff1af"),Hex("d4e7ac")};
        private static readonly Color[] mid={Hex("d86d75"),Hex("669fb4"),Hex("e2b860"),Hex("83ae83")};
        private static readonly Color[] dark={Hex("883f50"),Hex("355875"),Hex("96723d"),Hex("436b57")};
        public static Color Light(string c) => light[ColorIndex(c)];
        public static Color Mid(string c) => mid[ColorIndex(c)];
        public static Color Dark(string c) => dark[ColorIndex(c)];
        public static Color Hex(string s) { ColorUtility.TryParseHtmlString("#"+s,out var c);return c; }
        public Sprite Gate(int level,int side,int layer,int mask)
        {
            if(gateCache==null){gateCache=new Dictionary<string,Sprite>();foreach(var g in gates)gateCache[$"{g.level}_{g.side}_{g.layer}_{g.mask}"]=g.sprite;}
            gateCache.TryGetValue($"{level}_{(side==0?"N":"E")}_{layer}_{mask}",out var sprite);return sprite;
        }
        public string[] Glyph(char ch)
        {
            if(glyphCache==null){glyphCache=new Dictionary<char,string[]>();foreach(var g in glyphs)glyphCache[g.key[0]]=g.rows;}
            return glyphCache.TryGetValue(char.ToUpperInvariant(ch),out var rows)?rows:glyphCache['?'];
        }
    }
}
