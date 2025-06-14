# BVHシステム (Bounding Volume Hierarchy)

## 概要

BVHシステムは、3Dシーン内のレンダラーオブジェクトを効率的な空間データ構造に組織化するためのコンポーネントです。SAH（Surface Area Heuristic）を使用して最適化されたBVH（Bounding Volume Hierarchy）を構築し、レンダリングパフォーマンスの向上とシーンクエリの高速化を実現します。

## 主要コンポーネント

### コアクラス

**BVHTree (`Scripts/BVHTree.cs`)**
- BVH構造の中核となるクラス
- SAH（Surface Area Heuristic）による最適化されたツリー構築
- リーフサイズの設定可能（デフォルト: 4）
- ビルド時間の計測機能
- シーン全体またはRenderer配列からの構築をサポート

**BVHNode (`Scripts/BVHNode.cs`)**
- BVHツリーのノードを表現するクラス
- 境界ボックス（Bounds）と左右の子ノードを保持
- リーフノードの場合はRendererリストを含有
- SerializeReferenceによるUnityエディターでの可視化

**SceneBVHTree (`Scripts/SceneBVHTree.cs`)**
- BVHTreeをUnityシーンで使用するためのMonoBehaviourラッパー
- Inspector UIからの手動構築機能
- エディターツールとの統合ポイント

### エディターツール

**BVHViewerWindow (`Editor/BVHViewerWindow.cs`)**
- BVH構造を視覚化するメインエディターウィンドウ
- TreeViewによる階層構造表示
- ノード選択と詳細情報パネル
- Gizmo描画との連携

**BVHTreeView (`Editor/BVHTreeView.cs`)**
- UIElementsのTreeViewを使用したBVH階層表示
- ノード選択イベントの処理
- デバッグ機能とアイテム情報の詳細表示

**BVHDetailPanel (`Editor/BVHDetailPanel.cs`)**
- 選択ノードの詳細情報表示
- 境界情報（center, size）とレンダラーリスト
- ダブルクリックによるGameObject選択機能

**BVHGizmoDrawer (`Editor/BVHGizmoDrawer.cs`)**
- シーンビューでのBVH可視化
- 選択ノードの境界ボックス描画
- 階層レベルに応じた色分け表示

**BVHDebugger (`Editor/BVHDebugger.cs`)**
- TreeViewとBVH状態のデバッグユーティリティ
- 階層構造のコンソール出力
- アイテム情報の詳細ログ

## 技術仕様

### SAH（Surface Area Heuristic）

BVH構築において最適な分割位置を決定するためのヒューリスティック：
- 各分割候補の表面積とオブジェクト数を考慮
- レイトレーシング性能の理論的最適化
- O(n log n)の構築時間複雑度

### 実装の特徴

- **パフォーマンス重視**: Stopwatchによるビルド時間計測
- **メモリ効率**: 構造体ベースの最適化されたデータ構造
- **Unity統合**: SerializeReferenceによる完全なエディターサポート
- **デバッグ対応**: 包括的なログ機能とリアルタイム可視化

## 使用方法

### 基本的な使用手順

1. **BVHの構築**:
   ```csharp
   var bvhTree = new BVHTree();
   bvhTree.BuildFromScene(leafSize: 4);
   ```

2. **シーンでの使用**:
   - GameObjectにSceneBVHTreeコンポーネントを追加
   - Inspectorから"Build BVH"ボタンをクリック

3. **視覚化**:
   - Window > BVH Viewerからエディターウィンドウを開く
   - Target欄にSceneBVHTreeコンポーネントを設定

### エディターワークフロー

1. **構築**: SceneBVHTreeコンポーネントのInspectorから構築
2. **確認**: BVH Viewerで階層構造を確認
3. **デバッグ**: Debug Itemsボタンでコンソール出力確認
4. **可視化**: シーンビューでGizmo表示による境界確認

## パフォーマンス

- **構築時間**: シーンサイズに依存（1000オブジェクトで約10-50ms）
- **メモリ使用量**: オブジェクト数にほぼ線形
- **クエリ性能**: O(log n)の期待性能（バランス取れたツリーの場合）

## 今後の拡張予定

- レイキャスト/オクルージョンクエリAPI
- 動的オブジェクト更新サポート
- マルチスレッド構築
- GPU加速による並列処理

## 関連システム

- **FrustumIntersection**: 視錐台カリングでの空間クエリ
- **HierarchicalCulling**: 階層カリングでのBVH活用
- **ScenePartitioning**: 大規模シーンでの空間分割との組み合わせ