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

            public void Execute(int index)
            {
                int i = index * 3;
                Vector3 v0 = Vertices[Indices[i]];
                Vector3 v1 = Vertices[Indices[i + 1]];
                Vector3 v2 = Vertices[Indices[i + 2]];
                bool inside = true;
                for (int p = 0; p < Planes.Length && inside; ++p)
                {
                    Plane pl = Planes[p];
                    float d0 = pl.GetDistanceToPoint(v0);
                    float d1 = pl.GetDistanceToPoint(v1);
                    float d2 = pl.GetDistanceToPoint(v2);
                    if (d0 < 0f && d1 < 0f && d2 < 0f)
                        inside = false;
                }

                Results[index] = inside ? 1 : 0;
            }
        }

        /// <summary>
        /// Intersect mesh triangles using the Job System.
        /// </summary>
        public IntersectionResult Intersect(Mesh mesh, Plane[] planes, IntersectionOptions options)
        {
            int triCount = mesh.triangles.Length / 3;
            var result = new IntersectionResult
            {
                Intersections = new bool[triCount]
            };
            Stopwatch watch = null;
            if (options.MeasureTime)
                watch = Stopwatch.StartNew();

            Mesh.MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            var vertices = new NativeArray<Vector3>(dataArray[0].vertexCount, Allocator.TempJob);
            dataArray[0].GetVertices(vertices);
            var indices = new NativeArray<int>(mesh.triangles, Allocator.TempJob);
            var jobPlanes = new NativeArray<Plane>(planes, Allocator.TempJob);

            var results = new NativeArray<int>(triCount, Allocator.TempJob);
            var job = new IntersectJob
            {
                Vertices = vertices,
                Indices = indices,
                Planes = jobPlanes,
                Results = results
            };

            JobHandle handle = job.Schedule(triCount, 64);
            handle.Complete();

            for (int i = 0; i < triCount; ++i)
            {
                result.Intersections[i] = results[i] != 0;
            }

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
