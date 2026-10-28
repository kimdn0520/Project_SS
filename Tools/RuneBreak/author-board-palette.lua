local root=app.params['root']
local base=root..'/Tools/RuneBreak/Source/BlockPunch/Surface/'
local s=Sprite(1440,1956,ColorMode.RGB)
s.layers[1].name='Lavender gray molded material'
s:newCel(s.layers[1],1,Image{fromFile=base..'cabinet_lavender-source.png'},Point(0,0))
s:saveAs(base..'cabinet_lavender.aseprite')
s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/cabinet_lavender.png')
s:close()
