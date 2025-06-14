# FrustumIntersectionシステム

## 概要

FrustumIntersectionシステムは、カメラの視錐台（Frustum）と3Dオブジェクトの交差判定を高速に実行するためのシステムです。CPU、JobSystem（マルチスレッド）、GPU（コンピュートシェーダー）の3つの実装を提供し、異なる処理環境での最適なパフォーマンスを実現します。視錐台カリングによるレンダリング最適化の中核機能として動作します。

## 主要コンポーネント

### アーキテクチャ

**Strategyパターンによる実装分離**
- `ITriangleIntersectionChecker`インターフェースによる統一API
- 実行時での実装切り替え可能
- パフォーマンス比較とベンチマーク機能

### コアクラス

**FrustumIntersectionSystem (`Scripts/FrustumIntersectionSystem.cs`)**
- システム全体の管理クラス
- 3つの実装の切り替えとパフォーマンス計測
- Unityカメラとの統合
- 結果の統計情報とデバッグ出力

**FrustumIntersectionTypes (`Scripts/FrustumIntersectionTypes.cs`)**
- システム全体で使用される共通データ型定義
- 視錐台平面、三角形データ、結果構造体
- NativeArrayとの互換性を考慮した設計

### 実装クラス

**CPUImplementation (`Scripts/CPUImplementation.cs`)**
- シングルスレッドCPU実装
- 基準となる参照実装
- 小規模シーンでの高速処理
- デバッグとプロファイリングに最適

**JobSystemImplementation (`Scripts/JobSystemImplementation.cs`)**
- Unity Job Systemを使用したマルチスレッド実装
- IJobParallelForによる並列化
- CPU集約的なワークロードに最適
- メモリアクセスパターンの最適化

**GPUImplementation (`Scripts/GPUImplementation.cs`)**
- ComputeShaderを使用したGPU実装
- 大量の三角形に対する並列処理
- GPUメモリとの効率的なデータ転送
- 非同期実行とフレーム分散処理

**DefaultTriangleChecker (`Scripts/DefaultTriangleChecker.cs`)**
- 基本的な三角形-視錐台交差判定の実装
- 6つの視錐台平面との距離計算
- 高精度な幾何学計算

### インターフェース

**ITriangleIntersectionChecker (`Scripts/ITriangleIntersectionChecker.cs`)**
- 三角形交差判定の統一インターフェース
- 実装間での一貫したAPI
- テスト可能性とモジュラー設計

### シェーダー

**TriangleIntersection.compute (`Shaders/TriangleIntersection.compute`)**
- GPU実装用のコンピュートシェーダー
- HLSL（High Level Shading Language）で記述
- 三角形ごとの並列処理スレッド
- 視錐台平面との効率的な交差判定

## 技術仕様

### 視錐台カリング理論

**視錐台の定義**:
- 6つの平面（上下左右遠近）で構成
- 各平面は法線ベクトルと距離で表現
- カメラのProjectionMatrixから抽出

**交差判定アルゴリズム**:
1. 三角形の各頂点と6つの平面の距離を計算
2. 全頂点が1つの平面の外側にある場合は不可視
3. そうでなければ可視と判定

### パフォーマンス特性

**CPU実装**:
- 単純なループ処理
- 小規模シーン（~1000三角形）で最適
- 計算時間: O(n)、nは三角形数

**JobSystem実装**:
- 並列化による高速化
- 中規模シーン（1000-10000三角形）で最適
- スケーラビリティ: CPUコア数に比例

**GPU実装**:
- 大規模並列処理
- 大規模シーン（10000+三角形）で最適
- GPU転送オーバーヘッドを考慮

## 使用方法

### 基本的な使用例

```csharp
// システムの初期化
var frustumSystem = new FrustumIntersectionSystem();

// 実装の選択
frustumSystem.SetImplementation(ImplementationType.JobSystem);

// カメラの視錐台で交差判定を実行
var results = frustumSystem.CheckIntersections(Camera.main, triangleData);
```

### 実装の切り替え

```csharp
// CPU実装（デバッグ用）
frustumSystem.SetImplementation(ImplementationType.CPU);

// JobSystem実装（中規模シーン）
frustumSystem.SetImplementation(ImplementationType.JobSystem);

// GPU実装（大規模シーン）
frustumSystem.SetImplementation(ImplementationType.GPU);
```

### パフォーマンス計測

```csharp
// ベンチマークモードで実行
frustumSystem.EnableBenchmarking(true);
var results = frustumSystem.CheckIntersections(camera, triangles);

// 結果の取得
Debug.Log($"処理時間: {results.ProcessingTime}ms");
Debug.Log($"可視三角形: {results.VisibleTriangles}/{results.TotalTriangles}");
```

## データフロー

1. **入力準備**: 三角形データをNativeArrayに変換
2. **視錐台抽出**: カメラの ProjectionMatrix × ViewMatrix から6平面を計算
3. **交差判定**: 選択された実装で並列処理実行
4. **結果収集**: 可視フラグと統計情報を返却
5. **メモリ解放**: NativeArrayとGPUバッファの適切な破棄

## パフォーマンス最適化

### CPU最適化
- ベクトル演算のSIMD活用
- メモリアクセスパターンの局所性
- 分岐予測の最適化

### JobSystem最適化
- バッチサイズの調整
- メモリコピーの最小化
- キャッシュライン境界の考慮

### GPU最適化
- スレッドグループサイズの最適化
- メモリ帯域幅の効率的活用
- GPU占有率の向上

## 統合システム

**他のカリングシステムとの連携**:
- HierarchicalCulling: 階層カリングの最終段階
- ScenePartitioning: セル単位での視錐台カリング
- BVH: 空間データ構造と組み合わせた最適化

## デバッグとプロファイリング

- Unity Profilerとの統合
- 実装別の詳細パフォーマンス計測
- 視覚的なカリング結果の確認
- メモリ使用量の監視

## 今後の拡張予定

- オクルージョンカリングとの統合
- LOD（Level of Detail）システムとの連携
- VR/ARでの最適化対応
- モバイルプラットフォーム特有の最適化