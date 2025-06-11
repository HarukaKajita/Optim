using System.Collections.Generic;
using UnityEngine;

namespace Optim.FrustumIntersection
{
    /// <summary>
    /// Provides frustum intersection utilities.
    /// </summary>
    public class FrustumIntersectionSystem
    {
        private readonly CPUImplementation cpu;
        private readonly JobSystemImplementation jobSystem;
        private readonly GPUImplementation gpu;

        public FrustumIntersectionSystem(ComputeShader gpuShader = null)
        {
            var checker = new DefaultTriangleChecker();
            cpu = new CPUImplementation(checker);
            jobSystem = new JobSystemImplementation(checker);
            if (gpuShader != null)
                gpu = new GPUImplementation(gpuShader);
        }

        /// <summary>
        /// Compute frustum intersection using the selected implementation.
        /// </summary>
        public IntersectionResult Intersect(Mesh mesh, Camera cam, IntersectionOptions options, Implementation impl)
        {
            Plane[] planes = BuildPlanes(cam, options.Frustum);
            return impl switch
            {
                Implementation.CPU => cpu.Intersect(mesh, planes, options),
                Implementation.JobSystem => jobSystem.Intersect(mesh, planes, options),
                Implementation.GPU when gpu != null => gpu.Intersect(mesh, planes, options),
                _ => cpu.Intersect(mesh, planes, options)
            };
        }

        private Plane[] BuildPlanes(Camera cam, FrustumType type)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
            if (type == FrustumType.AccurateFrustum)
                return planes;

            var list = new List<Plane>();
            // skip near plane (index 4 or 5 depending on Unity version). Use names to be safe.
            for (int i = 0; i < planes.Length; ++i)
            {
                // Unity order: left, right, bottom, top, near, far
                if (i == 4) // near plane index
                    continue;
                list.Add(planes[i]);
            }
            return list.ToArray();
        }
    }

    /// <summary>
    /// Selectable implementations.
    /// </summary>
    public enum Implementation
    {
        CPU,
        JobSystem,
        GPU
    }
}
