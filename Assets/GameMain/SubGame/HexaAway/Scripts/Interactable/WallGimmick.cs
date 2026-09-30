using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 墙阻挡
    /// </summary>
    public class WallGimmick : GimmickBehavior
    {
        public override bool TileCanGoThrough(TileBehavior tile) => false;
        public override bool ShouldStopMovementHere(TileBehavior tile) => false;
    }

}