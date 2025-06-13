using System.Collections.Generic;
using System.Linq;
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
        private VisualElement detailPane;
        private Label detailLabel;
        private ListView rendererList;

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
            var split = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);

            treeView = new TreeView
            {
                makeItem = MakeItem,
                bindItem = BindItem,
                selectionType = SelectionType.Single
            };
            treeView.selectionChanged += OnSelectionChanged;
            split.Add(treeView);

            detailPane = new VisualElement();
            detailPane.style.flexGrow = 1;
            detailPane.style.paddingLeft = 4;
            detailLabel = new Label();
            rendererList = new ListView();
            detailPane.Add(detailLabel);
            detailPane.Add(rendererList);
            split.Add(detailPane);

            rootVisualElement.Add(split);
            BVHGizmoDrawer.SelectedNode = null;
            if (BVHGizmoDrawer.SelectedNode != null)
                BVHGizmoDrawer.SelectedNode = null;
                int count = GetRendererCount(node);
                    ? $"Leaf ({count}) {node.Bounds.center}"
                    : $"Node ({count}) {node.Bounds.center}";
            }
        }

        private void OnSelectionChanged(IEnumerable<object> selected)
        {
            var node = selected != null ? System.Linq.Enumerable.FirstOrDefault(selected) as BVHNode : null;
            BVHGizmoDrawer.SelectedNode = node;
            UpdateDetails(node);
        }

        private void UpdateDetails(BVHNode node)
        {
            if (node == null)
            {
                detailLabel.text = string.Empty;
                rendererList.itemsSource = null;
                rendererList.Rebuild();
                return;
            }

            var renderers = CollectRenderers(node);
            detailLabel.text = $"Center: {node.Bounds.center}\nSize: {node.Bounds.size}\nRenderers: {renderers.Count}";

            if (rendererList.makeItem == null)
                rendererList.makeItem = () => new Label();
            if (rendererList.bindItem == null)
                rendererList.bindItem = (ve, i) => ((Label)ve).text = ((Renderer)rendererList.itemsSource[i]).name;
            rendererList.itemsSource = renderers;
            rendererList.Rebuild();
        }

        private static List<Renderer> CollectRenderers(BVHNode node)
        {
            var list = new List<Renderer>();
            CollectRecursive(node, list);
            return list;
        }

        private static void CollectRecursive(BVHNode node, List<Renderer> list)
        {
            if (node == null) return;
            if (node.IsLeaf)
            {
                list.AddRange(node.Renderers);
            }
            else
            {
                CollectRecursive(node.Left, list);
                CollectRecursive(node.Right, list);
        }

        private static int GetRendererCount(BVHNode node)
        {
            if (node == null) return 0;
            if (node.IsLeaf) return node.Renderers.Count;
            return GetRendererCount(node.Left) + GetRendererCount(node.Right);
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
                    ? $"Leaf ({node.Renderers.Count}) {volume:F2} m³"
                    : $"Node {volume:F2} m³";
            }
        }
    }
}
