using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor
{
    public sealed class TileMotionTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [Test]
        public void BombCollectsAtFirstArrivalWithoutUsingSawPolicy()
        {
            var root = new GameObject("Bomb policy test");
            try
            {
                var bomb = root.AddComponent<BombGimmick>();
                Assert.IsTrue(bomb.CollectsTileOnArrival);
                Assert.IsTrue(bomb.TriggerOnFirstPiece);
                Assert.IsFalse(bomb.ConsumesPieces);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(false)] [TestCase(true)]
        public void ReverseExposesBusyStateAsTileInputLock(bool square)
        {
            var root = new GameObject("Reverse policy test");
            try
            {
                GimmickBehavior reverse = square ? (GimmickBehavior)root.AddComponent<SquareReverseGimmick>() : root.AddComponent<ReverseGimmick>();
                Assert.IsFalse(reverse.BlocksTileInput);
                FieldInfo busy = reverse.GetType().GetField("isBusy", PrivateInstance);
                busy.SetValue(reverse, true);
                Assert.IsTrue(reverse.BlocksTileInput);
                busy.SetValue(reverse, false);
                Assert.IsFalse(reverse.BlocksTileInput);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ArrivalIsOncePerRouteIndexNotPerCell()
        {
            int arrivals = 0, impacts = 0;
            var path = new[] { new TileMoveStep(Vector2Int.one), new TileMoveStep(Vector2Int.one) };
            var task = new TileMotionTask(Vector3.zero, path, p => new Vector3(p.x, 0, p.y),
                TileMotionEnding.Return, .15f, .4f, .18f, _ => arrivals++, _ => impacts++);
            task.Arrive(0);
            task.Arrive(0);
            task.Arrive(1);
            task.Impact(0, Vector3.zero);
            task.Impact(0, Vector3.zero);
            task.Impact(1, Vector3.zero);
            Assert.AreEqual(2, arrivals);
            Assert.AreEqual(2, impacts);
        }

        [Test]
        public void CancellationSuppressesLateCallbacks()
        {
            int calls = 0;
            var task = new TileMotionTask(Vector3.zero, new[] { new TileMoveStep(Vector2Int.one) },
                _ => Vector3.one, TileMotionEnding.Shatter, .15f, .4f, .18f, _ => calls++, _ => calls++);
            task.Cancel();
            task.Cancel();
            task.Arrive(0);
            task.Impact(0, Vector3.one);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void TaskSnapshotsRouteAndWorldPositions()
        {
            var path = new List<TileMoveStep> { new TileMoveStep(Vector2Int.one) };
            Vector3 target = Vector3.one;
            var task = new TileMotionTask(Vector3.zero, path, _ => target, TileMotionEnding.Fall,
                0f, 0f, 0f, null, null);
            path.Clear();
            target = Vector3.zero;
            Assert.AreEqual(1, task.Steps.Length);
            Assert.AreEqual(Vector3.one, task.Positions[0]);
            Assert.Greater(task.StepDuration, 0f);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void BlockedPieceTiltsTowardsArrowAndRestoresPose(int direction)
        {
            string path = AssetDatabase.GUIDToAssetPath("59b6094d2ea3ec94880c85bb44ab7356");
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                TileBehavior tile = root.GetComponent<TileBehavior>();
                root.GetComponent<TileVisuals>().Init(tile);
                tile.OverrideDirection((HexaAwayDirection)direction, true);
                PieceTileMovement movement = root.GetComponent<PieceTileMovement>();
                movement.Init(tile);
                movement.CapturePose();
                int callbacks = 0;
                var task = new TileMotionTask(root.transform.position, new List<TileMoveStep>(), _ => Vector3.zero,
                    TileMotionEnding.Return, .15f, .4f, .18f, _ => callbacks++, _ => callbacks++);
                var pieces = (System.Array)typeof(PieceTileMovement).GetField("pieces", PrivateInstance).GetValue(movement);
                object leader = pieces.GetValue(0);
                var type = leader.GetType();
                Transform layer = (Transform)type.GetField("Transform").GetValue(leader);
                Vector3 startPosition = layer.position;
                Quaternion startRotation = layer.rotation;
                Vector3 forward = Quaternion.Euler(0f, DirectionHelper.Get(tile.Direction).Angle, 0f) * Vector3.forward;
                var support = new object[] { layer.rotation, forward, 0f, 0f, 0f };
                type.GetMethod("Support").Invoke(leader, support);
                float floor = ((Vector3)type.GetProperty("WorldCenter").GetValue(leader)).y + (float)support[2];
                var track = (IList)typeof(PieceTileMovement).GetMethod("BuildTrack", PrivateInstance)
                    .Invoke(movement, new object[] { task, 0 });
                Assert.AreEqual(1, track.Count);
                object clip = track[0];
                Assert.AreEqual(-1, clip.GetType().GetField("Step").GetValue(clip));
                MethodInfo sample = typeof(PieceTileMovement).GetMethod("Sample", PrivateInstance);
                for (int frame = 0; frame <= 50; frame++)
                {
                    float progress = frame / 50f;
                    sample.Invoke(movement, new[] { leader, clip, (object)progress });
                    support[0] = layer.rotation;
                    type.GetMethod("Support").Invoke(leader, support);
                    Vector3 center = (Vector3)type.GetProperty("WorldCenter").GetValue(leader);
                    Assert.GreaterOrEqual(center.y + (float)support[2], floor - .0001f);
                    if (frame == 20)
                    {
                        float angle = (float)typeof(PieceTileMovement).GetField("m_BlockedTiltAngle", PrivateInstance).GetValue(movement);
                        Quaternion expected = Quaternion.AngleAxis(angle, Vector3.Cross(Vector3.up, forward)) * startRotation;
                        Assert.Less(Quaternion.Angle(expected, layer.rotation), .05f);
                        Assert.Greater(Quaternion.Angle(startRotation, layer.rotation), 1f);
                    }
                }
                Assert.Less(Vector3.Distance(startPosition, layer.position), .0001f);
                Assert.Less(Quaternion.Angle(startRotation, layer.rotation), .05f);
                Assert.AreEqual(0, callbacks);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void WormholeReturnRestoresPieceRotationWithoutLandingSnap(int direction)
        {
            string path = AssetDatabase.GUIDToAssetPath("59b6094d2ea3ec94880c85bb44ab7356");
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                root.transform.SetPositionAndRotation(new Vector3(3f, 0.7f, -2f), Quaternion.Euler(0f, 60f, 0f));
                TileBehavior tile = root.GetComponent<TileBehavior>();
                root.GetComponent<TileVisuals>().Init(tile);
                PieceTileMovement movement = root.GetComponent<PieceTileMovement>();
                movement.Init(tile);
                var initialRotations = new Dictionary<Transform, Quaternion>();
                var piecesField = typeof(PieceTileMovement).GetField("pieces", PrivateInstance);
                MethodInfo build = typeof(PieceTileMovement).GetMethod("BuildTrack", PrivateInstance);
                MethodInfo sample = typeof(PieceTileMovement).GetMethod("Sample", PrivateInstance);
                foreach (object piece in (System.Array)piecesField.GetValue(movement))
                {
                    Transform layer = (Transform)piece.GetType().GetField("Transform").GetValue(piece);
                    initialRotations.Add(layer, layer.rotation);
                }

                // Repeat both flip parities without restoring the original stack order between moves.
                for (int repeat = 0; repeat < 4; repeat++)
                {
                    movement.CapturePose();
                    Vector3 origin = root.transform.position;
                    Quaternion heading = Quaternion.Euler(0f, direction * 60f, 0f);
                    Vector3 forward = heading * Vector3.forward * (Mathf.Sqrt(3f) * .52f);
                    Vector3 exit = origin + heading * new Vector3(2.25f, 0f, 3.1f);
                    var positions = new List<Vector3> { origin + forward, exit, exit + forward };
                    if (repeat % 2 != 0) positions.Add(exit + forward * 2f);
                    var route = new List<TileMoveStep>();
                    for (int i = 0; i < positions.Count; i++)
                        route.Add(new TileMoveStep(new Vector2Int(i, 0), i == 0 ? TileMoveStepType.TeleportEnter
                            : i == 1 ? TileMoveStepType.TeleportExit : TileMoveStepType.Move));
                    var task = new TileMotionTask(origin, route, p => positions[p.x], TileMotionEnding.Return,
                        .15f, .4f, .18f, null, null);
                    var pieces = (System.Array)piecesField.GetValue(movement);
                    var tracks = new IList[pieces.Length];
                    for (int i = 0; i < pieces.Length; i++)
                        tracks[i] = (IList)build.Invoke(movement, new object[] { task, i });
                    float previousHeight = float.NegativeInfinity;
                    for (int i = 0; i < pieces.Length; i++)
                    {
                        object piece = pieces.GetValue(i);
                        var type = piece.GetType();
                        Transform layer = (Transform)type.GetField("Transform").GetValue(piece);
                        foreach (object clip in tracks[i])
                        {
                            if (clip != tracks[i][tracks[i].Count - 1])
                            {
                                sample.Invoke(movement, new[] { piece, clip, (object)1f });
                                continue;
                            }
                            sample.Invoke(movement, new[] { piece, clip, (object)0f });
                            Quaternion departure = layer.rotation;
                            sample.Invoke(movement, new[] { piece, clip, (object).25f });
                            Assert.Greater(Quaternion.Angle(departure, layer.rotation), 20f, "Return lost its flip.");
                            sample.Invoke(movement, new[] { piece, clip, (object).999f });
                            Assert.Less(Quaternion.Angle(initialRotations[layer], layer.rotation), .1f, "Rotation snaps at landing.");
                            sample.Invoke(movement, new[] { piece, clip, (object)1f });
                        }
                        Assert.Less(Quaternion.Angle(initialRotations[layer], layer.rotation), .05f, "Return changed piece orientation.");
                        Vector3 center = (Vector3)type.GetProperty("WorldCenter").GetValue(piece);
                        Assert.That(center.x, Is.EqualTo(origin.x).Within(.001f));
                        Assert.That(center.z, Is.EqualTo(origin.z).Within(.001f));
                        Assert.Greater(center.y, previousHeight, "Departure order must become bottom-to-top order.");
                        previousHeight = center.y;
                        var support = new object[] { layer.rotation, Vector3.forward, 0f, 0f, 0f };
                        type.GetMethod("Support").Invoke(piece, support);
                        if (i == 0) Assert.That(center.y + (float)support[2], Is.EqualTo(origin.y).Within(.001f));
                    }
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void PieceReturnPreservesDepartureOrderAndGroundContact(int direction)
        {
            string path = AssetDatabase.GUIDToAssetPath("59b6094d2ea3ec94880c85bb44ab7356");
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                TileBehavior tile = root.GetComponent<TileBehavior>();
                TileVisuals visuals = root.GetComponent<TileVisuals>();
                PieceTileMovement movement = root.GetComponent<PieceTileMovement>();
                Assert.NotNull(tile);
                Assert.NotNull(visuals);
                Assert.NotNull(movement);
                visuals.Init(tile);
                movement.Init(tile);
                movement.CapturePose();
                Vector3 forward = Quaternion.Euler(0, direction * 60, 0) * Vector3.forward;
                var route = new List<TileMoveStep>();
                for (int i = 1; i <= 4; i++) route.Add(new TileMoveStep(new Vector2Int(0, i)));
                var task = new TileMotionTask(Vector3.zero, route, p => forward * (p.y * Mathf.Sqrt(3f) * .52f),
                    TileMotionEnding.Return, .15f, .4f, .18f, null, null);
                var pieces = (System.Array)typeof(PieceTileMovement).GetField("pieces", PrivateInstance).GetValue(movement);
                MethodInfo build = typeof(PieceTileMovement).GetMethod("BuildTrack", PrivateInstance);
                MethodInfo sample = typeof(PieceTileMovement).GetMethod("Sample", PrivateInstance);
                var tracks = new IList[3];
                // Snapshot every track before sampling any pose, just like the production scheduler.
                for (int i = 0; i < 3; i++) tracks[i] = (IList)build.Invoke(movement, new object[] { task, i });
                float topReturnStart = (float)tracks[0][tracks[0].Count - 1].GetType().GetField("StartTime").GetValue(tracks[0][tracks[0].Count - 1]);
                float middleReturnStart = (float)tracks[1][tracks[1].Count - 1].GetType().GetField("StartTime").GetValue(tracks[1][tracks[1].Count - 1]);
                float bottomReturnStart = (float)tracks[2][tracks[2].Count - 1].GetType().GetField("StartTime").GetValue(tracks[2][tracks[2].Count - 1]);
                Assert.Less(topReturnStart, middleReturnStart);
                Assert.Less(middleReturnStart, bottomReturnStart);
                for (int i = 0; i < 3; i++)
                {
                    object piece = pieces.GetValue(i);
                    var pieceType = piece.GetType();
                    Transform layer = (Transform)pieceType.GetField("Transform").GetValue(piece);
                    foreach (object clip in tracks[i])
                    {
                        for (int frame = 0; frame <= 30; frame++)
                        {
                            sample.Invoke(movement, new[] { piece, clip, (object)(frame / 30f) });
                            var arguments = new object[] { layer.rotation, forward, 0f, 0f, 0f };
                            pieceType.GetMethod("Support").Invoke(piece, arguments);
                            Vector3 center = (Vector3)pieceType.GetProperty("WorldCenter").GetValue(piece);
                            Assert.GreaterOrEqual(center.y + (float)arguments[2], -0.0001f, "Mesh penetrated the support plane.");
                        }
                    }
                }
                Transform first = root.transform.Find("ChildPiece_2");
                Transform second = root.transform.Find("ChildPiece_1");
                Transform third = root.transform.Find("Piece");
                float y0 = first.GetComponentInChildren<MeshRenderer>().bounds.center.y;
                float y1 = second.GetComponentInChildren<MeshRenderer>().bounds.center.y;
                float y2 = third.GetComponentInChildren<MeshRenderer>().bounds.center.y;
                Assert.Less(y0, y1);
                Assert.Less(y1, y2);
                Assert.That(first.GetComponentInChildren<MeshRenderer>().bounds.min.y, Is.EqualTo(0f).Within(.0001f));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
