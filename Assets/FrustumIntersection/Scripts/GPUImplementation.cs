using System.Diagnostics;
using UnityEngine;

namespace Optim.FrustumIntersection
{
    /// <summary>
    /// Compute shader based intersection implementation.
    /// </summary>
    public class GPUImplementation
    {
        private readonly ComputeShader shader;
        private readonly int kernel;

        public GPUImplementation(ComputeShader shader)
        {
            this.shader = shader;
            kernel = shader.FindKernel("CSMain");
        }

        /// <summary>
        /// Intersect mesh triangles using a compute shader.
        /// </summary>
        public IntersectionResult Intersect(Mesh mesh, Plane[] planes, IntersectionOptions options)
        {
            var vertices = mesh.vertices;
            var indices = mesh.triangles;
            int triCount = indices.Length / 3;

            var result = new IntersectionResult
            {
                Intersections = new bool[triCount]
            };
            Stopwatch watch = null;
            if (options.MeasureTime)
                watch = Stopwatch.StartNew();

            ComputeBuffer vbuf = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
            ComputeBuffer ibuf = new ComputeBuffer(indices.Length, sizeof(int));
            ComputeBuffer pbuf = new ComputeBuffer(planes.Length, sizeof(float) * 4);
            ComputeBuffer rbuf = new ComputeBuffer(triCount, sizeof(int));
            vbuf.SetData(vertices);
            ibuf.SetData(indices);
            pbuf.SetData(planes);

            shader.SetBuffer(kernel, "_Vertices", vbuf);
            shader.SetBuffer(kernel, "_Indices", ibuf);
            shader.SetBuffer(kernel, "_Planes", pbuf);
            shader.SetBuffer(kernel, "_Results", rbuf);
            shader.SetInt("_PlaneCount", planes.Length);

            uint threadGroupSizeX;
            shader.GetKernelThreadGroupSizes(kernel, out threadGroupSizeX, out _, out _);
            int groups = Mathf.CeilToInt((float)triCount / threadGroupSizeX);
            shader.Dispatch(kernel, groups, 1, 1);

            var results = new int[triCount];
            rbuf.GetData(results);
            for (int i = 0; i < triCount; ++i)
            {
                result.Intersections[i] = results[i] != 0;
            }

            vbuf.Dispose();
            ibuf.Dispose();
            pbuf.Dispose();
            rbuf.Dispose();

            if (watch != null)
            {
                watch.Stop();
                result.TimeSeconds = watch.ElapsedMilliseconds / 1000f;
            }

            return result;
        }
    }
}
