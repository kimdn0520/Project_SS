// Offline render of the prototype's oscillator/envelope/filter recipes. No runtime synthesis.
const fs=require('fs'),path=require('path');const rate=44100;
const out=path.resolve(__dirname,'../../Assets/RuneBreak/Audio');fs.mkdirSync(out,{recursive:true});
let seed=1337;function random(){seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;}
for(const kind of (process.argv.length>2?process.argv.slice(2):['launch','relaunch','store','hit','break','blocked','loss','win','chain','reflect','reveal','rebound','swoosh','catch'])){
 const samples=new Float64Array(rate*2);
 function tone(freq,end,duration,type='square',volume=.2,delay=0){let phase=0;const n=Math.floor(rate*duration),start=Math.floor(rate*delay);for(let i=0;i<n;i++){let t=i/rate;phase+=freq*Math.pow(Math.max(20,end)/freq,t/duration)/rate;let wave=type==='sine'?Math.sin(phase*2*Math.PI):type==='triangle'?2/Math.PI*Math.asin(Math.sin(phase*2*Math.PI)):Math.sin(phase*2*Math.PI)>=0?1:-1;let env=t<.004?.001+(volume-.001)*t/.004:volume*Math.pow(.001/volume,(t-.004)/(duration-.004));samples[start+i]+=wave*env;}}
 function noise(duration,volume=.16,frequency=1000){const n=Math.floor(rate*duration),omega=2*Math.PI*frequency/rate,alpha=Math.sin(omega)/(2*.6),a0=1+alpha;let x1=0,x2=0,y1=0,y2=0;for(let i=0;i<n;i++){let x=(random()*2-1)*(1-i/n),y=(alpha*x-alpha*x2+2*Math.cos(omega)*y1-(1-alpha)*y2)/a0;x2=x1;x1=x;y2=y1;y1=y;samples[i]+=y*volume;}}
 if(kind==='launch'){tone(260,660,.075,'triangle',.22);noise(.065,.06,1800);}
 if(kind==='relaunch'){tone(350,1300,.11,'triangle',.24);tone(100,60,.07,'sine',.15);}
 if(kind==='store'){tone(710,500,.055,'square',.11);tone(260,220,.07,'triangle',.14,.025);}
 if(kind==='hit'){noise(.14,.38,1600);tone(155,45,.16,'sine',.75);tone(470,240,.07,'square',.12);}
 if(kind==='break'){noise(.33,.6,1250);tone(180,33,.3,'sine',.95);[680,920,1250].forEach((f,i)=>tone(f,f*.6,.14,'triangle',.2,i*.035));}
 if(kind==='blocked')tone(160,100,.06,'triangle',.27);
 if(kind==='loss')[250,180,110].forEach((f,i)=>tone(f,f*.7,.22,'square',.15,i*.13));
 if(kind==='win')[392,494,587,784].forEach((f,i)=>tone(f,f,.2,'triangle',.24,i*.1));
 if(kind==='chain'){noise(.19,.34,2400);tone(170,48,.16,'sine',.45);[820,1390,2170].forEach((f,i)=>tone(f,f*.78,.22,'triangle',.18,i*.027));tone(1800,340,.06,'square',.09,.035);}
 if(kind==='reflect'){tone(1568,2093,.105,'sine',.5);tone(3136,2350,.11,'triangle',.16,.012);noise(.035,.12,3200);}
 if(kind==='reveal'){tone(330,660,.08,'triangle',.3);[784,988,1175].forEach((f,i)=>tone(f,f,.15,'sine',.2,.03+i*.028));noise(.08,.09,2800);}
 if(kind==='rebound'){tone(172,43,.16,'sine',.82);tone(680,220,.052,'triangle',.35);noise(.075,.34,1350);}
 if(kind==='catch'){tone(180,70,.08,'sine',.30);tone(1000,590,.048,'triangle',.17);noise(.026,.10,2700);}
 if(kind==='swoosh'){
  const duration=.23,n=Math.floor(rate*duration);let x1=0,x2=0,y1=0,y2=0;
  for(let i=0;i<n;i++){const t=i/rate,f=t<.035?900*Math.pow(4300/900,t/.035):4300*Math.pow(600/4300,(t-.035)/(duration-.035)),w=2*Math.PI*f/rate,alpha=Math.sin(w)/(2*.75),x=random()*2-1,y=(alpha*x-alpha*x2+2*Math.cos(w)*y1-(1-alpha)*y2)/(1+alpha);x2=x1;x1=x;y2=y1;y1=y;const gain=t<.016?.001+.429*t/.016:.43*Math.pow(.001/.43,(t-.016)/(duration-.016));samples[i]+=y*gain;}
  tone(1180,260,.18,'triangle',.10);
 }
 let n=samples.length;while(n>1&&Math.abs(samples[n-1])<.000001)n--;n=Math.min(samples.length,n+882);
 const wav=Buffer.alloc(44+n*2);wav.write('RIFF');wav.writeUInt32LE(36+n*2,4);wav.write('WAVEfmt ',8);wav.writeUInt32LE(16,16);wav.writeUInt16LE(1,20);wav.writeUInt16LE(1,22);wav.writeUInt32LE(rate,24);wav.writeUInt32LE(rate*2,28);wav.writeUInt16LE(2,32);wav.writeUInt16LE(16,34);wav.write('data',36);wav.writeUInt32LE(n*2,40);for(let i=0;i<n;i++)wav.writeInt16LE(Math.round(Math.max(-1,Math.min(1,samples[i]*.42))*32767),44+i*2);fs.writeFileSync(path.join(out,kind+'.wav'),wav);
}
console.log('Rendered requested prototype sound recipes.');
