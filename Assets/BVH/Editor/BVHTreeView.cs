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
    /// UIElementsのTreeViewを使用してBVHの階層構造を視覚化し、
    /// ノード選択機能とデバッグ情報の提供を行う
    /// </summary>
    internal class BVHTreeView
    {
        #region Fields
        /// <summary>UIElementsのTreeView本体</summary>
        private readonly TreeView treeView;
        
        /// <summary>表示対象のBVHツリーを持つSceneBVHTreeコンポーネント</summary>
        private SceneBVHTree targetTree;
        #endregion

        #region Events
        /// <summary>
        /// ノードが選択された時に発火するイベント
        /// BVHGizmoDrawerや詳細パネルの更新に使用される
        /// </summary>
        public event Action<BVHNode> OnNodeSelected;
        #endregion

        #region Properties
        /// <summary>TreeViewのUIElement（外部のレイアウトに配置するため）</summary>
        public TreeView TreeViewElement => treeView;
        #endregion

        #region コンストラクタ
        /// <summary>
        /// BVHTreeViewのコンストラクタ
        /// TreeViewの初期化とイベントハンドラの設定を行う
        /// </summary>
        public BVHTreeView()
        {
            // TreeViewの初期化
            treeView = new TreeView
            {
                makeItem = MakeItem,           // アイテム要素の生成関数
                bindItem = BindItem,           // データとUI要素のバインド関数
                selectionType = SelectionType.Single  // 単一選択モード
            };
            
            // レイアウト設定
            treeView.style.flexGrow = 1;  // 親要素内で拡張
            
            // イベントハンドラの設定
            treeView.selectionChanged += OnSelectionChanged;
            
            // 見た目の改善：交互の背景色を設定
            treeView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 表示対象のBVHツリーを設定し、TreeViewを更新する
        /// </summary>
        /// <param name="tree">表示するSceneBVHTreeコンポーネント</param>
        public void SetTarget(SceneBVHTree tree)
        {
            targetTree = tree;
            RefreshTree();
        }

        /// <summary>
        /// BVHツリーの表示を更新する
        /// 現在設定されているtargetTreeからBVH構造を読み取り、TreeViewに反映する
        /// </summary>
        public void RefreshTree()
        {
            Debug.Log($"Refreshing BVH Tree for {targetTree?.name ?? "null"}");
            
            // null チェック：targetTreeが設定されていない場合
            if (targetTree == null)
            {
                Debug.LogWarning("Target tree is null.");
                treeView.SetRootItems(new List<TreeViewItemData<BVHNode>>());
                return;
            }

            // null チェック：BVHTreeが構築されていない場合
            if (targetTree.Tree == null)
            {
                Debug.LogWarning("Target tree.Tree is null.");
                treeView.SetRootItems(new List<TreeViewItemData<BVHNode>>());
                return;
            }

            // null チェック：BVHのルートノードが存在しない場合
            if (targetTree.Tree.Root == null)
            {
                Debug.LogWarning("Target tree.Tree.Root is null.");
                treeView.SetRootItems(new List<TreeViewItemData<BVHNode>>());
                return;
            }
            
            // BVHツリーをTreeViewItemDataの階層構造に変換
            Debug.Log($"Building tree for {targetTree.name} with root node {targetTree.Tree.Root.Bounds}");
            int id = 0;
            var rootItem = BuildItem(targetTree.Tree.Root, ref id);
            var rootItems = new List<TreeViewItemData<BVHNode>> { rootItem };
            Debug.Log($"Created {rootItems.Count} root items with total {id} nodes");
            
            // TreeViewにデータを設定し、表示を更新
            treeView.SetRootItems(rootItems);
            treeView.Rebuild();
            
            // 全ノードを展開して階層構造を表示
            treeView.ExpandAll();
            Debug.Log("TreeView rebuild and expand completed");
        }
        #endregion

        #region Public Debug Methods
        /// <summary>
        /// デバッグ用：TreeViewの内部状態を取得する
        /// BVHDebuggerからの呼び出しで使用される
        /// </summary>
        /// <param name="rootItemCount">ルートアイテム数</param>
        /// <param name="allItemIds">全アイテムのIDリスト</param>
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
        #endregion

        #region Private Methods
        /// <summary>
        /// TreeViewの選択変更イベントハンドラ
        /// 選択されたノードをイベント経由で通知する
        /// </summary>
        /// <param name="selected">選択されたオブジェクトのコレクション</param>
        private void OnSelectionChanged(IEnumerable<object> selected)
        {
            var node = selected != null ? Enumerable.FirstOrDefault(selected) as BVHNode : null;
            OnNodeSelected?.Invoke(node);
        }

        /// <summary>
        /// TreeViewの各行に表示するUIElement（Label）を生成する
        /// TreeViewのmakeItemコールバックとして使用される
        /// </summary>
        /// <returns>各行に使用するLabel要素</returns>
        private VisualElement MakeItem()
        {
            var label = new Label();
            // テキストを左寄せ・垂直中央配置
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.alignSelf = Align.FlexStart;
            // レイアウトの拡張・縮小設定
            label.style.flexGrow = 1f;
            label.style.flexShrink = 0;
            return label;
        }

        /// <summary>
        /// TreeViewの各行にデータをバインドする
        /// TreeViewのbindItemコールバックとして使用される
        /// </summary>
        /// <param name="element">バインド対象のUIElement（Label）</param>
        /// <param name="index">TreeView内のインデックス</param>
        private void BindItem(VisualElement element, int index)
        {
            Debug.Log($"BindItem called for index {index}");
            
            // インデックスからBVHNodeデータを取得
            if (treeView.GetItemDataForIndex<BVHNode>(index) is BVHNode node)
            {
                // ノードの体積を計算
                var volume = node.Bounds.size.x * node.Bounds.size.y * node.Bounds.size.z;
                // ノードに含まれるレンダラー数を取得
                int count = GetRendererCount(node);
                
                var label = element as Label;
                // リーフノードか分岐ノードかで表示テキストを変更
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

        /// <summary>
        /// BVHNodeからTreeViewItemDataを再帰的に構築する
        /// BVHの階層構造をTreeViewで表示するためのデータ構造に変換する
        /// </summary>
        /// <param name="node">変換対象のBVHNode</param>
        /// <param name="id">アイテムIDの参照（再帰呼び出しで更新される）</param>
        /// <returns>TreeViewで使用するアイテムデータ</returns>
        private static TreeViewItemData<BVHNode> BuildItem(BVHNode node, ref int id)
        {
            int currentId = id++;
            Debug.Log($"Building item ID={currentId}, IsLeaf={node.IsLeaf}, Bounds={node.Bounds}");
            
            // リーフノードの場合は子要素なしで作成
            if (node.IsLeaf)
            {
                Debug.Log($"Creating leaf item ID={currentId} with {node.Renderers?.Count ?? 0} renderers");
                return new TreeViewItemData<BVHNode>(currentId, node);
            }

            // 分岐ノードの場合は左右の子ノードを再帰的に構築
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

        /// <summary>
        /// 指定されたノード以下に含まれるレンダラーの総数を取得する
        /// リーフノードの場合は直接カウント、分岐ノードの場合は再帰的に集計
        /// </summary>
        /// <param name="node">カウント対象のBVHNode</param>
        /// <returns>ノード以下のレンダラー総数</returns>
        private static int GetRendererCount(BVHNode node)
        {
            if (node == null) return 0;
            
            // リーフノードの場合は直接レンダラー数を返す
            if (node.IsLeaf) return node.Renderers.Count;
            
            // 分岐ノードの場合は左右の子ノードの合計を返す
            return GetRendererCount(node.Left) + GetRendererCount(node.Right);
        }

        /// <summary>
        /// TreeViewの全アイテムIDを再帰的に収集する
        /// デバッグ情報の表示に使用される
        /// </summary>
        /// <param name="items">収集対象のアイテムコレクション</param>
        /// <param name="result">結果を格納するリスト</param>
        private void CollectAllItemIds(IEnumerable<TreeViewItemData<BVHNode>> items, List<int> result)
        {
            foreach (var item in items)
            {
                result.Add(item.id);
                // 子要素がある場合は再帰的に収集
                if (item.hasChildren)
                {
                    CollectAllItemIds(item.children.Cast<TreeViewItemData<BVHNode>>(), result);
                }
            }
        }
        #endregion
    }
}