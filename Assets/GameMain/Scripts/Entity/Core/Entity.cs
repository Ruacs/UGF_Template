using GameFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    /// <summary>
    /// 所有实体的父类
    /// </summary>
    public class Entity : EntityLogic
    {
        [SerializeField]
        private EntityData m_EntityData = null;

        public int Id
        {
            get
            {
                return Entity.Id;
            }
        }


        /// <summary>
        /// 实体初始化
        /// </summary>
        /// <param name="userData"></param>
        protected override void OnInit(object userData)

        {
            base.OnInit(userData);

        }
        /// <summary>
        /// 实体回收
        /// </summary>
        protected override void OnRecycle()

        {
            base.OnRecycle();
        }
        /// <summary>
        /// 显示实体
        /// </summary>
        /// <param name="userData">实体表</param>
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_EntityData = userData as EntityData;

            if (m_EntityData == null)
            {
                Log.Error("实体表无效");
                return;
            }
            //实体显示时的默认名
            Name = Utility.Text.Format("[Entity {0}]", Id);
            //根据实体表中的数据对实体的一些初始处理
            //比如一些Transform的信息
            //更详细的信息可以在子类中添加
            CachedTransform.localPosition = m_EntityData.Position;
            CachedTransform.localRotation = m_EntityData.Rotation;
            CachedTransform.localScale = Vector3.one;
        }
        /// <summary>
        /// 隐藏实体
        /// </summary>
        /// <param name="isShutdown"></param>
        /// <param name="userData"></param>
        protected override void OnHide(bool isShutdown, object userData)

        {
            base.OnHide(isShutdown, userData);
        }
        /// <summary>
        /// 附加子实体
        /// </summary>
        /// <param name="childEntity"></param>
        /// <param name="parentTransform"></param>
        /// <param name="userData"></param>
        protected override void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)

        {
            base.OnAttached(childEntity, parentTransform, userData);
        }
        /// <summary>
        /// 解除子实体
        /// </summary>
        /// <param name="childEntity"></param>
        /// <param name="userData"></param>
        protected override void OnDetached(EntityLogic childEntity, object userData)

        {
            base.OnDetached(childEntity, userData);
        }
        /// <summary>
        /// 附加成子实体
        /// </summary>
        /// <param name="parentEntity"></param>
        /// <param name="parentTransform"></param>
        /// <param name="userData"></param>
        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);
        }
        /// <summary>
        /// 解除子实体
        /// </summary>
        /// <param name="parentEntity"></param>
        /// <param name="userData"></param>
        protected override void OnDetachFrom(EntityLogic parentEntity, object userData)

        {
            base.OnDetachFrom(parentEntity, userData);
        }

        /// <summary>
        /// 生命周期
        /// </summary>
        /// <param name="elapseSeconds"></param>
        /// <param name="realElapseSeconds"></param>
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
        }


        public virtual void HideSelf()
        {
            GameEntry.Entity.HideEntity(this);
        }
    }
}
