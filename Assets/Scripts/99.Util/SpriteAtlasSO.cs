using UnityEngine;
using UnityEngine.U2D;

[CreateAssetMenu(fileName = "SpriteAtlasScriptable", menuName = "ProjectSS/Sprite Atlas Registry")]
public class SpriteAtlasSO : ScriptableObject
{
    [SerializeField]
    private SpriteAtlas[] atlases;

    public SpriteAtlas[] Atlases => atlases;
}
