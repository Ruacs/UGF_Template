using GameFramework;
using GameFramework.ObjectPool;
using System.Collections;
using UnityEngine;

namespace Lokas
{
    public class GameObjectPool : ObjectBase
    {
        public static GameObjectPool Create(GameObject gameObject)
        {
            
            GameObjectPool instancePool = ReferencePool.Acquire<GameObjectPool>();
            instancePool.Initialize(gameObject);
            return instancePool;

        }

        public static GameObjectPool Create(string name, GameObject gameObject)
        {

            GameObjectPool instancePool = ReferencePool.Acquire<GameObjectPool>();
            instancePool.Initialize(name,gameObject);
            return instancePool;

        }




        public override void Clear()
        {
            base.Clear();
        }

        protected override void Release(bool isShutdown)
        {
            if (Target is GameObject target)
            {
                GameObject.Destroy(target);  // 销毁 Target
            }
        
        }


    }
}