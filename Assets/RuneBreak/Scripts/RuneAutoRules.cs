using System;
using System.Collections.Generic;

namespace RuneBreak
{
    // The HTML AutoRules oracle, using the existing immutable rule transitions.
    // Presentation readiness only selects when a reserved Slot action is issued.
    public static class RuneAutoRules
    {
        public static int ReadySlot(RuneRules rules,RuneState state,RuneState visible=null,bool[] pending=null,float[] eligibleAt=null,float time=0)
        {
            if(state.phase!=RunePhase.Playing)return -1;
            for(int slot=0;slot<state.slots.Length;slot++)
            {
                int id=state.slots[slot];if(id<0||(pending!=null&&pending[id])||(eligibleAt!=null&&eligibleAt[id]>time))continue;
                string color=rules.Level.blocks[id].color;int side=rules.Target(state,color);if(side<0)continue;
                if(visible!=null){var shown=rules.Gate(visible,side);var reserved=rules.Gate(state,side);if(shown.color!=color||shown.layer!=reserved.layer)continue;}
                return slot;
            }
            return -1;
        }
        public static RuneState Settle(RuneRules rules,RuneState state,List<RuneShot> events=null)
        {
            for(int i=0;i<=rules.Level.blocks.Length&&state.phase==RunePhase.Playing;i++)
            {
                int slot=ReadySlot(rules,state);if(slot<0)break;
                if(!rules.Apply(state,new RuneMove(RuneMoveKind.Slot,state.slots[slot]),out var next,out var shot,out _))throw new InvalidOperationException("Automatic shot rejected.");
                state=next;events?.Add(shot);
            }
            return state;
        }
        public static bool Step(RuneRules rules,RuneState before,int id,out RuneState after,List<RuneShot> events=null)
        {
            var ready=Settle(rules,before,events);
            if(!rules.Apply(ready,new RuneMove(RuneMoveKind.Board,id),out after,out var shot,out _))return false;
            events?.Add(shot);after=Settle(rules,after,events);return true;
        }
        public static RuneSolution Solve(RuneRules rules,RuneState initial,int maxNodes=300000)
        {
            var dead=new HashSet<ulong>();var route=new List<RuneMove>();int nodes=0;bool limited=false;
            bool Visit(RuneState before)
            {
                var state=Settle(rules,before);if(state.phase==RunePhase.Won)return true;if(state.phase!=RunePhase.Playing)return false;
                ulong key=state.board+((ulong)state.spent[0]<<28)+((ulong)state.spent[1]<<36);if(dead.Contains(key))return false;
                if(++nodes>maxNodes){limited=true;return false;}
                // HTML's stable sort: direct board hits first, then stores in board order.
                for(int pass=0;pass<2;pass++)for(int id=0;id<rules.Level.blocks.Length;id++)
                {
                    var move=new RuneMove(RuneMoveKind.Board,id);
                    if(!rules.Apply(state,move,out var next,out var shot,out _)||shot.overflow||(pass==0)==shot.store)continue;
                    route.Add(move);if(Visit(next))return true;route.RemoveAt(route.Count-1);if(limited)return false;
                }
                dead.Add(key);return false;
            }
            bool solved=Visit(initial);return new RuneSolution{status=solved?RuneSolveStatus.Solved:limited?RuneSolveStatus.Limit:RuneSolveStatus.Dead,route=solved?route:null,nodes=nodes};
        }
    }
}
