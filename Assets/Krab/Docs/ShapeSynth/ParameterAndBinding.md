# Phase 2b：Parameter / Binding

## 実装とAPI

Coreは数値の評価のみを行う。Unity、p5.Unite、MIDI、Inspector、Prefsには依存しない。

```csharp
var time = new VoiceTime(startSeconds, startBpm);
var adsr = new AdsrInstance(adsrDefinition, time);
var lfo = new LfoInstance(lfoDefinition, time);
var signals = new IModulator[] { adsr, lfo };
var definition = new ParameterDefinition(
    new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: 400),
    baseValue: 0.5,
    new BindingDefinition(modulatorIndex: 0, depth: 0.5),
    new BindingDefinition(modulatorIndex: 1, depth: 0.2));
var parameter = new ParameterEvaluator(definition, signals);
var value = parameter.Evaluate(nowSeconds, new ReferenceContext(1920, 1080));
```

計算は `Reference × (Base + Σ Depth × Signal)`。例の信号がADSR=0.8、LFO=-0.5なら320。加算はBindingの順序通りで、正規化やClampはしない。

- `IModulator.Evaluate(double nowSeconds)`：数値出力だけの共通API。既存ADSR/LFOはこのインターフェースを実装するだけで、従来の振る舞いとAPIを維持する。ReleaseはADSR固有のまま。寿命判定は後続Voiceフェーズで別の `ILifetime` に分離する予定（[実装計画](ShapeSynthImplementationPlan.md)）。Timerは値を生成せず、Bindingには接続しない。
- `BindingDefinition(int modulatorIndex, double depth)`：呼出側が用意する小さな配列/リストのスロット番号とDepth。実行状態なし。
- `ParameterDefinition(ReferenceDefinition reference, double baseValue, params BindingDefinition[] bindings)`：不変の数値レシピ。Binding配列を複製して読み取り専用で公開する。
- `ParameterEvaluator(ParameterDefinition definition, IReadOnlyList<IModulator> modulators)`：生成時にBindingのスロットを既存Instanceへの参照として解決し保持する。Instanceの新規生成・複製・Releaseは行わない。
- `Evaluate(double nowSeconds, ReferenceContext context)`：指定時刻の値を直接返す。Base/Definitionを書き換えない。信号は同じ時刻の評価に副作用を持たないことを前提とする。キャッシュやManagerは未導入。

## Reference

`ReferenceDefinition(ReferenceSource source, double multiplier = 1, double absoluteValue = 1)`。

| Source | 評価値 |
|---|---|
| Absolute | AbsoluteValue × Multiplier |
| CanvasWidth | Context.CanvasWidth × Multiplier |
| CanvasHeight | Context.CanvasHeight × Multiplier |
| MinDimension | min(Width, Height) × Multiplier |
| MaxDimension | max(Width, Height) × Multiplier |

`ReferenceContext(width, height)` は有限かつ非負の明示的なCanvas寸法。defaultは0×0で有効。Canvas由来Referenceは評価時に解決するため、同じEvaluatorへ別寸法を渡すとリサイズに追従する。親Boundsや座標変換は扱わない。

寸法の単位は呼出側が統一する。サンプルは出力CameraのRTピクセル寸法を渡し、UCanvasの「最後に描画したCameraの寸法」へは依存しない。将来Positionへ適用する場合も、Referenceの評価と中央原点/親ローカル座標の変換は別の処理となる。

## Definition / Runtime / 共有

同じParameterDefinitionから複数Evaluatorを作り、それぞれ別のModulator一覧を渡せる。複数ParameterのBindingが同じスロットを参照する場合、一覧に同じADSR/LFO Instanceを置くことで実行状態を共有する。ReleaseはそのADSRへ一度送るだけで全参照先へ反映される。

Modulator一覧の参照は生成時に解決して保持する。呼出側のリスト要素を後から差し替えても、既存Evaluatorの接続は変わらない。再Triggerで新Instanceを使う場合はEvaluatorを作り直す。サンプルもこの方式。

## 数値検証方針

- Base/Depth/固定Reference/Multiplierは有限の正負値を許可する。出力は負数や1超でも返す。
- 不明なReferenceSource、負のスロット、NaN/Infinity、負のCanvas寸法は拒否する。
- null Definition/配列/Binding、欠落またはnullの参照先スロットも拒否する。
- 信号、各寄与、加算途中、Reference計算、最終積が非有限になれば例外。ReferenceやDepthが0でも、不正信号を黙って隠さない。
- 有限値が丸めで精度を失うことや、浮動小数点の加算順依存は通常のdouble演算に従う。

## 検証Scene

`Assets/Krab/ShapeSynth/Samples/ShapeSynthSmokeTest.unity` をPlayする。既存Common・DI・出力を維持し、同じUCanvas/コンポーネントで4つの円を描く。

- 上段左：従来のADSRによる直径変化。
- 上段右：従来のLFOによる左右移動。
- 下段左（紫）：MinDimension Referenceを使用し、同じParameterへADSRとLFOを加算して直径を変える。
- 下段右（緑）：下段左と同一のADSR InstanceをOpacityへ接続。固定直径もBindingなしのParameterで評価する。

下段はParameterEvaluatorを経由し、検証用コード内で合成式を再実装しない。負の直径は描画を省略し、Opacityは描画側のみ0〜1へClampする。

Space/R/L/Dの操作は従来通り。Inspectorの「Parameter / Binding comparison」でBase、ADSR Depth、LFO Depth、短辺に対するサイズ係数を変更できる。再Trigger/設定編集時には共有Definitionを書き換えず、新しいレシピ/評価器を作る。

## 後続フェーズの境界・制約

- スロットは小さなローカル番号であり、永続IDやグラフではない。GroupやPrefsで順序を変更する際はBinding番号も対応させる必要がある。将来の保存DTOで安定ID→ローカルスロットへ解決する境界を追加できる。
- Modulatorの生成・寿命・所有権は呼出側にある。Group/Voice管理をこのEvaluatorへ持ち込まない。共有InstanceのRelease/再Trigger対象も所有者が決める。
- Variationは生成時にBase/Depth/Modulator設定を解決して新しい不変Definitionを作り、共有Definitionへ書き戻さない。偶奇反転はDepthの符号変更で表現できる。
- Prefs/Inspector用DTOとCoreの型を分離する。Coreの不変性をUnityシリアライズ都合で崩さない。
- 既存のMIDI用 `Krab.Common.Midi.Core.Param<T>` とは独立し、変更通知やMIDIの割当は扱わない。
