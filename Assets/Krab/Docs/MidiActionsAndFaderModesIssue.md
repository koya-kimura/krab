# MIDI Action拡張とAPC mini mk2フェーダーモード

## Status

仕様確定済み。実装着手可能。

## Why

現在のMIDI基盤は、Note押下でboolを反転する`ToggleMidiAction`と、Control Changeを0〜1のfloatへ変換する`FloatMidiAction`だけを持つ。VJ操作では、押している間だけ有効になるゲート、1フレームだけのトリガー、複数状態の巡回、排他的な選択、beat同期シーケンスが必要だが、現状は各利用側が個別実装しなければならない。

また、APC mini mk2の8本のChannel FaderにはTrackボタンが対応しているが、現在はTrackボタンを通常Bindingとして扱っており、フェーダー出力を`normal`、`random`、`mute`で切り替える共通機能がない。Master Faderには対応する発光ボタンがなく、ShiftをMaster用に予約すると汎用トリガーとして利用できなくなる。

MIDI Actionとデバイス固有のフェーダーモードをCommon側へ集約し、Scene固有コードが同じ状態遷移、保存、beat同期、LED表現を再実装しなくてよい状態を目指す。

## What

### MIDI Action

次のActionをC#コードから登録できるようにする。

- `toggle`
- `radio`
- `oneshot`
- `momentary`
- `state`
- `sequence`

`random`はMIDI Actionとして実装しない。ランダム処理はAPC mini mk2のフェーダーモードだけに存在する。

Action種別と`state`の状態数・状態色はC#登録時に固定する。`radio`は永続IDを持つ動的リストとの同期を許可し、`sequence`は登録後にステップ数を変更できる。ランタイムUIからAction種別は変更しない。

### Actionの挙動

#### `toggle`

- 押下ごとにboolを反転する。
- Note Offでは変更しない。
- 現在値をPrefsへ反映し、明示的な`Prefs.Save`時に永続化する。
- ONは設定されたアクティブ色、OFFは同色の25%輝度で表示する。

#### `radio`

- 1つのグループに1個以上のBindingを持つ。
- 各Bindingは個別にMIDI Learnする。
- 押されたBindingの0始まりインデックスを選択値にする。
- 常に1つの有効な選択位置を保持する。
- 選択位置をPrefsへ反映し、明示的な`Prefs.Save`時に永続化する。
- 選択中はアクティブ色、非選択は同色の25%輝度で表示する。

#### `oneshot`

- 押下でboolをON、解放でOFFにする。
- 押している時間だけONを維持する。
- 押下中の一時状態は永続化しない。再起動時は必ずOFFとする。
- ONはアクティブ色、OFFは同色の25%輝度で表示する。

名称は一般的なMIDI用語よりも本プロジェクトで合意した仕様を優先する。

#### `momentary`

- 押下したフレームでboolをONにする。
- `Time.frameCount`が押下フレームより大きくなった最初の更新でOFFに戻す。
- Note Offでは変更しない。
- パルス状態は永続化しない。再起動時は必ずOFFとする。
- LED状態も値と同じ1フレームだけ変化させる。視認用の保持時間は設けない。

名称は一般的なMIDI用語よりも本プロジェクトで合意した仕様を優先する。

#### `state`

- 状態数は2以上とし、C#登録時に指定する。
- 押下ごとに`0 → 1 → ... → stateCount - 1 → 0`と巡回する。
- 現在状態をintとして公開する。
- 現在状態をPrefsへ反映し、明示的な`Prefs.Save`時に永続化する。
- 状態色が登録されている場合はその色を使う。
- 状態色が省略された場合、状態0はアクティブ色の25%輝度、それ以外はアクティブ色を使う。

#### `sequence`

- 1つのグループに1個以上のステップBindingを持つ。
- 各ステップBindingは個別にMIDI Learnする。
- ステップ押下でそのステップの有効・無効を反転する。
- `floor(IBPMManager.Beat) % stepCount`を現在位置とする。
- 外部には`CurrentStepIndex`と`IsCurrentStepEnabled`だけを公開する。
- パターン全体を外部から読み書きするAPIは提供しない。
- ステップパターンをPrefsへ反映し、明示的な`Prefs.Save`時に永続化する。
- LEDは「現在位置」「有効」「無効」を区別する。既定値は現在位置を黄、有効をBindingのアクティブ色、無効を同色の25%輝度とする。

### 保存契約

- Actionの状態変更、MIDI Learn、LED色変更、フェーダー設定変更はPrefsのメモリ上の値だけを更新する。
- ディスクへ書き込むのは、既存のSaveボタンまたはPrefs Save用Input Actionが`Prefs.Save`を明示的に呼んだときだけとする。
- 自動保存、デバウンス保存、`OnDisable`、`OnDestroy`、`OnApplicationQuit`からの暗黙保存は行わない。
- 保存前に終了した変更が失われることは仕様とする。

### APC mini mk2 Channel Faderモード

Channel Fader 0〜7にだけ、対応するTrackボタン0〜7を使ったモード切替を提供する。

- 各Channel Faderには`random`または`mute`を割り当てる。
- 割り当てはランタイムUIから変更し、Prefsへ保持する。
- Trackボタン押下ごとに`normal`と割り当てモードを切り替える。
- Trackボタンはフェーダーモード専用とし、通常のMIDI Learnには渡さない。
- フェーダーモードとTrackボタンはページ非依存とする。
- 物理Track LEDは、`random`と`mute`のどちらでもアクティブ時は赤点灯、`normal`時は消灯とする。
- シミュレーターでは`random`をピンク、`mute`を赤、`normal`を消灯で表示する。

既定割り当ては次のとおりとする。

| Fader index | Assignment |
| --- | --- |
| 0 | `mute` |
| 1 | `mute` |
| 2 | `mute` |
| 3 | `mute` |
| 4 | `mute` |
| 5 | `mute` |
| 6 | `mute` |
| 7 | `mute` |

#### `normal`

- 出力は物理フェーダー位置の0.0〜1.0とする。
- Track LEDは消灯する。

#### `random`

- 出力は0または1とする。
- 有効化した時点で現在の`floor(beat)`から値を生成する。
- 以降、beat整数部分が変わるごとに更新する。
- 同じbeat整数とFader indexからは、アプリ再起動後も同じ値を生成する。
- 同じ値が連続することを許可する。
- モード中も物理位置は記録するが出力には使わない。
- `normal`へ戻した時点で、最後に受信した物理位置を即時出力する。

#### `mute`

- 出力を0に固定する。
- モード中も物理位置は記録するが出力には使わない。
- `normal`へ戻した時点で、最後に受信した物理位置を即時出力する。

#### Master Fader

- Master Fader（index 8）は常に`normal`とする。
- モード割り当て、モード切替、モード状態のPrefsキー、対応ボタンを持たない。
- UIには`normal (fixed)`と表示する。

### Shift

- Shiftはフェーダーモードに使用しない。
- Shiftは通常のMIDI Action Bindingへ割り当て可能とする。
- Shift Bindingはページ概念を持たず、どのページを表示中でも同じ1つのBindingとして扱う。
- Shiftは実機LEDを持たない。シミュレーターだけAction状態色を表示する。

## Scope

- MIDI Actionの状態遷移、LED状態、明示保存用Prefs連携
- beat／フレーム更新をActionへ伝えるランタイム契約
- `radio`／`sequence`のグループ登録と個別Learn
- APC mini mk2のFader 0〜7モードと設定UI
- Master Faderの固定`normal`化
- ShiftのグローバルBinding化
- 実機LED、シミュレーター、サンプルUI、技術HTMLの更新

## Non-Goals

- MIDI Actionとしての`random`
- Master Faderのモード切替
- Action種別・状態数・グループ数をランタイムUIから変更する機能
- Sequenceパターン全体を外部から読み書きするAPI
- `momentary` LEDの視認時間延長
- Track LEDで`random`と`mute`を色分けすること
- Shiftの実機LEDフィードバック
- 自動保存
- MIDI Clock受信や外部クロック同期
- 既存コード全体へのasmdef導入

## Acceptance Criteria

1. 6種類のActionがC#から登録でき、上記の押下・解放・フレーム・beat契約どおりに値が変わる。
2. `radio`は常に1つを選択し、各選択肢を個別にLearnできる。
3. `sequence`はbeat整数に同期して巡回し、外部から現在ステップ番号と有効状態だけを取得できる。
4. 保存対象の状態は`Prefs.Save`後の再起動で復元され、保存しなかった変更は復元されない。
5. `oneshot`と`momentary`は再起動時にOFFになる。
6. Fader 0〜7の割り当てとアクティブ状態がPrefsから復元される。
7. Fader 0〜7の実効値が`normal`、`random`、`mute`の仕様どおりになり、モード変更・beat変更・ページ変更時に現在ページのBindingへ同期される。
8. Master Faderは常に物理値を返し、モード設定UIを持たない。
9. Trackボタンはページに関係なく対応Faderのモードを切り替え、通常Bindingには使われない。
10. Shiftはページを切り替えても同じBindingを実行し、通常のMIDI Learn対象になる。
11. 実機Track LEDは割り当てモード中に赤点灯し、シミュレーターは`random`をピンク、`mute`を赤で表示する。
12. Unityのコンパイルエラーと実行時例外がなく、既存Toggle／Float Binding、ページ切替、MIDI Learnを壊さない。

## Agreed Decisions

- Action種別はC#登録時に固定する。Radio optionとSequence step countは後続合意により動的変更を許可する。
- MIDI Actionの`random`は廃止する。
- Sequenceパターンは永続化するが外部公開しない。
- Master Faderは常時`normal`とする。
- Shiftはページ非依存の汎用Bindingとする。
- Track LEDは`random`／`mute`の両方を赤点灯にする。
- 自動保存せず、既存の明示的な`Prefs.Save`だけを保存境界とする。

## Follow-up: Action表示と利用例

- 各MIDI Bindingは、設定ウインドウから登録済みAction種別を確認できること。
- `radio`と`sequence`の子Bindingは、種別だけでなくoption／step番号とグループ要素数も表示すること。
- Sample ComponentはAction単体の値表示だけでなく、実際のUI値との接続例を示すこと。
  - `state`は現在の名前付き状態を表示する。Sampleでは動作しないdropdownを使用しない。
  - 追加・削除可能なPrefsリスト選択を`radio`で操作し、各要素を個別Learnする。
  - 0〜1の`float`を角度など別レンジへ変換して利用する。
- `sequence`パターン全体の外部読み出しは、既存のNon-Goalを維持する。Sampleでは現在位置と現在ステップの有効状態、および各stepのLearn入口までを表示する。
- Sampleウインドウは高さを制限して縦スクロール可能にし、同じActionと同じUI接続を繰り返すだけのサンプルは1件へ集約する。
- 動的Radio要素はindexではなく永続IDでBindingを識別する。並べ替え時も割り当てを同じ要素へ追従させ、削除時は実行中登録だけを解除してPrefs上の割り当てを残す。
- Radioは`●/○ + 名前 + MIDI + 削除`のFold、Sequenceは横1列のステップFoldとして表示する。
- Sequenceは1〜16ステップへ変更可能とし、常に横1列＋横スクロールで表示する。縮小時に超過patternを破棄し、再拡張したstepはOFFとする。削除されたstepのMIDI割り当てPrefsは残し、同じindexの再追加時に復元する。
- 各Sequence stepはUIボタンでもON/OFFを切り替えられる。ボタンは無効＝暗色、有効＝シアン、現在位置＝黄色の3状態とし、現在位置表示に文字記号は使用しない。
- 利用側にはstep単位の`IsStepEnabled(index)`と`ToggleStep(index)`を公開するが、pattern配列そのものの取得・置換APIは引き続き提供しない。

## Related Plan

- [MidiActionsAndFaderModesImplementationPlan.md](MidiActionsAndFaderModesImplementationPlan.md)
