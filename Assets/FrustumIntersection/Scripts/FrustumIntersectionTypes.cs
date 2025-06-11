using UnityEngine;

namespace Optim.FrustumIntersection
{
    /// <summary>
    /// Frustum representation type.
    /// </summary>
    public enum FrustumType
    {
        AccurateFrustum,
        SimplifiedFrustum
    }

    /// <summary>
    /// Options for intersection operation.
    /// </summary>
    public class IntersectionOptions
    {
        /// <summary>
        /// Frustum shape selection.
        /// </summary>
        public FrustumType Frustum = FrustumType.AccurateFrustum;

        /// <summary>
        /// When true, measure processing time in seconds.
        /// </summary>
        public bool MeasureTime = false;
    }

    /// <summary>
    /// Result of an intersection operation.
    /// </summary>
    public class IntersectionResult
    {
        /// <summary>
        /// True when the triangle at the corresponding index intersects the frustum.
        /// </summary>
        public bool[] Intersections;

        /// <summary>
        /// Processing time in seconds. Valid only when <see cref="IntersectionOptions.MeasureTime"/> is enabled.
        /// </summary>
        public float TimeSeconds;
    }
}
