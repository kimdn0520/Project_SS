-- Preserve editable Aseprite masters of the deterministic glossy UI skin recipe.
local root=app.params['root']
for _,name in ipairs({'violet','blue','gold','red','green','panel','inset','base'}) do
  local sprite=app.open(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/lobby_'..name..'.png')
  sprite.layers[1].name='Glossy bevel - RuneLobbySkinAuthoring recipe'
  sprite:saveAs(root..'/Tools/RuneBreak/Source/BlockPunch/Lobby/'..name..'.aseprite')
  sprite:saveCopyAs(root..'/Temp/LobbyPunchSources/'..name..'.png')
  sprite:close()
end
