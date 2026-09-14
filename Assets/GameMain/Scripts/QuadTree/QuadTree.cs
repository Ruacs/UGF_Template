using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace QuadTree
{ 
    public class QuadTree<T>
    {
        private const int MAX_DEPTH = 5;                // 最大深度 防止无限分裂

        private readonly Rect bounds;                   // 当前节点范围
        private readonly int capacity;                  // 当前节点最大容量，超过则分裂
        private readonly int depth;                     // 当前节点深度
        private readonly Func<T, Vector2> getPosition;  // 获取对象位置的函数

        private List<T> objects;                        // 当前节点存储的对象列表
        private QuadTree<T>[] children;                 // 子节点数组 最大4个
        private bool isSplit;                           // 是否已分裂


        public bool debugHit;   // 是否被 Query 命中（仅 Debug 用）


        public QuadTree(Rect bounds,int capacity,Func<T, Vector2> getPosition,int depth = 0)
        {
            this.bounds = bounds;
            this.capacity = capacity;
            this.depth = depth;
            this.getPosition = getPosition;

            objects = new List<T>(capacity);
        }

        #region Insert


        /// <summary>
        /// 插入对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Insert(T obj)
        {
            Vector2 pos;
            pos = getPosition(obj);

            if (!bounds.Contains(pos))
                return false;

            if (!isSplit && objects.Count < capacity || depth >= MAX_DEPTH)
            {
                objects.Add(obj);
                return true;
            }

            if (!isSplit)
                Split();

            foreach (var child in children)
            {
                if (child.Insert(obj))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 移除对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Remove(T obj)
        {
            Vector2 pos = getPosition(obj);

            // 不在这个节点范围内，直接跳过
            if (!bounds.Contains(pos))
                return false;

            // 先试着从当前节点移除
            if (objects.Remove(obj))
                return true;

            // 如果没有子节点，说明不在这条分支
            if (!isSplit)
                return false;

            // 递归子节点
            foreach (var child in children)
            {
                if (child.Remove(obj))
                {
                    // 子节点删成功后，尝试合并
                    TryMerge();
                    return true;
                }
                 
            }

            return false;
        }

        /// <summary>
        /// 分裂节点
        /// </summary>
        private void Split()
        {
            children = new QuadTree<T>[4];

            float hw = bounds.width * 0.5f;
            float hh = bounds.height * 0.5f;
            float x = bounds.x;
            float y = bounds.y;

            children[0] = CreateChild(x, y, hw, hh); // 左上
            children[1] = CreateChild(x + hw, y, hw, hh); // 右上
            children[2] = CreateChild(x, y + hh, hw, hh); // 左下
            children[3] = CreateChild(x + hw, y + hh, hw, hh); // 右下

            foreach (var obj in objects)
            {
                foreach (var child in children)
                {
                    if (child.Insert(obj))
                        break;
                }
            }

            objects.Clear();
            isSplit = true;
        }


        /// <summary>
        /// 合并
        /// </summary>
        private void TryMerge()
        {
            if (!isSplit)
                return;

            int totalCount = GetTotalObjectCount();

            // 数量还很多，没必要合并
            if (totalCount > capacity)
                return;

            // 把子节点里的对象全部收回来
            for (int i = 0; i < children.Length; i++)
            {
                CollectObjects(children[i]);
            }

            // 清空子节点
            children = null;
            isSplit = false;
        }

        private void CollectObjects(QuadTree<T> node)
        {
            objects.AddRange(node.objects);

            if (!node.isSplit)
                return;

            foreach (var child in node.children)
            {
                CollectObjects(child);
            }
        }


        /// <summary>
        /// 获取对象总数
        /// </summary>
        /// <returns></returns>
        private int GetTotalObjectCount()
        {
            int count = objects.Count;

            if (!isSplit)
                return count;

            foreach (var child in children)
            {
                count += child.GetTotalObjectCount();
            }

            return count;
        }


        private QuadTree<T> CreateChild(float x, float y, float w, float h)
        {
            return new QuadTree<T>(new Rect(x, y, w, h), capacity, getPosition, depth + 1);
        }

        #endregion

        #region Query
        /// <summary>
        /// 查询范围内的对象
        /// </summary>
        /// <param name="range">范围</param>
        /// <param name="result">范围内的对象列表</param>
        public void Query(Rect range, List<T> result)
        {
            if (!bounds.Overlaps(range))
                return;

#if UNITY_EDITOR
            debugHit = true; // 能走到这里，说明这个格子被 Query 访问到了
#endif

            foreach (var obj in objects)
            {
                if (range.Contains(getPosition(obj)))
                    result.Add(obj);
            }

            if (!isSplit)
                return;

            foreach (var child in children)
            {
                child.Query(range, result);
            }
        }

        #endregion

        #region Utility

        public void Clear()
        {
            objects.Clear();

            if (!isSplit)
                return;

            foreach (var child in children)
                child.Clear();

            children = null;
            isSplit = false;
        }

        #endregion

        public void DebugDraw()
        {
#if UNITY_EDITOR
            // 画当前节点边框
            Vector3 center = new Vector3(bounds.center.x, 0f, bounds.center.y);
            Vector3 size = new Vector3(bounds.width, 0f, bounds.height);

            // 命中 Query 的节点 → 高亮
            Gizmos.color = debugHit ? Color.yellow : Color.green;
            Gizmos.DrawWireCube(center, size);

            // 在格子中显示对象数量

            if(objects.Count > 0)
            {
                Handles.Label(center,
                 objects.Count.ToString(),
                 new GUIStyle()
                 {
                     normal = new GUIStyleState() { textColor = Color.red },
                     alignment = TextAnchor.MiddleCenter,
                     fontSize = 20
                 }

               );

            }


            if (!isSplit)
                return;
            // 递归画子节点
            foreach (var child in children)
            {
                child.DebugDraw();
            }
#endif
        }

        public void ClearDebugHit()
        {
#if UNITY_EDITOR
            debugHit = false;

            if (!isSplit)
                return;

            foreach (var child in children)
            {
                child.ClearDebugHit();
            }
#endif
        }


    }
}
