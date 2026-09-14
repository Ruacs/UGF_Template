using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConfigSO
{
    public abstract class IdOnlyConfigSO : ScriptableObject
    {
        [Header("ID"),ReadOnly]
        public int id; // 全局唯一，只干这一件事
    }

}