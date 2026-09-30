using System.Collections;
using UnityEngine;

namespace Lokas
{
    public class BombGimmick : GimmickBehavior
    {
        [SerializeField] private int m_SoundID = -1;
        [SerializeField] private int m_EffectID = -1;
        private bool triggered;

        public override bool CollectsTileOnArrival => true;
        public override bool TriggerOnFirstPiece => true;

        public override void OnTileStepped(TileBehavior tile)
        {
            if (triggered || tile == null || tile.IsCollected) return;
            triggered = true;
            LevelRuntime level = context.Manager.LevelRepresentation;

            // Retire the arriving tile before cancelling its followers, so no pose is restored.
            Collect(level, tile, tile.MovementStartPosition);
            if (level.InteractablesGrid.Get(position.x, position.y) == this)
                level.InteractablesGrid.Remove(position.x, position.y);
            level.Interactables.Remove(this);

            foreach (Vector2Int neighbor in DirectionHelper.GetNeighbors(position))
                if (level.TilesGrid.TryGet(neighbor.x, neighbor.y, out TileBehavior neighborTile))
                    Collect(level, neighborTile, neighborTile.MatrixPosition);

            if (m_SoundID > 0) GameEntry.Sound.PlaySound(m_SoundID);
            if (m_EffectID > 0)
            {
                GameEntry.Entity.ShowEffect(new EffectData(GameEntry.Entity.GenerateSerialId(), m_EffectID)
                {
                    Position = transform.position,
                    KeepTime = 1
                });
            }

            Destroy(gameObject);
            context.Manager.CheckCompleteStatus();
        }

        private static void Collect(LevelRuntime level, TileBehavior tile, Vector2Int collectionPosition)
        {
            if (tile == null || tile.IsCollected) return;
            tile.OnTileCollected(collectionPosition);
            tile.DisableEffects();
            level.OnTileDestructed(tile);
            tile.gameObject.SetActive(false);
            Destroy(tile.gameObject);
        }

        public override bool ShouldStopMovementHere(TileBehavior tile) => true;
        public override bool TileCanGoThrough(TileBehavior tile) => true;
    }
}
