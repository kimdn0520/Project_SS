local root=app.params['root']
local base=root..'/Tools/RuneBreak/Source/BlockPunch/Lobby/Icons/'
do
 local s=Sprite(144,144,ColorMode.RGB)
 local layer=s.layers[1]
 layer.name="Contact shadow"
 s:newCel(layer,1,Image{fromFile=base..'coin-0.png'},Point(0,0))
 local layer=s:newLayer()
 layer.name="Milled gold thickness"
 s:newCel(layer,1,Image{fromFile=base..'coin-1.png'},Point(0,0))
 local layer=s:newLayer()
 layer.name="Rounded outer bevel"
 s:newCel(layer,1,Image{fromFile=base..'coin-2.png'},Point(0,0))
 local layer=s:newLayer()
 layer.name="Vivid yellow recessed face"
 s:newCel(layer,1,Image{fromFile=base..'coin-3.png'},Point(0,0))
 local layer=s:newLayer()
 layer.name="Chamfered star relief"
 s:newCel(layer,1,Image{fromFile=base..'coin-4.png'},Point(0,0))
 local layer=s:newLayer()
 layer.name="Fine rim reflection"
 s:newCel(layer,1,Image{fromFile=base..'coin-5.png'},Point(0,0))
 s:saveAs(base..'coin.aseprite')
 s:saveCopyAs(base..'coin-roundtrip.png')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Icons/lobby_coin.png')
 s:close()
end
