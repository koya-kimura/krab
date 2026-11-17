# LFO Core

## LFO Core（実装済み）

- `LfoDefinition(double cyclesPerBeat, double phaseOffset = 0, LfoWaveform waveform = Sine)`：不変で共有可能。Depth・Shape・Parameterへの参照なし。
- `LfoInstance(LfoDefinition definition, VoiceTime time)` / `Evaluate(double nowSeconds)`：VoiceTimeの開始BPMを固定し、指定秒の信号を直接評価。更新順・呼出回数への依存なし。DefinitionとTimeは読み取り可能。
- CyclesPerBeatは有限かつ非負。0は位相オフセットで停止。負の周波数による逆再生は今回サポートしない。
- PhaseOffsetは1周期=1。任意の有限値を `[0, 1)` へラップする（1.25と-0.75は0.25）。開始前はElapsedBeats=0として、その位相の値を返す。ADSRのような開始前の無音化は行わない。
- Sineは `sin(2πphase)`。Triangleは位相0で0、1/4で+1、1/2で0、3/4で-1。Sawtoothは位相0で-1から上昇し1周期で-1へ戻る。Squareは前半+1、後半-1（1/2境界は-1）。
- 時刻とBPMの検証は既存VoiceTimeを利用。未初期化VoiceTime、null Definition、不明な波形を拒否。不正値を黙って補正しない。
- 数値安定性のため、経過拍を `1 / CyclesPerBeat` の周期で剰余してから乗算する。周期がdoubleの範囲を超える極小周波数は直接乗算する（この場合は1周期未満）。三角関数へ巨大な位相を渡さない。
- 有限の入力時刻でもVoiceTimeの減算/拍換算がオーバーフローする場合はArgumentOutOfRangeException。IEEE doubleで失われた時刻の小数部分は復元できず、極端な値で位相精度を保証するものではない。
- `LfoDefinition` は `IModulatorDefinition`、`LfoInstance` は `IModulator` を実装する。Parameterへの接続は [Parameter / Binding](ParameterAndBinding.md) を参照。
