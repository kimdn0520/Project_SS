BLOCK PUNCH / MAINPAGE AND PLAYPAGE ART

Source: the user's Desktop/block-punch.html, preserved verbatim here.
The embedded engraved-* PNGs are the exact block art from the supplied image.
BlockPunch.aseprite contains all 20 directional/stored variants.
LongGate-*.aseprite separates the original continuous gate body and the two
matching round studs copied from the block. The light direction stays fixed.

Reproduction, from the Unity project root:
  node Tools/RuneBreak/extract-block-punch.cjs
  Open blocks-master.png in Aseprite, save BlockPunch.aseprite and export
  blocks-roundtrip.png (the exporter requires a pixel-identical round trip).
  Aseprite -b --script-param root=<absolute source directory>
    --script Tools/RuneBreak/author-punch-gates.lua
  node Tools/RuneBreak/export-block-punch.cjs
  node Tools/RuneBreak/export-audio.cjs rebound swoosh catch
  Unity menu: Rune Break / Apply Block Punch to PlayPage only

The normal RuneProjectBuilder also retains this skin when rebuilding.
Original RuneArt remains preserved. MainPage now shares the Block Punch blocks
and long gates; RuneLobbyPunchAuthoring applies its glossy navy/violet skin.
Its panel recipes are in RuneLobbySkinAuthoring and editable Aseprite masters
are in this directory's Lobby subfolder. Use author-lobby-panels.lua to refresh
those masters after changing a recipe. SpriteManager uses the same registry
for both atlases. No new manager or runtime hierarchy.

The lobby showcase plays and loops automatically, with no pointer interaction,
surrounding frame, cell wells, subtitle or tap hint. Glossy block fragments
use a fixed 96-shard buffer in RuneFxGraphic (four brief shards in reduced
motion). This effect is invoked only by the lobby showcase.
Currency icons are layered glossy sprites in Lobby/Icons. Reproduce them with:
  node Tools/RuneBreak/export-lobby-icons.cjs
  Aseprite -b --script-param root=<absolute project directory>
    --script Tools/RuneBreak/author-lobby-icons.lua
  node Tools/RuneBreak/verify-lobby-icons.cjs
  Unity menu: Rune Break / Apply Block Punch to MainPage
The icons share the point-filtered, uncompressed BlockPunch atlas.

Block Crash buttons and popup art:
  node Tools/RuneBreak/export-block-ui.cjs
  Aseprite -b --script-param root=<absolute project directory>
    --script Tools/RuneBreak/author-block-ui.lua
  node Tools/RuneBreak/verify-block-ui.cjs
  Unity menu: Rune Break / Apply Block Crash buttons and popups
The original stored gold/blue blocks supply curved bevels, material highlights
and contact shadows. Violet is a muted hue variant of the blue material.
Each button size is baked separately so its studs stay round. Aseprite masters
live in Lobby/Blocks. MainPage shows only Lv. N from the existing save value;
settings and win/loss overlays retain their existing callbacks and managers.
BlockUiQA checks the real press/release, level text, both settings contexts,
audio toggles, close/resume, result layouts and existing result callbacks.

Verification entry points (Tools/RuneBreak/PunchQA.cs via the Unity bridge):
  Motion: 3240 HTML samples plus all existing rules/oracle checks.
  Runtime: every campaign/mechanic level in full and reduced motion, rapid
    board taps, slot reservation, pool returns, and solved outcomes.
  FinalChecks: authored references, interrupted flights, reuse of all 48
    instances, no hierarchy growth, single-number gate counter, legacy preservation.
  Film: actual Unity frame captures; export-punch-preview.lua creates the GIF.

MainPage checks in Tools/RuneBreak/LobbyPunchQA.cs cover five tab states,
automatic showcase and reset, ignored center taps, bounded fragment lifetime,
six-instance pool reuse, press/release behavior,
settings pause, unchanged campaign progress and safe areas at 540x960,
540x1200 and 768x1024. StartPlay/VerifyPlay verify the actual button transition.

QA artifacts live in Tools/RuneBreak/QA/BlockPunch-* and block-punch-*.json.
Comfy Desktop backend is connected at http://127.0.0.1:8188.
Run node Tools/RuneBreak/comfy-connection-test.cjs to verify upload, workflow
execution and PNG output through its local API. The workflow is preserved in
comfy-connection-api.json and the last result in QA/comfy-connection.json.
This is an image I/O connection test, not a generative art pass; it does not
replace the pixel-verified game assets. The current server exposes no local
checkpoint, diffusion model or text encoder. Desktop window automation remains
unapproved, but the local API successfully executes jobs.
