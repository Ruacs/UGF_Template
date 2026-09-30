using System.Collections;
using UnityEngine;

namespace Lokas
{
    [RequireComponent(typeof(TileBehavior))]
    public abstract class TileMovement : MonoBehaviour
    {
        protected TileBehavior behavior;
        protected Vector3 initialScale;
        private Vector3 savedPosition;
        private Quaternion savedRotation;

        public void Init(TileBehavior owner)
        {
            behavior = owner;
            initialScale = transform.localScale;
            OnInit();
        }

        public virtual void OnInit() { }
        public virtual void CapturePose()
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;
        }

        public virtual void RestorePose()
        {
            transform.SetPositionAndRotation(savedPosition, savedRotation);
            ResetWormHoleAnimationState();
        }

        public abstract IEnumerator Execute(TileMotionTask task);
        public virtual void PlayMovementAnimation() { }
        public virtual void PlayBackwardsAnimation() { }

        public virtual IEnumerator PlayWormHoleEnterAnimation(float duration)
        {
            yield return PlayScaleAnimation(initialScale, initialScale * 0.05f, duration);
        }

        public virtual IEnumerator PlayWormHoleExitAnimation(float duration)
        {
            yield return PlayScaleAnimation(initialScale * 0.05f, initialScale, duration);
        }

        public virtual void ResetWormHoleAnimationState() => transform.localScale = initialScale;

        protected IEnumerator PlayScaleAnimation(Vector3 start, Vector3 end, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(start, end, elapsed / duration);
                yield return null;
            }
            transform.localScale = end;
        }
    }
}
