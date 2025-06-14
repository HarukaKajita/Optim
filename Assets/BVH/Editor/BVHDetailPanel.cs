using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Optim.BVH.Editor
{
    /// <summary>
    /// 選択されたBVHノードの詳細情報を表示するパネル
    /// ノードの境界情報、含まれるレンダラーのリストを表示し、
    /// ユーザーがBVHの構造を詳細に確認できる機能を提供する
    /// </summary>
    internal class BVHDetailPanel
    {
        #region Fields
        /// <summary>詳細パネル全体のコンテナ要素</summary>
        private readonly VisualElement detailPane;
        
        /// <summary>ノードの詳細情報（境界、サイズ、レンダラー数）を表示するラベル</summary>
        private readonly Label detailLabel;
        
        /// <summary>ノードに含まれるレンダラーのリストを表示するListView</summary>
        private readonly ListView rendererList;
        #endregion

        #region プロパティ
        /// <summary>パネル全体のUIElement（外部のレイアウトに配置するため）</summary>
        public VisualElement PanelElement => detailPane;
        #endregion

        #region Constructor
        /// <summary>
        /// BVHDetailPanelのコンストラクタ
        /// 詳細情報表示用のUI要素を初期化し、レイアウトを設定する
        /// </summary>
        public BVHDetailPanel()
        {
            // メインコンテナの初期化
            detailPane = new VisualElement();
            detailPane.style.flexGrow = 1;      // 親要素内で拡張
            detailPane.style.paddingLeft = 4;   // 左側の余白
            
            // 詳細情報ラベルの初期化
            detailLabel = new Label();
            
            // レンダラーリストの初期化
            rendererList = new ListView();
            rendererList.style.flexGrow = 1;    // 縦方向に拡張
            rendererList.style.flexShrink = 1;  // 必要に応じて縮小
            rendererList.style.paddingLeft = 16; // インデント
            // 交互の背景色で見やすくする
            rendererList.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            
            // パネルに要素を追加
            detailPane.Add(detailLabel);
            detailPane.Add(rendererList);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 選択されたBVHノードの詳細情報を表示パネルに反映する
        /// ノードの境界情報と含まれるレンダラーのリストを更新する
        /// </summary>
        /// <param name="node">表示対象のBVHNode（nullの場合は表示をクリア）</param>
        public void UpdateDetails(BVHNode node)
        {
            // ノードが選択されていない場合は表示をクリア
            if (node == null)
            {
                detailLabel.text = string.Empty;
                rendererList.itemsSource = null;
                rendererList.Rebuild();
                return;
            }

            // ノードに含まれる全レンダラーを収集
            var renderers = CollectRenderers(node);
            
            // 詳細情報テキストを更新（境界の中心、サイズ、レンダラー数）
            detailLabel.text = $"Center: {node.Bounds.center}\nSize: {node.Bounds.size}\nRenderers: {renderers.Count}";

            // ListViewのコールバック関数を設定（初回のみ）
            if (rendererList.makeItem == null)
                rendererList.makeItem = () => new Label(); // 各行のUI要素生成
            if (rendererList.bindItem == null)
                rendererList.bindItem = (ve, i) => ((Label)ve).text = ((Renderer)rendererList.itemsSource[i]).name; // データのバインド
            
            // レンダラーリストを更新
            rendererList.itemsSource = renderers;
            rendererList.Rebuild();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 指定されたBVHノード以下に含まれる全レンダラーを収集する
        /// リーフノードから直接、分岐ノードからは再帰的に収集
        /// </summary>
        /// <param name="node">収集対象のBVHNode</param>
        /// <returns>ノード以下の全レンダラーのリスト</returns>
        private static List<Renderer> CollectRenderers(BVHNode node)
        {
            var list = new List<Renderer>();
            CollectRecursive(node, list);
            return list;
        }

        /// <summary>
        /// BVHノードから再帰的にレンダラーを収集するヘルパーメソッド
        /// リーフノードの場合は直接レンダラーを追加、
        /// 分岐ノードの場合は左右の子ノードを再帰的に処理
        /// </summary>
        /// <param name="node">処理対象のBVHNode</param>
        /// <param name="list">結果を格納するレンダラーリスト</param>
        private static void CollectRecursive(BVHNode node, List<Renderer> list)
        {
            if (node == null) return;
            
            if (node.IsLeaf)
            {
                // リーフノードの場合：直接レンダラーを追加
                list.AddRange(node.Renderers);
            }
            else
            {
                // 分岐ノードの場合：左右の子ノードを再帰的に処理
                CollectRecursive(node.Left, list);
                CollectRecursive(node.Right, list);
            }
        }
        #endregion
    }
}