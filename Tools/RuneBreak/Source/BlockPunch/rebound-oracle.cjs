/* PUNCH REBOUND / render-only motion, in the existing 480 × 584 canvas.
   Straight accelerating approach → 62ms LOCAL impact hold → fast inward recoil
   → one airborne curve to the reserved slot. No global game freeze, no HP changes.
   All sample/pose functions are pure and deterministic. Times are seconds. */
(function(root){
'use strict';
const D={N:{x:0,y:-1},E:{x:1,y:0},S:{x:0,y:1},W:{x:-1,y:0}};
const clamp=t=>Math.max(0,Math.min(1,t));
const lerp=(a,b,t)=>({x:a.x+(b.x-a.x)*t,y:a.y+(b.y-a.y)*t});
const out=t=>1-(1-t)**1.65;
function cubic(a,b,c,d,t){const u=1-t;return{x:u*u*u*a.x+3*u*u*t*b.x+3*u*t*t*c.x+t*t*t*d.x,y:u*u*u*a.y+3*u*u*t*b.y+3*u*t*t*c.y+t*t*t*d.y};}
function create(from,edge,to,dir,reduced=false){
 if(!D[dir])throw Error('Invalid rebound direction');
 const d=D[dir],contact={x:edge.x-d.x*25,y:edge.y-d.y*25};
 const distance=Math.hypot(contact.x-from.x,contact.y-from.y);
 const recoil=reduced?7:(dir==='S'?37:43),kick={x:contact.x-d.x*recoil,y:contact.y-d.y*recoil};
 const hopDistance=Math.hypot(to.x-kick.x,to.y-kick.y);
 const launch=reduced?.10:.085+Math.min(.125,distance/1600);
 const press=reduced?.025:.062,back=reduced?.035:.055;
 const hop=reduced?.145:.145+Math.min(.105,hopDistance/3700);
 const dx=to.x-kick.x,dy=to.y-kick.y;
 // Tangent begins inward, then bends toward the tray. Southern exits kick UP
 // before returning down: this is the important visible reversal, not a fall.
 const c1={x:Math.max(57,Math.min(423,kick.x+dx*.10-d.x*(reduced?3:33))),y:kick.y+dy*.12-d.y*(reduced?3:37)-(reduced?0:10)};
 const c2={x:to.x-dx*.12,y:to.y-(reduced?16:Math.min(102,hopDistance*.19+36))};
 return{version:'punch-1',from:{...from},contact,kick,to:{...to},c1,c2,dir,reduced,
  launch,press,back,hop,release:launch+press,depart:launch+press+back,
  duration:launch+press+back+hop,surface:{x:contact.x+d.x*15,y:contact.y+d.y*15}};
}
function sample(m,elapsed){
 const t=Math.max(0,elapsed),d=D[m.dir];
 if(t>=m.duration)return {...m.to};
 if(t<m.launch){const p=t/m.launch,hold=.12;if(p<hold)return{x:m.from.x-d.x*2.4*Math.sin(p/hold*Math.PI),y:m.from.y-d.y*2.4*Math.sin(p/hold*Math.PI)};return lerp(m.from,m.contact,((p-hold)/(1-hold))**2.25);}
 if(t<m.release)return {...m.contact};
 if(t<m.depart){const p=(t-m.release)/m.back;return lerp(m.contact,m.kick,1-(1-p)**1.65);}
 const p=clamp((t-m.depart)/m.hop);return cubic(m.kick,m.c1,m.c2,m.to,out(p));
}
function pose(m,elapsed){
 const t=Math.max(0,elapsed),d=D[m.dir],vertical=!d.x;
 let sx=1,sy=1,angle=0,size=36,lift=0,flash=0,phase='approach';
 const hop=clamp((t-m.depart)/m.hop),airborne=t>=m.release&&t<m.duration;
 const pressStart=m.launch-.018;
 if(t>=pressStart&&t<m.release){
  const rise=clamp((t-pressStart)/.027),squeeze=(m.reduced?.07:.43)*Math.sin(rise*Math.PI/2);
  sx=vertical?1+squeeze*.62:1-squeeze;sy=vertical?1-squeeze:1+squeeze*.62;
  phase='impact';flash=m.reduced?0:Math.max(0,1-(t-m.launch)/.055)*.42;
 }else if(airborne){
  const q=clamp((t-m.release)/(m.duration-m.release));
  const pop=m.reduced?0:Math.sin(Math.min(1,q/.42)*Math.PI)*.20;
  size=36+(33-36)*q+(m.reduced?0:5.5*Math.sin(q*Math.PI));
  const spin=m.dir==='E'?-.48:m.dir==='W'?.48:(m.to.x>=m.from.x?.36:-.36);
  angle=m.reduced?0:Math.sin(q*Math.PI)*spin;lift=m.reduced?0:Math.sin(q*Math.PI)*22;
  // Short stretch at release, rather than a jelly wobble throughout the journey.
  sx=vertical?1-pop*.55:1+pop;sy=vertical?1+pop:1-pop*.55;
  phase=t<m.depart?'kick':'flight';
 }else if(t>=m.duration){size=33;phase='landed';}
 return{sx,sy,angle,size,lift,flash,airborne,hop,phase};
}
const api={create,sample,pose};root.ReboundMotion=api;if(typeof module!=='undefined')module.exports=api;
})(typeof window!=='undefined'?window:globalThis);
