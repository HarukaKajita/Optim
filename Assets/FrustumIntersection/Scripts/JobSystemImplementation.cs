using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Optim.FrustumIntersection
{
    /// <summary>
    /// Job System based intersection implementation.
    /// </summary>
    public class JobSystemImplementation
    {
        private readonly ITriangleIntersectionChecker checker;

        public JobSystemImplementation(ITriangleIntersectionChecker checker)
        {
            this.checker = checker;
        }

        private struct IntersectJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> Vertices;
            [ReadOnly] public NativeArray<int> Indices;
            [ReadOnly] public NativeArray<Plane> Planes;
            [WriteOnly] public NativeArray<int> Results;

            public ITriangleIntersectionChecker Checker;

            public void Execute(int index)
            {
                int i = index * 3;
                Vector3 v0 = Vertices[Indices[i]];
                Vector3 v1 = Vertices[Indices[i + 1]];
                Vector3 v2 = Vertices[Indices[i + 2]];
                bool inside = Checker.Intersects(v0, v1, v2, Planes);
                Results[index] = inside ? 1 : 0;
            }
        }

        /// <summary>
        /// Intersect mesh triangles using the Job System.
        /// </summary>
        public IntersectionResult Intersect(Mesh mesh, Plane[] planes, IntersectionOptions options)
        {
            var result = new IntersectionResult();
            Stopwatch watch = null;
            if (options.MeasureTime)
                watch = Stopwatch.StartNew();

            Mesh.MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            var vertices = new NativeArray<Vector3>(dataArray[0].vertexCount, Allocator.TempJob);
            dataArray[0].GetVertices(vertices);
            var indices = new NativeArray<int>(mesh.triangles, Allocator.TempJob);
            var jobPlanes = new NativeArray<Plane>(planes, Allocator.TempJob);

            int triCount = mesh.triangles.Length / 3;
            var results = new NativeArray<int>(triCount, Allocator.TempJob);
            var job = new IntersectJob
            {
                Vertices = vertices,
                Indices = indices,
                Planes = jobPlanes,
                Results = results,
                Checker = checker
            };

            JobHandle handle = job.Schedule(triCount, 64);
            handle.Complete();

            result.Intersections = new bool[triCount];
            for (int i = 0; i < triCount; ++i)
                result.Intersections[i] = results[i] != 0;

            results.Dispose();
            vertices.Dispose();
            indices.Dispose();
            jobPlanes.Dispose();
            dataArray.Dispose();

            if (watch != null)
            {
                watch.Stop();
                result.TimeSeconds = watch.ElapsedMilliseconds / 1000f;
            }

            return result;
        }
    }
}
