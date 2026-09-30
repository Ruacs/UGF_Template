using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    public class WoodBoxEffect : TileEffectBehavior
    {
        public override void OnTileCollectedGlobal(TileBehavior behavior, Vector2Int collectionPosition)
        {
            if (linkedTile == null || behavior == null || behavior == linkedTile)
            {
                return;
            }

            Vector2Int[] neighbors = DirectionHelper.GetNeighbors(linkedTile.MatrixPosition);
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] == collectionPosition)
                {
                    DisableEffect();
                    return;
                }
            }
        }

        public override bool IsClickable() => false;
    }

}
