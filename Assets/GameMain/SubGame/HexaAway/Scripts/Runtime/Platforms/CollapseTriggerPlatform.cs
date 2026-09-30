namespace Lokas
{
    public class CollapseTriggerPlatform : PlatformBehavior
    {
        public override bool IsInteractivePlatform => true;
        public override bool TileCanGoThrough(TileBehavior tile) => true;

        public override void OnTileStepped(TileBehavior tile)
        {
            context?.Manager?.LevelRepresentation?.RemovePlatform(this);
        }
    }
}
