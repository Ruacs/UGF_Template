using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEntry = Lokas.GameEntry;
using UnityGameFramework.Runtime;

namespace GameFramework.ObjectPool
{
    public class ObjectPoolComponent<T> : GameFrameworkComponent where T : Component,IPoolable
    {
        [SerializeField] protected T prefab;
        [SerializeField] protected Transform root;

        [SerializeField] protected int capacity = 200;
        [SerializeField] protected int prewarmCount = 50;

        protected IObjectPool<PoolObject<T>> pool;
        protected readonly List<T> activeList = new();

        public int ActiveCount => activeList.Count;
        public List<T> ActiveList => activeList;

        protected virtual void Start()
        {
            pool = GameEntry.ObjectPool.CreateSingleSpawnObjectPool<PoolObject<T>>(typeof(T).Name, capacity);

            Prewarm(prewarmCount);
        }


        protected virtual T CreateInstance()
        {
            if (prefab is Component)
            {
                var go = Object.Instantiate(prefab as Component, root);
                return go as T;
            }

            return System.Activator.CreateInstance<T>();
        }

        protected void Prewarm(int count)
        {
            int createCount = Mathf.Min(count, capacity);

            for (int i = 0; i < createCount; i++)
            {
                var item = CreateInstance();
                CallSpawn(item);
                pool.Register(PoolObject<T>.Create(item), false);
                CallRecycle(item);
            }
        }

        public T Spawn()
        {
            var obj = pool.Spawn();

            T item;
            if (obj != null)
            {
                item = (T)obj.Target;
                if (item == null)
                {
                    item = CreateInstance();
                    pool.Register(PoolObject<T>.Create(item), true);
                }
            }
            else
            {
                item = CreateInstance();
                pool.Register(PoolObject<T>.Create(item), true);
            }

            activeList.Add(item);
            CallSpawn(item);

            return item;
        }

        public void Recycle(T item)
        {
            if (item == null) return;

            CallRecycle(item);

            if (root != null && item is Component component && component != null)
                component.transform.SetParent(root, false);

            activeList.Remove(item);
            pool.Unspawn(item);
        }

        public void RecycleAll()
        {
            for (int i = activeList.Count - 1; i >= 0; i--)
            {
                Recycle(activeList[i]);
            }
        }

        private void CallSpawn(T item)
        {
            if (item is IPoolable p)
                p.OnSpawn();
        }

        private void CallRecycle(T item)
        {
            if (item is IPoolable p)
                p.OnRecycle();
        }
    }

}
