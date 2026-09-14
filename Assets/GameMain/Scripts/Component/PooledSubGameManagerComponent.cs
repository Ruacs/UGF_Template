using System.Collections.Generic;
using GameFramework.ObjectPool;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public abstract class PooledSubGameManagerComponent<T> : SubGameManagerComponent where T : Component, IPoolable
    {
        [SerializeField] protected T prefab;
        [SerializeField] protected Transform root;
        [SerializeField] protected int capacity = 200;
        [SerializeField] protected int prewarmCount = 0;

        protected IObjectPool<PoolObject<T>> pool;
        protected readonly List<T> activeList = new();
        private Transform runtimeRoot;

        public int ActiveCount => activeList.Count;
        public List<T> ActiveList => activeList;

        protected virtual void Start()
        {
            EnsurePool();
            Prewarm(prewarmCount);
        }

        public override void ResetGame()
        {
            RecycleAll();
            base.ResetGame();
        }

        protected virtual T CreateInstance()
        {
            if (prefab == null)
            {
                Log.Warning("{0} prefab is not assigned.", typeof(T).Name);
                return null;
            }

            return Instantiate(prefab, GetPoolInstanceRoot());
        }

        protected void SetPoolRoot(Transform poolRoot)
        {
            runtimeRoot = poolRoot;
        }

        protected void Prewarm(int count)
        {
            EnsurePool();
            if (pool == null || count <= 0) return;

            int createCount = Mathf.Min(count, capacity);
            for (int i = 0; i < createCount; i++)
            {
                T item = CreateInstance();
                if (item == null) continue;

                CallSpawn(item);
                pool.Register(PoolObject<T>.Create(item), false);
                CallRecycle(item);
            }
        }

        protected T Spawn()
        {
            EnsurePool();
            if (pool == null) return null;

            PoolObject<T> obj = pool.Spawn();
            T item = obj?.Target as T;

            if (item == null)
            {
                item = CreateInstance();
                if (item == null) return null;

                pool.Register(PoolObject<T>.Create(item), true);
            }

            if (!activeList.Contains(item))
            {
                activeList.Add(item);
            }

            SetParentIfChanged(item.transform, GetPoolRuntimeRoot());

            CallSpawn(item);
            return item;
        }

        protected void Recycle(T item)
        {
            if (item == null || pool == null) return;

            CallRecycle(item);

            SetParentIfChanged(item.transform, GetPoolInstanceRoot());

            activeList.Remove(item);
            pool.Unspawn(item);
        }

        protected void RecycleAll()
        {
            for (int i = activeList.Count - 1; i >= 0; i--)
            {
                Recycle(activeList[i]);
            }
        }

        private void EnsurePool()
        {
            if (pool != null) return;
            if (GameEntry.ObjectPool == null) return;

            string poolName = $"{GetType().Name}.{typeof(T).Name}";
            pool = GameEntry.ObjectPool.CreateSingleSpawnObjectPool<PoolObject<T>>(poolName, capacity);
        }

        protected virtual Transform GetPoolInstanceRoot()
        {
            return root != null ? root : transform;
        }

        protected virtual Transform GetPoolRuntimeRoot()
        {
            return runtimeRoot != null ? runtimeRoot : GetPoolInstanceRoot();
        }

        private static void SetParentIfChanged(Transform itemTransform, Transform targetParent)
        {
            if (itemTransform == null || targetParent == null || itemTransform.parent == targetParent)
            {
                return;
            }

            itemTransform.SetParent(targetParent, false);
        }

        private static void CallSpawn(T item)
        {
            item.OnSpawn();
        }

        private static void CallRecycle(T item)
        {
            item.OnRecycle();
        }
    }
}
