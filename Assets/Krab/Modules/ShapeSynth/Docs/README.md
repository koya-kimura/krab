# ShapeSynth ドキュメント

## 操作と仕様

- [ユーザープリセットの演奏UI](UserPresetPerformance.md)
- [8スロットルーパー](Looper.md)
- [Motionプリセット（12原則）](MotionPresets.md)
- [Preset Editor](PresetEditorWorkspace.md)
- [共通Valueライブラリ](SharedValueLibrary.md)
- [検証Sceneの操作手順](../Samples/README.md)
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

数値Coreは `Modules/ShapeSynth/Runtime/Core/`、Unity接続は `Runtime/Unity/`、演奏UIは `Runtime/Performance/`、編集UIは `Runtime/PresetEditor/` に置く。
テストと検証SceneはGit管理し、実行ごとのログ・画像・JSON/XMLはプロジェクト直下の `Reports/`（gitignore対象）へ出力する。
