using System;
using System.Collections.Generic;

namespace RuneBreak
{
    public enum RunePhase { Playing, Won, Lost }
    public enum RuneMoveKind { Board, Slot }
    public struct RuneMove { public RuneMoveKind kind; public int id; public RuneMove(RuneMoveKind k, int i) { kind = k; id = i; } }
    public sealed class RuneState
    {
        public uint board;
        public int[] slots = { -1, -1, -1, -1, -1 };
        public int[] spent = new int[2];
        public RunePhase phase;
        public int taps, direct, stored, relaunch, peak;
        public RuneState Copy() => new RuneState { board=board, slots=(int[])slots.Clone(), spent=(int[])spent.Clone(), phase=phase, taps=taps, direct=direct, stored=stored, relaunch=relaunch, peak=peak };
        public int Filled { get { int n=0; foreach(int id in slots) if(id>=0)n++; return n; } }
    }
    public struct RuneGate { public string color; public int hp, max, layer, damage; }
    [Serializable] public struct RuneCell { public int x,y; public RuneCell(int x,int y){this.x=x;this.y=y;} }
    public sealed class RuneRoute
    {
        public RuneCell[] turns=Array.Empty<RuneCell>();
        public int exitX,exitY;
        public string exitDir;
        public bool loop;
    }
    public struct RuneShot
    {
        public int id, side, slot, layer, damageIndex, max, spent;
        public bool store, overflow, relaunch, layerBreak, sealBreak;
        public uint unlocked, revealed;
    }
    // Pure deterministic rules. Damage and storage are reserved at input time, never at arrival.
    public sealed class RuneRules
    {
        public readonly RuneLevel Level;
        public readonly uint[] Blockers;
        public readonly uint[] Coverers;
        public readonly RuneRoute[] Routes;
        public readonly int[] ChainSides, ChainThresholds;
        public RuneRules(RuneLevel level)
        {
            Level=level; Blockers=new uint[level.blocks.Length];Coverers=new uint[level.blocks.Length];
            Routes=new RuneRoute[level.blocks.Length];ChainSides=new int[level.blocks.Length];ChainThresholds=new int[level.blocks.Length];
            for(int i=0;i<level.blocks.Length;i++)
            {
                var b=level.blocks[i]; Direction(b.dir,out int dx,out int dy);
                ChainSides[i]=b.chainSide=="N"?0:b.chainSide=="E"?1:-1;
                if(ChainSides[i]>=0)for(int l=0;l<=b.chainLayer;l++)ChainThresholds[i]+=level.Layers(ChainSides[i])[l].hp;
                for(int j=0;j<level.blocks.Length;j++)if(level.blocks[j].x==b.x&&level.blocks[j].y==b.y&&level.blocks[j].depth<b.depth)Coverers[i]|=1u<<j;
                var turns=new List<RuneCell>();var visited=new HashSet<int>();int x=b.x,y=b.y;
                var route=new RuneRoute();Routes[i]=route;
                while(true)
                {
                    x+=dx;y+=dy;
                    if(x<0||x>=level.size||y<0||y>=level.size)break;
                    int heading=dx>0?0:dy>0?1:dx<0?2:3;
                    if(!visited.Add((y*level.size+x)*4+heading)){route.loop=true;break;}
                    for(int j=0;j<level.blocks.Length;j++)if(j!=i&&level.blocks[j].x==x&&level.blocks[j].y==y)Blockers[i]|=1u<<j;
                    if(level.mirrors!=null)foreach(var mirror in level.mirrors)if(mirror.x==x&&mirror.y==y)
                    {int old=dx;dx=mirror.slash?-dy:dy;dy=mirror.slash?-old:old;turns.Add(new RuneCell(x,y));break;}
                }
                route.turns=turns.ToArray();route.exitX=x;route.exitY=y;route.exitDir=dx>0?"E":dx<0?"W":dy>0?"S":"N";
            }
        }
        public static void Direction(string d,out int x,out int y) { x=d=="E"?1:d=="W"?-1:0; y=d=="S"?1:d=="N"?-1:0; }
        public RuneState Initial() => new RuneState { board=(1u<<Level.blocks.Length)-1 };
        public int Total(int side) { int n=0;foreach(var l in Level.Layers(side))n+=l.hp;return n; }
        public RuneGate Gate(RuneState s,int side)
        {
            if(side<0)return default;
            int spent=s.spent[side];var layers=Level.Layers(side);
            for(int i=0;i<layers.Length;i++) {var l=layers[i];if(spent<l.hp)return new RuneGate { color=l.color,hp=l.hp-spent,max=l.hp,layer=i,damage=spent };spent-=l.hp;}
            return new RuneGate { layer=layers.Length };
        }
        public int Target(RuneState s,string color) { for(int i=0;i<2;i++)if(Gate(s,i).color==color)return i;return -1; }
        public bool IsVisible(RuneState s,int id) => id>=0&&id<Level.blocks.Length&&(s.board&(1u<<id))!=0&&(s.board&Coverers[id])==0;
        public bool IsChained(RuneState s,int id) => ChainSides[id]>=0&&s.spent[ChainSides[id]]<ChainThresholds[id];
        public bool IsFree(RuneState s,int id) => IsVisible(s,id)&&!IsChained(s,id)&&!Routes[id].loop&&(s.board&Blockers[id])==0;
        public int Underneath(RuneState s,int id)
        {
            var b=Level.blocks[id];int found=-1,depth=int.MaxValue;
            for(int j=0;j<Level.blocks.Length;j++){var other=Level.blocks[j];if((s.board&(1u<<j))!=0&&other.x==b.x&&other.y==b.y&&other.depth>b.depth&&other.depth<depth){found=j;depth=other.depth;}}
            return found;
        }
        public bool Apply(RuneState before,RuneMove move,out RuneState after,out RuneShot shot,out string reason)
        {
            after=before;shot=default;reason=null;
            if(before.phase!=RunePhase.Playing) {reason="finished";return false;}
            int id=move.id;
            if(id<0||id>=Level.blocks.Length){reason="missing";return false;}
            var b=Level.blocks[id];int side=-1,slot=-1;
            if(move.kind==RuneMoveKind.Board)
            {
                if((before.board&(1u<<id))==0){reason="missing";return false;}
                if(!IsFree(before,id)){reason=!IsVisible(before,id)?"covered":IsChained(before,id)?"chained":Routes[id].loop?"loop":"blocked";return false;}
                string exit=Routes[id].exitDir;side=exit=="N"?0:exit=="E"?1:-1;
                if(side>=0&&Gate(before,side).color!=b.color)side=-1;
                if(side<0)slot=Array.IndexOf(before.slots,-1);
            }
            else
            {
                slot=Array.IndexOf(before.slots,id);if(slot<0){reason="missing";return false;}
                side=Target(before,b.color);if(side<0){reason="sealed";return false;}
            }
            after=before.Copy();after.taps++;shot=new RuneShot { id=id,side=side,slot=slot,relaunch=move.kind==RuneMoveKind.Slot };
            if(move.kind==RuneMoveKind.Board)after.board&=~(1u<<id);else after.slots[slot]=-1;
            if(side<0)
            {
                if(slot<0){after.phase=RunePhase.Lost;shot.overflow=true;}
                else {after.slots[slot]=id;after.stored++;after.peak=Math.Max(after.peak,after.Filled);shot.store=true;}
            }
            else
            {
                var g=Gate(before,side);after.spent[side]++;
                shot.layer=g.layer;shot.damageIndex=g.damage;shot.max=g.max;shot.spent=after.spent[side];shot.layerBreak=g.hp==1;shot.sealBreak=Gate(after,side).color==null;
                if(shot.relaunch)after.relaunch++;else after.direct++;
            }
            for(int i=0;i<Level.blocks.Length;i++)if((after.board&(1u<<i))!=0)
            {
                if(IsChained(before,i)&&!IsChained(after,i))shot.unlocked|=1u<<i;
                if(!IsVisible(before,i)&&IsVisible(after,i))shot.revealed|=1u<<i;
            }
            if(after.phase==RunePhase.Playing&&after.board==0&&after.Filled==0&&after.spent[0]==Total(0)&&after.spent[1]==Total(1))after.phase=RunePhase.Won;
            return true;
        }
    }
    public enum RuneSolveStatus { Solved, Dead, Limit }
    public sealed class RuneSolution { public RuneSolveStatus status; public List<RuneMove> route; public int nodes; }
    public static class RuneSolver
    {
        // Store contents can be derived from removed runes minus spent color counts.
        // This collapses slot permutations without changing solvability.
        public static RuneSolution Solve(RuneRules rules,RuneState state,int capacity=5,int maxNodes=250000)
        {
            if(state.phase==RunePhase.Lost)return new RuneSolution {status=RuneSolveStatus.Dead};
            var level=rules.Level;string[] colors={"red","blue","gold","green"};
            int n=level.blocks.Length;var indices=new int[n];var totals=new int[4];
            for(int i=0;i<n;i++){indices[i]=Array.IndexOf(colors,level.blocks[i].color);totals[indices[i]]++;}
            var flat=new int[2][];var prefix=new int[2][][];
            for(int side=0;side<2;side++)
            {
                var f=new List<int>();foreach(var l in level.Layers(side))for(int j=0;j<l.hp;j++)f.Add(Array.IndexOf(colors,l.color));flat[side]=f.ToArray();
                prefix[side]=new int[f.Count+1][];prefix[side][0]=new int[4];
                for(int j=0;j<f.Count;j++){prefix[side][j+1]=(int[])prefix[side][j].Clone();prefix[side][j+1][f[j]]++;}
            }
            var dead=new HashSet<ulong>();var plan=new List<(bool slot,int value)>();int nodes=0;bool limited=false;
            bool Search(uint mask,int p0,int p1)
            {
                ulong key=mask+((ulong)p0<<28)+((ulong)p1<<36);if(dead.Contains(key))return false;
                if(++nodes>maxNodes){limited=true;return false;}
                if(mask==0&&p0==flat[0].Length&&p1==flat[1].Length)return true;
                var remaining=new int[4];for(int i=0;i<n;i++)if((mask&(1u<<i))!=0)remaining[indices[i]]++;
                var stored=new int[4];int count=0;for(int i=0;i<4;i++){stored[i]=totals[i]-remaining[i]-prefix[0][p0][i]-prefix[1][p1][i];count+=stored[i];}
                if(count>capacity){dead.Add(key);return false;}
                int c0=p0<flat[0].Length?flat[0][p0]:-1,c1=p1<flat[1].Length?flat[1][p1]:-1;
                for(int side=0;side<2;side++)
                {
                    int c=side==0?c0:c1;if(c<0||stored[c]==0)continue;
                    plan.Add((true,c));if(Search(mask,p0+(side==0?1:0),p1+(side==1?1:0)))return true;plan.RemoveAt(plan.Count-1);if(limited)return false;
                }
                for(int i=0;i<n;i++)
                {
                    if((mask&(1u<<i))==0||(mask&(rules.Blockers[i]|rules.Coverers[i]))!=0||rules.Routes[i].loop)continue;
                    int lockSide=rules.ChainSides[i];if(lockSide>=0&&(lockSide==0?p0:p1)<rules.ChainThresholds[i])continue;
                    string dir=rules.Routes[i].exitDir;int side=dir=="N"&&indices[i]==c0?0:dir=="E"&&indices[i]==c1?1:-1;
                    if(side<0&&count>=capacity)continue;
                    plan.Add((false,i));if(Search(mask&~(1u<<i),p0+(side==0?1:0),p1+(side==1?1:0)))return true;plan.RemoveAt(plan.Count-1);if(limited)return false;
                }
                dead.Add(key);return false;
            }
            bool solved=Search(state.board,state.spent[0],state.spent[1]);
            var result=new RuneSolution {status=solved?RuneSolveStatus.Solved:limited?RuneSolveStatus.Limit:RuneSolveStatus.Dead,nodes=nodes};
            if(!solved)return result;
            result.route=new List<RuneMove>();var current=state;
            foreach(var p in plan)
            {
                int id=p.value;if(p.slot){id=-1;foreach(int k in current.slots)if(k>=0&&indices[k]==p.value){id=k;break;}}
                var move=new RuneMove(p.slot?RuneMoveKind.Slot:RuneMoveKind.Board,id);result.route.Add(move);rules.Apply(current,move,out var next,out _,out _);current=next;
            }
            return result;
        }
    }
}
