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
        private BVHTreeView bvhTreeView;
        private BVHDetailPanel detailPanel;

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
            
            // コンポーネントの初期化
            bvhTreeView = new BVHTreeView();
            detailPanel = new BVHDetailPanel();
            
            // TreeViewの選択イベントをBVHGizmoDrawerと詳細パネルに転送
            bvhTreeView.OnNodeSelected += OnNodeSelected;
            
            // ツールバーを追加
            var toolbar = new Toolbar();
            var refreshButton = new ToolbarButton(() => bvhTreeView.RefreshTree()) { text = "Refresh" };
            var debugButton = new ToolbarButton(() => BVHDebugger.DebugTreeViewItems(targetTree, bvhTreeView)) { text = "Debug Items" };
            var targetField = new ObjectField("Target") { objectType = typeof(SceneBVHTree) };
            targetField.RegisterValueChangedCallback(evt => SetTarget(evt.newValue as SceneBVHTree));
            targetField.value = targetTree;
            
            toolbar.Add(targetField);
            toolbar.Add(refreshButton);
            toolbar.Add(debugButton);
            rootVisualElement.Add(toolbar);
            
            // 分割レイアウトを作成
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            
            // 左側にTreeView
            var leftPane = new VisualElement();
            leftPane.style.flexGrow = 1;
            leftPane.Add(bvhTreeView.TreeViewElement);
            splitView.Add(leftPane);
            
            // 右側に詳細パネル
            var rightPane = new VisualElement();
            rightPane.style.flexGrow = 1;
            rightPane.Add(detailPanel.PanelElement);
            splitView.Add(rightPane);

            rootVisualElement.Add(splitView);
            BVHGizmoDrawer.SelectedNode = null;
        }

        private void OnNodeSelected(BVHNode node)
        {
            BVHGizmoDrawer.SelectedNode = node;
            detailPanel.UpdateDetails(node);
        }

        private void OnEnable()
        {
            BVHGizmoDrawer.ActiveTree = targetTree;
            bvhTreeView?.SetTarget(targetTree);
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
            bvhTreeView?.SetTarget(tree);
        }
    }
}
