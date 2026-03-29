using System.Collections.Generic;
using DG.Tweening.Core.Easing;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PoolManager : SingletonMonoBehaviour<PoolManager>
{
    private readonly Dictionary<string, PoolContainer> pools = new Dictionary<string, PoolContainer>();

    private bool isInitialized = false;

    public void Initialize()
    {
        if (isInitialized) return;


        GameObject containerMaster = new GameObject("PoolContainer_Master");
        DontDestroyOnLoad(containerMaster);

        var poolSO = Resources.Load<PoolSO>("SO_Base/PoolSO");

        if (poolSO != null)
        {
            foreach (var preset in poolSO.presets)
            {
                // �̸��� ��������� ������ �̸����� ��ü
                string poolName = string.IsNullOrWhiteSpace(preset.name) ? preset.prefab.name : preset.name;

                if (pools.ContainsKey(poolName))
                {
                    Debug.LogWarning($"'{poolName}' �̸��� ���� Ǯ�����̳ʰ� �̹� �����մϴ�. �ǳʶݴϴ�.");
                    continue;
                }

                Transform containerTr = new GameObject($"{poolName} Container").transform;
                containerTr.SetParent(containerMaster.transform, false);

                pools[poolName] = new PoolContainer(poolName, preset.prefab, preset.initialCount, containerTr);
            }
        }

        isInitialized = true;
    }

    public T Get<T>(string poolName, Transform parent = null, Vector3? position = null, Quaternion? rotation = null) where T : Component
    {
        GameObject obj = Get(poolName, parent, position, rotation);
        if (obj == null) return null;

        if (obj.TryGetComponent<T>(out T component))
        {
            return component;
        }
        else
        {
            Debug.LogError($"'{poolName}' Ǯ�� �����տ� '{typeof(T)}' ������Ʈ�� �����ϴ�.");
            Return(obj);
            return null;
        }
    }

    public GameObject Get(string poolName, Transform parent = null, Vector3? position = null, Quaternion? rotation = null)
    {
        if (!isInitialized)
        {
            Debug.LogError("PoolManager�� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            return null;
        }

        if (!pools.ContainsKey(poolName))
        {
            Debug.LogError($"'{poolName}' �̸��� ���� Ǯ�� ã�� �� �����ϴ�.");
            return null;
        }

        Vector3 finalPos = position ?? Vector3.zero;
        Quaternion finalRot = rotation ?? Quaternion.identity;

        GameObject obj = pools[poolName].Get();
        obj.transform.SetParent(parent, false);
        obj.transform.SetPositionAndRotation(finalPos, finalRot);

        return obj;
    }

    public void Return(GameObject obj)
    {
        // PoolObject ������Ʈ�� ���� � Ǯ�� �����ִ��� Ȯ���մϴ�.
        if (!obj.TryGetComponent<PoolObject>(out var poolObj))
        {
            Debug.LogError($"'{obj.name}'���� PoolObject ������Ʈ�� ���� Ǯ�� ��ȯ�� �� �����ϴ�. ��� �ı��մϴ�.");
            Destroy(obj);
            return;
        }

        if (!pools.ContainsKey(poolObj.poolName))
        {
            Debug.LogError($"'{poolObj.poolName}' Ǯ�� ã�� �� ���� '{obj.name}'�� ��ȯ�� �� �����ϴ�. ��� �ı��մϴ�.");
            Destroy(obj);
            return;
        }

        pools[poolObj.poolName].Return(obj);
    }
}
