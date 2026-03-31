using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;

public class SceneBuilder : MonoBehaviour
{
    [MenuItem("ProjectSS/Build Professional Scene Structure")]
    public static void BuildScene()
    {
        // 1. Root 폴더 생성
        GameObject systemRoot = CreateRoot("-- SYSTEM --");
        GameObject worldRoot = CreateRoot("-- WORLD (Grid 기반) --");
        GameObject entitiesRoot = CreateRoot("-- ENTITIES --");
        GameObject interactRoot = CreateRoot("-- INTERACTABLES --");
        GameObject lightRoot = CreateRoot("-- LIGHTING & VFX --");
        GameObject uiRoot = CreateRoot("-- UI (Canvas) --");

        // 2. WORLD 구성
        GameObject gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(worldRoot.transform);
        gridGO.AddComponent<Grid>();

        CreateTilemapLayer(gridGO.transform, "Floor", 0, false);
        GameObject walls = CreateTilemapLayer(gridGO.transform, "Walls", 1, true);
        CreateTilemapLayer(gridGO.transform, "Decorations", 2, false);

        // 3. UI 구성
        GameObject overlay = CreateCanvas(uiRoot.transform, "UI_Overlay_Canvas", 0);
        GameObject hudPanel = new GameObject("HUD_Panel");
        hudPanel.transform.SetParent(overlay.transform);
        // 여기서 슬라이더 등 추가 가능

        // 4. LIGHTING 구성
        GameObject globalLight = new GameObject("GlobalLight_2D");
        globalLight.transform.SetParent(lightRoot.transform);
        var light = globalLight.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 0.15f; // 아주 어둡게

        Debug.Log("Professional Scene Structure Build Completed!");
    }

    private static GameObject CreateRoot(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    private static GameObject CreateTilemapLayer(Transform parent, string name, int order, bool hasCollider)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = order;

        if (hasCollider)
        {
            var col = go.AddComponent<TilemapCollider2D>();
            var composite = go.AddComponent<CompositeCollider2D>();
            col.usedByComposite = true;
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            
            var rb = go.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            // 시야 차단용 Shadow Caster 2D (유니티 버전에 따라 컴포넌트 이름 확인 필요)
            go.AddComponent<ShadowCaster2D>();
        }
        return go;
    }

    private static GameObject CreateCanvas(Transform parent, string name, int sortOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }
}
#endif
