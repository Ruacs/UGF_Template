using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    public enum TileMotionEnding { Return, Stop, Fall, Shatter }

    // One logical move owns all pieces. Deduplicate by route index, not cell:
    // a route may visit the same cell more than once.
    public sealed class TileMotionTask
    {
        public readonly Vector3 Origin;
        public readonly TileMoveStep[] Steps;
        public readonly Vector3[] Positions;
        public readonly TileMotionEnding Ending;
        public readonly float StepDuration;
        public readonly float ReturnDuration;
        public readonly float TeleportDuration;
        private readonly bool[] arrived;
        private readonly HashSet<int> impacted = new();
        private readonly Action<int> onArrival;
        private readonly Action<Vector3> onImpact;
        public bool IsCancelled { get; private set; }

        public TileMotionTask(Vector3 origin, IList<TileMoveStep> steps, Func<Vector2Int, Vector3> getPosition,
            TileMotionEnding ending, float stepDuration, float returnDuration, float teleportDuration,
            Action<int> onArrival, Action<Vector3> onImpact)
        {
            Origin = origin;
            Ending = ending;
            StepDuration = Mathf.Max(0.02f, stepDuration);
            ReturnDuration = Mathf.Max(0.02f, returnDuration);
            TeleportDuration = Mathf.Max(0.02f, teleportDuration);
            Steps = new TileMoveStep[steps.Count];
            Positions = new Vector3[steps.Count];
            arrived = new bool[steps.Count];
            for (int i = 0; i < steps.Count; i++)
            {
                Steps[i] = steps[i];
                Positions[i] = getPosition(steps[i].Position);
            }
            this.onArrival = onArrival;
            this.onImpact = onImpact;
        }

        public void Arrive(int index)
        {
            if (IsCancelled || arrived[index]) return;
            arrived[index] = true;
            onArrival?.Invoke(index);
        }

        public void Impact(int pieceIndex, Vector3 position)
        {
            if (!IsCancelled && impacted.Add(pieceIndex)) onImpact?.Invoke(position);
        }

        public void Cancel() => IsCancelled = true;
    }
}
