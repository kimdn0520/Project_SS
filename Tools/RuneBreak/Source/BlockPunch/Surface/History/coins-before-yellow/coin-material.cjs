// Shared, editable gold material for the wallet icon and the reward pile.
// One broad upper-left light; no bloom, lens flare, or white painted hot spots.
function gradient(c,x,y,x2,y2,stops){const g=c.createLinearGradient(x,y,x2,y2);stops.forEach(([at,color])=>g.addColorStop(at,color));return g;}
function ellipse(c,x,y,rx,ry,fill){c.beginPath();c.ellipse(x,y,rx,ry,0,0,Math.PI*2);c.fillStyle=fill;c.fill();}
function polygon(c,points,fill){c.beginPath();points.forEach(([x,y],i)=>i?c.lineTo(x,y):c.moveTo(x,y));c.closePath();c.fillStyle=fill;c.fill();}
function starPoints(x,y,r){return Array.from({length:10},(_,i)=>{const a=-Math.PI/2+i*Math.PI/5,s=i%2?r*.47:r;return [x+Math.cos(a)*s,y+Math.sin(a)*s];});}
const coinLayers=[
 ['Contact shadow',c=>{ellipse(c,25,28,20.4,18.6,'#10102535');ellipse(c,25,29,18.5,17,'#21182430');}],
 ['Milled gold thickness',c=>{
  ellipse(c,25,25.8,19.8,19.4,gradient(c,6,18,44,37,[[0,'#b97b18'],[.24,'#dba13a'],[.54,'#a96b11'],[.85,'#875014'],[1,'#683c13']]));
  // Short, shaded grooves follow the front ellipse instead of white dotted trim.
  for(let i=0;i<17;i++){const a=.10+i*(Math.PI-.20)/16,x=24+Math.cos(a)*19.1,y=22+Math.sin(a)*18.6;const dx=1,dy=3.8;
   c.lineCap='round';c.lineWidth=.58;c.strokeStyle='#754414b0';c.beginPath();c.moveTo(x,y+1);c.lineTo(x+dx,y+dy);c.stroke();
   c.lineWidth=.45;c.strokeStyle='#edb947aa';c.beginPath();c.moveTo(x-.65,y+.9);c.lineTo(x+dx-.65,y+dy-.4);c.stroke();
  }
 }],
 ['Rounded outer bevel',c=>{
  ellipse(c,23.5,21.6,20,19.6,gradient(c,8,3,38,41,[[0,'#f9dfa0'],[.19,'#f2cd68'],[.45,'#dfaa32'],[.74,'#b7791c'],[1,'#d49c30']]));
  ellipse(c,23.5,21.3,18.9,18.5,gradient(c,9,3,36,40,[[0,'#f9e6ad'],[.30,'#efc45a'],[.72,'#d89523'],[1,'#e7b440']]));
  ellipse(c,23.5,21.3,17,16.6,gradient(c,12,8,32,37,[[0,'#95621c'],[.5,'#be8320'],[1,'#f2d27c']]));
 }],
 ['Satin recessed face',c=>{
  ellipse(c,23.6,21.8,15.8,15.4,gradient(c,12,9,33,36,[[0,'#d39929'],[.38,'#e4ae3e'],[.73,'#edbd52'],[1,'#e8b447']]));
  c.strokeStyle='#b47c25';c.lineWidth=.35;c.beginPath();c.ellipse(23.6,21.8,14.4,14,0,.95,3.75);c.stroke();
 }],
 ['Chamfered star relief',c=>{
  const outer=starPoints(23.5,21.7,10.4),inner=starPoints(23.5,21.4,8.7);
  polygon(c,outer.map(([x,y])=>[x+.65,y+1.2]),'#a06a1d');
  polygon(c,outer,'#eac165');
  for(let i=0;i<10;i++){
   const j=(i+1)%10,dx=outer[j][0]-outer[i][0],dy=outer[j][1]-outer[i][1],light=(dy*-.65-dx*-.76)/Math.hypot(dx,dy);
   polygon(c,[outer[i],outer[j],inner[j],inner[i]],light>.30?'#f6dda1':light>-.2?'#e5b34d':'#ba8124');
  }
  polygon(c,inner,gradient(c,18,13,28,30,[[0,'#f5d88e'],[.58,'#ecc66d'],[1,'#dfad45']]));
 }],
 ['Fine rim reflection',c=>{
  c.lineCap='round';c.lineWidth=.65;c.strokeStyle='#fff0bdc0';c.beginPath();c.ellipse(23.5,21.3,19.1,18.7,0,3.65,4.95);c.stroke();
  c.lineWidth=.45;c.strokeStyle='#f5d78a';c.beginPath();c.ellipse(23.5,21.4,16.6,16.2,0,.25,1.5);c.stroke();
 }]
];
function drawCoin(c,shadow=true){for(let i=shadow?0:1;i<coinLayers.length;i++){c.save();coinLayers[i][1](c);c.restore();}}
module.exports={coinLayers,drawCoin};
