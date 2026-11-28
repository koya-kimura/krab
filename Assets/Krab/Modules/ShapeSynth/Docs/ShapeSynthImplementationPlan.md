# ShapeSynth：実装状況と今後の検討

2026-11-17時点。完了済みの初期実装手順と廃止した組み込みプリセットの計画は削除し、現在の接続と未決事項をまとめる。

## 実装済み

- Unity非依存のCore：VoiceTime、ADSR、LFO、Parameter / Binding、Shape / Group / Transform、VoiceとLifetime、Random / Delay / Flip。
- p5.Unite描画Adapter、指定Cameraへの描画と既存Camera → RenderTexture → Preview / Syphonの接続。
- PresetDocument / PresetCompiler / PresetRepositoryによる編集・コンパイル・JSON保存。現行SchemaVersionは5。色はHSB Parameter、Stroke WidthもBinding可能。Transformは Position に加えて Direction / Distance で動く方向を持つ。
- RosettaUIのPreset Editor。現在の操作は [Preset Editor](PresetEditorWorkspace.md) を参照。
- ユーザープリセットの演奏UIとMIDI Trigger。保存済みプリセットを自動検出し、Trigger時に読み直してCompileする。[演奏UI](UserPresetPerformance.md) を参照。

## 保存データの互換性

2026-11-18のSchemaVersion 5で、旧版（1〜4）からの移行処理 `PresetJsonMigration` を削除した。旧版のJSONは読み込めず、一覧の下にエラーとして表示される。Rampの廃止とDirection / Distanceの追加に合わせた破壊的変更で、本番用の保存済みプリセットはない前提。

## 既存基盤との接続

- Core asmdefの `noEngineReferences` を維持する。CoreからCommon・UI・描画へ依存しない。
- Trigger時の `IBPMManager.CurrentBpm` とゲーム時間を取得し、VoiceTimeへ渡す。生成済みVoiceのBPMは固定する。累積BeatやBPM予約値をVoiceの時計にしない。
- 共通DI、RosettaUI / UiBuilder、MIDI Action / Bindingを再利用する。MIDI割り当ての永続化は明示的なPrefs Saveで行う。プリセットJSON保存とは別。
- OutputTextureProviderがRenderTextureの生成・再生成・解放を所有する。Preview / SyphonはTextureChanged経由で更新する。

## 今後の検討（未実装・未合意）

- MIDI NoteOffとVoiceの対応は、2026-11-24にプリセットごとのHold（OneShot）割り当てとして追加した（[演奏UI](UserPresetPerformance.md)）。ベロシティ、同じプリセットを複数のパッドで同時に保持する扱いは未対応。
- 最大Voice数、Pooling、想定負荷・FPS・GCの基準と測定。
- 演奏中のDefinition変更を既存Voiceへ反映する仕様。現在は生成時のスナップショット。
- ゲーム時間以外の時計や明示的なトランスポート、厳密な拍同期。ルーパーは2026-11-24にスロットごとのBPM同期（小節への丸め）を選べるようにした（[ルーパー](Looper.md)）。
- ADSRの積分値、Triggerをまたいで積み重なる値。

これらは依頼範囲と仕様を合意してから実装する。検証Sceneの操作は [Samples README](../Samples/README.md) を参照。
