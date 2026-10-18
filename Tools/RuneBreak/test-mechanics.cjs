const fs=require('fs'),vm=require('vm'),path=require('path'),assert=require('assert'),crypto=require('crypto');
const html=fs.readFileSync(path.join(__dirname,'Source/rune-break-layers.html'),'utf8');
const scripts=[...html.matchAll(/<script[^>]*>([\s\S]*?)<\/script>/g)].map(x=>x[1]);for(const i of [0,1])vm.runInThisContext(scripts[i]);
const E=require('./mechanics-oracle.cjs')(RuneEngine),levels=JSON.parse(fs.readFileSync(path.join(__dirname,'../../Assets/RuneBreak/Data/levels.json'))).levels;
let seed=12437;function random(n){seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed%n;}
let originalComparisons=0;
for(const level of levels.filter(l=>!l.demo)){
 let state=RuneEngine.initial(level);
 for(let i=0;i<500;i++){
  if(state.phase!=='play'||random(16)===0)state=RuneEngine.initial(level);
  const actions=RuneEngine.choices(level,state,true),a=actions.length&&random(4)!==0?actions[random(actions.length)]:{type:random(2)?'board':'slot',id:level.blocks[random(level.blocks.length)].id};
  const actual=E.step(level,state,a),expected=RuneEngine.step(level,state,a);assert.strictEqual(actual.ok,expected.ok);
  assert.deepStrictEqual(actual.state,expected.state);if(actual.ok)state=actual.state;originalComparisons++;
 }
}
// Geometry fixtures cover every incoming heading and both mirror diagonals.
for(const slash of [true,false])for(const dir of ['N','E','S','W']){
 const pos={N:[2,4],E:[0,2],S:[2,0],W:[4,2]}[dir];
 levels.push({id:'geometry-'+slash+'-'+dir,size:6,capacity:5,seals:{N:[{color:'green',hp:1}],E:[{color:'red',hp:1}]},mirrors:[{x:2,y:2,slash}],blocks:[{id:'ray',x:pos[0],y:pos[1],dir,color:'red'}]});
}
levels.push({id:'loop',size:6,capacity:5,seals:{N:[],E:[{color:'red',hp:1}]},mirrors:[{x:3,y:1,slash:false},{x:3,y:3,slash:true},{x:0,y:3,slash:false},{x:0,y:1,slash:true}],blocks:[{id:'ray',x:1,y:1,dir:'E',color:'red'}]});
levels.push({id:'after-bend-blocker',size:6,capacity:5,seals:{N:[],E:[{color:'red',hp:1},{color:'blue',hp:1}]},mirrors:[{x:1,y:1,slash:true}],blocks:[{id:'ray',x:1,y:4,dir:'N',color:'red'},{id:'blocker',x:4,y:1,dir:'S',color:'blue'}]});
levels.push({id:'locked-under-stack',size:6,capacity:5,seals:{N:[{color:'green',hp:1}],E:[{color:'red',hp:1},{color:'blue',hp:1}]},blocks:[{id:'top',x:2,y:3,dir:'E',color:'red'},{id:'lower',x:2,y:3,dir:'E',color:'blue',depth:1,chainSide:'N',chainLayer:0},{id:'key',x:0,y:0,dir:'N',color:'green'}]});
const cases=[],routes=[];let unlocked=0,revealed=0,reflected=0;
for(let li=6;li<levels.length;li++){
 const level=levels[li];let s=E.initial(level);const count=level.demo?1000:40;
 for(const b of level.blocks)routes.push({level:li,id:b.id,...E.path(level,b)});
 for(let i=0;i<count;i++){
  if(s.phase!=='play'||random(17)===0)s=E.initial(level);
  const choices=E.choices(level,s,true),action=choices.length&&random(3)!==0?choices[random(choices.length)]:{type:random(3)?'board':'slot',id:level.blocks[random(level.blocks.length)].id};
  const snapshot=JSON.stringify(s),out=E.step(level,s,action);assert.strictEqual(JSON.stringify(s),snapshot,'Oracle mutated its input');
  const event=out.events?.[0]||{unlocked:[],revealed:[]};
  cases.push({level:li,before:s,action,ok:out.ok,reason:out.reason||'',after:out.state,unlocked:event.unlocked,revealed:event.revealed});
  if(out.ok){unlocked+=event.unlocked.length;revealed+=event.revealed.length;if(action.type==='board'&&E.path(level,E.block(level,action.id)).turns.length)reflected++;s=out.state;}
 }
}
assert(unlocked>0&&revealed>0&&reflected>0);
const root=path.resolve(__dirname,'../..'),temporary=path.join(root,'Temp/RuneBreakTests');fs.mkdirSync(temporary,{recursive:true});
fs.writeFileSync(path.join(temporary,'mechanic-fixtures.json'),JSON.stringify({levels,cases,routes}));
const before=JSON.parse(fs.readFileSync(path.join(__dirname,'QA/art-before-mechanics.json'))),changed=[];
for(const [file,hash] of Object.entries(before)){const current=crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'Assets/RuneBreak/Art',file))).digest('hex');if(current!==hash)changed.push(file);}
assert.deepStrictEqual(changed,[],'Existing exported pixel art changed');
const report={originalRuleComparisons:originalComparisons,mechanicCases:cases.length,geometryRoutes:routes.length,unlockEvents:unlocked,revealEvents:revealed,reflectedShots:reflected,originalArtFiles:Object.keys(before).length,changedOriginalArt:changed};
fs.writeFileSync(path.join(__dirname,'QA/mechanic-oracle-report.json'),JSON.stringify(report,null,2));console.log(report);
