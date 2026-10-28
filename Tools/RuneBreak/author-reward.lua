local root=app.params['root']
local base=root..'/Tools/RuneBreak/Source/BlockPunch/Surface/'
do
 local sprite=Sprite(1440,1956,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Original chassis without gate grooves"
 sprite:newCel(layer,1,Image{fromFile=base..'cabinet_clean-layer-0.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Existing recessed storage"
 sprite:newCel(layer,1,Image{fromFile=base..'cabinet_clean-layer-1.png'},Point(0,0))
 sprite:saveAs(base..'cabinet_clean.aseprite')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/cabinet_clean.png')
 sprite:close()
end
do
 local sprite=Sprite(960,750,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Soft contact shadow"
 sprite:newCel(layer,1,Image{fromFile=base..'reward_coins-layer-0.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Coin stack 1"
 sprite:newCel(layer,1,Image{fromFile=base..'reward_coins-layer-1.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Coin stack 2"
 sprite:newCel(layer,1,Image{fromFile=base..'reward_coins-layer-2.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Coin stack 3"
 sprite:newCel(layer,1,Image{fromFile=base..'reward_coins-layer-3.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Coin stack 4"
 sprite:newCel(layer,1,Image{fromFile=base..'reward_coins-layer-4.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Coin stack 5"
 sprite:newCel(layer,1,Image{fromFile=base..'reward_coins-layer-5.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Coin stack 6"
 sprite:newCel(layer,1,Image{fromFile=base..'reward_coins-layer-6.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Coin stack 7"
 sprite:newCel(layer,1,Image{fromFile=base..'reward_coins-layer-7.png'},Point(0,0))
 local layer=sprite:newLayer()
 layer.name="Leaning foreground coins"
 sprite:newCel(layer,1,Image{fromFile=base..'reward_coins-layer-8.png'},Point(0,0))
 sprite:saveAs(base..'reward_coins.aseprite')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_coins.png')
 sprite:close()
end
do
 local sprite=Sprite(1320,1320,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Soft golden rays"
 sprite:newCel(layer,1,Image{fromFile=base..'reward_rays-layer-0.png'},Point(0,0))
 sprite:saveAs(base..'reward_rays.aseprite')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_rays.png')
 sprite:close()
end
do
 local sprite=Sprite(480,168,ColorMode.RGB)
 local layer=sprite.layers[1]
 layer.name="Glossy navy currency bar"
 sprite:newCel(layer,1,Image{fromFile=base..'reward_wallet-layer-0.png'},Point(0,0))
 sprite:saveAs(base..'reward_wallet.aseprite')
 sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_wallet.png')
 sprite:close()
end
