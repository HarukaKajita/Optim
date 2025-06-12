using System.Collections.Generic;
using UnityEngine;

namespace Optim.BVH
{
    /// <summary>
    /// Node of a bounding volume hierarchy.
    /// </summary>
    public class BVHNode
    {
        /// <summary>Bounds covering this node.</summary>
        public Bounds Bounds;
        /// <summary>Child node containing primitives on the left side.</summary>
        public BVHNode Left;
        /// <summary>Child node containing primitives on the right side.</summary>
        public BVHNode Right;
        /// <summary>Renderers contained in this leaf. Null if this node is internal.</summary>
        public List<Renderer> Renderers;

        /// <summary>Returns true if this node is a leaf node.</summary>
        public bool IsLeaf => Renderers != null;
    }
}
