using UnityEngine;

namespace Lokas
{
    public class WormHolePlatform : PlatformBehavior
    {
        public override bool IsInteractivePlatform => true;
        private WormHolePlatform linkedWormHole;

        public override void OnLevelSpawned()
        {
            linkedWormHole = FindLinkedWormHole();

            if (linkedWormHole == null)
            {
                Debug.LogWarning($"HexaAway wormhole at {position} has no linked pair. PairId: {data?.PairId}", this);
            }
        }

        public override bool TileCanGoThrough(TileBehavior tile) => linkedWormHole != null;

        public override bool TryGetTeleportExit(TileBehavior tile, out Vector2Int exitPosition)
        {
            if (linkedWormHole != null)
            {
                exitPosition = linkedWormHole.Position;
                return true;
            }

            exitPosition = default;
            return false;
        }

        private WormHolePlatform FindLinkedWormHole()
        {
            LevelRuntime levelRepresentation = context?.Manager?.LevelRepresentation;
            if (levelRepresentation?.Platforms == null || data == null)
            {
                return null;
            }

            for (int i = 0; i < levelRepresentation.Platforms.Count; i++)
            {
                if (levelRepresentation.Platforms[i] is WormHolePlatform wormHole &&
                    wormHole != this &&
                    wormHole.Data != null &&
                    wormHole.Data.PairId == data.PairId)
                {
                    return wormHole;
                }
            }

            return null;
        }
    }
}
