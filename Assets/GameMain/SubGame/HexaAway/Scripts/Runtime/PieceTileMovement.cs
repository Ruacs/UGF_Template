using UnityEngine;
using System.Collections.Generic;

namespace Lokas
{
    public sealed class PieceTileMovement : TileMovement
    {
        [SerializeField] private Transform[] m_ChildPieces;
        [SerializeField, Min(0.02f)] private float m_StepDuration = 0.15f;
        [SerializeField, Min(0.02f)] private float m_ChildDelay = 0.13f;
        [SerializeField, Min(0.02f)] private float m_ReturnDuration = 0.4f;
        [SerializeField, Min(0f)] private float m_ReturnArcHeight = 0.8f;
        [SerializeField, Min(0.02f)] private float m_FallDuration = 0.18f;
        [SerializeField, Min(0.1f)] private float m_FallDepth = 1.35f;
        [SerializeField, Range(1f, 45f)] private float m_BlockedTiltAngle = 18f;
        [SerializeField, Min(0.02f)] private float m_BlockedDuration = 0.32f;
        private TilePieceGeometry[] pieces;
        private Vector3[] savedPositions;
        private Quaternion[] savedRotations;
        private PieceTileVisuals visuals;

        private enum ClipKind { Roll, Enter, Exit, Return, Fall, Shatter, Blocked }
        private sealed class Clip
        {
            public ClipKind Kind;
            public float StartTime, Duration, Floor, StartSupport, EndSupport;
            public Vector3 Start, End, Axis, Direction;
            public Quaternion Rotation, LandingRotation;
            public int Step = -1;
            public bool Reported;
        }

        public override void OnInit()
        {
            if (m_ChildPieces == null || m_ChildPieces.Length == 0)
                m_ChildPieces = new[] { transform.Find("Piece"), transform.Find("ChildPiece_1"), transform.Find("ChildPiece_2") };
            pieces = new TilePieceGeometry[m_ChildPieces.Length];
            for (int i = 0; i < pieces.Length; i++) pieces[i] = new TilePieceGeometry(m_ChildPieces[i]);
            savedPositions = new Vector3[pieces.Length];
            savedRotations = new Quaternion[pieces.Length];
            visuals = GetComponent<PieceTileVisuals>();
        }

        public override void CapturePose()
        {
            base.CapturePose();
            System.Array.Sort(pieces, (a, b) => b.WorldCenter.y.CompareTo(a.WorldCenter.y));
            for (int i = 0; i < pieces.Length; i++)
            {
                savedPositions[i] = pieces[i].Transform.localPosition;
                savedRotations[i] = pieces[i].Transform.localRotation;
            }
        }

        public override void RestorePose()
        {
            base.RestorePose();
            for (int i = 0; i < pieces.Length; i++)
            {
                Transform piece = pieces[i].Transform;
                piece.localPosition = savedPositions[i];
                piece.localRotation = savedRotations[i];
                piece.localScale = pieces[i].LocalScale;
                piece.gameObject.SetActive(true);
            }
            visuals?.SetMoving(false);
        }

        public override System.Collections.IEnumerator Execute(TileMotionTask task)
        {
            bool blockedAtOrigin = task.Steps.Length == 0 && task.Ending == TileMotionEnding.Return;
            if (task.Steps.Length == 0 && !blockedAtOrigin) yield break;
            visuals?.SetMoving(true);
            int movingPieces = blockedAtOrigin ? 1 : pieces.Length;
            var tracks = new List<Clip>[movingPieces];
            var cursors = new int[movingPieces];
            float duration = 0f;
            for (int i = 0; i < movingPieces; i++)
            {
                tracks[i] = BuildTrack(task, i);
                Clip last = tracks[i][tracks[i].Count - 1];
                duration = Mathf.Max(duration, last.StartTime + last.Duration);
            }

            // A single clock owns every pose and completion. No delayed child coroutines survive cancellation.
            for (float elapsed = 0f; ; elapsed = Mathf.Min(duration, elapsed + Time.deltaTime))
            {
                for (int i = 0; i < movingPieces && !task.IsCancelled; i++)
                {
                    while (cursors[i] < tracks[i].Count)
                    {
                        Clip clip = tracks[i][cursors[i]];
                        if (elapsed < clip.StartTime) break;
                        float progress = Mathf.Clamp01((elapsed - clip.StartTime) / clip.Duration);
                        Sample(pieces[i], clip, progress);
                        if (clip.Kind == ClipKind.Shatter) task.Impact(i, clip.Start);
                        if (progress >= 1f && !clip.Reported)
                        {
                            clip.Reported = true;
                            if (clip.Step >= 0) task.Arrive(clip.Step);
                        }
                        if (progress < 1f || task.IsCancelled) break;
                        cursors[i]++;
                    }
                }
                if (task.IsCancelled) yield break;
                if (elapsed >= duration) break;
                yield return null;
            }

            if (blockedAtOrigin) RestorePose();
            if (task.Ending == TileMotionEnding.Stop)
            {
                // Commit the logical root only after followers finish; keep their world poses.
                Vector3 delta = task.Positions[task.Positions.Length - 1] - transform.position;
                transform.position += delta;
                foreach (TilePieceGeometry piece in pieces) piece.Transform.position -= delta;
            }
            if (task.Ending == TileMotionEnding.Stop || task.Ending == TileMotionEnding.Return)
                visuals?.SetMoving(false);
        }

        private List<Clip> BuildTrack(TileMotionTask task, int index)
        {
            TilePieceGeometry piece = pieces[index];
            var result = new List<Clip>();
            float time = index * Mathf.Max(0.02f, m_ChildDelay);
            float stepDuration = Mathf.Max(0.02f, m_StepDuration);
            Vector3 center = piece.WorldCenter;
            Quaternion rotation = piece.Transform.rotation;
            if (task.Steps.Length == 0 && task.Ending == TileMotionEnding.Return)
            {
                Vector3 direction = Quaternion.Euler(0f, DirectionHelper.Get(behavior.Direction).Angle, 0f) * Vector3.forward;
                piece.Support(rotation, direction, out float baseHeight, out _, out float front);
                // Rock around the forward lower edge while keeping the two supporting pieces still.
                result.Add(new Clip { Kind = ClipKind.Blocked, Duration = Mathf.Max(0.02f, m_BlockedDuration),
                    Start = center, End = center + direction * front + Vector3.up * baseHeight,
                    Rotation = rotation, Axis = Vector3.Cross(Vector3.up, direction), Direction = direction,
                    Floor = center.y + baseHeight });
                return result;
            }
            Vector3 horizontalOffset = center - task.Origin;
            horizontalOffset.y = 0f;
            piece.Support(rotation, Vector3.forward, out float bottom, out _, out _);
            float startSupport = center.y + bottom - task.Origin.y;
            // Return order is departure order: leader -> bottom, next -> middle, last -> top.
            TilePieceGeometry slot = pieces[pieces.Length - 1 - index];
            slot.Support(slot.Transform.rotation, Vector3.forward, out float slotBottom, out _, out _);
            float slotSupport = slot.WorldCenter.y + slotBottom - task.Origin.y;
            Vector3 previous = task.Origin;
            Vector3 lastDirection = Vector3.forward;

            for (int step = 0; step < task.Steps.Length; step++)
            {
                Vector3 target = task.Positions[step] + horizontalOffset;
                bool exit = task.Steps[step].Type == TileMoveStepType.TeleportExit;
                if (exit)
                {
                    piece.Support(rotation, lastDirection, out bottom, out _, out _);
                    target.y -= bottom;
                    result.Add(new Clip { Kind = ClipKind.Exit, StartTime = time, Duration = task.TeleportDuration,
                        Start = target, End = target, Rotation = rotation, Step = step });
                    time += task.TeleportDuration;
                    center = target;
                }
                else
                {
                    Vector3 direction = task.Positions[step] - previous;
                    direction.y = 0f;
                    direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : lastDirection;
                    Vector3 axis = Vector3.Cross(Vector3.up, direction).normalized;
                    Quaternion landing = Quaternion.AngleAxis(180f, axis) * rotation;
                    piece.Support(landing, direction, out bottom, out _, out _);
                    float endSupport = task.Ending == TileMotionEnding.Stop && step == task.Steps.Length - 1 ? slotSupport : 0f;
                    target.y += endSupport - bottom;
                    result.Add(new Clip { Kind = ClipKind.Roll, StartTime = time, Duration = stepDuration,
                        Start = center, End = target, Rotation = rotation, Axis = axis, Direction = direction,
                        Floor = task.Positions[step].y, StartSupport = step == 0 ? startSupport : 0f,
                        EndSupport = endSupport, Step = step });
                    time += stepDuration;
                    center = target;
                    rotation = landing;
                    lastDirection = direction;
                }
                if (task.Steps[step].Type == TileMoveStepType.TeleportEnter)
                {
                    result.Add(new Clip { Kind = ClipKind.Enter, StartTime = time, Duration = task.TeleportDuration,
                        Start = center, End = center, Rotation = rotation });
                    time += task.TeleportDuration;
                }
                previous = task.Positions[step];
            }

            if (task.Ending == TileMotionEnding.Return)
            {
                Vector3 direction = task.Origin - previous;
                direction.y = 0f;
                direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : -lastDirection;
                Vector3 axis = Vector3.Cross(Vector3.up, direction).normalized;
                Quaternion landing = transform.rotation * savedRotations[index];
                piece.Support(landing, direction, out bottom, out _, out _);
                Vector3 target = task.Origin + horizontalOffset;
                target.y += slotSupport - bottom;
                result.Add(new Clip { Kind = ClipKind.Return, StartTime = time, Duration = Mathf.Max(0.02f, m_ReturnDuration),
                    Start = center, End = target, Rotation = rotation, LandingRotation = landing,
                    Axis = axis, Direction = direction, Floor = task.Origin.y });
            }
            else if (task.Ending == TileMotionEnding.Fall || task.Ending == TileMotionEnding.Shatter)
            {
                result.Add(new Clip { Kind = task.Ending == TileMotionEnding.Fall ? ClipKind.Fall : ClipKind.Shatter,
                    StartTime = time, Duration = Mathf.Max(0.02f, m_FallDuration), Start = center, End = center,
                    Rotation = rotation, Axis = Vector3.Cross(Vector3.up, lastDirection) });
            }
            return result;
        }

        private void Sample(TilePieceGeometry piece, Clip clip, float progress)
        {
            float t = progress * progress * (3f - 2f * progress);
            Vector3 center = clip.Start;
            Quaternion rotation = clip.Rotation;
            float scale = 1f;
            if (clip.Kind == ClipKind.Blocked)
            {
                float tilt = progress < 0.4f ? Mathf.SmoothStep(0f, 1f, progress / 0.4f)
                    : 1f - Mathf.SmoothStep(0f, 1f, (progress - 0.4f) / 0.6f);
                Quaternion flip = Quaternion.AngleAxis(Mathf.Clamp(m_BlockedTiltAngle, 1f, 45f) * tilt, clip.Axis);
                rotation = flip * rotation;
                center = clip.End + flip * (clip.Start - clip.End);
                piece.Support(rotation, clip.Direction, out float bottom, out _, out _);
                center.y = Mathf.Max(center.y, clip.Floor - bottom);
                if (progress >= 1f) { center = clip.Start; rotation = clip.Rotation; }
            }
            else if (clip.Kind == ClipKind.Roll)
            {
                Quaternion flip = Quaternion.AngleAxis(180f * t, clip.Axis);
                rotation = flip * rotation;
                Vector3 pivot = (clip.Start + clip.End) * 0.5f;
                // Edge-axis horizontal trajectory; support from real vertices keeps the mesh on the surface.
                Vector3 radius = clip.Start - pivot;
                radius.y = 0f;
                center = pivot + flip * radius;
                piece.Support(rotation, clip.Direction, out float bottom, out float rear, out _);
                float support = clip.Floor;
                if (clip.StartSupport > 0f)
                {
                    piece.Support(clip.Rotation, clip.Direction, out _, out _, out float front);
                    piece.Support(Quaternion.AngleAxis(180f, clip.Axis) * clip.Rotation, clip.Direction,
                        out _, out float landingRear, out _);
                    float clearance = Vector3.Dot(center - clip.Start, clip.Direction) + rear - front;
                    float landingClearance = Vector3.Dot(clip.End - clip.Start, clip.Direction) + landingRear - front;
                    float release = landingClearance > 0.0001f ? Mathf.Clamp01(clearance / landingClearance)
                        : Mathf.InverseLerp(0.85f, 1f, t);
                    release = release * release * (3f - 2f * release);
                    support += clip.StartSupport * (1f - release);
                }
                support += clip.EndSupport * t;
                center.y = support - bottom;
                if (progress >= 1f) center = clip.End;
            }
            else if (clip.Kind == ClipKind.Return)
            {
                rotation = Quaternion.AngleAxis(180f * t, clip.Axis) * rotation;
                // A teleport can make the return axis non-hexagonal. Correct in flight, not on landing.
                Quaternion naturalLanding = Quaternion.AngleAxis(180f, clip.Axis) * clip.Rotation;
                Quaternion correction = clip.LandingRotation * Quaternion.Inverse(naturalLanding);
                float alignment = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, t));
                rotation = Quaternion.Slerp(Quaternion.identity, correction, alignment) * rotation;
                if (progress >= 1f) rotation = clip.LandingRotation;
                center = Vector3.Lerp(clip.Start, clip.End, t);
                center.y += 4f * m_ReturnArcHeight * t * (1f - t);
                piece.Support(rotation, clip.Direction, out float bottom, out _, out _);
                center.y = Mathf.Max(center.y, clip.Floor - bottom);
            }
            else if (clip.Kind == ClipKind.Enter) scale = Mathf.Lerp(1f, 0.05f, t);
            else if (clip.Kind == ClipKind.Exit) scale = Mathf.Lerp(0.05f, 1f, t);
            else
            {
                float fall = progress * progress;
                if (clip.Kind == ClipKind.Fall)
                {
                    center.y -= m_FallDepth * fall;
                    rotation = Quaternion.AngleAxis(110f * fall, clip.Axis) * rotation;
                }
                scale = Mathf.Lerp(1f, 0.05f, fall);
            }
            piece.Apply(center, rotation, scale);
            piece.Transform.gameObject.SetActive(progress < 1f || (clip.Kind != ClipKind.Fall && clip.Kind != ClipKind.Shatter));
        }
    }
}
