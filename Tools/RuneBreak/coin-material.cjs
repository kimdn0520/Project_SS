// Shared, editable vivid yellow gold for the wallet icon and the reward pile.
// One broad upper-left light; no bloom, lens flare, or white painted hot spots.
function gradient(c,x,y,x2,y2,stops){const g=c.createLinearGradient(x,y,x2,y2);stops.forEach(([at,color])=>g.addColorStop(at,color));return g;}
function ellipse(c,x,y,rx,ry,fill){c.beginPath();c.ellipse(x,y,rx,ry,0,0,Math.PI*2);c.fillStyle=fill;c.fill();}
function polygon(c,points,fill){c.beginPath();points.forEach(([x,y],i)=>i?c.lineTo(x,y):c.moveTo(x,y));c.closePath();c.fillStyle=fill;c.fill();}
function starPoints(x,y,r){return Array.from({length:10},(_,i)=>{const a=-Math.PI/2+i*Math.PI/5,s=i%2?r*.47:r;return [x+Math.cos(a)*s,y+Math.sin(a)*s];});}
const coinLayers=[
 ['Contact shadow',c=>{ellipse(c,25,28,20.4,18.6,'#10102535');ellipse(c,25,29,18.5,17,'#21182430');}],
 ['Milled gold thickness',c=>{
  ellipse(c,25,25.8,19.8,19.4,gradient(c,6,18,44,37,[[0,'#e99500'],[.24,'#ffbf08'],[.54,'#d77900'],[.85,'#b35b00'],[1,'#874000']]));
  // Short, shaded grooves follow the front ellipse instead of white dotted trim.
  for(let i=0;i<17;i++){const a=.10+i*(Math.PI-.20)/16,x=24+Math.cos(a)*19.1,y=22+Math.sin(a)*18.6;const dx=1,dy=3.8;
   c.lineCap='round';c.lineWidth=.58;c.strokeStyle='#a55400b0';c.beginPath();c.moveTo(x,y+1);c.lineTo(x+dx,y+dy);c.stroke();
   c.lineWidth=.45;c.strokeStyle='#ffda32aa';c.beginPath();c.moveTo(x-.65,y+.9);c.lineTo(x+dx-.65,y+dy-.4);c.stroke();
  }
 }],
 ['Rounded outer bevel',c=>{
  ellipse(c,23.5,21.6,20,19.6,gradient(c,8,3,38,41,[[0,'#fff482'],[.19,'#ffe52b'],[.45,'#ffc500'],[.74,'#e68b00'],[1,'#ffbc00']]));
  ellipse(c,23.5,21.3,18.9,18.5,gradient(c,9,3,36,40,[[0,'#fff58a'],[.30,'#ffe300'],[.72,'#ffb400'],[1,'#ffce0c']]));
  ellipse(c,23.5,21.3,17,16.6,gradient(c,12,8,32,37,[[0,'#c87800'],[.5,'#e89b00'],[1,'#ffe76a']]));
 }],
 ['Vivid yellow recessed face',c=>{
  ellipse(c,23.6,21.8,15.8,15.4,gradient(c,12,9,33,36,[[0,'#ffc914'],[.38,'#ffdf16'],[.73,'#ffe92b'],[1,'#ffdc12']]));
  c.strokeStyle='#e8a713';c.lineWidth=.35;c.beginPath();c.ellipse(23.6,21.8,14.4,14,0,.95,3.75);c.stroke();
 }],
 ['Chamfered star relief',c=>{
  const outer=starPoints(23.5,21.7,10.4),inner=starPoints(23.5,21.4,8.7);
  polygon(c,outer.map(([x,y])=>[x+.65,y+1.2]),'#bf7100');
  polygon(c,outer,'#ffce32');
  for(let i=0;i<10;i++){
   const j=(i+1)%10,dx=outer[j][0]-outer[i][0],dy=outer[j][1]-outer[i][1],light=(dy*-.65-dx*-.76)/Math.hypot(dx,dy);
   polygon(c,[outer[i],outer[j],inner[j],inner[i]],light>.30?'#fff18a':light>-.2?'#ffd43a':'#df9000');
  }
  polygon(c,inner,gradient(c,18,13,28,30,[[0,'#ffdb47'],[.58,'#ffc421'],[1,'#f5a900']]));
 }],
 ['Fine rim reflection',c=>{
  c.lineCap='round';c.lineWidth=.65;c.strokeStyle='#fff7b0c0';c.beginPath();c.ellipse(23.5,21.3,19.1,18.7,0,3.65,4.95);c.stroke();
  c.lineWidth=.45;c.strokeStyle='#ffed71';c.beginPath();c.ellipse(23.5,21.4,16.6,16.2,0,.25,1.5);c.stroke();
 }]
];
function drawCoin(c,shadow=true){for(let i=shadow?0:1;i<coinLayers.length;i++){c.save();coinLayers[i][1](c);c.restore();}}
module.exports={coinLayers,drawCoin};
