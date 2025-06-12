using System.Collections.Generic;
using UnityEngine;

namespace Optim.BVH
{
    /// <summary>
    /// Bounding Volume Hierarchy のノードを表します。
    /// </summary>
    public class BVHNode
    {
        /// <summary>このノード全体を包む境界ボックス。</summary>
        public Bounds Bounds;
        /// <summary>左側のオブジェクトを保持する子ノード。</summary>
        public BVHNode Left;
        /// <summary>右側のオブジェクトを保持する子ノード。</summary>
        public BVHNode Right;
        /// <summary>葉ノードの場合に保持する Renderer リスト。内部ノードの場合は <c>null</c>。</summary>
        public List<Renderer> Renderers;

        /// <summary>葉ノードであるかどうか。</summary>
        public bool IsLeaf => Renderers != null;
    }
}
