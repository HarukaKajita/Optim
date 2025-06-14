using UnityEditor;
using UnityEngine;
using Optim.BVH;

namespace Optim.BVH.Editor
{
    /// <summary>
    /// BVH の境界をシーンビューに描画するヘルパークラス。
    /// </summary>
    [InitializeOnLoad]
    internal static class BVHGizmoDrawer
    {
        public static SceneBVHTree ActiveTree { get; set; }
        public static BVHNode SelectedNode { get; set; }

        static BVHGizmoDrawer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (ActiveTree == null || ActiveTree.Tree.Root == null)
                return;

            foreach (var node in ActiveTree.Tree.Traverse())
            {
                if (node == SelectedNode)
                    Handles.color = Color.yellow;
                else
                    Handles.color = new Color(0f, 1f, 0f, 0.25f);
                Handles.DrawWireCube(node.Bounds.center, node.Bounds.size);
            }
        }
    }
}
