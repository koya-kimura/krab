# ShapeSynth ドキュメント

## 操作と仕様

- [ユーザープリセットの演奏UI](UserPresetPerformance.md)
- [8スロットルーパー](Looper.md)
- [Preset Editor](PresetEditorWorkspace.md)
- [共通Valueライブラリ](SharedValueLibrary.md)
- [検証Sceneの操作手順](../../ShapeSynth/Samples/README.md)
- [実装状況と今後の検討](ShapeSynthImplementationPlan.md)

## Coreと描画

- [LFO](LfoAndPhase2bPlan.md)
- [Parameter / Binding](ParameterAndBinding.md)
- [Shape / Group / Transform](ShapeGroupTransform.md)
- [Voiceと寿命](VoiceAndLifetime.md)
- [Random / Delay / Flip](Variation.md)
- [ADSRのカーブ（Ramp廃止）](AdsrCurves.md)
- [HSBとStroke Width](HsbColor.md)
- [p5.Unite描画Adapter](RenderingAdapter.md)

数値Coreは `ShapeSynth/Scripts/Core/`、Unity接続は `Scripts/Unity/`、演奏UIは `Scripts/Performance/`、編集UIは `Scripts/PresetEditor/` に置く。
テストと検証SceneはGit管理し、実行ごとのログ・画像・JSON/XMLはプロジェクト直下の `Reports/`（gitignore対象）へ出力する。
