using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuneBreak
{
    [Serializable] public class RuneGateSprite { public string name, side; public int level, layer, mask; public Sprite sprite; }
    [Serializable] public class RuneGateJson { public RuneGateSprite[] gates; }
    [CreateAssetMenu(menuName="Rune Break/Art Library")]
    public sealed class RuneArtLibrary : ScriptableObject
    {
        public Sprite[] cabinets, runes, storedRunes,waitSlots,fullWaitSlots;
        public Sprite white, slot, fullSlot, locked, sparkle;
        public Sprite chainWrap,mirrorSlash,mirrorBackslash,stackMount;
        public bool punchStyle;
        public bool surfaceStyle;
        public Sprite[] longGates;
        public Sprite flightShadow;
        private static readonly Color[] punchMid={Hex("ff2446"),Hex("00aafa"),Hex("ffc900"),Hex("46d50a")};
        private static readonly Color[] surfaceMid={Hex("f53552"),Hex("03a7f5"),Hex("ffca0d"),Hex("58cf17")};
        public Color MidTone(string c)=>surfaceStyle?surfaceMid[ColorIndex(c)]:punchStyle?punchMid[ColorIndex(c)]:Mid(c);
        public Color LightTone(string c)=>punchStyle?Color.Lerp(MidTone(c),Color.white,surfaceStyle?.66f:.65f):Light(c);
        public Color DarkTone(string c)=>punchStyle?Color.Lerp(MidTone(c),Hex(surfaceStyle?"070d28":"070c21"),surfaceStyle?.42f:.55f):Dark(c);
        public RuneGateSprite[] gates;
        private Dictionary<string,Sprite> gateCache;
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
    }
}
