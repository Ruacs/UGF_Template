using DG.Tweening;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// Platform 交换器，负责交换平台及平台上的 Tile 和交互物。
    /// </summary>
    public sealed class SquareReverseGimmick : GimmickBehavior
    {
        [SerializeField] private ReverseType type;
        [SerializeField] private Transform m_RotatingVisualsTransform;
        [SerializeField] private int m_SoundId = -1;
        [SerializeField] private Ease m_Ease = Ease.InOutBack;
        private bool isBusy;
        private StoredData[] storedDatas;
        private int rotateAngle;
        private Sequence rotationSequence;
        private LevelRuntime animationLevel;
        private Vector3 restingPosition;
        private Quaternion finalRotation;

        public override bool BlocksTileInput => isBusy;

        public override void OnCreated()
        {
            DirectionData directionData = DirectionHelper.Get(data.Direction);
            m_RotatingVisualsTransform.localRotation = Quaternion.Euler(0, directionData.Angle, 0);
            storedDatas = type == ReverseType.Double ? Create(data.Direction, false) : Create(data.Direction, true);
            rotateAngle = 360 / storedDatas.Length;
        }

        public override void OnClicked()
        {
            LevelRuntime level = context?.Manager?.LevelRepresentation;
            if (isBusy || level == null || level.HasMovingTiles || level.IsTileInputBlocked
                || m_RotatingVisualsTransform == null || storedDatas == null) return;
            if (m_SoundId >= 0) GameEntry.Sound?.PlaySound(m_SoundId);

            isBusy = true;
            animationLevel = level;
            restingPosition = m_RotatingVisualsTransform.localPosition;
            Vector3 targetAngles = new Vector3(0f, m_RotatingVisualsTransform.localEulerAngles.y + rotateAngle, 0f);
            finalRotation = Quaternion.Euler(targetAngles);
            foreach (StoredData item in storedDatas)
            {
                item.Collect(level, position + item.GetOffset(position));
                item.SetParent(m_RotatingVisualsTransform);
            }

            const float duration = 0.5f;
            rotationSequence = DOTween.Sequence()
                .Append(m_RotatingVisualsTransform.DOLocalMoveY(restingPosition.y + 0.25f, duration * 0.15f))
                .Append(m_RotatingVisualsTransform.DOLocalRotate(targetAngles, duration * 0.7f).SetEase(m_Ease))
                .Append(m_RotatingVisualsTransform.DOLocalMoveY(restingPosition.y, duration * 0.15f))
                .OnComplete(ReleaseStoredData)
                .OnKill(FinishInterruptedAnimation);
            context.Manager.ConsumeMove();
        }

        private void OnDisable() => rotationSequence?.Kill();

        private void FinishInterruptedAnimation()
        {
            rotationSequence = null;
            if (!isBusy) return;
            // Complete the exchange before unlocking, including disable/kill during an animation.
            if (m_RotatingVisualsTransform != null)
            {
                m_RotatingVisualsTransform.localPosition = restingPosition;
                m_RotatingVisualsTransform.localRotation = finalRotation;
            }
            ReleaseStoredData();
        }

        private void FixedUpdate()
        {
            if (!isBusy || storedDatas == null) return;
            foreach (StoredData item in storedDatas) item.Update();
        }

        private void ReleaseStoredData()
        {
            if (!isBusy) return;
            LevelRuntime level = animationLevel;
            if (level != null && context?.Manager?.LevelRepresentation == level && level.LevelTransform != null)
            {
                for (int i = 0; i < storedDatas.Length; i++)
                {
                    StoredData item = storedDatas[i];
                    StoredData next = storedDatas[(i + 1) % storedDatas.Length];
                    item.SetParent(level.LevelTransform);
                    item.Release(level, position + next.GetOffset(position));
                }
                isBusy = false;
                context.Manager.CheckCompleteStatus();
            }
            else isBusy = false;
            animationLevel = null;
        }

        private static StoredData[] Create(HexaAwayDirection initial, bool triple)
        {
            if (!triple) return new[] { new StoredData(initial), new StoredData(DirectionHelper.Opposite(initial)) };
            int opposite = (int)DirectionHelper.Opposite(initial);
            return new[] { new StoredData(initial), new StoredData((HexaAwayDirection)((opposite + 5) % 6)), new StoredData((HexaAwayDirection)((opposite + 1) % 6)) };
        }

        public override bool TileCanGoThrough(TileBehavior tile) => false;
        public override bool ShouldStopMovementHere(TileBehavior tile) => false;

        private sealed class StoredData
        {
            private readonly HexaAwayDirection direction;
            private PlatformBehavior platform;
            private TileBehavior tile;
            private GimmickBehavior gimmick;
            private Quaternion platformRotation;
            private Quaternion tileRotation;
            private Quaternion gimmickRotation;

            public StoredData(HexaAwayDirection direction) => this.direction = direction;
            public Vector2Int GetOffset(Vector2Int position) => DirectionHelper.GetOffset(direction, position.x);

            public void Collect(LevelRuntime level, Vector2Int position)
            {
                if (level.PlatformsGrid.TryGet(position.x, position.y, out platform))
                {
                    level.PlatformsGrid.Remove(position.x, position.y);
                    platformRotation = platform.transform.rotation;
                }
                if (level.TilesGrid.TryGet(position.x, position.y, out tile))
                {
                    level.TilesGrid.Remove(position.x, position.y);
                    tileRotation = tile.transform.rotation;
                }
                if (level.InteractablesGrid.TryGet(position.x, position.y, out gimmick))
                {
                    level.InteractablesGrid.Remove(position.x, position.y);
                    gimmickRotation = gimmick.transform.rotation;
                }
            }

            public void Update()
            {
                if (platform != null) platform.transform.rotation = platformRotation;
                if (tile != null) tile.transform.rotation = tileRotation;
                if (gimmick != null) gimmick.transform.rotation = gimmickRotation;
            }

            public void SetParent(Transform parent)
            {
                if (platform != null) platform.transform.SetParent(parent);
                if (tile != null) tile.transform.SetParent(parent);
                if (gimmick != null) gimmick.transform.SetParent(parent);
            }

            public void Release(LevelRuntime level, Vector2Int position)
            {
                if (platform != null)
                {
                    level.PlatformsGrid.Set(position.x, position.y, platform);
                    platform.OverridePosition(position);
                    platform.transform.rotation = platformRotation;
                }
                if (tile != null)
                {
                    level.TilesGrid.Set(position.x, position.y, tile);
                    tile.OverridePosition(position);
                    tile.transform.rotation = tileRotation;
                }
                if (gimmick != null)
                {
                    level.InteractablesGrid.Set(position.x, position.y, gimmick);
                    gimmick.OverridePosition(position);
                    gimmick.transform.rotation = gimmickRotation;
                }
                platform = null;
                tile = null;
                gimmick = null;
            }
        }
    }
}
