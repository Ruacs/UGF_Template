using GameFramework.ObjectPool;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameEntry = Lokas.GameEntry;
    public class FloatingTextComponent : ObjectPoolComponent<FloatingText>
    {
        [SerializeField] protected Canvas m_InstanceRootCanvas;
        [Header("ComboCanvas（InstanceRoot）")]
        [SerializeField] private Canvas m_ComboCanvas;

        [SerializeField] private RectTransform m_canvasRect;

        public void Show(FloatingTextData data)
        {
            FloatingText item = Spawn();



            Vector2 localPos = ScreenToCanvasLocal(data.startScreenPos);
            Vector2 endLocalPos = Vector2.zero;
            if (data.endScreenPos != default)
                endLocalPos = ScreenToCanvasLocal(data.endScreenPos);

            item.Play(data, localPos, endLocalPos, () => Recycle(item));
        }
        public void Show(FloatingTextData data, int sortingOrder)
        {
            FloatingText item = Spawn();
            m_InstanceRootCanvas.sortingOrder = sortingOrder + 50;
            Debug.Log($"[FloatingTextComponent] sortingOrder: {sortingOrder}");
            Vector2 localPos = ScreenToCanvasLocal(data.startScreenPos);
            Vector2 endLocalPos = Vector2.zero;
            if (data.endScreenPos != default)
                endLocalPos = ScreenToCanvasLocal(data.endScreenPos);

            item.Play(data, localPos, endLocalPos, () => { Recycle(item); m_InstanceRootCanvas.sortingOrder -= 50; });
        }


        public void Show(FloatingTextData data, Action callback)
        {
            FloatingText item = Spawn();
            Vector2 localPos = ScreenToCanvasLocal(data.startScreenPos);
            Vector2 endLocalPos = Vector2.zero;
            if (data.endScreenPos != default)
                endLocalPos = ScreenToCanvasLocal(data.endScreenPos);
            item.Play(data, localPos, endLocalPos, () => { callback?.Invoke(); Recycle(item); });
        }
        private Vector2 ScreenToCanvasLocal(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_canvasRect, screenPos, GameEntry.UI.UICamera, out Vector2 local);

            return local;
        }
    }



