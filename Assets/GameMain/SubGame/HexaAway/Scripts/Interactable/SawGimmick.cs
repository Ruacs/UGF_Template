using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    public class SawGimmick : GimmickBehavior, IReverseControllable
    {
        [SerializeField] private Transform m_VisualsTransform;
        [SerializeField] private float m_RotateSpeed = 5;
        [SerializeField] private int m_SoundID = -1;
        [SerializeField] private int m_EffectID = -1;
        [SerializeField] private ParticleSystem m_PieceBreakEffect;

        void FixedUpdate()
        {
            if (m_VisualsTransform != null) m_VisualsTransform.Rotate(0, Time.fixedDeltaTime * m_RotateSpeed, 0);
        }

        public override bool ConsumesPieces => true;

        public override void OnPieceArrived(TileBehavior tile, Vector3 worldPosition)
        {
            Vector3 centerPosition = Vector3.Lerp(transform.position, worldPosition, 0.5f);

            if (m_PieceBreakEffect != null)
            {
                ParticleSystem effect = Instantiate(m_PieceBreakEffect, worldPosition, Quaternion.identity,
                    context.Manager.LevelRepresentation.LevelTransform);
                var main = effect.main; 
                if (tile.VisualData != null) main.startColor = tile.VisualData.FeedbackColor;
                effect.Play(true);
                Destroy(effect.gameObject, main.duration + main.startLifetime.constantMax + 0.5f);
            }

            if (m_SoundID > 0)
            {
                GameEntry.Sound.PlaySound(m_SoundID);
            }

            if (m_EffectID > 0)
            {
                GameEntry.Entity.ShowEffect(new EffectData(GameEntry.Entity.GenerateSerialId(), m_EffectID)
                {
                    Position = centerPosition,
                    KeepTime = 1
                });
            }
        }

        public override bool ShouldStopMovementHere(TileBehavior tile) => true;
        public override bool TileCanGoThrough(TileBehavior tile) => true;

    }
}
