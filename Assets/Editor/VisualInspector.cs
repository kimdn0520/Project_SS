using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class VisualInspector
{
    public static void Main()
    {
        Debug.Log("=== All SpriteRenderers in Scene ===");
        var srs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (var sr in srs)
        {
            Debug.Log($"SR: '{sr.gameObject.name}', Sprite: '{sr.sprite?.name}', Order: {sr.sortingOrder}, Pos: {sr.transform.position}, Scale: {sr.transform.lossyScale}, Bounds: {sr.bounds}");
        }

        Debug.Log("=== All UI Images in Scene ===");
        var imgs = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
        foreach (var img in imgs)
        {
            RectTransform rt = img.GetComponent<RectTransform>();
            Debug.Log($"UI Image: '{img.gameObject.name}', Sprite: '{img.sprite?.name}', Color: {img.color}, Rect: {rt.rect}, WorldPos: {rt.position}");
        }
    }
}
