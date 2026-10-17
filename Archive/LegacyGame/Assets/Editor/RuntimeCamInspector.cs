using UnityEngine;
using UnityEngine.UI;

public static class RuntimeCamInspector
{
    public static void Inspect()
    {
        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"=== Total Cameras: {cams.Length} ===");
        foreach (var c in cams)
        {
            Debug.Log($"Camera: {c.name}, active: {c.gameObject.activeInHierarchy}, enabled: {c.enabled}, clear: {c.clearFlags}, bg: {c.backgroundColor}, ortho: {c.orthographic}, size: {c.orthographicSize}, pos: {c.transform.position}, rect: {c.rect}");
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"=== Total Canvases: {canvases.Length} ===");
        foreach (var cv in canvases)
        {
            Debug.Log($"Canvas: {cv.name}, active: {cv.gameObject.activeInHierarchy}, renderMode: {cv.renderMode}, worldCam: {cv.worldCamera?.name}, order: {cv.sortingOrder}");
        }
    }
}
