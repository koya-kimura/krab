# ShapeSynth ドキュメント

- [複数プリセットとStroke](MultiPresetPerformance.md)：Phase 8の5キー演奏、輪郭描画、v1 JSON互換補完。

- [演奏サンプルとModulator UI](PerformanceAndModulators.md)：Phase 7のFX01、ID管理、編集と専用JSON保存。

- [Random / Delay / Flip](Variation.md)：旧Phase 6 Variationを置き換え。Seed付きRandom（Base/Depth/Delayへ加算）、Bindingの遅れ、複製ごとのFlip。

- [p5.Unite描画Adapter](RenderingAdapter.md)：Phase 4のAPI、Camera限定、座標と描画制約。
- [Voiceと寿命](VoiceAndLifetime.md)：Phase 5のTrigger、Groupごとのsignal scope、Lifetimeと検証Scene。

- [Shape / Group / Transform](ShapeGroupTransform.md)：Phase 3のCore階層、行列合成、Draw ItemとAdapter。
- [Parameter / Binding](ParameterAndBinding.md)：Phase 2bの実装API、Reference、共有と後続フェーズの境界。
- [LFO仕様とPhase 2bの設計案](LfoAndPhase2bPlan.md)：Phase 2aの仕様、Parameter/Bindingの候補比較（後者は未実装）。
- [実装計画](ShapeSynthImplementationPlan.md)：既存krabとの接続、段階的実装、Phase 1の仕様。
- [既存システムの設計調査](ShapeSynthArchitectureAudit-2026-11-08.md)：p5側の既存資産と設計上の検討事項。
- [p5.Unite API調査](P5UniteApiAndShapeSynthReport-2026-11-08.md)：導入済みパッケージのAPIと描画Adapterの検討。
- [検証Sceneの操作手順](../../ShapeSynth/Samples/README.md)：各Phaseの比較デモとVoice寿命デモの起動・操作。

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

- Phase 9: [Runtime Preset Editor](PresetEditor.md) — Group/Shape/Parameter/Modulator/Bindingの編集、Preview、JSON保存と別Sceneでの演奏。

- Phase 10: [Preset Editor Workspace](PresetEditorWorkspace.md) — Hierarchy/Preview/Inspector、選択連動、限定再構築、Zoom/Pan/Fit。

- [RampとADSRのカーブ](RampAndCurves.md) — signalの種類ごとのクラスの土台（`IModulatorDefinition` / `IReleasable`）、Ramp（Lerpを含む）、ADSRの区間ごとのカーブ。
