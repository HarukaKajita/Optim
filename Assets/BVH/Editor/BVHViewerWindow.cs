using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

namespace Optim.BVH.Editor
{
    /// <summary>
    /// BVH構造を視覚化するメインエディターウィンドウ
    /// UIElementsのTreeViewを使用してBVHの階層構造を表示し、
    /// 選択されたノードの詳細情報を表示する統合インターフェースを提供する。
    /// シーンビューでのGizmo表示との連携も行う。
    /// </summary>
    internal class BVHViewerWindow : EditorWindow
    {
        #region Fields
        /// <summary>表示対象のBVHツリーを持つSceneBVHTreeコンポーネント</summary>
        private SceneBVHTree targetTree;
        
        /// <summary>BVH階層構造を表示するTreeViewコンポーネント</summary>
        private BVHTreeView bvhTreeView;
        
        /// <summary>選択ノードの詳細情報を表示するパネルコンポーネント</summary>
        private BVHDetailPanel detailPanel;
        #endregion

        #region Static Methods Entry Points
        /// <summary>
        /// UnityメニューからBVH Viewerウィンドウを開く
        /// 選択中のGameObjectにSceneBVHTreeコンポーネントがある場合、自動的にターゲットとして設定する
        /// </summary>
        [MenuItem("Window/BVH Viewer")]
        private static void OpenFromMenu()
        {
            // 選択中のGameObjectからSceneBVHTreeコンポーネントを取得
            var tree = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<SceneBVHTree>() : null;
            GetWindow<BVHViewerWindow>("BVH Viewer").SetTarget(tree);
        }

        /// <summary>
        /// 指定したSceneBVHTreeをターゲットとしてBVH Viewerウィンドウを開く
        /// スクリプトからの直接呼び出しや他のエディターウィンドウからの連携用
        /// </summary>
        /// <param name="tree">表示対象のSceneBVHTree</param>
        public static void Open(SceneBVHTree tree)
        {
            Debug.Log($"Opening BVH Viewer for {tree?.name ?? "null"}");
            GetWindow<BVHViewerWindow>("BVH Viewer").SetTarget(tree);
        }
        #endregion

        #region Unity Editor Window Lifecycle
        /// <summary>
        /// UIElementsを使用したウィンドウのGUIを作成する
        /// ツールバー、TreeView、詳細パネルを含む分割レイアウトを構築する
        /// </summary>
        public void CreateGUI()
        {
            Debug.Log("Creating BVH Viewer Window");
            
            // コアコンポーネントの初期化
            bvhTreeView = new BVHTreeView();
            detailPanel = new BVHDetailPanel();
            
            // イベント連携の設定：TreeViewのノード選択をGizmoと詳細パネルに伝播
            bvhTreeView.OnNodeSelected += OnNodeSelected;
            
            // ツールバーの構築
            var toolbar = new Toolbar();
            
            // ターゲット選択フィールド
            var targetField = new ObjectField("Target") { objectType = typeof(SceneBVHTree) };
            targetField.RegisterValueChangedCallback(evt => SetTarget(evt.newValue as SceneBVHTree));
            targetField.value = targetTree;
            
            // 更新ボタン
            var refreshButton = new ToolbarButton(() => bvhTreeView.RefreshTree()) { text = "Refresh" };
            
            // デバッグボタン
            var debugButton = new ToolbarButton(() => BVHDebugger.DebugTreeViewItems(targetTree, bvhTreeView)) { text = "Debug Items" };
            
            toolbar.Add(targetField);
            toolbar.Add(refreshButton);
            toolbar.Add(debugButton);
            rootVisualElement.Add(toolbar);
            
            // メインコンテンツの分割レイアウトを作成（左：TreeView、右：詳細パネル）
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            
            // 左側ペイン：BVH階層表示TreeView
            var leftPane = new VisualElement();
            leftPane.style.flexGrow = 1;
            leftPane.Add(bvhTreeView.TreeViewElement);
            splitView.Add(leftPane);
            
            // 右側ペイン：選択ノードの詳細情報パネル
            var rightPane = new VisualElement();
            rightPane.style.flexGrow = 1;
            rightPane.Add(detailPanel.PanelElement);
            splitView.Add(rightPane);

            rootVisualElement.Add(splitView);
            
            // Gizmoの初期状態をクリア
            BVHGizmoDrawer.SelectedNode = null;
        }

        /// <summary>
        /// ウィンドウがアクティブになった時の処理
        /// Gizmo描画用にアクティブツリーを設定し、TreeViewを更新する
        /// </summary>
        private void OnEnable()
        {
            BVHGizmoDrawer.ActiveTree = targetTree;
            bvhTreeView?.SetTarget(targetTree);
        }

        /// <summary>
        /// ウィンドウが非アクティブになった時の処理
        /// 他のGizmo描画への影響を避けるため、アクティブツリーをクリアする
        /// </summary>
        private void OnDisable()
        {
            if (BVHGizmoDrawer.ActiveTree == targetTree)
                BVHGizmoDrawer.ActiveTree = null;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// TreeViewでノードが選択された時のイベントハンドラ
        /// 選択されたノードをGizmo描画と詳細パネルに反映する
        /// </summary>
        /// <param name="node">選択されたBVHNode（nullの場合は選択解除）</param>
        private void OnNodeSelected(BVHNode node)
        {
            // シーンビューのGizmo描画用に選択ノードを設定
            BVHGizmoDrawer.SelectedNode = node;
            
            // 詳細パネルの表示を更新
            detailPanel.UpdateDetails(node);
        }

        /// <summary>
        /// 表示対象のSceneBVHTreeを設定する
        /// ウィンドウ全体のターゲットを更新し、関連コンポーネントに反映する
        /// </summary>
        /// <param name="tree">新しいターゲットSceneBVHTree</param>
        private void SetTarget(SceneBVHTree tree)
        {
            Debug.Log($"Setting target BVH Tree: {tree?.name ?? "null"}");
            
            targetTree = tree;
            // Gizmo描画用にアクティブツリーを設定
            BVHGizmoDrawer.ActiveTree = tree;
            // TreeViewの表示を更新
            bvhTreeView?.SetTarget(tree);
        }
        #endregion
    }
}
