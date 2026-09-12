using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SpriteManager : SingletonMonoBehaviour<SpriteManager>
{
    [SerializeField]
    private SpriteAtlasSO spriteAtlasData;

    [Header("[Direct Registered Sprites]")]
    [SerializeField]
    private List<Sprite> registeredSprites = new List<Sprite>();

    private readonly Dictionary<string, Sprite> _spriteDic = new Dictionary<string, Sprite>();

    public bool HasData => spriteAtlasData != null || (registeredSprites != null && registeredSprites.Count > 0);

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    public void Initialize()
    {
        // 1. 인스펙터에 직접 등록된 개별 스프라이트 등록
        if (registeredSprites != null)
        {
            foreach (var sp in registeredSprites)
            {
                if (sp == null) continue;
                Register(sp.name, sp);
            }
        }

        // 2. SpriteAtlasSO의 아틀라스 스프라이트 등록
        if (spriteAtlasData != null && spriteAtlasData.Atlases != null)
        {
            foreach (SpriteAtlas atlas in spriteAtlasData.Atlases)
            {
                if (atlas == null) continue;
                Sprite[] sprites = new Sprite[atlas.spriteCount];
                atlas.GetSprites(sprites);

                foreach (Sprite sprite in sprites)
                {
                    if (sprite == null) continue;
                    string cleanedName = sprite.name.Replace("(Clone)", "");
                    Register(cleanedName, sprite);
                }
            }
        }
    }

    public void Register(Sprite sprite)
    {
        if (sprite != null)
        {
            Register(sprite.name, sprite);
        }
    }

    public void Register(string spriteName, Sprite sprite)
    {
        if (string.IsNullOrEmpty(spriteName) || sprite == null) return;

        string cleaned = spriteName.Replace("(Clone)", "");
        if (!_spriteDic.ContainsKey(cleaned))
        {
            _spriteDic.Add(cleaned, sprite);
        }
        else
        {
            _spriteDic[cleaned] = sprite;
        }
    }

    public Sprite Get(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;

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

    public bool TryGet(string spriteName, out Sprite sprite)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            sprite = null;
            return false;
        }
        return _spriteDic.TryGetValue(spriteName, out sprite);
    }
}
