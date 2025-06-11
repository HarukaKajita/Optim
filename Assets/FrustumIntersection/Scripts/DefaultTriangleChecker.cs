using UnityEngine;
using Unity.Collections;

namespace Optim.FrustumIntersection
{
    /// <summary>
    /// Default triangle-frustum intersection checker.
    /// </summary>
    public class DefaultTriangleChecker : ITriangleIntersectionChecker
    {
        public bool Intersects(Vector3 v0, Vector3 v1, Vector3 v2, NativeArray<Plane> planes)
        {
            int planeCount = planes.Length;
            for (int i = 0; i < planeCount; ++i)
            {
                Plane p = planes[i];
                float d0 = p.GetDistanceToPoint(v0);
                float d1 = p.GetDistanceToPoint(v1);
                float d2 = p.GetDistanceToPoint(v2);
                if (d0 < 0f && d1 < 0f && d2 < 0f)
                    return false;
            }
            return true;
        }
    }
}
