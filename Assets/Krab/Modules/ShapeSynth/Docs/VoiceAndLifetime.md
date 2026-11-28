# Phase 5：VoiceとLifetime

2026-11-11時点の実装API。数値CoreがTriggerごとの信号・寿命状態を所有し、Unity側Controllerが時計・BPM・描画を接続する。Phase 4以前の仕様は各API文書を参照。

## 生成と所有

- `TriggerTarget` は1つのTrigger配置を識別し、再利用可能な `GroupDefinition` を参照する。同じDefinitionを使う別配置は、別々のTriggerTargetにする。
- `VoiceCollection.Spawn(target, nowSeconds, bpm)` はその呼出し時刻とBPMを `VoiceTime` に固定してVoiceを追加する。時計やBPM管理はCoreにない。
- `VoiceCollection.Restart` は同じTriggerTarget参照のVoiceだけを置き換える。置換Voiceを先に生成・検証するため、生成失敗時には既存Voiceが残る。同じGroupDefinitionを参照する別TriggerTargetのVoiceには影響しない。
- VoiceはそのTrigger専用のModulatorとLifetimeを所有する。共有DefinitionやGroupを複数Voiceで使っても実行状態は共有しない。VoiceCollectionは挿入順で評価し、評価時に終了判定を先に行い、終了時刻ちょうどから描画対象外として一覧から除く。Restartで除去したVoiceは保持済み参照から評価しても終了済みとなる。
- Triggerされた階層には少なくとも1つのADSRまたはTimerが必要。LFOのみの構成は無効。寿命は信号値が0かではなく、すべての `ILifetime.IsFinished(nowSeconds)` がtrueかで決まる。

## GroupのscopeとBinding

`GroupDefinition` の各出現位置が実行scopeを持つ。`GroupRuntimeDefinition` の `modulators: null` は親scopeのModulator一覧を継承する。明示的な空リストを含め、Modulatorリストを指定すると、そのGroup出現に新しいscopeを作る。Bindingの添字は、そのGroupから子孫へ渡される最寄りの明示scope内の順序付きslotを指す。

Timerは `GroupRuntimeDefinition.Timers` に置き、指定されたGroup出現ごとにVoice開始時の `VoiceTime` で計時する。TimerはBinding slotにならない。`modulators: null` でsignalを継承してもTimersは追加できる。`GroupRuntimeDefinition` 自体がnullなら、signalもTimerも追加せず親scopeをそのまま通す。

同じscopeの複数slotが同一のsource Definitionを参照する場合、実行Instanceは1つにまとめられ、そのscope内の各slotから同じ状態を参照する。別Group出現や別Voiceではまとめない。同一ADSR sourceを同じscope内で異なる `AutoRelease` 値にした場合は拒否する。slotの順序と `BindingDefinition.ModulatorIndex` は対応させる。

```csharp
var runtime = new GroupRuntimeDefinition(
    modulators: new[] { new ModulatorSlotDefinition(adsr), new ModulatorSlotDefinition(lfo) },
    timers: new[] { new TimerDefinition(8) });
var target = new TriggerTarget(new GroupDefinition(transform, runtime, shape));
var voice = collection.Spawn(target, nowSeconds, currentBpm);
```

## 寿命とRelease

ADSRは `IModulator` と `ILifetime` の両方、Timerは `ILifetime` のみ、LFOは `IModulator` のみを実装する。Timerは拍数のDurationをVoice開始時BPMで秒へ換算し、開始時刻以上かつ経過拍がDuration以上で終了する。Timer終了は他のLifetimeへ作用せず、自動Releaseも発火しない。

`VoiceInstance.Release(nowSeconds)` はVoice内のADSRだけへ外部Releaseを送る。各ADSRは最初のRelease時刻を記録し、繰り返しReleaseでランプを再開しない。ADSRの `AutoRelease` が有効ならAttack+Decay+SustainTime後にReleaseへ進み、無効なら外部Releaseを待つ。ADSRが終了してもTimerなど別のLifetimeが残ればVoiceは存続する。Timerが終了しても保持中ADSRが残れば同様。

Timerのみ、ADSRのみ、または両方を持つVoiceを作れる。0拍Timerは開始時刻で終了するが、開始より前の評価では終了扱いにならない。Durationは有限かつ非負、BPMは有限かつ正、時刻は有限であることを検証する。負の絶対時刻は既存VoiceTime同様に許容する。

## Unity接続と検証Scene

`ShapeSynthController` は `TargetCameraCanvas` とOutput Cameraを使い、Unityの `Time.timeAsDouble` と呼出側の現在有効なBPMでSpawn/Restartする。毎フレームVoiceCollectionを評価し、`DrawItem` をCameraへ送る。Controller自身はBPMを取得しない。サンプル `VoiceSmokeTest` は `[Inject]` で `IBPMManager` を受け、各Trigger時の `CurrentBpm` を渡す。Trigger時に予約BPMではなく実際に有効な値を取得する。

`ShapeSynth/Voice Lifetime` のサンプルは画面上部の横並び（Timer、ADSR+Timer、保持ADSR+Timer）に配置する。起動時にTimerとADSR+Timerを各1つSpawnする。操作は [Samples README](../Samples/README.md) を参照。

## 今回の範囲外

永続的なVoice ID、任意Voiceの検索・個別停止、配置の保存形式、Variation / Layout、UI編集、MIDI Trigger/NoteOff接続、Voice数制限・poolingはこのAPIで提供しない。別TriggerTargetを同じDefinitionから作るとRestart単位も別になる。これらは後続Phaseで設計する。

## 検証結果（2026-11-11）

Unityコンパイル成功。既存94ケースを含むEditMode115ケースすべて成功。PlayでSpawn/対象Restart/Timer終了後の保持ADSR存続/Release後のVoice0を確認。BPM120→180の変更前後で既存Voice120・新Voice180を確認し、設定は元へ復元した。SceneはEditorで保存済み。外部Syphon受信と最終的な目視判断、実キーボード入力の確認はユーザー担当。

Collectionの終了Voice除去は破壊的であり、過去時刻へ戻して消えたVoiceを復元しない。親scopeへの明示パス参照、永続的な配置ID/参照リマップは後続で検討する。
