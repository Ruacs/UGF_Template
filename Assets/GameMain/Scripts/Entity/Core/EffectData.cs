using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Lokas
{
    [Serializable]
    public class EffectData : EntityData
    {
        [SerializeField]
        protected float m_KeepTime = 0;
        protected Vector3 m_Scale = Vector3.one;
        protected Transform m_Parent = null;
        public EffectData(int entityId, int typeId) : base(entityId, typeId)
        {
            
        }


        public float KeepTime { get => m_KeepTime; set => m_KeepTime = value; }
        public Transform Parent { get => m_Parent; set => m_Parent = value; }
        public Vector3 Scale { get => m_Scale; set => m_Scale = value; }



    }
}