const fs=require('fs'),vm=require('vm');
const scripts=[...fs.readFileSync('Tools/RuneBreak/Source/BlockPunch/Surface/block-play-surface-buttons.html','utf8').matchAll(/<script[^>]*>([\s\S]*?)<\/script>/g)].map(m=>m[1]);
const ctx={};ctx.window=ctx;vm.createContext(ctx);for(const i of [1,2,3])vm.runInContext(scripts[i],ctx);
const levels=JSON.parse(fs.readFileSync('Assets/RuneBreak/Data/levels.json')).levels.filter(l=>!l.demo);
const normalize=(l,s)=>({board:s.board.reduce((v,id)=>v+2**l.blocks.findIndex(b=>b.id===id),0),slots:s.slots.map(id=>id?l.blocks.findIndex(b=>b.id===id):-1),spent:[s.spent.N,s.spent.E],phase:s.phase,taps:s.taps,direct:s.direct,stored:s.stored,relaunch:s.relaunch,peak:s.peak});
const cases=levels.map(level=>{let state=ctx.RuneEngine.initial(level);const solution=ctx.AutoRules.solve(level,state);if(solution.status!=='solved')throw Error(level.id);const steps=[];for(const id of solution.route){const before=state;const r=ctx.AutoRules.step(level,state,id);state=r.state;steps.push({id:level.blocks.findIndex(b=>b.id===id),before:normalize(level,before),after:normalize(level,state)});}return {level,steps};});
ctx.RuneArt={P:{red:{mid:'#f53552'},blue:{mid:'#03a7f5'},gold:{mid:'#ffca0d'},green:{mid:'#58cf17'}},gateRect:s=>s==='N'?{x:72,y:60,w:336,h:28}:{x:420,y:100,w:28,h:336}};ctx.BlockDesign={};
vm.runInContext(scripts[11].replace('root.DepthFX={','root.DepthFX={inspect:()=>JSON.parse(JSON.stringify(pieces)),'),ctx);
const canvas=new Proxy({},{get:()=>()=>{}});const particles=[];
for(const reduced of [false,true])for(const side of ['N','E'])for(const big of [false,true]){
 ctx.DepthFX.clear();if(big)ctx.DepthFX.breakPanel({layerBreak:true,side,color:'blue'},0,reduced);ctx.DepthFX.emit(250,74,'blue',big,side,reduced);
 const frames=[{time:0,pieces:ctx.DepthFX.inspect()}];for(let i=1;i<=72;i++){ctx.DepthFX.updateDraw(canvas,1/60);if([1,12,30,45,72].includes(i))frames.push({time:i/60,pieces:ctx.DepthFX.inspect()});}particles.push({reduced,side:side==='N'?0:1,big,frames});
}
fs.writeFileSync('Tools/RuneBreak/QA/surface-motion-oracle.json',JSON.stringify({cases,particles}));console.log(JSON.stringify({campaignLevels:cases.length,boardSteps:cases.reduce((n,c)=>n+c.steps.length,0),particleScenarios:particles.length}));
