using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldManager : MonoBehaviour
{
    public Grid Grid { get; private set; }
    public Tilemap FloorTilemap { get; private set; }
    public Tilemap WallTilemap { get; private set; }

    [SerializeField] private Tile floorTile;
    [SerializeField] private Tile wallTile;

    private void Awake()
    {
        SetupWorld();
    }

    private void SetupWorld()
    {
        // 1. Grid 생성
        GameObject gridGO = new GameObject("Grid");
        Grid = gridGO.AddComponent<Grid>();

        // 2. Floor Tilemap 생성
        GameObject floorGO = new GameObject("Floor");
        floorGO.transform.SetParent(gridGO.transform);
        FloorTilemap = floorGO.AddComponent<Tilemap>();
        floorGO.AddComponent<TilemapRenderer>();

        // 3. Wall Tilemap 생성
        GameObject wallGO = new GameObject("Walls");
        wallGO.transform.SetParent(gridGO.transform);
        WallTilemap = wallGO.AddComponent<Tilemap>();
        wallGO.AddComponent<TilemapRenderer>();
        var wallCollider = wallGO.AddComponent<TilemapCollider2D>();
        wallGO.AddComponent<CompositeCollider2D>();
        var rb = wallGO.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        wallCollider.usedByComposite = true;

        GenerateSimpleMap();
    }

    private void GenerateSimpleMap()
    {
        // 간단한 방 모양 맵 생성 (20x20)
        int size = 20;
        for (int x = -size; x <= size; x++)
        {
            for (int y = -size; y <= size; y++)
            {
                // 바닥 깔기
                // FloorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);

                // 벽 세우기 (테두리)
                if (x == -size || x == size || y == -size || y == size)
                {
                    // WallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
                }
            }
        }
    }
}
