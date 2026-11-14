# Phase 7：FX01演奏サンプルとModulator Library

Phase 8で5キーとStrokeへ拡張した。最新の操作・JSON補完仕様は [複数プリセット](MultiPresetPerformance.md) を参照。

## 起動と操作

`Assets/Krab/ShapeSynth/Samples/ShapeSynthPerformance.unity` を開いてPlay。Game Viewで **Q** を押すとFX01をSpawnする。連打で別SeedのVoiceを重ねる。初期状態は黒画面で、自動Triggerはしない。

既存UiBuilderのSettingsから **ShapeSynthPerformance** を開く。Dは既存UI表示切替。ADSR/LFOのFoldを開いて名前と数値を編集し、**Trigger FX01 (Q)** で次のVoiceへ反映する。キーとボタンは同じ `TriggerFx01()` を使用する。UIにフォーカスがある間はQを受け付けず、名前入力で誤Triggerしない。演奏へ戻る際はUI外のGame Viewをクリックする。

- ADSR：Attack、Decay、Sustain time、Releaseの単位は拍。Sustain levelは0〜1。
- LFO：Waveform、Cycles per beat、Phase offset（周期単位）。
- Circle count：初期150、調整範囲1〜2000。初期Seedは1234。成功したTriggerごとにSeedを1増やす（intの周回はunchecked）。Initial seed編集とReload後はそのSeedから再開始。
- **Save ShapeSynth**：専用設定ファイルへ保存。
- **Reload ShapeSynth**：保存ファイルを検証して編集データを置換。既存Voiceは変更しない。

共通Settingsの **Save** / Cmd+S は従来PrefsGUI用。ShapeSynthの専用JSONを保存するには **Save ShapeSynth** を使う。不正値・参照切れ・保存エラーは同ウィンドウのStatusへ表示し、Trigger/保存/読み込みを拒否する。

## FX01

[旧synth-machineのfx01.ts](https://github.com/koya-kimura/synth-machine/blob/main/src/synth/presets/fx01.ts) をGitHub接続から調査。元の1000個やMovementクラスを移植せず、赤 `#C7190C` と水色 `#39E1BF`、ランダムな小円・位置・移動、出現/消滅という視覚的特徴を既存Coreで再構成した。

`Fx01Preset.Build(library, settings)` はFX01専用の生成関数。150個の子は2つの共有Circle Definitionを交互に参照する。Root scopeにはIDから解決したADSR slot0/LFO slot1だけを置く。OpacityはADSR、Position X/YはADSRとLFOのBindingで変調し、そのDepthを個体ごとにVariationで解決する。位置はCanvas Width/Height、サイズはMinDimensionをReferenceとして全画面へ配置する。ADSRがLifetimeでもあるため、全Voiceが自然に終了する。色の時間変調やLayoutは追加していない。

同じSeed・同じ設定・同じ経過拍なら同じ配置/動き。異なるSeedは別配置。新しいTrigger時刻やBPMによる位相/速度の差は従来VoiceTimeの仕様どおり。

## DefinitionのIDとRuntime

- `ModulatorEntry` は不変のId/NameとADSRまたはLFO Definitionを持つ。IdはOrdinalで識別し、空白だけ/空/null・重複を拒否。Nameは表示用で、変更しても参照IDは変わらない。空表示名は許可する。
- `ModulatorLibrary` は編集状態から作る不変スナップショット。`Get(id)` は不存在を拒否し、`ResolveSlots(ids)` は順序付きslotへ解決する。同じentryは同じslot/Definitionを参照する。
- `PerformanceSettings` / `ModulatorSettings` / `Fx01Settings` はUnity非依存の保存・編集用DTO。CoreのライブInstanceをシリアライズしない。FX01側はEnvelopeIdとOscillatorIdを保持し、Definitionの数値をVisualへ直接埋め込まない。
- TriggerごとにDTOを検証し、新しい不変Library/Groupを構築して既存ControllerへSpawnする。古いVoiceは以前のDefinitionを保持する。編集による既存Instanceの再生成、位相リセット、BPM追従は行わない。
- Binding評価は従来のscope内 `ModulatorIndex` のまま。IDをCoreのBinding/Voice全体へ持ち込む再設計はしていない。別VoiceのInstanceは独立する。

Libraryは名前・ID解決を担う汎用の小さなCore層。FX01は専用生成関数。入力・RosettaUI・永続保存の境界はScripts/PerformanceとUnityへ分離する。ModulatorごとのMonoBehaviourは作らない。

## 保存

`PerformanceSettingsStore` がVersion=1のJSONを読み書きする。既存PrefsGUIはCommonのプリミティブ設定を一括保存するが、今回のModulator ID参照と将来の構造化プリセット保存にはDTOの専用ファイルを選んだ。

保存先は `Application.persistentDataPath/ShapeSynth/Performance.v1.json`。現在のmacOS Editorでは `/Users/kimurakoya/Library/Application Support/DefaultCompany/krab/ShapeSynth/Performance.v1.json`。ビルドでもpersistentDataPathを使用し、ScriptableObjectアセットの変更に依存しない。ファイルパスは保存成功時のStatusにも表示する。

Saveは全DTOとID参照を検証してから一時ファイルへ書き、既存ファイルを置換する。無効な編集値で以前の正常ファイルを上書きしない。Reloadは全体を検証してから編集モデルを置き換え、無効なら現在のモデルを保持する。起動時の読み込みに失敗した場合は警告とStatusを出し既定値を使うが、壊れたファイルを自動上書きしない。

## Sceneの再利用

SmokeTestは変更せず、Editorから保存済みコピーを作り、コピー内の `ShapeSynth` 検証オブジェクト群だけを新しい `ShapeSynth Performance` に置換した。Common/SceneContainer/BPM/UiBuilder/OutputTextureProvider/Preview/Syphonを引き継ぐ。新オブジェクトはTargetCameraCanvas、既存ShapeSynthController、ShapeSynthPerformanceのみ。RenderTextureは従来Providerが所有し、新しいRenderer/OutputManagerは作らない。Common由来の操作・デバッグUIは演奏出力RTへは描かれない。

再作成は `Krab/Shape Synth/Create Performance Scene`。既存Performance Sceneがあれば上書きを拒否する。本番SceneとSmokeTestの未保存変更を破壊しない。

## 後続の課題

IDの追加/削除・任意VisualのIDルーティング編集、名前付きプリセット管理、schema migration、任意scopeのID配置、MIDI/Pooling/Layoutは対象外。今回は固定FX01の2つのID参照を保存する。将来Restartへ対応する場合、Triggerごとの新しいTargetを演奏配置IDへ関連付ける設計が必要。ファイルを複数プロセスで同時編集する競合解決は提供しない。

## 検証結果（2026-11-11）

Unityコンパイル成功、既存134件を含むEditMode157件すべて成功。実RosettaUIの入力・Trigger/Save/Reloadイベント、新旧Voiceの設定分離、Playを入り直した後のファイル自動復元を確認した。150個のRT描画・自然終了・Preview/Syphon参照一致も確認済み。最終Consoleはerror0/warning0。検証用保存ファイルは元の状態へ戻している。

Q入力ハンドラはInput Systemイベントの明示処理で確認したが、物理キーの通常操作は自動操作ツールの制約で未確認。Game ViewでのQ、最終映像、外部Syphon受信はユーザー確認対象。詳細は `Reports/Phase7Validation-2026-11-11.md`。
