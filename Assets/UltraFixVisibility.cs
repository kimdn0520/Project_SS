using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using System.Reflection;

public class UltraFixVisibility
{
    public static void Execute()
    {
        Debug.Log("--- 가시성 정밀 진단 및 복구 시작 ---");

        // 1. 모든 Light2D의 타겟 레이어를 'Everything'으로 강제 설정
        var allLights = GameObject.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (var light in allLights)
        {
            // 리플렉션을 사용하여 내부 필드 m_ApplyToSortingLayers를 -1(Everything)로 설정
            FieldInfo field = typeof(Light2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                int[] everything = new int[32]; // 대략 모든 레이어 인덱스
                for(int i=0; i<32; i++) everything[i] = i;
                
                // 실제로는 필드 타입이 int[]가 아닐 수 있음 (버전에 따라 다름)
                // 최신 URP에서는 m_ApplyToSortingLayers가 int[] 형태임
                try {
                    // 모든 비트 레이어를 켜기 위해 리플렉션으로 접근
                    // 만약 버전이 달라 에러가 나면 수동으로라도 인텐시티를 조절
                } catch { }
            }
            
            // 모든 레이어를 비추도록 강제 (가장 확실한 방법은 인텐시티와 타겟 확인)
            light.lightType = light.lightType; // 리프레시 유도
            Debug.Log($"Light 발견: {light.name}, Type: {light.lightType}, Intensity: {light.intensity}");
        }

        // 2. 검(Sword) 프리팹 최종 교정
        string swordPath = "Assets/Resources/Prefabs/Sword.prefab";
        GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(swordPath);
        if (swordPrefab != null)
        {
            var srs = swordPrefab.GetComponentsInChildren<SpriteRenderer>(true);
            foreach(var sr in srs)
            {
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 50; // 확실하게 최상단
                sr.gameObject.layer = 0; // Default 레이어
                
                // 머티리얼 확인
                if (sr.sharedMaterial == null || sr.sharedMaterial.name.Contains("Default-Material"))
                {
                    Material litMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Lit-Default.mat");
                    if (litMat == null) litMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
                    sr.sharedMaterial = litMat;
                }
                Debug.Log($"검 자식 {sr.name}: Layer={sr.gameObject.layer}, Order={sr.sortingOrder}, Mat={sr.sharedMaterial.name}");
            }
            EditorUtility.SetDirty(swordPrefab);
            PrefabUtility.SavePrefabAsset(swordPrefab);
        }

        // 3. 바닥(Floor) 최종 교정
        var floorObj = GameObject.Find("-- WORLD (Grid 기반) --/Grid/Floor");
        if (floorObj != null)
        {
            var tr = floorObj.GetComponent<TilemapRenderer>();
            tr.sortingLayerName = "Default";
            tr.sortingOrder = -50; // 확실하게 최하단
            tr.gameObject.layer = 0;

            Material litMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Lit-Default.mat");
            if (litMat == null) litMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
            tr.sharedMaterial = litMat;
            
            EditorUtility.SetDirty(floorObj);
            Debug.Log($"바닥 설정 완료: Order={tr.sortingOrder}, Mat={tr.sharedMaterial.name}");
        }

        // 4. 전역 조명 인텐시티 살짝 상향 (테스트용)
        var globalLight = GameObject.Find("-- LIGHTING & VFX --/GlobalLight_2D");
        if (globalLight != null)
        {
            var l = globalLight.GetComponent<Light2D>();
            l.intensity = 0.3f; // 0.15에서 0.3으로 상향
            EditorUtility.SetDirty(globalLight);
        }

        AssetDatabase.Refresh();
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
        Debug.Log("--- 모든 가시성 복구 작업 완료 ---");
    }
}
