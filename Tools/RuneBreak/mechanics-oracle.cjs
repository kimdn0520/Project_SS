// Independent, object-based extension of the untouched HTML rule oracle.
// No animation clocks, Unity types, bit masks, or C# route caches are used here.
module.exports=function extend(base){
 const clone=base.clone, block=base.block;
 function chained(level,s,b){return !!b.chainSide&&base.gate(level,s,b.chainSide).index<=b.chainLayer;}
 function visible(level,s,b){return s.board.includes(b.id)&&!s.board.some(id=>{const q=block(level,id);return q.x===b.x&&q.y===b.y&&(q.depth||0)<(b.depth||0);});}
 function path(level,b){
  let {x,y}=b,{x:dx,y:dy}=base.DIRS[b.dir];const turns=[],cells=[],seen=new Set();
  for(;;){
   x+=dx;y+=dy;if(x<0||y<0||x>=level.size||y>=level.size)break;
   const key=[x,y,dx,dy].join(',');if(seen.has(key))return{loop:true,turns,cells};seen.add(key);cells.push({x,y});
   const m=(level.mirrors||[]).find(m=>m.x===x&&m.y===y);
   if(m){const old=dx;dx=m.slash?-dy:dy;dy=m.slash?-old:old;turns.push({x,y});}
  }
  return{turns,cells,x,y,side:dx>0?'E':dx<0?'W':dy>0?'S':'N',loop:false};
 }
 function probe(level,s,id){
  const b=block(level,id);if(!b||!s.board.includes(id))return{ok:false,reason:'missing'};
  if(!visible(level,s,b))return{ok:false,reason:'covered'};
  if(chained(level,s,b))return{ok:false,reason:'chained'};
  const route=path(level,b);if(route.loop)return{ok:false,reason:'loop'};
  for(const cell of route.cells){const found=s.board.find(id=>{const q=block(level,id);return id!==b.id&&q.x===cell.x&&q.y===cell.y;});if(found)return{ok:false,reason:'blocked',blocker:found};}
  const direct=base.gate(level,s,route.side).color===b.color;
  return{ok:true,kind:direct?'direct':'store',side:route.side,overflow:!direct&&!s.slots.includes(null),route};
 }
 function step(level,state,action){
  if(state.phase!=='play')return{ok:false,reason:'finished',state};
  const b=block(level,action.id);if(!b)return{ok:false,reason:'missing',state};
  const s=clone(state);let side,slot=-1;
  if(action.type==='board'){
   const p=probe(level,s,b.id);if(!p.ok)return{...p,state};
   s.board=s.board.filter(id=>id!==b.id);side=p.kind==='direct'?p.side:null;
   if(!side){slot=s.slots.indexOf(null);if(slot<0){s.phase='lost';s.failed=b.id;}else{s.slots[slot]=b.id;s.stored++;s.peak=Math.max(s.peak,s.slots.filter(Boolean).length);}}
  }else if(action.type==='slot'){
   slot=s.slots.indexOf(b.id);if(slot<0)return{ok:false,reason:'missing',state};
   side=base.target(level,s,b.color);if(!side)return{ok:false,reason:'sealed',state};s.slots[slot]=null;
  }else return{ok:false,reason:'bad-action',state};
  s.taps++;const event={id:b.id,side,slot,unlocked:[],revealed:[]};
  if(side){const g=base.gate(level,s,side);s.spent[side]++;s.hp[side]--;s.out.push(b.id);if(action.type==='slot')s.relaunch++;else s.direct++;event.layerBreak=g.hp===1;}
  for(const id of s.board){const q=block(level,id);if(chained(level,state,q)&&!chained(level,s,q))event.unlocked.push(id);if(!visible(level,state,q)&&visible(level,s,q))event.revealed.push(id);}
  if(s.phase==='play'&&!s.board.length&&s.slots.every(x=>x===null)&&base.SIDES.every(k=>s.hp[k]===0))s.phase='won';
  return{ok:true,state:s,events:[event]};
 }
 function choices(level,s,includeOverflow=false){
  if(s.phase!=='play')return[];const moves=[];
  for(const id of s.slots)if(id&&base.target(level,s,block(level,id).color))moves.push({type:'slot',id});
  for(const id of s.board){const p=probe(level,s,id);if(p.ok&&(includeOverflow||!p.overflow))moves.push({type:'board',id});}
  return moves;
 }
 function analyze(level,state=base.initial(level),options={}){
  const capacity=options.capacity??level.capacity,dead=new Set(),plan=[];let nodes=0,limited=false;
  function visit(s){
   if(++nodes>(options.maxNodes||250000)){limited=true;return false;}
   if(s.phase==='lost'||s.slots.filter(Boolean).length>capacity)return false;if(s.phase==='won')return true;
   const key=s.board.join(',')+'|'+s.spent.N+','+s.spent.E;if(dead.has(key))return false;
   const moves=choices(level,s).sort((a,b)=>score(b)-score(a));
   function score(a){return a.type==='slot'?3:probe(level,s,a.id).kind==='direct'?4:0;}
   for(const a of moves){plan.push(a);if(visit(step(level,s,a).state))return true;plan.pop();if(limited)return false;}dead.add(key);return false;
  }
  const solved=visit(state);return{status:solved?'solved':limited?'limit':'dead',route:solved?plan.slice():null,nodes};
 }
 function validate(level){
  if(level.blocks.length>28||level.capacity!==5)throw Error('Unsupported bounded board');
  const cells=new Set(),ids=new Set(),need={},have={},owners={};
  for(const side of base.SIDES)for(const l of level.seals[side]){if(owners[l.color]&&owners[l.color]!==side)throw Error('Ambiguous auto-target');owners[l.color]=side;need[l.color]=(need[l.color]||0)+l.hp;}
  for(const b of level.blocks){const key=[b.x,b.y,b.depth||0].join(',');if(cells.has(key)||ids.has(b.id))throw Error('Duplicate rune/depth');cells.add(key);ids.add(b.id);have[b.color]=(have[b.color]||0)+1;
   if(b.chainSide&&(!level.seals[b.chainSide]||!level.seals[b.chainSide][b.chainLayer]))throw Error('Invalid chain');
   if(!base.DIRS[b.dir]||!base.COLORS.includes(b.color)||b.x<0||b.y<0||b.x>=level.size||b.y>=level.size)throw Error('Invalid rune');
  }
  const mirrors=new Set();for(const m of level.mirrors||[]){const key=m.x+','+m.y;if(mirrors.has(key)||level.blocks.some(b=>b.x===m.x&&b.y===m.y))throw Error('Mirror overlaps a rune/mirror');mirrors.add(key);}
  for(const c of base.COLORS)if((have[c]||0)!==(need[c]||0))throw Error('Rune / damage mismatch '+c);
  const answer=analyze(level);if(answer.status!=='solved')throw Error('Unsolvable: '+level.id);return true;
 }
 return{...base,chained,visible,path,probe,step,choices,analyze,solve:(l,s,o)=>analyze(l,s,o).route,validate};
};
