using UnityEditor;
using UnityEngine;

namespace Optim.BVH.Editor
{
    /// <summary>
    /// BVH の境界をシーンビューに描画するヘルパークラス。
    /// </summary>
    [InitializeOnLoad]
    internal static class BVHGizmoDrawer
    {
        public static SceneBVHTree ActiveTree { get; set; }

        static BVHGizmoDrawer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (ActiveTree == null || ActiveTree.Tree.Root == null)
                return;

            Handles.color = Color.green;
            foreach (var node in ActiveTree.Tree.Traverse())
            {
                Handles.DrawWireCube(node.Bounds.center, node.Bounds.size);
            }
        }
    }
}
