using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Optim.BVH
{
    /// <summary>
    /// SAH (Surface Area Heuristic) を利用して BVH を構築するユーティリティクラス。
    /// </summary>
    [System.Serializable]
    public class BVHTree
    {
        /// <summary>構築された BVH のルートノード。</summary>
        [SerializeField, HideInInspector]
        private BVHNode root;
        /// <summary>BVH 構築に要した時間 (秒)。</summary>
        [SerializeField, HideInInspector]
        private float buildTimeSeconds;

        public BVHNode Root => root;
        public float BuildTimeSeconds => buildTimeSeconds;

        /// <summary>
        /// 現在のシーンに存在するすべての Renderer から BVH を構築します。
        /// </summary>
        public void BuildFromScene(int leafSize = 4)
        {
            Build(Object.FindObjectsOfType<Renderer>(), leafSize);
        }

        /// <summary>
        /// 渡された Renderer 群から BVH を構築します。
        /// </summary>
        public void Build(IReadOnlyList<Renderer> renderers, int leafSize = 4)
        {
            if (renderers == null || renderers.Count == 0)
            {
                root = null;
                buildTimeSeconds = 0f;
                return;
            }

            // 各 Renderer の AABB を事前に取得しておく
            var list = new List<RendererBounds>(renderers.Count);
            foreach (var r in renderers)
            {
                list.Add(new RendererBounds
                {
                    Renderer = r,
                    Bounds = r.bounds
                });
            }

            // 処理時間を計測しながら再帰的に BVH を構築
            var watch = Stopwatch.StartNew();
            root = BuildRecursive(list, leafSize);
            watch.Stop();
            buildTimeSeconds = watch.ElapsedMilliseconds / 1000f;
        }

        /// <summary>
        /// Renderer とその境界ボックスのセット。
        /// </summary>
        private struct RendererBounds
        {
            public Renderer Renderer;
            public Bounds Bounds;
        }

        /// <summary>
        /// 再帰的に BVH ノードを構築します。
        /// </summary>
        private static BVHNode BuildRecursive(List<RendererBounds> items, int leafSize)
        {
            // ノードが覆う境界を計算
            Bounds nodeBounds = items[0].Bounds;
            for (int i = 1; i < items.Count; ++i)
                nodeBounds.Encapsulate(items[i].Bounds);

            // 閾値以下なら葉ノードとして生成
            if (items.Count <= leafSize)
            {
                var node = new BVHNode
                {
                    Bounds = nodeBounds,
                    Renderers = new List<Renderer>(items.Count)
                };
                foreach (var rb in items)
                    node.Renderers.Add(rb.Renderer);
                return node;
            }

            // 最も長い軸でソートし、SAH で分割位置を求める
            int axis = LargestAxis(nodeBounds.size);
            items.Sort((a, b) => a.Bounds.center[axis].CompareTo(b.Bounds.center[axis]));

            int bestIndex = FindSplitIndex(items, axis);

            var leftList = items.GetRange(0, bestIndex);
            var rightList = items.GetRange(bestIndex, items.Count - bestIndex);
            var result = new BVHNode { Bounds = nodeBounds };
            result.Left = BuildRecursive(leftList, leafSize);
            result.Right = BuildRecursive(rightList, leafSize);
            return result;
        }

        /// <summary>
        /// SAH を用いて分割位置のインデックスを計算します。
        /// </summary>
        private static int FindSplitIndex(List<RendererBounds> items, int axis)
        {
            int count = items.Count;
            var leftBounds = new Bounds[count];
            var rightBounds = new Bounds[count];

            leftBounds[0] = items[0].Bounds;
            for (int i = 1; i < count; ++i)
            {
                leftBounds[i] = leftBounds[i - 1];
                leftBounds[i].Encapsulate(items[i].Bounds);
            }

            rightBounds[count - 1] = items[count - 1].Bounds;
            for (int i = count - 2; i >= 0; --i)
            {
                rightBounds[i] = rightBounds[i + 1];
                rightBounds[i].Encapsulate(items[i].Bounds);
            }

            float bestCost = float.MaxValue;
            int bestIndex = count / 2;
            for (int i = 1; i < count; ++i)
            {
                float leftArea = SurfaceArea(leftBounds[i - 1]);
                float rightArea = SurfaceArea(rightBounds[i]);
                float cost = leftArea * i + rightArea * (count - i);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        /// <summary>
        /// 与えられたサイズベクトルの中で最も大きい軸を返します。
        /// </summary>
        private static int LargestAxis(Vector3 size)
        {
            if (size.y > size.x && size.y >= size.z)
                return 1;
            if (size.z > size.x && size.z > size.y)
                return 2;
            return 0;
        }

        /// <summary>
        /// 境界ボックスの表面積を計算します。
        /// </summary>
        private static float SurfaceArea(Bounds b)
        {
            Vector3 s = b.size;
            return 2f * (s.x * s.y + s.y * s.z + s.z * s.x);
        }
    }
}
