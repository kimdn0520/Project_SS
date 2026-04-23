using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class UltraFixVisibilitySimple
{
    public static void Execute()
    {
        Debug.Log("--- 가시성 단순 복구 시작 ---");

        // 1. 검(Sword) 프리팹 - 모든 자식 SpriteRenderer의 소팅 오더 50으로 강제
        string swordPath = "Assets/Resources/Prefabs/Sword.prefab";
        GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(swordPath);
        if (swordPrefab != null)
        {
            var srs = swordPrefab.GetComponentsInChildren<SpriteRenderer>(true);
            foreach(var sr in srs)
            {
                sr.sortingOrder = 50; 
                sr.gameObject.layer = 0; 
                
                Material litMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Lit-Default.mat");
                if (litMat == null) litMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
                sr.sharedMaterial = litMat;
            }
            EditorUtility.SetDirty(swordPrefab);
            PrefabUtility.SavePrefabAsset(swordPrefab);
            Debug.Log("Sword 프리팹 교정 완료 (Order 50)");
        }

        // 2. 바닥(Floor) - 소팅 오더 -50 및 Lit 머티리얼
        var floorObj = GameObject.Find("-- WORLD (Grid 기반) --/Grid/Floor");
        if (floorObj != null)
        {
            var tr = floorObj.GetComponent<TilemapRenderer>();
            tr.sortingOrder = -50;

            Material litMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Lit-Default.mat");
            if (litMat == null) litMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
            tr.sharedMaterial = litMat;
            
            EditorUtility.SetDirty(floorObj);
            Debug.Log("Floor 교정 완료 (Order -50)");
        }

        // 3. 글로벌 조명 리프레시
        var globalLight = GameObject.Find("-- LIGHTING & VFX --/GlobalLight_2D");
        if (globalLight != null)
        {
            var l = globalLight.GetComponent<Light2D>();
            l.intensity = 0.5f; // 가시성을 위해 인텐시티 대폭 상향
            EditorUtility.SetDirty(globalLight);
        }

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
        Debug.Log("--- 복구 작업 완료 ---");
    }
}
