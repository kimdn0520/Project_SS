// Intentional product UI changes; keep the supplied HTML intact as the art/rules reference.
module.exports=function presentation(script){
 script=script.replace(/\r\n/g,'\n');
 const replaceOnce=(before,after)=>{
  if(script.split(before).length!==2)throw new Error('Prototype drawing changed: '+before);
  script=script.replace(before,after);
 };
 replaceOnce("  if(side==='S'){box(c,199,392,82,17,'#172a32','#3a5553',2);pixText(c,'TO POUCH',240,397,1,'#a2b39a','center');}", '');
 replaceOnce("  const hx=side==='E'?438:240,hy=side==='N'?57:228;box(c,hx-13,hy-10,26,20,'#14262d','#6c967e',3);pixText(c,'OK',hx,hy-4,1,'#b6d4aa','center');return;", '  return;');
 // One visible block per hit point. Existing impact masks remove one block per hit.
 replaceOnce('const cells=Math.max(8,Math.min(18,total*3)),seg=', 'const cells=total,seg=');
 replaceOnce(" const hx=side==='E'?438:240,hy=side==='N'?57:228;\n box(c,hx-13,hy-10,26,20,'#14262d',p.dark,3);pixText(c,String(visual),hx,hy-4,1,p.light,'center');", '');
 return script;
};
