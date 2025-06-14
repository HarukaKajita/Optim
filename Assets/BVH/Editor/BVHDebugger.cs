using System;
using System.Collections.Generic;
using UnityEngine;

namespace Optim.BVH.Editor
{
    /// <summary>
    /// BVHViewerのデバッグ機能を提供する静的クラス
    /// TreeViewの状態、BVH階層構造、アイテムの詳細情報をコンソールに出力し、
    /// 開発・デバッグ時の問題調査をサポートする
    /// </summary>
    internal static class BVHDebugger
    {
        #region Public Methods
        /// <summary>
        /// TreeViewとBVHツリーの詳細なデバッグ情報をコンソールに出力する
        /// 以下の情報を段階的にチェックし、問題の特定を支援する：
        /// 1. ターゲットツリーの存在と状態
        /// 2. BVH階層構造の詳細
        /// 3. TreeViewの内部状態とアイテム情報
        /// </summary>
        /// <param name="targetTree">デバッグ対象のSceneBVHTree</param>
        /// <param name="treeView">デバッグ対象のBVHTreeView</param>
        public static void DebugTreeViewItems(SceneBVHTree targetTree, BVHTreeView treeView)
        {
            Debug.Log("=== TreeView Debug Info ===");
            Debug.Log($"TreeView is null: {treeView == null}");
            Debug.Log($"Target tree is null: {targetTree == null}");
            
            // SceneBVHTreeの状態を詳細チェック
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
                        // BVH階層構造を再帰的にログ出力
                        LogNodeHierarchy(targetTree.Tree.Root, 0);
                    }
                }
            }

            // TreeViewの内部状態をチェック
            if (treeView != null)
            {
                try
                {
                    // TreeViewからデバッグ情報を取得
                    treeView.GetDebugInfo(out int rootItemCount, out List<int> allItemIds);
                    Debug.Log($"TreeView root items count: {rootItemCount}");
                    Debug.Log($"Total visible items (including children): {allItemIds.Count}");
                    
                    // 各TreeViewアイテムの詳細を表示
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
        #endregion

        #region Private Methods
        /// <summary>
        /// BVHノードの階層構造を再帰的にログ出力するヘルパーメソッド
        /// インデントを使用して階層の深さを視覚的に表現し、
        /// 各ノードの詳細情報（リーフ/分岐、境界、レンダラー数）を表示する
        /// </summary>
        /// <param name="node">ログ出力対象のBVHNode</param>
        /// <param name="depth">現在の階層の深さ（インデント計算用）</param>
        private static void LogNodeHierarchy(BVHNode node, int depth)
        {
            if (node == null) return;
            
            // 階層の深さに応じてインデントを生成
            string indent = new string(' ', depth * 2);
            Debug.Log($"{indent}Node: IsLeaf={node.IsLeaf}, Bounds={node.Bounds}, RendererCount={node.Renderers?.Count ?? 0}");
            
            // 分岐ノードの場合は左右の子ノードを再帰的に処理
            if (!node.IsLeaf)
            {
                LogNodeHierarchy(node.Left, depth + 1);
                LogNodeHierarchy(node.Right, depth + 1);
            }
        }
        #endregion
    }
}