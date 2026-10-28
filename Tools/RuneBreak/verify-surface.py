"""Verify raw RGBA, avoiding the premultiplied Canvas decoder's low-alpha rounding."""
import json
from pathlib import Path
from PIL import Image, ImageChops

root=Path(__file__).resolve().parent
source=root/'Source/BlockPunch/Surface'
output=root.parent.parent/'Assets/RuneBreak/Art/BlockPunch/Surface'
manifest=json.loads((source/'manifest.json').read_text())
report={'status':'passed','aseprite':[],'comfy':[]}
for name,item in manifest.items():
    original=Image.open(source/(name+'-composite.png')).convert('RGBA')
    shipped=Image.open(output/(name+'.png')).convert('RGBA')
    assert original.size==shipped.size,(name,'size')
    extrema=ImageChops.difference(original,shipped).getextrema()
    assert all(high==0 for low,high in extrema),(name,extrema)
    assert (source/(name+'.aseprite')).is_file(),name
    report['aseprite'].append({'name':name,'rawChannelDifferences':0})
    processed=source/(name+'-comfy.png')
    if processed.is_file():
        image=Image.open(processed).convert('RGBA')
        diff=ImageChops.difference(shipped,image).getextrema()
        assert all(high==0 for low,high in diff[:3]),(name,diff)
        assert diff[3][1]<=1,(name,diff)
        report['comfy'].append({'name':name,'rgbMaximumDifference':0,'alphaMaximumDifference':diff[3][1],'shipped':'exact Aseprite composite'})
(root/'QA/surface-art.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'status':report['status'],'asepriteSprites':len(report['aseprite']),'comfyMaterials':len(report['comfy']),'rawRgbDifferences':0,'maximumComfyAlphaRounding':1,'shippedPixelDifferences':0}))
