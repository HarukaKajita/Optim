using System;
using System.Collections.Generic;
using UnityEngine;

namespace Optim.BVH.Editor
{
    /// <summary>
    /// BVHViewerのデバッグ機能を提供するクラス
    /// </summary>
    internal static class BVHDebugger
    {
        public static void DebugTreeViewItems(SceneBVHTree targetTree, BVHTreeView treeView)
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
                try
                {
                    treeView.GetDebugInfo(out int rootItemCount, out List<int> allItemIds);
                    Debug.Log($"TreeView root items count: {rootItemCount}");
                    Debug.Log($"Total visible items (including children): {allItemIds.Count}");
                    
                    // 各アイテムの内容を表示
                    for (int i = 0; i < allItemIds.Count; i++)
                    {
                        var itemId = allItemIds[i];
                        var itemData = treeView.TreeViewElement.GetItemDataForId<BVHNode>(itemId);
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
                catch (Exception e)
                {
                    Debug.LogError($"Error debugging TreeView: {e.Message}");
                }
            }
            Debug.Log("=== End TreeView Debug ===");
        }

        private static void LogNodeHierarchy(BVHNode node, int depth)
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