using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Optim.BVH.Editor
{
    /// <summary>
    /// BVH構造をTreeViewで表示するためのコンポーネント
    /// </summary>
    internal class BVHTreeView
    {
        private readonly TreeView treeView;
        private SceneBVHTree targetTree;

        public event Action<BVHNode> OnNodeSelected;
        public TreeView TreeViewElement => treeView;

        public BVHTreeView()
        {
            treeView = new TreeView
            {
                makeItem = MakeItem,
                bindItem = BindItem,
                selectionType = SelectionType.Single
            };
            treeView.style.flexGrow = 1;
            treeView.selectionChanged += OnSelectionChanged;
            treeView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
        }

        public void SetTarget(SceneBVHTree tree)
        {
            targetTree = tree;
            RefreshTree();
        }

        public void RefreshTree()
        {
            Debug.Log($"Refreshing BVH Tree for {targetTree?.name ?? "null"}");
            
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
            
            // TreeViewを展開して子要素を表示
            treeView.ExpandAll();
            Debug.Log("TreeView rebuild and expand completed");
        }

        private void OnSelectionChanged(IEnumerable<object> selected)
        {
            var node = selected != null ? Enumerable.FirstOrDefault(selected) as BVHNode : null;
            OnNodeSelected?.Invoke(node);
        }

        private VisualElement MakeItem()
        {
            var label = new Label();
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.alignSelf = Align.FlexStart;
            label.style.flexGrow = 1f;
            label.style.flexShrink = 0;
            return label;
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

        private static int GetRendererCount(BVHNode node)
        {
            if (node == null) return 0;
            if (node.IsLeaf) return node.Renderers.Count;
            return GetRendererCount(node.Left) + GetRendererCount(node.Right);
        }

        public void GetDebugInfo(out int rootItemCount, out List<int> allItemIds)
        {
            rootItemCount = treeView.itemsSource?.Count ?? 0;
            allItemIds = new List<int>();
            
            try
            {
                var rootIds = treeView.GetRootIds().Select(id => treeView.GetItemDataForId<TreeViewItemData<BVHNode>>(id));
                CollectAllItemIds(rootIds, allItemIds);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error collecting debug info: {e.Message}");
            }
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
    }
}