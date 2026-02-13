using System.Collections.Generic;
using UnityEngine;

public class PoolingManager : Singleton<PoolingManager>
{
    // Dictionary lưu Pool theo Prefab
    private Dictionary<GameObject, Pool> _pools = new Dictionary<GameObject, Pool>();
    // Dictionary để tra cứu: Instance này thuộc về Prefab nào?
    // Key: InstanceID của object được spawn, Value: Prefab gốc
    private Dictionary<int, GameObject> _instanceToPrefab = new Dictionary<int, GameObject>();

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!_pools.ContainsKey(prefab))
        {
            // Tạo Pool mới (Pool class không còn là MonoBehaviour)
            _pools.Add(prefab, new Pool(prefab));
        }

        // Lấy object từ pool
        GameObject instance = _pools[prefab].Get();

        // Thiết lập vị trí/xoay
        instance.transform.SetPositionAndRotation(pos, rot);
        instance.transform.SetParent(parent);

        // Lưu vết: Object này (ID) thuộc về Prefab này
        if (!_instanceToPrefab.ContainsKey(instance.GetInstanceID()))
        {
            _instanceToPrefab.Add(instance.GetInstanceID(), prefab);
        }

        return instance;
    }

    // Overload đơn giản
    public GameObject Spawn(GameObject prefab)
    {
        return Spawn(prefab, Vector3.zero, Quaternion.identity);
    }

    // Overload trả về Component
    public T Spawn<T>(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null) where T : Component
    {
        GameObject obj = Spawn(prefab, pos, rot, parent);
        return obj.GetComponent<T>();
    }

    public void Despawn(GameObject instance)
    {

        if (_instanceToPrefab.TryGetValue(instance.GetInstanceID(), out GameObject prefab))
        {
            // Tìm thấy prefab gốc, trả về đúng pool của nó
            if (_pools.TryGetValue(prefab, out Pool pool))
            {
                pool.Release(instance);
            }
            else
            {
                // Trường hợp hiếm: Có map nhưng mất pool, thì destroy luôn
                Destroy(instance);
            }
        }
        else
        {
            // Object này không được spawn từ PoolingManager, destroy thường
            Debug.LogWarning($"Object {instance.name} không thuộc PoolingManager. Destroying normally.");
            Destroy(instance);
        }
    }
}

// Bỏ MonoBehaviour, class C# thuần sẽ nhẹ hơn và dùng được 'new'
public class Pool
{
    private readonly Queue<GameObject> _inactiveObjects = new Queue<GameObject>();
    private readonly GameObject _prefab;

    public Pool(GameObject prefab)

    {
        _prefab = prefab;
    }

    // Hàm lấy object ra (Spawn)
    public GameObject Get()
    {
        // Nếu trong kho có đồ cũ, lấy ra dùng lại
        while (_inactiveObjects.Count > 0)
        {
            GameObject obj = _inactiveObjects.Dequeue();

            // Kiểm tra null phòng trường hợp object bị destroy bên ngoài (ví dụ đổi scene)
            if (obj != null)
            {
                obj.SetActive(true);
                return obj;
            }
        }

        // Nếu kho hết, tạo mới
        // KHÔNG Enqueue ở đây. Chỉ Enqueue khi Despawn.
        GameObject newObj = Object.Instantiate(_prefab);
        return newObj;
    }

    // Hàm cất object đi (Despawn)
    public void Release(GameObject instance)
    {
        instance.SetActive(false);
        _inactiveObjects.Enqueue(instance);
    }
}