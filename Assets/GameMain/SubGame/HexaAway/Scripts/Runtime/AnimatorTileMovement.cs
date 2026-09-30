using System.Collections;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 基于 Animator 的 Tile 移动表现实现。
    /// </summary>
    public sealed class AnimatorTileMovement : TileMovement
    {
        [SerializeField] private Animator animator;

        private static readonly int WormHoleEnterState = Animator.StringToHash("WormHoleEnter");
        private static readonly int WormHoleExitState = Animator.StringToHash("WormHoleExit");

        public override IEnumerator Execute(TileMotionTask task)
        {
            for (int i = 0; i < task.Steps.Length && !task.IsCancelled; i++)
            {
                if (task.Steps[i].Type == TileMoveStepType.TeleportExit)
                {
                    transform.position = task.Positions[i];
                    yield return PlayWormHoleExitAnimation(task.TeleportDuration);
                }
                else
                {
                    Vector3 start = transform.position;
                    Vector3 delta = task.Positions[i] - start;
                    if (delta.sqrMagnitude > 0.0001f)
                        transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                    PlayMovementAnimation();
                    yield return Translate(start, task.Positions[i], task.StepDuration);
                }
                task.Arrive(i);
                if (task.IsCancelled) yield break;
                if (task.Steps[i].Type == TileMoveStepType.TeleportEnter)
                    yield return PlayWormHoleEnterAnimation(task.TeleportDuration);
            }
            if (task.IsCancelled) yield break;
            if (task.Ending == TileMotionEnding.Return && task.Steps.Length > 0)
            {
                PlayBackwardsAnimation();
                yield return Translate(transform.position, task.Origin, task.ReturnDuration);
            }
            else if (task.Ending == TileMotionEnding.Shatter)
            {
                task.Impact(0, transform.position);
                yield return PlayScaleAnimation(initialScale, Vector3.zero, 0.12f);
            }
            else if (task.Ending == TileMotionEnding.Fall)
            {
                Vector3 start = transform.position;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.18f)
                {
                    transform.position = start + Vector3.down * (1.35f * t * t);
                    transform.localScale = initialScale * (1f - t);
                    yield return null;
                }
            }
            ResetWormHoleAnimationState();
        }

        public override void RestorePose()
        {
            base.RestorePose();
            if (animator != null) { animator.Rebind(); animator.Update(0f); }
        }

        private IEnumerator Translate(Vector3 start, Vector3 target, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            transform.position = target;
        }

        void Awake()
        {
            animator ??= GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// 播放正向移动动画。
        /// </summary>
        public override void PlayMovementAnimation()
        {
            if (animator != null)
            {
                animator.Play("Move", -1, 0);
            }
        }

        /// <summary>
        /// 播放移动失败后的回退动画。
        /// </summary>
        public override void PlayBackwardsAnimation()
        {
            if (animator != null)
            {
                animator.Play("MoveBackwards", -1, 0);
            }
        }

        public override IEnumerator PlayWormHoleEnterAnimation(float duration)
        {
            if (HasAnimatorState(WormHoleEnterState))
            {
                animator.Play(WormHoleEnterState, -1, 0);
            }

            yield return base.PlayWormHoleEnterAnimation(duration);
        }

        public override IEnumerator PlayWormHoleExitAnimation(float duration)
        {
            if (HasAnimatorState(WormHoleExitState))
            {
                animator.Play(WormHoleExitState, -1, 0);
            }

            yield return base.PlayWormHoleExitAnimation(duration);
        }

        private bool HasAnimatorState(int stateHash)
        {
            return animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, stateHash);
        }
    }
}
