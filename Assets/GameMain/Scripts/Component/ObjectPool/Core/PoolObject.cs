using GameFramework;
using UnityEngine;

namespace GameFramework.ObjectPool
{
    public class PoolObject<T> : ObjectBase where T : class
    {
        public static PoolObject<T> Create(T target)
        {
            var obj = ReferencePool.Acquire<PoolObject<T>>();
            obj.Initialize(target);
            return obj;
        }

        protected override void Release(bool isShutdown)
        {
            if (Target is Component c)
            {
                Object.Destroy(c.gameObject);
            }
        }
    }

}