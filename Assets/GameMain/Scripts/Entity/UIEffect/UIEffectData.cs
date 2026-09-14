using System;
using System.Collections;
using UnityEngine;

namespace Lokas
{
    [Serializable]
    public class UIEffectData : EffectData
    {
        private RectTransform canvasRect;
        /// <summary>
        /// 生成位置
        /// </summary>
        private Vector2 spawnScreenPos = Vector2.zero;  

        public object userData;
        public UIEffectData(int entityId, int typeId) : base(entityId, typeId)
        {
        }


        public Vector2 SpawnScreenPos { get => spawnScreenPos; set => spawnScreenPos = value; }
        public RectTransform CanvasRect { get => canvasRect; set => canvasRect = value; }

        
    }
}