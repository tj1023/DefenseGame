using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Managers
{
    /// <summary>
    /// 객체에 붙어 어떤 프리팹에서 생성되었는지 정보를 저장하는 컴포넌트
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public GameObject originPrefab;
    }

    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        private readonly Dictionary<GameObject, IObjectPool<GameObject>> _pools = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            IObjectPool<GameObject> pool = GetPool(prefab);
            GameObject obj = pool.Get();
            
            obj.transform.position = position;
            obj.transform.rotation = rotation;

            // IPoolable 구현체가 있다면 실행
            if (obj.TryGetComponent<IPoolable>(out var poolable))
                poolable.OnSpawn();

            return obj;
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null) return;

            // PooledObject 컴포넌트를 통해 원본 프리팹 확인
            if (obj.TryGetComponent<PooledObject>(out var pooledObj))
            {
                if (_pools.TryGetValue(pooledObj.originPrefab, out var pool))
                {
                    pool.Release(obj);
                    return;
                }
            }

            // 풀 정보가 없으면 일반 파괴
            Destroy(obj);
        }

        private IObjectPool<GameObject> GetPool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool))
                return pool;

            pool = new ObjectPool<GameObject>(
                createFunc: () => 
                {
                    GameObject obj = Instantiate(prefab);
                    // 생성 시 본인의 프리팹 정보를 심어둠
                    var pooled = obj.AddComponent<PooledObject>();
                    pooled.originPrefab = prefab;
                    return obj;
                },
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: 20,
                maxSize: 1000
            );

            _pools.Add(prefab, pool);
            return pool;
        }
    }
}
