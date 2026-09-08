using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SpriteManager : SingletonMonoBehaviour<SpriteManager>
{
    [SerializeField]
    private SpriteAtlasSO spriteAtlasData;

    private Dictionary<string, Sprite> _spriteDic = new Dictionary<string, Sprite>();

    public bool HasData => spriteAtlasData != null;

    protected override void Awake()
    {
        base.Awake();
    }

    public void Initialize()
    {
        if (spriteAtlasData == null)
        {
            Debug.LogWarning("[SpriteManager] SpriteAtlasSO가 아직 할당되지 않아 초기화를 건너뜁니다.");
            return;
        }

        foreach (SpriteAtlas atlas in spriteAtlasData.Atlases)
        {
            if (atlas == null) continue;
            Sprite[] sprites = new Sprite[atlas.spriteCount];
            atlas.GetSprites(sprites);

            foreach (Sprite sprite in sprites)
            {
                if (sprite == null) continue;
                string cleanedName = sprite.name.Replace("(Clone)", "");

                if (_spriteDic.ContainsKey(cleanedName))
                {
                    continue;
                }

                _spriteDic.Add(cleanedName, sprite);
            }
        }
    }

    public Sprite Get(string spriteName)
    {
        if (_spriteDic.TryGetValue(spriteName, out Sprite sprite))
        {
            return sprite;
        }
        else
        {
            Debug.LogWarning($"[SpriteManager] '{spriteName}' 이름의 스프라이트를 찾을 수 없습니다.");
            return null;
        }
    }
}
