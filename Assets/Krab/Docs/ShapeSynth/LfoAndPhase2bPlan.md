# Phase 2a：LFO仕様とPhase 2bへの設計メモ

## LFO Core（実装済み）

- `LfoDefinition(double cyclesPerBeat, double phaseOffset = 0, LfoWaveform waveform = Sine)`：不変で共有可能。Depth・Shape・Parameterへの参照なし。
- `LfoInstance(LfoDefinition definition, VoiceTime time)` / `Evaluate(double nowSeconds)`：VoiceTimeの開始BPMを固定し、指定秒の信号を直接評価。更新順・呼出回数への依存なし。DefinitionとTimeは読み取り可能。
- CyclesPerBeatは有限かつ非負。0は位相オフセットで停止。負の周波数による逆再生は今回サポートしない。
- PhaseOffsetは1周期=1。任意の有限値を `[0, 1)` へラップする（1.25と-0.75は0.25）。開始前はElapsedBeats=0として、その位相の値を返す。ADSRのような開始前の無音化は行わない。
- Sineは `sin(2πphase)`。Triangleは位相0で0、1/4で+1、1/2で0、3/4で-1。Sawtoothは位相0で-1から上昇し1周期で-1へ戻る。Squareは前半+1、後半-1（1/2境界は-1）。
- 時刻とBPMの検証は既存VoiceTimeを利用。未初期化VoiceTime、null Definition、不明な波形を拒否。不正値を黙って補正しない。
- 数値安定性のため、経過拍を `1 / CyclesPerBeat` の周期で剰余してから乗算する。周期がdoubleの範囲を超える極小周波数は直接乗算する（この場合は1周期未満）。三角関数へ巨大な位相を渡さない。
- 有限の入力時刻でもVoiceTimeの減算/拍換算がオーバーフローする場合はArgumentOutOfRangeException。IEEE doubleで失われた時刻の小数部分は復元できず、極端な値で位相精度を保証するものではない。
- Modulator共通インターフェースやManagerはまだ導入しない。

## 検証Scene

`Assets/Krab/ShapeSynth/Samples/ShapeSynthSmokeTest.unity` を再利用。共通DI・UI・MIDI基盤・Preview・Syphon・Graphyは維持。

`ShapeSynth/Modulator Comparison` の1コンポーネントとUCanvasが両方の円を描く。

- 左：シアン、ADSRによる直径変化。従来の自動Trigger/Release、Spaceによる再Trigger、Rによる外部Releaseを維持。
- 右：オレンジ、LFO信号を左右の座標へ直接変換。初期値Sine、0.25 cycles/beat（4拍で1周期）。LでLFOのみ再開始。
- 初回はADSRとLFOで同じVoiceTimeを共有。以降のADSRの自動反復はLFOをリセットしない。BPM変更は新たにTrigger/RestartしたInstanceに適用する。
- InspectorからLFO周波数・位相・波形を変更できる。Play中の編集は同じ開始時刻/BPMでDefinitionを置き換えるため、位相が飛ぶ場合がある。これはサンプルの編集動作であり正式なライブ編集仕様ではない。
- 座標/直径への変換と色は検証用コード内のみ。正式Parameter/Bindingは未実装。

## Phase 2b：Parameter / Binding（設計案、未実装）

計算式：`P(t) = Reference × (Base + Σ Depth_i × Signal_i(t))`。Coreで一律Clampしない。

### 信号の統一的な取得

候補A：`IScalarSignal.Evaluate(nowSeconds)` をADSR/LFOが実装する。型によらずBindingから呼べて単純だが、ADSR固有のRelease/IsFinishedは別APIで維持する必要がある。

候補B：Voice側の信号レジストリがIDから数値を返す。内部でADSR/LFOを評価し、BindingはIDだけを持つ。Coreクラスの変更を抑えられるが、型の振り分けと参照解決が必要。

Phase 2bで初めて共通の利用側ができるため、そこで最小インターフェースを検討する。推奨はAとIDによる参照解決の組合せ。今回の段階では追加しない。Release・寿命・Triggerを汎用インターフェースへ押し込まない。

### DefinitionとInstanceの参照

- Definition内の各Modulatorに安定したローカルIDを付け、BindingDefinitionはModulator IDとDepthを保持する。
- Voice生成時にID→Modulator Instanceを一度だけ構築する。複数Bindingはその同じInstanceを参照し、Bindingごとに生成しない。同じ時刻の信号をVoice側で一度評価して共有することも可能だが、キャッシュは必要になってから追加する。
- 同じDefinitionを複数Voiceで共有しても、IDの解決先レジストリとRelease状態はVoiceごとに分離する。
- Definitionの参照共有はそのまま設定を再利用する操作。独立した編集用複製では子ModulatorとBindingをまとめて複製し、内部参照を維持する。IDを振り直す場合はBinding側も同時にリマップする。実行中Instanceの状態はコピーしない。

### Reference / Base / Depth

- ParameterDefinitionはReference設定、無次元Base、BindingDefinitionのリストを保持する。BindingはModulator IDと無次元Depthを持つ。Depthは負数も許容し反転に使える。
- Referenceは「固定数値」「CanvasWidth」「CanvasHeight」「MinDimension」「MaxDimension」の小さな種別と固定値で表現する案。固定値以外は評価時のCanvas寸法Contextから解決する。
- Canvas寸法はUnity Adapterが論理描画座標のWidth/HeightとしてCoreに供給する。RTのピクセル寸法を使うか論理解像度を使うかを統一し、Unity型やCameraは渡さない。
- 固定Referenceは生成時に解決できるが、Canvas由来Referenceは毎回解決するとリサイズに追従できる。初期Voice寸法を固定する挙動との違いは実装前に合意する。

### Variationの接続

生成時にIndex/seed/PositionのContextからBase・Depth・ADSR/LFO設定を解決し、Instance用の設定スナップショットを作る。共有Definitionを書き換えない。偶奇でDepthの符号を変えるだけで反転を表現できる。

Modulator設定にVariationを適用する場合も、1つのVoice内ではIDにつき1回だけ解決する。Bindingごとに適用すると共有信号が別Instanceに分裂するため避ける。演奏中のライブVariationやDefinition編集は別途仕様を決める。
