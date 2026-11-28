# ADSRのカーブ

2026-11-17に追加。2026-11-18にRampを廃止した（下記）。Parameterの式 `Base + Σ Depth × Weight × signal` は変えていない。

## 考え方

2つの別々の話として扱う。

1. **カーブ（どう進むか）**：ある値から次の値へ移るときの進み方。単体のsignalではなく、区間に付ける属性。`EasingCurve` と `EasingFunctions` が担う。
2. **signal（最後にどこへ行くか）**：種類ごとに独立したクラスを持つ。

| signal | 動き | 寿命 |
| --- | --- | --- |
| ADSR | 0→1→Sustain→0。Attack・Decay・Releaseにそれぞれカーブを付けられる | あり |
| LFO | くり返し行き来する | なし |

「値が戻らない」は1回のTriggerの中の話として扱う（案A）。Triggerをまたいで値を積み重ねる仕組み（案B）は作っていない。

## 土台

| 型 | 役割 |
| --- | --- |
| `IModulatorDefinition` | 種類ごとの不変設定。`Create(VoiceTime)` でTriggerごとの独立した状態を作る。ADSR / LFOが実装する |
| `ModulatorSlotDefinition` | `Definition`（`IModulatorDefinition`）とADSR専用の `AutoRelease` を持つ。`Adsr` / `Lfo` は型が一致するときだけ値を返す |
| `IReleasable` | Voiceのrelease通知を受けるsignal。`VoiceInstance` は `AdsrInstance` を名指しせずこれに通知する |
| `PresetCompiler.ModulatorDefinition(PresetComponent)` | 編集DTOからCore設定への唯一の変換。Preset EditorのSignal Graphもここを通して標本化する |

新しい種類を足すときは、Definition・Instance・`ModulatorKind`・`PresetComponent` のフィールド・`ModulatorDefinition` の分岐・Editorの表示（`ComponentTitle`、`ComponentFields`、`PresetEditorPalette.Modulator`、Signal Graphの `Signature`、Previewの `ComputeLoopBeats`）を揃える。

## カーブ

- `EasingCurve`：Linear、Sine / Quad / Cubic / Expo / Back / Elastic / Bounce のそれぞれIn・Out・InOut（計22種）。式は一般的なeasings.netの定義に合わせた。
- `EasingFunctions.Evaluate(curve, progress)`：progressは0〜1にClampし、両端は全種類で正確に0と1を返す。Back・Elasticは途中で範囲外に出る（意図した挙動）。NaNは例外。

## ADSRのカーブ

- `AdsrDefinition` に `AttackCurve` / `DecayCurve` / `ReleaseCurve` を追加した。既定値はすべてLinearで、従来の直線ADSRと同じ値になる。
- 各区間は「区間の始まりの値 + (終わりの値 − 始まりの値) × カーブ(区間内の進み)」。Attackは0→1、Decayは1→Sustain Level、Releaseはその時点の値→0。
- 外部Releaseは、これまでどおりその時点の値から始まり、Release Curveに沿って0へ下がる。
- Sustainは一定値のためカーブを持たない。

## Rampの廃止（2026-11-18）

Ramp（Duration拍で0→1へ進み、届いた瞬間に0へ戻る）は、ADSRで値も寿命も同じになるため廃止した。

```
Ramp(duration = d, curve = c) ≡ ADSR(Attack = d, Decay = 0, Sustain Level = 1, Sustain = 0, Release = 0, AttackCurve = c)
```

Attackが終わる瞬間にRelease（0拍）が始まり、0へ落ちる。寿命もd拍で終わる。Lerpとして使うときは、Attack Curveを `Linear` にし、Baseを開始値、Depthを「終了値 − 開始値」にする。

## 保存データ

- `ModulatorKind` は `Adsr = 0` / `Lfo = 1` の2種類。
- `PresetComponent` は `AttackCurve` / `DecayCurve` / `ReleaseCurve`（既定Linear）を持つ。
- PresetのTrigger・Previewには、ADSRが1つ以上必要。
- カーブは `PresetDocument` のValue専用。

## Preset Editor

- ADSRには、Attack・Decay・Releaseの下にそれぞれCurveのドロップダウンを表示する。

## 検証

2026-11-17：Unity Editor経由で再コンパイルに成功。EditModeテスト309件成功・失敗0。内訳は `EasingTests` 11件、`RampTests` 13件（2026-11-18に削除）、`AdsrCurveTests` 13件。既存の `AdsrTests` 25件も変更なしで成功し、Linearの既定値で従来と同じ値になることを確認した。同じ日の1回目の実行は、テストが1件も始まらないまま600秒でタイムアウトした。Editorはready状態で、ダイアログもSceneの未保存変更もなく、再実行では成功した。Preset Editor UIの操作と見た目は未確認。
