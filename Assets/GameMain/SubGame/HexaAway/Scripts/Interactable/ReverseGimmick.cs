using DG.Tweening;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// Tile 交换器，只负责交换 Tile。
    /// </summary>
    public class ReverseGimmick : GimmickBehavior
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

            storedDatas = type == ReverseType.Double ? GetDoubleStoredData(data.Direction) : GetTrippleStoredData(data.Direction);
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
            if (!isBusy || storedDatas == null)
            {
                return;
            }

            for (int i = 0; i < storedDatas.Length; i++)
            {
                storedDatas[i].Update();
            }
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

        private StoredData[] GetDoubleStoredData(HexaAwayDirection initialDirection)
        {
            return new StoredData[2]
            {
                new(initialDirection),
                new(DirectionHelper.Opposite(initialDirection))
            };
        }

        private StoredData[] GetTrippleStoredData(HexaAwayDirection initialDirection)
        {
            (HexaAwayDirection a, HexaAwayDirection b) directions = NeighborsOfOpposite(initialDirection);

            return new StoredData[3]
            {
                new(initialDirection),
                new(directions.a),
                new(directions.b)
            };
        }

        private static (HexaAwayDirection a, HexaAwayDirection b) NeighborsOfOpposite(HexaAwayDirection direction)
        {
            int oppositeIndex = (int)DirectionHelper.Opposite(direction);
            return ((HexaAwayDirection)((oppositeIndex + 5) % 6), (HexaAwayDirection)((oppositeIndex + 1) % 6));
        }

        public override bool TileCanGoThrough(TileBehavior tile)
        {
            return false;
        }

        public override bool ShouldStopMovementHere(TileBehavior tile)
        {
            return false;
        }

        private sealed class StoredData
        {
            private readonly HexaAwayDirection direction;
            private TileBehavior tileBehavior;
            private GimmickBehavior gimmickBehavior;
            private Quaternion tileRotation;
            private Quaternion gimmickRotation;
            private bool isCollected;

            public StoredData(HexaAwayDirection direction)
            {
                this.direction = direction;
            }

            public Vector2Int GetOffset(Vector2Int position)
            {
                return DirectionHelper.GetOffset(direction, position.x);
            }

            public void Collect(LevelRuntime levelRepresentation, Vector2Int position)
            {
                isCollected = true;

                if (levelRepresentation.TilesGrid.TryGet(position.x, position.y, out tileBehavior))
                {
                    levelRepresentation.TilesGrid.Remove(position.x, position.y);
                    tileRotation = tileBehavior.transform.rotation;
                }

                if (levelRepresentation.InteractablesGrid.TryGet(position.x, position.y, out gimmickBehavior) && gimmickBehavior is IReverseControllable)
                {
                    levelRepresentation.InteractablesGrid.Remove(position.x, position.y);
                    gimmickRotation = gimmickBehavior.transform.rotation;
                }

            }

            public void Update()
            {
                if (!isCollected)
                {
                    return;
                }

                if (tileBehavior != null)
                {
                    tileBehavior.transform.rotation = tileRotation;
                }


            }

            public void SetParent(Transform parent)
            {
                if (tileBehavior != null)
                {
                    tileBehavior.transform.SetParent(parent);
                }

                if (gimmickBehavior != null)
                {
                    gimmickBehavior.transform.SetParent(parent);
                }

            }

            public void Release(LevelRuntime levelRepresentation, Vector2Int position)
            {
                if (tileBehavior != null)
                {
                    levelRepresentation.TilesGrid.Set(position.x, position.y, tileBehavior);
                    tileBehavior.OverridePosition(position);
                    tileBehavior.transform.rotation = tileRotation;
                }

                if (gimmickBehavior != null)
                {
                    levelRepresentation.InteractablesGrid.Set(position.x, position.y, gimmickBehavior);
                    gimmickBehavior.OverridePosition(position);
                    gimmickBehavior.transform.rotation = gimmickRotation;
                }

                tileBehavior = null;
                gimmickBehavior = null;
                isCollected = false;
            }
        }
    }
}
