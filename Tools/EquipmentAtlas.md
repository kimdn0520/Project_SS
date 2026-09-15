# 장비 아틀라스

- 사용 중인 무기/갑옷/투구/부적 텍스처 11개: `Assets/Textures/Equipment`.
- 폴더 패킹: `Assets/SpriteAtlas/Equipment.spriteatlasv2`. Objects for Packing에는 Equipment 폴더 하나만 등록한다.
- SpriteManager 레지스트리: `Assets/SpriteAtlas/EquipmentAtlasData.asset`. 기존 사용자 SpriteAtlasSO를 이동하여 재사용했다.
- SpriteManager의 개별 registeredSprites 목록을 제거했다. 기존 이름(spriteKey)으로 아틀라스에서 조회한다.
- Unity AssetDatabase.MoveAsset로 이미지와 meta를 함께 이동하여 GUID와 기존 참조를 유지했다. 부적 이미지는 기존 256px 소스를 사용한다.
- 프로젝트 Sprite Packer는 SpriteAtlasV2를 사용한다. 회전/타이트 패킹/mipmap은 끄고 padding 4로 설정했다.
- 신규 장비 텍스처를 Equipment 폴더에 넣으면 재임포트/빌드 시 자동 패킹된다. 중복 파일명(스프라이트 이름)은 피한다. 파티클 전용 텍스처는 넣지 않는다.
- 검증: Unity 컴파일 통과. Play Mode에서 장비 키 11개 모두 실제 packed sprite로 조회되고 장비 아이콘 참조가 유지됨을 확인했다. `PrototypeQA/equipment-atlas.txt` 참조. 모바일 빌드는 미실행.
