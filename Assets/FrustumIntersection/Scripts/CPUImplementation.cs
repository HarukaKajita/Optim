using System.Diagnostics;
using UnityEngine;

namespace Optim.FrustumIntersection
{
    /// <summary>
    /// CPU based intersection implementation.
    /// </summary>
    public class CPUImplementation
    {
        private readonly ITriangleIntersectionChecker checker;

        public CPUImplementation(ITriangleIntersectionChecker checker)
        {
            this.checker = checker;
        }

        /// <summary>
        /// Intersect mesh triangles against the provided frustum planes.
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
            {
                watch = Stopwatch.StartNew();
            }

            var vertices = mesh.vertices;
            var indices = mesh.triangles;
            for (int i = 0; i < indices.Length; i += 3)
            {
                int triIndex = i / 3;
                Vector3 v0 = vertices[indices[i]];
                Vector3 v1 = vertices[indices[i + 1]];
                Vector3 v2 = vertices[indices[i + 2]];
                result.Intersections[triIndex] = checker.Intersects(v0, v1, v2, planes);
            }

            if (watch != null)
            {
                watch.Stop();
                result.TimeSeconds = watch.ElapsedMilliseconds / 1000f;
            }

            return result;
        }
    }
}
