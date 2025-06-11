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
            [WriteOnly] public NativeList<int>.ParallelWriter Results;
            public bool Collect;

            public ITriangleIntersectionChecker Checker;

            public void Execute(int index)
            {
                int i = index * 3;
                Vector3 v0 = Vertices[Indices[i]];
                Vector3 v1 = Vertices[Indices[i + 1]];
                Vector3 v2 = Vertices[Indices[i + 2]];
                if (Checker.Intersects(v0, v1, v2, Planes.ToArray()))
                {
                    if (Collect)
                        Results.AddNoResize(index);
                }
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

            var results = new NativeList<int>(Allocator.TempJob);
            var job = new IntersectJob
            {
                Vertices = vertices,
                Indices = indices,
                Planes = jobPlanes,
                Results = results.AsParallelWriter(),
                Collect = options.CollectIndices,
                Checker = checker
            };

            JobHandle handle = job.Schedule(mesh.triangles.Length / 3, 64);
            handle.Complete();

            if (options.CollectIndices)
                result.IntersectedIndices.AddRange(results.AsArray().ToArray());

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
