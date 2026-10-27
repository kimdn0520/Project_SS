local root=app.params['root']
local base=root..'/Tools/RuneBreak/Source/BlockPunch/Lobby/Blocks/'
do
 local sprite=Sprite(1212,228,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Inset edge"
 sprite:newCel(layer,1,Image{fromFile=base..'popup_row-0.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Inset face"
 sprite:newCel(layer,1,Image{fromFile=base..'popup_row-1.png'},Point(0,0))
 sprite:saveAs(base..'popup_row.aseprite')
 sprite:saveCopyAs(base..'popup_row-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/popup_row.png')
 sprite:close()
end
do
 local sprite=Sprite(144,144,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Thickness"
 sprite:newCel(layer,1,Image{fromFile=base..'toggle_on-0.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Beveled switch"
 sprite:newCel(layer,1,Image{fromFile=base..'toggle_on-1.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Raised symbol"
 sprite:newCel(layer,1,Image{fromFile=base..'toggle_on-2.png'},Point(0,0))
 sprite:saveAs(base..'toggle_on.aseprite')
 sprite:saveCopyAs(base..'toggle_on-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/toggle_on.png')
 sprite:close()
end
do
 local sprite=Sprite(144,144,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Thickness"
 sprite:newCel(layer,1,Image{fromFile=base..'toggle_off-0.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Beveled switch"
 sprite:newCel(layer,1,Image{fromFile=base..'toggle_off-1.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Raised symbol"
 sprite:newCel(layer,1,Image{fromFile=base..'toggle_off-2.png'},Point(0,0))
 sprite:saveAs(base..'toggle_off.aseprite')
 sprite:saveCopyAs(base..'toggle_off-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/toggle_off.png')
 sprite:close()
end
do
 local sprite=Sprite(1116,264,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original molded block surface"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_gold_372_88-0.png'},Point(0,0))
 sprite:saveAs(base..'molded_gold_372_88.aseprite')
 sprite:saveCopyAs(base..'molded_gold_372_88-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_gold_372_88.png')
 sprite:close()
end
do
 local sprite=Sprite(1116,300,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Cast block thickness"
 sprite:newCel(layer,1,Image{fromFile=base..'foundation_gold_372_88-0.png'},Point(0,0))
 sprite:saveAs(base..'foundation_gold_372_88.aseprite')
 sprite:saveCopyAs(base..'foundation_gold_372_88-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/foundation_gold_372_88.png')
 sprite:close()
end
do
 local sprite=Sprite(780,180,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original molded block surface"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_gold_260_60-0.png'},Point(0,0))
 sprite:saveAs(base..'molded_gold_260_60.aseprite')
 sprite:saveCopyAs(base..'molded_gold_260_60-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_gold_260_60.png')
 sprite:close()
end
do
 local sprite=Sprite(780,216,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Cast block thickness"
 sprite:newCel(layer,1,Image{fromFile=base..'foundation_gold_260_60-0.png'},Point(0,0))
 sprite:saveAs(base..'foundation_gold_260_60.aseprite')
 sprite:saveCopyAs(base..'foundation_gold_260_60-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/foundation_gold_260_60.png')
 sprite:close()
end
do
 local sprite=Sprite(582,180,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original molded block surface"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_gold_194_60-0.png'},Point(0,0))
 sprite:saveAs(base..'molded_gold_194_60.aseprite')
 sprite:saveCopyAs(base..'molded_gold_194_60-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_gold_194_60.png')
 sprite:close()
end
do
 local sprite=Sprite(582,216,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Cast block thickness"
 sprite:newCel(layer,1,Image{fromFile=base..'foundation_gold_194_60-0.png'},Point(0,0))
 sprite:saveAs(base..'foundation_gold_194_60.aseprite')
 sprite:saveCopyAs(base..'foundation_gold_194_60-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/foundation_gold_194_60.png')
 sprite:close()
end
do
 local sprite=Sprite(1200,192,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original molded block surface"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_gold_400_64-0.png'},Point(0,0))
 sprite:saveAs(base..'molded_gold_400_64.aseprite')
 sprite:saveCopyAs(base..'molded_gold_400_64-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_gold_400_64.png')
 sprite:close()
end
do
 local sprite=Sprite(1200,228,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Cast block thickness"
 sprite:newCel(layer,1,Image{fromFile=base..'foundation_gold_400_64-0.png'},Point(0,0))
 sprite:saveAs(base..'foundation_gold_400_64.aseprite')
 sprite:saveCopyAs(base..'foundation_gold_400_64-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/foundation_gold_400_64.png')
 sprite:close()
end
do
 local sprite=Sprite(582,180,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original molded block surface"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_violet_194_60-0.png'},Point(0,0))
 sprite:saveAs(base..'molded_violet_194_60.aseprite')
 sprite:saveCopyAs(base..'molded_violet_194_60-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_violet_194_60.png')
 sprite:close()
end
do
 local sprite=Sprite(582,216,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Cast block thickness"
 sprite:newCel(layer,1,Image{fromFile=base..'foundation_violet_194_60-0.png'},Point(0,0))
 sprite:saveAs(base..'foundation_violet_194_60.aseprite')
 sprite:saveCopyAs(base..'foundation_violet_194_60-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/foundation_violet_194_60.png')
 sprite:close()
end
do
 local sprite=Sprite(840,162,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original molded block surface"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_violet_280_54-0.png'},Point(0,0))
 sprite:saveAs(base..'molded_violet_280_54.aseprite')
 sprite:saveCopyAs(base..'molded_violet_280_54-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_violet_280_54.png')
 sprite:close()
end
do
 local sprite=Sprite(840,198,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Cast block thickness"
 sprite:newCel(layer,1,Image{fromFile=base..'foundation_violet_280_54-0.png'},Point(0,0))
 sprite:saveAs(base..'foundation_violet_280_54.aseprite')
 sprite:saveCopyAs(base..'foundation_violet_280_54-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/foundation_violet_280_54.png')
 sprite:close()
end
do
 local sprite=Sprite(756,204,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original molded block surface"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_violet_252_68-0.png'},Point(0,0))
 sprite:saveAs(base..'molded_violet_252_68.aseprite')
 sprite:saveCopyAs(base..'molded_violet_252_68-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_violet_252_68.png')
 sprite:close()
end
do
 local sprite=Sprite(756,240,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Cast block thickness"
 sprite:newCel(layer,1,Image{fromFile=base..'foundation_violet_252_68-0.png'},Point(0,0))
 sprite:saveAs(base..'foundation_violet_252_68.aseprite')
 sprite:saveCopyAs(base..'foundation_violet_252_68-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/foundation_violet_252_68.png')
 sprite:close()
end
do
 local sprite=Sprite(1380,1200,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original beveled block frame"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_panel_400-0.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Recessed interior"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_panel_400-1.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Four original round studs"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_panel_400-2.png'},Point(0,0))
 sprite:saveAs(base..'molded_panel_400.aseprite')
 sprite:saveCopyAs(base..'molded_panel_400-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_panel_400.png')
 sprite:close()
end
do
 local sprite=Sprite(1380,1404,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original beveled block frame"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_panel_468-0.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Recessed interior"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_panel_468-1.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Four original round studs"
 sprite:newCel(layer,1,Image{fromFile=base..'molded_panel_468-2.png'},Point(0,0))
 sprite:saveAs(base..'molded_panel_468.aseprite')
 sprite:saveCopyAs(base..'molded_panel_468-roundtrip.png')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/molded_panel_468.png')
 sprite:close()
end
