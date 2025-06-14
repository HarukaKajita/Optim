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
        private TwoPaneSplitView splitView;
        private VisualElement leftPane;
        private VisualElement rightPane;
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
            Debug.Log($"Opening BVH Viewer for {tree?.name ?? "null"}");
            GetWindow<BVHViewerWindow>("BVH Viewer").SetTarget(tree);
        }

        public void CreateGUI()
        {
            Debug.Log("Creating BVH Viewer Window");
            
            // ツールバーを追加
            var toolbar = new Toolbar();
            var refreshButton = new ToolbarButton(() => RefreshTree()) { text = "Refresh" };
            var debugButton = new ToolbarButton(() => DebugTreeViewItems()) { text = "Debug Items" };
            var targetField = new ObjectField("Target") { objectType = typeof(SceneBVHTree) };
            targetField.RegisterValueChangedCallback(evt => SetTarget(evt.newValue as SceneBVHTree));
            targetField.value = targetTree;
            
            toolbar.Add(targetField);
            toolbar.Add(refreshButton);
            toolbar.Add(debugButton);
            rootVisualElement.Add(toolbar);
            
            splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            // VisualElementでTreeViewをラップしないとSplitView内で正しく表示されないため、ラップする
            leftPane = new VisualElement();
            leftPane.style.flexGrow = 1;
            splitView.Add(leftPane);
            rightPane = new VisualElement();
            rightPane.style.flexGrow = 1;
            splitView.Add(rightPane);
            
            // TreeViewの初期化
            treeView = new TreeView
            {
                makeItem = MakeItem,
                bindItem = BindItem,
                selectionType = SelectionType.Single
            };
            treeView.style.flexGrow = 1;
            treeView.selectionChanged += OnSelectionChanged;
            leftPane.Add(treeView);
            
            // detailPaneの初期化
            detailPane = new VisualElement();
            detailPane.style.flexGrow = 1;
            detailPane.style.paddingLeft = 4;
            detailLabel = new Label();
            rendererList = new ListView();
            rendererList.style.flexGrow = 1;
            rendererList.style.flexShrink = 1;
            detailPane.Add(detailLabel);
            detailPane.Add(rendererList);
            rightPane.Add(detailPane);

            rootVisualElement.Add(splitView);
            BVHGizmoDrawer.SelectedNode = null;
        }

        private void OnSelectionChanged(IEnumerable<object> selected)
        {
            var node = selected != null ? Enumerable.FirstOrDefault(selected) as BVHNode : null;
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
            label.style.flexGrow = 1f;
            label.style.flexShrink = 0;
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
            Debug.Log($"Setting target BVH Tree: {tree?.name ?? "null"}");
            targetTree = tree;
            BVHGizmoDrawer.ActiveTree = tree;
            RefreshTree();
        }

        private void RefreshTree()
        {
            Debug.Log($"Refreshing BVH Tree for {targetTree?.name ?? "null"}");
            if (treeView == null)
            {
                Debug.LogWarning("TreeView is not initialized.");
                return;
            }

            if (targetTree == null)
            {
                Debug.LogWarning("Target tree is null.");
                treeView.SetRootItems(new List<TreeViewItemData<BVHNode>>());
                return;
            }

            if (targetTree.Tree == null)
            {
                Debug.LogWarning("Target tree.Tree is null.");
                treeView.SetRootItems(new List<TreeViewItemData<BVHNode>>());
                return;
            }

            if (targetTree.Tree.Root == null)
            {
                Debug.LogWarning("Target tree.Tree.Root is null.");
                treeView.SetRootItems(new List<TreeViewItemData<BVHNode>>());
                return;
            }
            
            Debug.Log($"Building tree for {targetTree.name} with root node {targetTree.Tree.Root.Bounds}");
            int id = 0;
            var rootItem = BuildItem(targetTree.Tree.Root, ref id);
            var rootItems = new List<TreeViewItemData<BVHNode>> { rootItem };
            Debug.Log($"Created {rootItems.Count} root items with total {id} nodes");
            treeView.SetRootItems(rootItems);
            treeView.Rebuild();
            leftPane.Clear();
            leftPane.Add(treeView);
            
            // TreeViewを展開して子要素を表示
            treeView.ExpandAll();
            Debug.Log("TreeView rebuild and expand completed");
        }

        private static TreeViewItemData<BVHNode> BuildItem(BVHNode node, ref int id)
        {
            int currentId = id++;
            Debug.Log($"Building item ID={currentId}, IsLeaf={node.IsLeaf}, Bounds={node.Bounds}");
            
            if (node.IsLeaf)
            {
                Debug.Log($"Creating leaf item ID={currentId} with {node.Renderers?.Count ?? 0} renderers");
                return new TreeViewItemData<BVHNode>(currentId, node);
            }

            var children = new List<TreeViewItemData<BVHNode>>();
            if (node.Left != null)
            {
                Debug.Log($"Adding left child to item ID={currentId}");
                children.Add(BuildItem(node.Left, ref id));
            }
            if (node.Right != null)
            {
                Debug.Log($"Adding right child to item ID={currentId}");
                children.Add(BuildItem(node.Right, ref id));
            }
            Debug.Log($"Creating branch item ID={currentId} with {children.Count} children");
            return new TreeViewItemData<BVHNode>(currentId, node, children);
        }

        private void BindItem(VisualElement element, int index)
        {
            Debug.Log($"BindItem called for index {index}");
            if (treeView.GetItemDataForIndex<BVHNode>(index) is BVHNode node)
            {
                var volume = node.Bounds.size.x * node.Bounds.size.y * node.Bounds.size.z;
                int count = GetRendererCount(node);
                var label = element as Label;
                var text = node.IsLeaf
                    ? $"Leaf ({count} renderers) {volume:F2} m³"
                    : $"Node ({count} renderers) {volume:F2} m³";
                label.text = text;
                Debug.Log($"Bound item {index}: {text}");
            }
            else
            {
                Debug.LogWarning($"Failed to get item data for index {index}");
            }
        }

        private void DebugTreeViewItems()
        {
            Debug.Log("=== TreeView Debug Info ===");
            Debug.Log($"TreeView is null: {treeView == null}");
            Debug.Log($"Target tree is null: {targetTree == null}");
            
            if (targetTree != null)
            {
                Debug.Log($"Target tree name: {targetTree.name}");
                Debug.Log($"Target tree.Tree is null: {targetTree.Tree == null}");
                if (targetTree.Tree != null)
                {
                    Debug.Log($"Target tree.Tree.Root is null: {targetTree.Tree.Root == null}");
                    if (targetTree.Tree.Root != null)
                    {
                        Debug.Log($"Root bounds: {targetTree.Tree.Root.Bounds}");
                        Debug.Log($"Root is leaf: {targetTree.Tree.Root.IsLeaf}");
                        LogNodeHierarchy(targetTree.Tree.Root, 0);
                    }
                }
            }

            if (treeView != null)
            {
                // TreeViewの内部状態をダンプ
                try
                {
                    var itemCount = treeView.itemsSource?.Count ?? 0;
                    Debug.Log($"TreeView root items count: {itemCount}");
                    
                    // すべての表示可能なアイテムを取得
                    var allItems = new List<int>();
                    CollectAllItemIds(treeView.GetRootIds().Select(id=> treeView.GetItemDataForId<TreeViewItemData<BVHNode>>(id)), allItems);
                    Debug.Log($"Total visible items (including children): {allItems.Count}");
                    
                    // 各アイテムの内容を表示
                    for (int i = 0; i < allItems.Count; i++)
                    {
                        var itemId = allItems[i];
                        var itemData = treeView.GetItemDataForId<BVHNode>(itemId);
                        if (itemData != null)
                        {
                            Debug.Log($"Item ID={itemId}: Bounds={itemData.Bounds}, IsLeaf={itemData.IsLeaf}");
                        }
                        else
                        {
                            Debug.Log($"Item ID={itemId}: null");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error debugging TreeView: {e.Message}");
                }
            }
            Debug.Log("=== End TreeView Debug ===");
        }

        private void CollectAllItemIds(IEnumerable<TreeViewItemData<BVHNode>> items, List<int> result)
        {
            foreach (var item in items)
            {
                result.Add(item.id);
                if (item.hasChildren)
                {
                    CollectAllItemIds(item.children.Cast<TreeViewItemData<BVHNode>>(), result);
                }
            }
        }

        private void LogNodeHierarchy(BVHNode node, int depth)
        {
            if (node == null) return;
            
            string indent = new string(' ', depth * 2);
            Debug.Log($"{indent}Node: IsLeaf={node.IsLeaf}, Bounds={node.Bounds}, RendererCount={node.Renderers?.Count ?? 0}");
            
            if (!node.IsLeaf)
            {
                LogNodeHierarchy(node.Left, depth + 1);
                LogNodeHierarchy(node.Right, depth + 1);
            }
        }
    }
}
