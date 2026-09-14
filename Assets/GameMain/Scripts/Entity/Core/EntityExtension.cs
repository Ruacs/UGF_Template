//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.DataTable;
using System;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public static class EntityExtension
    {
        private static int s_SerialId = 0;

        public static Entity GetGameEntity(this EntityComponent entityComponent, int entityId)
        {
            UnityGameFramework.Runtime.Entity entity = entityComponent.GetEntity(entityId);
            if (entity == null)
            {
                return null;
            }

            return (Entity)entity.Logic;
        }

        public static void HideEntity(this EntityComponent entityComponent, Entity entity)
        {
            entityComponent.HideEntity(entity.Entity);
        }

        public static void AttachEntity(this EntityComponent entityComponent, Entity entity, int ownerId, string parentTransformPath = null, object userData = null)
        {
            entityComponent.AttachEntity(entity.Entity, ownerId, parentTransformPath, userData);
        }

        public static void ShowEffect(this EntityComponent entityComponent, EffectData data)
        {
            entityComponent.ShowEntity(typeof(Effect), "Effect", Constant.AssetPriority.EffectAsset, data);
        }

        public static void ShowUIEffect(this EntityComponent entityComponent, UIEffectData data)
        {
            entityComponent.ShowEntity(typeof(UIEffect), "Effect", Constant.AssetPriority.EffectAsset, data);
        }

        /// <summary>显示 A→B 飞行粒子特效</summary>
        public static void ShowUIFlyEffect(this EntityComponent entityComponent, UIFlyEffectData data)
        {
            entityComponent.ShowEntity(typeof(UIFlyEffect), "Effect", Constant.AssetPriority.EffectAsset, data);
        }

        // public static void ShowLevel(this EntityComponent entityComponent,LevelData data)
        // {
        //     entityComponent.ShowEntity(typeof(Level), "Level", Constant.AssetPriority.LevelAsset, data);
        // }

        // public static void ShowPlayer(this EntityComponent entityComponent, PlayerData data)
        // {
        //     entityComponent.ShowEntity(typeof(Player), "Player", Constant.AssetPriority.PlayerAsset, data);
        // }


        /// <summary>
        /// 显示实体
        /// </summary>
        /// <param name="entityComponent">实体组件</param>
        /// <param name="logicType">实体逻辑类型</param>
        /// <param name="entityGroup">实体组名称</param>
        /// <param name="priority">加载优先级</param>
        /// <param name="data">用户自定义数据</param>
        private static void ShowEntity(this EntityComponent entityComponent, Type logicType, string entityGroup, int priority, EntityData data)
        {
            if (data == null)
            {
                Log.Warning("Data is invalid.");
                return;
            }

            IDataTable<DREntity> dtEntity = GameEntry.DataTable.GetDataTable<DREntity>();
            DREntity drEntity = dtEntity.GetDataRow(data.TypeId);
            if (drEntity == null)
            {
                Log.Warning("Can not load entity id '{0}' from data table.", data.TypeId.ToString());
                return;
            }
            //Log.Warning(entityGroup);
            //Log.Warning(priority);

            string assetName =  AssetUtility.GetEntityAsset(entityGroup + "/" + drEntity.AssetName);

            entityComponent.ShowEntity(data.Id, logicType,assetName, entityGroup, priority, data);
        }

        public static int GenerateSerialId(this EntityComponent entityComponent)
        {
            return --s_SerialId;
        }
    }
}
