using System;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// A→B 飞行特效数据。
    /// ScreenPos（继承自 UIEffectData）为发射起点（屏幕坐标）。
    /// TargetRect 或 TargetScreenPos 为吸引终点。
    /// 优先使用 TargetRect（动态跟随 UI 元素），否则使用 TargetScreenPos。
    /// </summary>
    [Serializable]
    public class UIFlyEffectData : UIEffectData
    {
        public UIFlyEffectData(int entityId, int typeId) : base(entityId, typeId)
        {
            KeepTime = 1.5f;
        }

        /// <summary>吸引目标 UI 元素（动态跟随，优先级高于 TargetScreenPos）</summary>
        public RectTransform TargetRect { get; set; }

        /// <summary>吸引目标屏幕坐标（TargetRect 为 null 时使用）</summary>
        public Vector2 TargetScreenPos { get; set; }

        /// <summary>移动完成且粒子消亡后的回调（仅触发一次）</summary>
        public Action OnAllCompleted { get; set; }
 
        public string MoveRootPath { get; set; }
        public FlyMoveMode? MoveMode { get; set; }
        public bool? AlignToMoveDirection { get; set; }
        public float? MoveDirectionAngleOffset { get; set; }
        public CurveDirection? CurveDirection { get; set; }
        public float? CurveStrengthMin { get; set; }
        public float? CurveStrengthMax { get; set; }
        public float? MoveDuration { get; set; }
    }
}
