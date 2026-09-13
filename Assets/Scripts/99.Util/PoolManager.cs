using System.Collections.Generic;
using DG.Tweening.Core.Easing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class PoolManager : SingletonMonoBehaviour<PoolManager>
{
    [System.Serializable]
    public class BakedPool
    {
        public string name;
        public PoolObject[] instances;
    }

    [Header("[Inspector-prewarmed pools]")]
    [SerializeField] private bool useBakedPools;
    [SerializeField] private BakedPool[] bakedPools = System.Array.Empty<BakedPool>();
    private readonly Dictionary<string, Queue<PoolObject>> bakedQueues = new Dictionary<string, Queue<PoolObject>>();
    private readonly HashSet<PoolObject> rentedObjects = new HashSet<PoolObject>();

    public PoolObject RentCached(string name)
    {
        if (!isInitialized) Initialize();
        if (!bakedQueues.TryGetValue(name, out var queue) || queue.Count == 0) return null;
        PoolObject item = queue.Dequeue();
        rentedObjects.Add(item);
        item.gameObject.SetActive(true);
        return item;
    }

    public void ReturnCached(PoolObject item)
    {
        if (item == null || !rentedObjects.Remove(item)) return;
        item.gameObject.SetActive(false);
        bakedQueues[item.poolName].Enqueue(item);
    }

    private readonly Dictionary<string, PoolContainer> pools = new Dictionary<string, PoolContainer>();

    private bool isInitialized = false;

    public void Initialize()
    {
        if (isInitialized) return;

        if (useBakedPools)
        {
            foreach (var pool in bakedPools)
            {
                var queue = new Queue<PoolObject>();
                bakedQueues.Add(pool.name, queue);
                foreach (var item in pool.instances)
                {
                    if (item == null) continue;
                    item.poolName = pool.name;
                    item.gameObject.SetActive(false);
                    queue.Enqueue(item);
                }
            }
            isInitialized = true;
            return;
        }


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

    private void Start()
    {
        Initialize();
    }

    private Transform cachedMonsterGroup;

    private Transform GetMonsterGroup()
    {
        if (cachedMonsterGroup != null) return cachedMonsterGroup;

        GameObject entities = GameObject.Find("-- ENTITIES --");
        if (entities == null) entities = new GameObject("-- ENTITIES --");

        Transform group = entities.transform.Find("Monster_Group");
        if (group == null)
        {
            GameObject groupGO = new GameObject("Monster_Group");
            groupGO.transform.SetParent(entities.transform);
            group = groupGO.transform;
        }

        cachedMonsterGroup = group;
        return cachedMonsterGroup;
    }

    public T Get<T>(string poolName, Transform parent = null, Vector3? position = null, Quaternion? rotation = null) where T : Component
    {
        // 규칙 적용: 부모가 지정되지 않은 경우 Monster_Group 설정
        if (parent == null) parent = GetMonsterGroup();

        GameObject obj = Get(poolName, parent, position, rotation);
        if (obj == null) return null;

        if (obj.TryGetComponent<T>(out T component))
        {
            return component;
        }
        else
        {
            Debug.LogError($"'{poolName}' Ǯ տ '{typeof(T)}' Ʈ ϴ.");
            Return(obj);
            return null;
        }
    }

    public GameObject Get(string poolName, Transform parent = null, Vector3? position = null, Quaternion? rotation = null)
    {
        if (!isInitialized)
        {
            Debug.LogError("PoolManager ʱȭ ʾҽϴ.");
            return null;
        }

        if (!pools.ContainsKey(poolName))
        {
            Debug.LogError($"'{poolName}' ̸  Ǯ ã  ϴ.");
            return null;
        }

        // 규칙 적용: 부모가 지정되지 않은 경우 Monster_Group 설정
        if (parent == null) parent = GetMonsterGroup();

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
