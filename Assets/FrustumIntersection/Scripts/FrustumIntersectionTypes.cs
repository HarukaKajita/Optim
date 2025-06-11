using System.Collections.Generic;
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
        /// When true, collect intersecting triangle indices.
        /// </summary>
        public bool CollectIndices = false;

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
        /// Indices of intersecting triangles when <see cref="IntersectionOptions.CollectIndices"/> is enabled.
        /// </summary>
        public List<int> IntersectedIndices = new();

        /// <summary>
        /// Processing time in seconds. Valid only when <see cref="IntersectionOptions.MeasureTime"/> is enabled.
        /// </summary>
        public float TimeSeconds;
    }
}
