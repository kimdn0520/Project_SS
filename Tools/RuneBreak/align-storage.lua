local root=app.params['root']
local base=root..'/Tools/RuneBreak/Source/BlockPunch/Surface/'
local sprite=Sprite(1440,1956,ColorMode.RGB)
sprite.layers[1].name='Original HTML cabinet - preserved'
sprite:newCel(sprite.layers[1],1,Image{fromFile=base..'cabinet-composite.png'},Point(0,0))
local layer=sprite:newLayer();layer.name='Five aligned recessed storage pockets'
sprite:newCel(layer,1,Image{fromFile=base..'storage-alignment-overlay.png'},Point(0,0))
sprite:saveAs(base..'cabinet-aligned.aseprite')
sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/cabinet_aligned.png')
sprite:close()
