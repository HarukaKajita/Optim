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
            var result = new IntersectionResult();
            Stopwatch watch = null;
            if (options.MeasureTime)
            {
                watch = Stopwatch.StartNew();
            }

            var vertices = mesh.vertices;
            var indices = mesh.triangles;
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 v0 = vertices[indices[i]];
                Vector3 v1 = vertices[indices[i + 1]];
                Vector3 v2 = vertices[indices[i + 2]];
                if (checker.Intersects(v0, v1, v2, planes))
                {
                    if (options.CollectIndices)
                    {
                        result.IntersectedIndices.Add(i / 3);
                    }
                    else
                    {
                        break;
                    }
                }
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
