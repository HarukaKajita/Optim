# CLAUDE.md

このファイルは、このリポジトリでコードを操作する際のClaude Code (claude.ai/code) へのガイダンスを提供します。

## プロジェクト概要

高度な空間データ構造とカリング技術に焦点を当てたUnityレンダリング最適化ツールキットです。BVH（Bounding Volume Hierarchy）、視錐台カリング、階層カリング、シーン分割システムなど、シーン最適化への複数のアプローチを実装しています。

**Unityバージョン**: 2022.3.22f1 LTS  
**レンダリングパイプライン**: Universal Render Pipeline (URP) 14.0.10  
**ネームスペース**: `Optim.*` (コンポーネント別に整理)

## 開発コマンド

Unityプロジェクトは開発にUnity Editorを使用します。主要な操作：

- **ビルド**: Unity Editor → File → Build Settings
- **テスト**: Unity Editor → Window → General → Test Runner
- **プロファイリング**: Unity Editor → Window → Analysis → Profiler

## アーキテクチャ概要

### コアシステム

**BVHシステム (`Optim.BVH`)**
- `BVHTree`: 設定可能なリーフサイズを持つSAHベースのツリー構築
- `BVHNode`: バウンドとレンダラーコレクションを持つツリーノード
- `SceneBVHTree`: シーン統合用のMonoBehaviourラッパー
- エディターツール: BVH Viewer Window、Gizmo Drawer

**FrustumIntersectionシステム (`Optim.FrustumIntersection`)**
- 3つの実装: CPU、JobSystem（マルチスレッド）、GPU（コンピュートシェーダー）
- 交換可能な`ITriangleIntersectionChecker`を使用したStrategyパターン
- 実装間でのパフォーマンス比較機能

**HierarchicalCullingシステム (`Optim.HierarchicalCulling`)**
- ネストされたカリング操作のための親子階層
- 自動レンダラー収集とバウンド計算
- 設定可能なタイミング間隔でのリアルタイムカリング

**ScenePartitioningシステム (`Optim.ScenePartitioning`)**
- グリッドベース（2D/3D）およびボロノイベース（2D/3D）分割
- カメラドリブンなセルベースのカリングとストリーミング
- 設定可能な解像度とマージンパラメータ

### 主要パターン

- **Strategyパターン**: 視錐台交差と分割の複数の実装
- **階層設計**: 自動伝播を持つ親子関係
- **パフォーマンス第一**: 組み込みタイミング、ネイティブ配列、コンピュートバッファ
- **エディター統合**: デバッグ用のカスタムウィンドウ、ギズモ、ツールバー

### ファイル構成

各システムは以下のパターンに従います：
- `Scripts/`: コアランタイムクラス
- `Editor/`: カスタムエディターツールとインスペクター
- `Shaders/`: コンピュートシェーダー（GPU実装用）

## 開発ノート

- 日本語のコメントとコミットメッセージがこのコードベースでは一般的です
- すべてのシステムにパフォーマンス計測機能が含まれています
- エディターツールはリアルタイムの可視化とデバッグを提供します
- システムは比較ベンチマーク用に設計されています（CPU vs GPU vs JobSystem）
- パフォーマンス最適化にUnity Job Systemとコンピュートシェーダーを使用