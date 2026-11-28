# Performance：ユーザープリセットだけの演奏UI（2026-11-17）

Performanceでは、Preset Editorで作って保存したプリセット（ユーザープリセット）だけを使う。コードで定義していた組み込みプリセット（FX01、Pad01など5種）と、その設定・保存の仕組みは削除した。

## ルーパー

2026-11-22に8スロットのルーパーを追加した。Performance上部のLoopersからRecord → Play → Clearを操作できる。記録・再生CoreはCommonに置き、ShapeSynthはプリセットIDの発火だけを接続する。使い方と検証Sceneは [ルーパー](Looper.md)。

## UI

保存済みプリセットが、名前順に1行ずつ縦に並ぶ。

```text
[プリセット名] | Trigger | MIDI | Hold MIDI
```

- Trigger：そのプリセットのVoiceを1つ生成する。Trigger時にファイルを読み直してCompileするので、Preset Editorで保存した変更は次のTriggerから反映される。
- MIDI：MIDI割り当てのウインドウを開く。割り当て方はMIDI Sampleと同じ。割り当てたNoteやボタンを押すとTriggerされる（離しでは何もしない）。割り当てはプリセットIDで保存されるので、名前を変えても外れない。割り当ての保存は、ほかのMIDI設定と同じくPrefsの保存（Settingsの「Save」かCmd+S）。
- Hold MIDI（2026-11-24）：OneShot（押している間だけtrue）のMIDI割り当て。押すとADSRのAutoReleaseを切ってCompileしたVoiceを作り、離すまでSustain Levelを保つ（Sustain Timeは無視）。離すとそのVoiceへReleaseを送り、Release Curveに沿って消える。Attack／Decayの途中で離した場合はその時点の値からRelease。Action IDは `ShapeSynth.UserPresetHold.{プリセットID}`。TriggerとHoldは別々に割り当てられ、Holdに割り当てたパッドではHoldの挙動になる。画面上のボタンでは押下と離しを取れないため、Holdは MIDI からのみ使える。
  - 離す前にもう一度押下が来た場合（Note Offの取りこぼしなど）は前のVoiceをReleaseしてから新しく作る。MIDIのリセットやComponentの無効化でも保持中のVoiceはReleaseされる。
  - ADSRのないプリセットはTriggerと同じくエラーになる。Timerは保持と無関係に計時する（ADSRを保持している間はVoiceが残る）。
- プリセットがないときは「No presets yet. Save one in the Preset Editor.」と表示する。
- 読めないファイルやTriggerの失敗（ADSRがないプリセット、SchemaVersion 5より前のファイルなど）は、一覧の下に赤い行で表示する。

## 一覧の更新

Refreshボタンはない。約1秒ごとに保存フォルダのファイル名と更新時刻を確認し、変わったときだけ読み直す。Preset Editorを別のSceneで使っていても、保存すれば一覧に反映される。

行を作り直すのは、プリセットの増減か名前の変更があったときだけにした。行にはMIDI割り当てのウインドウが含まれるので、中身の保存のたびに作り直さないようにするため。

## 乱数

Trigger のたびに Seed を1つ進める（初期値1234）。Rand は Trigger ごとに変わる。Seed の入力欄はない。

## 削除したもの

- Core：`PresetCatalog`、`Fx01Preset`、`AdditionalPresets`、`PerformanceSettings`（Modulator一覧・プリセット設定）、`ModulatorLibrary`
- Unity：`PerformanceSettingsStore`（`Performance.v1.json`）
- Performance：Modulator編集UI、組み込みプリセットのTriggerボタン、キーボード（Q / W / E / A / S）でのTrigger、User seed欄
- テスト：上記だけを対象にしていたもの（Fx01、ModulatorLibrary、MultiPreset、PerformanceSettingsStore、MultiPresetSettings）
- `ModulatorKind`（ADSR / LFO）は `Krab.ShapeSynth.Core.Presets` に移した。保存JSONの数値は変わらない。
