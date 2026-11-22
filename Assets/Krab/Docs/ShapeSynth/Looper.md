# ShapeSynthの8スロットルーパー

2026-11-22追加。p5の `/Users/kimurakoya/Documents/develop/p5/synth-machine/src/synth/looper.ts` と `src/manager/visualComposer.ts` の使い方を参考にした。

## 設計

ルーパーはCommonの3層に分かれ、MIDIは任意の追加部品にしている（詳細は [Commonのルーパー](../Looping.md)）。

```text
Common/Scripts/Looping/
  Core/   EventLooper・LooperBank（8スロットの録音→再生→クリア）  ← Unity・MIDIに非依存
  Views/  LooperBankUi（RosettaUIの行）                          ← MIDIに非依存
  Midi/   LooperBankMidi（Cycle/ClearのMIDI Action・LED）        ← CommonのMidiを使う唯一の部分
```

ShapeSynth側は `LooperBank<string>`（PayloadはプリセットID）を持ち、時計とプリセットの発火を接続する。MIDIが注入されている場合だけ `LooperBankMidi` を付け、UIにはMIDI学習ボタンを外から差し込む。MIDIを使わない別の利用側は、CoreとViewsだけで同じルーパーを使える。

p5版と同じく秒数固定で記録する。時計は `Time.unscaledTimeAsDouble`（実時間の秒、timeScaleの影響なし）。録音開始から停止までの実時間をそのままループ長とし、BPMを変えてもループの長さとリズムは変わらない。BPMは各Voiceの生成時に使うだけで、ルーパーの時計には使わない。

クオンタイズ、カウントイン、小節への長さ丸めは行わない。停止が2.075秒なら2.075秒として反復する。

（Codexによる初版は `IBPMManager.Beat` を時計にした拍数基準だった。ユーザーの選択により2026-11-22に秒数固定へ変更した。Coreは単位を持たないため、変更は接続側の時計だけ。）

## Sceneと操作

専用検証Scene：`Assets/Krab/ShapeSynth/Samples/ShapeSynthLooper.unity`。

1. Sceneを開いてPlayする。
2. Settings → ShapeSynth → Performance → Performanceを開く。
3. 上部のLoopersを展開する。
4. Loop 1のRecordを押す。
5. 下のプリセットタブからTriggerを押す。MIDIで叩いた発火も同様に録音される。
6. Loop 1のRecording → Playを押すと、その瞬間から繰り返す。
7. Playing → Clear、またはClearで内容を消して停止する。

8スロットは独立。複数のスロットへ同時に録音でき、複数のフレーズを重ねて再生できる。空の録音を再生しようとするとEmptyへ戻る。

通常の `ShapeSynthPerformance.unity` にも同じUIが出る。Preset EditorのGalleryは保存済みプリセットの比較用で、ルーパーの録音入力にはしない。

## MIDI

各スロットに2つの割り当てがある。

- 状態ボタンの横のMIDI：1回でRecord → もう1回でPlay → もう1回でClear。既存の3状態MIDI Actionを使い、対応LEDではIdle=灰、Recording=赤、Playing=緑。
- Clear横のMIDI：そのスロットだけをクリア。

Action IDは `ShapeSynth.Looper.1.Cycle` / `ShapeSynth.Looper.1.Clear` から8まで（`LooperBankMidi` に渡す接頭辞 `ShapeSynth.Looper` から生成）。既存のMIDI学習・保存を利用し、構造変更前に学習した割り当てもそのまま使える。機器への既定割り当ては追加しないので、既存のプリセット操作やフェーダーを奪わない。

ループ内容はセッション中のみ保持する。MIDIの割り当ては保存できるが、次のPlayで録音・再生状態だけが復活しないよう、状態はIdleへ揃える。Component無効化・Play終了時は録音内容をクリアし、ルーパーのMIDI Actionを解除する。

## 記録と再生の規則

- 記録対象は、成功した手動のプリセットTrigger。UI／MIDIの入口を共通にする。
- ループ再生から生まれたTriggerは録音しない。別スロットを録音中でも、既存ループの内容が混ざらない。
- 一覧の番号ではなくプリセットIDを保存するので、名前変更やタブ変更で対象が変わらない。
- 再生時には既存経路でプリセットを読み直してCompileする。保存した変更は次の発火から反映し、RandのSeedも新しいVoiceごとに進める。
- プリセットが削除された／不正になった場合は、影響を受けたスロットだけ停止してエラーを表示する。他のループや手動演奏は継続できる。
- Clearは以降の発火を止める。すでに出ているVoiceは元の寿命まで残る。
- 長いフレーム停止ではCoreの上限を超える期限超過イベントを破棄し、UIに通知する。

再生はフレーム単位の即時Trigger。予定時刻へさかのぼってVoiceを生成する仕組みは今回追加していない。

## 確認したこと

- Unityコンパイル成功。共通CoreのEditModeテスト9件成功。
- 専用SceneでUI操作・既存MIDI Actionからの録音／再生／クリアを確認。
- 0.999拍の録音が丸められないこと、同じ更新時刻で二重発火しないことを確認（拍数基準の初版で実施）。
- 3層への分割後：コンパイル成功、Core 15件成功（LooperBank 6件を追加）。Play Modeで8スロット分のMIDI Actionの登録、MIDIからの録音／再生／クリア、再生が別スロットの録音に混ざらないこと、空録音のIdle復帰、無効化時のクリアとAction解除、再有効化後の再登録を確認（`Reports/ShapeSynthLooper/VerifyMidi.cs`）。秒数固定の確認も再実行して成功。
- 秒数固定への変更後：コンパイル成功、Core 9件成功。Play Modeで拍の時計を止めた状態でも録音・再生でき、ループ長が実録音時間（1.67秒）と一致、再生中にBPMを変えても3周で約6回（実測7回）の発火で長さが変わらないことを確認（`Reports/ShapeSynthLooper/VerifySeconds.cs`）。
- 別スロットの録音中に再生イベントが混入しないこと、空録音のIdle復帰を確認。
- 欠損プリセットで当該スロットだけが停止すること、無効化時のクリア／MIDI解除、再有効化時の再登録を確認。
- 実時間で2スロットへそれぞれ2イベントを録音し、同時再生によるRenderTexture出力と操作画面を確認。
- Console Errorなし。Sceneの未保存変更なし。実行ログ・画像は `Reports/ShapeSynthLooper/`。

実機MIDI、外部Syphon／Spout受信、長時間のライブ運用、Windowsは未検証。録音保存、オーバーダブ、停止内容を保持した再開、拍への吸着は今回追加していない。使い心地と見た目の最終評価はユーザーが行う。
