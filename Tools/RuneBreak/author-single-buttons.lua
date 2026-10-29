local root=app.params['root']
local base=root..'/Tools/RuneBreak/Source/BlockPunch/Lobby/Blocks/'
do
 local sprite=Sprite(1020,228,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original molded block surface"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_gold_340_76-0.png'},Point(0,0))
 sprite:saveAs(base..'molded_gold_340_76.aseprite')
 sprite:saveCopyAs(base..'molded_gold_340_76-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_gold_340_76.png')
 sprite:close()
end
do
 local sprite=Sprite(900,246,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original molded block surface"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_gold_300_82-0.png'},Point(0,0))
 sprite:saveAs(base..'molded_gold_300_82.aseprite')
 sprite:saveCopyAs(base..'molded_gold_300_82-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_gold_300_82.png')
 sprite:close()
end
do
 local sprite=Sprite(630,234,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original molded block surface"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_green_210_78-0.png'},Point(0,0))
 sprite:saveAs(base..'molded_green_210_78.aseprite')
 sprite:saveCopyAs(base..'molded_green_210_78-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_green_210_78.png')
 sprite:close()
end
do
 local sprite=Sprite(630,234,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original molded block surface"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_amber_210_78-0.png'},Point(0,0))
 sprite:saveAs(base..'molded_amber_210_78.aseprite')
 sprite:saveCopyAs(base..'molded_amber_210_78-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_amber_210_78.png')
 sprite:close()
end
