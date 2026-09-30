using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Collections;
using UnityEditor;
#endif

namespace Lokas
{
    // Reads authored vertices once; no generated mesh, normal changes, or runtime collider.
    internal sealed class TilePieceGeometry
    {
        private static readonly Dictionary<Mesh, Vector3[]> meshVertices = new();
        public readonly Transform Transform;
        public readonly Vector3 Center;
        private readonly Vector3[] vertices;
        private readonly Vector3 scale;
        public readonly Vector3 LocalScale;

        public TilePieceGeometry(Transform layer)
        {
            Transform = layer;
            LocalScale = layer.localScale;
            scale = layer.lossyScale;
            var points = new List<Vector3>();
            foreach (MeshFilter filter in layer.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null) continue;
                if (!meshVertices.TryGetValue(mesh, out Vector3[] source))
                    meshVertices.Add(mesh, source = ReadMeshVertices(mesh));
                Matrix4x4 matrix = layer.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (Vector3 vertex in source) points.Add(matrix.MultiplyPoint3x4(vertex));
            }
            if (points.Count == 0) throw new System.InvalidOperationException("Tile layer requires an authored mesh.");
            vertices = points.ToArray();
            Bounds bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (Vector3 point in vertices) bounds.Encapsulate(point);
            Center = bounds.center;
        }

        public Vector3 WorldCenter => Transform.TransformPoint(Center);

        public void Apply(Vector3 center, Quaternion rotation, float size = 1f)
        {
            Transform.localScale = LocalScale * size;
            Transform.rotation = rotation;
            Transform.position = center - Transform.TransformVector(Center);
        }

        public void Support(Quaternion rotation, Vector3 direction, out float bottom, out float rear, out float front)
        {
            bottom = rear = float.PositiveInfinity;
            front = float.NegativeInfinity;
            foreach (Vector3 vertex in vertices)
            {
                Vector3 offset = rotation * Vector3.Scale(vertex - Center, scale);
                bottom = Mathf.Min(bottom, offset.y);
                float distance = Vector3.Dot(offset, direction);
                rear = Mathf.Min(rear, distance);
                front = Mathf.Max(front, distance);
            }
        }

        private static Vector3[] ReadMeshVertices(Mesh mesh)
        {
#if UNITY_EDITOR
            // Read the authored mesh without changing its Read/Write import setting.
            using (Mesh.MeshDataArray data = MeshUtility.AcquireReadOnlyMeshData(mesh))
            using (var vertices = new NativeArray<Vector3>(data[0].vertexCount, Allocator.Temp))
            {
                data[0].GetVertices(vertices);
                return vertices.ToArray();
            }
#else
            if (mesh.isReadable)
            {
                return mesh.vertices;
            }

            return ReadVertexBuffer(mesh);
#endif
        }

        private static Vector3[] ReadVertexBuffer(Mesh mesh)
        {
            const UnityEngine.Rendering.VertexAttribute attribute = UnityEngine.Rendering.VertexAttribute.Position;
            if (mesh.GetVertexAttributeFormat(attribute) != UnityEngine.Rendering.VertexAttributeFormat.Float32)
            {
                throw new System.InvalidOperationException("Tile motion requires Float32 mesh positions.");
            }

            int stream = mesh.GetVertexAttributeStream(attribute);
            int stride = mesh.GetVertexBufferStride(stream) / sizeof(float);
            int offset = mesh.GetVertexAttributeOffset(attribute) / sizeof(float);
            var bufferData = new float[mesh.vertexCount * stride];
            using (GraphicsBuffer buffer = mesh.GetVertexBuffer(stream))
            {
                buffer.GetData(bufferData);
            }

            var vertices = new Vector3[mesh.vertexCount];
            for (int i = 0; i < vertices.Length; i++)
            {
                int position = i * stride + offset;
                vertices[i] = new Vector3(bufferData[position], bufferData[position + 1], bufferData[position + 2]);
            }

            return vertices;
        }


    }
}
