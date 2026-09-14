using GameFramework;
using GameFramework.ObjectPool;
using System.Collections;
using UnityEngine;

namespace Lokas
{
    public class ItemUIObject : ObjectBase
    {
        public static ItemUIObject Create(ItemUI skinItem, string name = "ItemUIPool")
        {
            ItemUIObject instancePool = ReferencePool.Acquire<ItemUIObject>();
            instancePool.Initialize(name, skinItem);
            return instancePool;
        }

        protected override void Release(bool isShutdown)
        {
            ItemUI skinItem = (ItemUI)Target;
            if (skinItem != null)
            {
                Object.Destroy(skinItem.gameObject);
            }
        }
    }
}