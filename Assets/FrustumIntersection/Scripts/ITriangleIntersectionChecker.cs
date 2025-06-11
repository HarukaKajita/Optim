using UnityEngine;

namespace Optim.FrustumIntersection
{
    /// <summary>
    /// Per triangle intersection check interface.
    /// </summary>
    public interface ITriangleIntersectionChecker
    {
        /// <summary>
        /// Returns true when the triangle defined by vertices intersects the frustum planes.
        /// </summary>
        /// <param name="v0">Triangle vertex 0.</param>
        /// <param name="v1">Triangle vertex 1.</param>
        /// <param name="v2">Triangle vertex 2.</param>
        /// <param name="planes">Frustum planes.</param>
        bool Intersects(Vector3 v0, Vector3 v1, Vector3 v2, Plane[] planes);
    }
}
