# ShapeSynth ドキュメント

- [実装計画](ShapeSynthImplementationPlan.md)：既存krabとの接続、段階的実装、Phase 1の仕様。
- [既存システムの設計調査](ShapeSynthArchitectureAudit-2026-11-08.md)：p5側の既存資産と設計上の検討事項。
- [p5.Unite API調査](P5UniteApiAndShapeSynthReport-2026-11-08.md)：導入済みパッケージのAPIと描画Adapterの検討。
- [検証Sceneの操作手順](../../ShapeSynth/Samples/README.md)：ADSR Circleデモの起動と操作。

各調査文書は記載日時点の結果。後続のUnity Pipeline検証ではPhase 1のEditModeテスト25件が成功し、専用Sceneの保存・Play・RT描画を確認済み。

## 配置とGit管理

```text
Assets/Krab/
  Common/                  共通基盤
  ShapeSynth/              図形シンセ機能
    Core/                  数値ロジック
    Tests/Editor/          再実行できるテストコード（Git管理）
    Samples/               検証Scene・デモ・構築用Editorコード（Git管理）
  Docs/ShapeSynth/         設計・調査・計画（Git管理）
Reports/                   実行結果・ログ・画像（ローカルのみ、gitignore）
```

`Reports/` には実行ごとのJSON/XML/PNGや検証ログを出力する。長期参照する設計資料はここに置く。テストコードや検証Sceneは検証を再現するためにGit管理を続ける。
