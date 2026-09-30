using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 停止地板
    /// </summary>
    public class StepperPlatform : PlatformBehavior
    {
        public override bool IsInteractivePlatform => true;
        public override bool ShouldStopMovementHere(TileBehavior tile) { return true; }
        public override bool TileCanGoThrough(TileBehavior levelTileBehavior) => true;
    }

}
