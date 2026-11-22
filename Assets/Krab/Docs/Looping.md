# イベントルーパーCore

2026-11-22。ShapeSynthのルーパーから利用する、Unity非依存の記録・再生処理。

## 責務と依存

`Assets/Krab/Common/Scripts/Looping/Core/` の `Krab.Common.Looping.Core` は `noEngineReferences: true`。Unity、BPMManager、MIDI、描画、ShapeSynthを参照しない。

`EventLooper<T>` が記録するのは、呼び出し元が渡したPayloadと時刻の組である。秒か拍かはCoreには定義しない。同じ単調な時計・単位でStart／Record／Finish／Advanceを呼ぶ。ShapeSynthはPayloadに保存プリセットID（string）、時計に実時間の秒（`Time.unscaledTimeAsDouble`）を使う。

```text
Common.Looping.Core  ←  ShapeSynth.Performance
                        ├ 実時間の秒を渡す
                        ├ 手動TriggerでプリセットIDを記録
                        ├ 再生イベントでShapeSynthを発火
                        └ RosettaUIと既存MIDIへの接続
```

将来、文字や画像等にも適用する場合は、それぞれがPayloadと発火処理を用意する。共通Coreへ描画種別を追加しない。現段階では共通の汎用イベントバスや永続化まで広げない。

## 8スロットのバンクとUI・MIDI

- `LooperBank<T>`（Core）：同じ時計を共有する複数のEventLooper。`Cycle(slot)` で Idle → Recording → Playing → Idle（p5版の1ボタン操作）、`Clear(slot)`、`Record(payload)` は録音中の全スロットへ記録、`Advance(slot, output)` で再生。時計は `Func<double>` で受け取る。状態が変わると `StateChanged(slot)`。
- `ILooperBank`（Core）：Payloadに依存しない操作面。UIやMIDIなどの入力部品はこれだけを見る。
- `LooperBankUi`（`Views/`、namespace `Krab.Common.Looping.Views`）：RosettaUIの行。MIDI学習ボタンなど入力系の部品は `Func<int, Element>` で外から差し込むため、MIDIを参照しない。
- `LooperBankMidi`（`Midi/`、namespace `Krab.Common.Looping.Midi`）：任意。`MidiBindingManager` に、スロットごとの3状態Cycle（LED：灰／赤／緑）とClearのActionを `{接頭辞}.{番号}.Cycle|Clear` で登録する。保存されたState値は復元せず、常に実際のスロット状態へ揃える。Disposeで登録を解除する。

CommonのMidiはasmdefを持たないため、Coreのasmdef（`noEngineReferences`）でMIDI非依存を保証し、ViewsとMidiはフォルダとnamespaceで分ける。MIDIを使わない利用側は `LooperBankMidi` を生成しなければよい。

## 操作とAPI

- `StartRecording(position)`：旧内容を消して録音開始。
- `Record(payload, position)`：Recording中だけ記録。録音開始からの相対位置と入力順を保持。
- `FinishRecording(position)`：実際の経過位置をループ長にし、停止位置を再生の原点としてPlayingへ。空録音／長さ0はIdleへ戻る。
- `Advance(position, output)`：現在位置までに到来したイベントをoutputへ追記。各イベントのPositionは予定された絶対位置。同じ時刻を再度渡しても二重発火しない。
- `Clear()`：内容・カーソル・状態を初期化。

StateはIdle、Recording、Playing。Coreはクリック順や8スロットを知らない。それらは接続側の操作方針である。

## 境界条件

- NaN／Infinity、処理中の時計の逆行を拒否する。新しいStartRecordingは時計の原点を取り直せる。
- 同時刻のイベントは録音順。前周末尾と次周先頭が同じ時刻の場合は、前周末尾を先に返す。
- フレームが複数周をまたいでもイベントを列挙する。ただしAdvance一回につき先頭256件を上限にし、それ以外の期限超過分は破棄する。次フレームに同じ滞留分を再放出しない。
- 破棄数はDroppedEventCountに累積し、上限では飽和する。利用側で表示・計測に使える。
- Coreはコールバックを実行しない。返却リストを利用側で処理し、再生が再録音されるか等も接続側で決める。

## 検証

`Assets/Krab/Common/Tests/Editor/Looping/` の専用EditModeテストアセンブリ `Krab.Common.Looping.Core.Tests`。Unity Test Runnerで9件成功、失敗0。オフセット、同時刻・周回の順序、空・長さ0、再録、独立性、時刻不正、長い停止からの復帰を確認した。

ShapeSynth接続と使用手順は [ShapeSynth Looper](ShapeSynth/Looper.md) を参照。
