using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SpriteManager : SingletonMonoBehaviour<SpriteManager>
{
    [SerializeField]
    private SpriteAtlasSO spriteAtlasData;

    private readonly Dictionary<string, Sprite> _spriteDic = new Dictionary<string, Sprite>();

    public bool HasData => spriteAtlasData != null;

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    public void Initialize()
    {
        _spriteDic.Clear();
        // Folder-packed atlases are the source of runtime equipment sprites.
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
                    // The registry lists gameplay equipment first. UI thumbnails can have
                    // the same name; they must not replace a character's equipped sprite.
                    if (!_spriteDic.ContainsKey(cleanedName)) Register(cleanedName, sprite);
                    Register(atlas.name + "/" + cleanedName, sprite);
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
