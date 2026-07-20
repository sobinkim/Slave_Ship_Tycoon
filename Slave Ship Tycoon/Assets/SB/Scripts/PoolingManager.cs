using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts
{
    public interface IPoolable
    {
        void OnSpawnedFromPool();
        void OnDespawnedToPool();
    }

    [System.Serializable]
    public class PoolPrefabData
    {
        public GameObject Prefab;
        public int PreloadCount;
        public Transform Parent;
    }

    [DisallowMultipleComponent]
    public class PoolingManager : MonoBehaviour
    {
        public static PoolingManager Instance { get; private set; }

        [SerializeField] private PoolPrefabData[] preloadPrefabs;

        private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> prefabByInstance = new Dictionary<GameObject, GameObject>();
        private readonly Dictionary<GameObject, Transform> parentByPrefab = new Dictionary<GameObject, Transform>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            PreloadPools();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
                return null;

            if (!pools.ContainsKey(prefab))
                CreatePool(prefab, 0, parent);

            GameObject instance = pools[prefab].Count > 0
                ? pools[prefab].Dequeue()
                : CreateInstance(prefab, parentByPrefab[prefab]);

            Transform instanceTransform = instance.transform;
            instanceTransform.SetParent(parent, false);
            instanceTransform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            NotifySpawned(instance);
            return instance;
        }

        public T Get<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            GameObject instance = Get(prefab.gameObject, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public void Release(Component component)
        {
            if (component == null)
                return;

            Release(component.gameObject);
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
                return;

            if (!prefabByInstance.TryGetValue(instance, out GameObject prefab))
            {
                Debug.LogWarning($"{instance.name} is not managed by {nameof(PoolingManager)}.");
                Destroy(instance);
                return;
            }

            NotifyDespawned(instance);
            instance.SetActive(false);
            instance.transform.SetParent(parentByPrefab[prefab], false);
            pools[prefab].Enqueue(instance);
        }

        public void Clear()
        {
            foreach (Queue<GameObject> pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject instance = pool.Dequeue();

                    if (instance != null)
                        Destroy(instance);
                }
            }

            pools.Clear();
            prefabByInstance.Clear();
            parentByPrefab.Clear();
        }

        private void PreloadPools()
        {
            foreach (PoolPrefabData data in preloadPrefabs)
            {
                if (data == null || data.Prefab == null)
                    continue;

                CreatePool(data.Prefab, data.PreloadCount, data.Parent);
            }
        }

        private void CreatePool(GameObject prefab, int preloadCount, Transform parent)
        {
            if (pools.ContainsKey(prefab))
                return;

            pools.Add(prefab, new Queue<GameObject>());
            parentByPrefab.Add(prefab, parent != null ? parent : transform);

            for (int i = 0; i < preloadCount; i++)
            {
                GameObject instance = CreateInstance(prefab, parentByPrefab[prefab]);
                instance.SetActive(false);
                pools[prefab].Enqueue(instance);
            }
        }

        private GameObject CreateInstance(GameObject prefab, Transform parent)
        {
            GameObject instance = Instantiate(prefab, parent);
            prefabByInstance.Add(instance, prefab);
            return instance;
        }

        private void NotifySpawned(GameObject instance)
        {
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);

            foreach (IPoolable poolable in poolables)
                poolable.OnSpawnedFromPool();
        }

        private void NotifyDespawned(GameObject instance)
        {
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);

            foreach (IPoolable poolable in poolables)
                poolable.OnDespawnedToPool();
        }
    }
}
