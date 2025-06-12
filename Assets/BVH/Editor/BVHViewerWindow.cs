using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Optim.BVH;

namespace Optim.BVH.Editor
{
    /// <summary>
    /// BVH の構造をツリービューで表示し、シーン上にバウンズを描画するウィンドウ。
    /// </summary>
    internal class BVHViewerWindow : EditorWindow
    {
        private SceneBVHTree targetTree;
        private Vector2 scroll;
        private readonly Dictionary<BVHNode, bool> foldouts = new();

        [MenuItem("Window/BVH Viewer")]
        private static void OpenFromMenu()
        {
            var tree = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<SceneBVHTree>() : null;
            GetWindow<BVHViewerWindow>("BVH Viewer").SetTarget(tree);
        }

        public static void Open(SceneBVHTree tree)
        {
            GetWindow<BVHViewerWindow>("BVH Viewer").SetTarget(tree);
        }

        private void OnEnable()
        {
            BVHGizmoDrawer.ActiveTree = targetTree;
        }

        private void OnDisable()
        {
            if (BVHGizmoDrawer.ActiveTree == targetTree)
                BVHGizmoDrawer.ActiveTree = null;
        }

        private void SetTarget(SceneBVHTree tree)
        {
            targetTree = tree;
            BVHGizmoDrawer.ActiveTree = tree;
            Repaint();
        }

        private void OnGUI()
        {
            if (targetTree == null || targetTree.Tree.Root == null)
            {
                EditorGUILayout.HelpBox("SceneBVHTree を選択してください", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawNode(targetTree.Tree.Root, 0);
            EditorGUILayout.EndScrollView();
        }

        private void DrawNode(BVHNode node, int depth)
        {
            if (node == null) return;
            bool expanded = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 16);
                if (!node.IsLeaf)
                {
                    foldouts.TryGetValue(node, out expanded);
                    expanded = EditorGUILayout.Foldout(expanded, $"Node {node.Bounds.center}", true);
                    foldouts[node] = expanded;
                }
                else
                {
                    GUILayout.Label($"Leaf ({node.Renderers.Count}) {node.Bounds.center}");
                }
            }
            if (expanded)
            {
                DrawNode(node.Left, depth + 1);
                DrawNode(node.Right, depth + 1);
            }
        }
    }
}
