using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    public class AreaChangeDirectionGimmick : GimmickBehavior
    {
        [SerializeField] private Transform m_ButtonVisaulTransform;
        [SerializeField] int m_SoundId = -1;

        public override void OnClicked()
        {
            base.OnClicked();

            LevelRuntime levelRepresentation = context?.Manager?.LevelRepresentation;
            if (levelRepresentation == null || levelRepresentation.HasMovingTiles || levelRepresentation.IsTileInputBlocked)
            {
                return;
            }

            if (m_SoundId >= 0) GameEntry.Sound?.PlaySound(m_SoundId);

            Vector2Int[] neighbors = DirectionHelper.GetNeighbors(position);

            foreach (Vector2Int neighbor in neighbors)
            {
                if (levelRepresentation.TilesGrid.TryGet(neighbor.x, neighbor.y, out var neighborTile))
                {
                    neighborTile.OverrideDirection(DirectionHelper.Opposite(neighborTile.Direction), true);
                }
            }

            context.Manager.ConsumeMove();
            context.Manager.CheckCompleteStatus();
        }

    }

}
