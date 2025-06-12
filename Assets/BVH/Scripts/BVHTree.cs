using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Optim.BVH
{
    /// <summary>
    /// Utility class that builds a BVH from scene renderers using SAH.
    /// </summary>
    public class BVHTree
    {
        public BVHNode Root { get; private set; }
        public float BuildTimeSeconds { get; private set; }

        /// <summary>
        /// Build a BVH from all renderers in the current scene.
        /// </summary>
        public void BuildFromScene(int leafSize = 4)
        {
            Build(Object.FindObjectsOfType<Renderer>(), leafSize);
        }

        /// <summary>
        /// Build a BVH from a set of renderers.
        /// </summary>
        public void Build(IReadOnlyList<Renderer> renderers, int leafSize = 4)
        {
            var list = new List<RendererBounds>(renderers.Count);
            foreach (var r in renderers)
                list.Add(new RendererBounds { Renderer = r, Bounds = r.bounds });

            var watch = Stopwatch.StartNew();
            Root = BuildRecursive(list, leafSize);
            watch.Stop();
            BuildTimeSeconds = watch.ElapsedMilliseconds / 1000f;
        }

        private struct RendererBounds
        {
            public Renderer Renderer;
            public Bounds Bounds;
        }

        private static BVHNode BuildRecursive(List<RendererBounds> items, int leafSize)
        {
            Bounds nodeBounds = items[0].Bounds;
            for (int i = 1; i < items.Count; ++i)
                nodeBounds.Encapsulate(items[i].Bounds);

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

        private static int LargestAxis(Vector3 size)
        {
            if (size.y > size.x && size.y >= size.z)
                return 1;
            if (size.z > size.x && size.z > size.y)
                return 2;
            return 0;
        }

        private static float SurfaceArea(Bounds b)
        {
            Vector3 s = b.size;
            return 2f * (s.x * s.y + s.y * s.z + s.z * s.x);
        }
    }
}
