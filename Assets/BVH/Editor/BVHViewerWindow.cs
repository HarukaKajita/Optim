using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

namespace Optim.BVH.Editor
{
    /// <summary>
    /// BVH 構造を UIElements の TreeView で表示し、シーン上に境界を描画するエディタウィンドウ。
    /// </summary>
    internal class BVHViewerWindow : EditorWindow
    {
        private SceneBVHTree targetTree;
        private TreeView treeView;

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

        public void CreateGUI()
        {
            treeView = new TreeView
            {
                makeItem = MakeItem,
                bindItem = BindItem
            };
            treeView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            rootVisualElement.Add(treeView);
            RefreshTree();
        }

        /// <summary>
        /// ツリービューの各行に表示する Label を生成します。
        /// テキストが垂直方向の中央に配置されるようにスタイルを調整します。
        /// </summary>
        private VisualElement MakeItem()
        {
            var label = new Label();
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.alignSelf = Align.FlexStart;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.flexGrow = 1f;
            return label;
        }

        private void OnEnable()
        {
            BVHGizmoDrawer.ActiveTree = targetTree;
            RefreshTree();
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
            RefreshTree();
        }

        private void RefreshTree()
        {
            if (treeView == null)
                return;

            if (targetTree == null || targetTree.Tree.Root == null)
            {
                treeView.SetRootItems(new List<TreeViewItemData<BVHNode>>());
                return;
            }

            int id = 0;
            var rootItem = BuildItem(targetTree.Tree.Root, ref id);
            treeView.SetRootItems(new List<TreeViewItemData<BVHNode>> { rootItem });
            treeView.Rebuild();
        }

        private static TreeViewItemData<BVHNode> BuildItem(BVHNode node, ref int id)
        {
            int currentId = id++;
            if (node.IsLeaf)
            {
                return new TreeViewItemData<BVHNode>(currentId, node);
            }

            var children = new List<TreeViewItemData<BVHNode>>();
            if (node.Left != null)
                children.Add(BuildItem(node.Left, ref id));
            if (node.Right != null)
                children.Add(BuildItem(node.Right, ref id));
            return new TreeViewItemData<BVHNode>(currentId, node, children);
        }

        private void BindItem(VisualElement element, int index)
        {
            if (treeView.GetItemDataForIndex<BVHNode>(index) is BVHNode node)
            {
                var volume = node.Bounds.size.x * node.Bounds.size.y * node.Bounds.size.z;
                var label = element as Label;
                label.text = node.IsLeaf
                    ? $"Leaf ({node.Renderers.Count}) {volume}"
                    : $"Node {volume}";
            }
        }
    }
}
