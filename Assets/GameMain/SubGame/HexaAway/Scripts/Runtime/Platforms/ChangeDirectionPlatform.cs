using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Lokas
{
    /// <summary>
    /// Tile移动方向转向器
    /// </summary>
    public class ChangeDirectionPlatform : PlatformBehavior
    {
        public override bool IsInteractivePlatform => true;

        private HexaAwayDirection m_OverrideDirection;

        public override void OnCreated()
        {
            m_OverrideDirection = data.Direction;

            transform.localRotation = Quaternion.Euler(0, DirectionHelper.Get(m_OverrideDirection).Angle, 0);

        }


        public override bool TileCanGoThrough(TileBehavior levelTileBehavior) => true;
        public override HexaAwayDirection GetOverridedDirection(HexaAwayDirection direction) => m_OverrideDirection;
    }

}
