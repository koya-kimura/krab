# MIDI Action拡張とAPC mini mk2フェーダーモード 実装計画

## Objective

既存のMIDI Learn、ページ、Prefs、実機／シミュレーター共通経路を維持しながら、`toggle`、`radio`、`oneshot`、`momentary`、`state`、`sequence`をCommonの再利用可能なActionとして提供する。同時に、APC mini mk2のChannel Fader 0〜7へページ非依存の`normal`／`random`／`mute`モードを追加する。

成功条件は、関連IssueのAcceptance Criteriaをすべて満たし、Master Faderは常時`normal`、Shiftはページ非依存Binding、永続化は明示的な`Prefs.Save`だけ、という合意を実装・UI・文書の全てで一貫させることである。

関連Issue: [MidiActionsAndFaderModesIssue.md](MidiActionsAndFaderModesIssue.md)

## Current State

### 調査基準

- Branch: `feature/2612_shirt-meets-lausbub`
- HEAD: `213416c` (`[add] BPMManger追加`)
- Unity: `6000.3.12f1`
- MIDI基盤はHEADに未コミットで、関連ファイルがステージ済みである。本計画はそのステージ済み内容を実装ベースラインとして扱う。
- 計画作成時にはコード変更、Unityコンパイル、Play Mode確認を実施していない。

### 現在の構造

- `Assets/Krab/Common/Scripts/Midi/Actions/IMidiAction.cs`
  - `void Execute(MidiMessage message)`だけを持つ。
  - フレーム更新、beat更新、LED色解決、破棄契約を持たない。
- `Assets/Krab/Common/Scripts/Midi/Actions/ToggleMidiAction.cs`
  - Note押下で`Param<bool>`を反転する。
- `Assets/Krab/Common/Scripts/Midi/Actions/FloatMidiAction.cs`
  - Control Changeを0〜1へClampして`Param<float>`へ書く。
- `Assets/Krab/Common/Scripts/Midi/Bindings/MidiBindingManager.cs`
  - `paramId`と`MidiControl`を1対1で管理する。
  - 同じ物理Controlの重複割り当てを解除する。
  - 一致するBindingのActionへ入力イベントを1回渡す。
  - Actionグループと時間更新の概念はない。
- `Assets/Krab/Common/Scripts/Midi/Bindings/MidiBindingRepository.cs`
  - `Midi.Binding.{paramId}`以下にControlとLED色をPrefsで保持する。
  - `Set`はPrefsのメモリキャッシュを更新し、ディスク書き込みは`Prefs.Save`に委ねる。
- `Assets/Krab/Common/Scripts/Midi/Devices/ApcMiniMk2/ApcMiniMk2InputAdapter.cs`
  - Fader CC 48〜56の物理値を9要素配列に保持する。
  - `GetFaderValue`は物理値をそのまま返す。
  - TrackとShiftを現在ページ付きNoteとしてBindingManagerへ渡す。
- `Assets/Krab/Common/Scripts/Midi/Input/MidiInputManager.cs`
  - Input System購読、BindingManager／InputAdapter所有、LED希望状態計算、15Hz差分送信を担当する。
  - `Update`ではLED更新だけを行う。
- `Assets/Krab/Common/Scripts/Midi/Devices/ApcMiniMk2/ApcMiniMk2OutputAdapter.cs`
  - RGB Padは任意色、Track／Pageは単色ON/OFFを送信する。
- `Assets/Krab/Common/Scripts/Midi/Devices/ApcMiniMk2/ApcMiniMk2Simulator.cs`
  - 64 Pad、8 Page、8 Track、Shift、9 Faderを実機と同じAdapterへ接続する。
- `Assets/Krab/Common/Scripts/Timing/IBPMManager.cs`
  - `Beat`と`OnBeat`を提供する。
- `Assets/Krab/Common/Scripts/Utils/Pcg.cs`
  - 再現可能なPCGハッシュを提供する。
- `Assets/Krab/Common/Scripts/Sample/ParamSampleComponent.cs`
  - Toggle 16個とFloat 9個だけを登録し、MIDI Learnの試験画面を提供する。
- `Assets/Krab/Common/Scripts/Views/UiBuilder.cs`
  - SaveボタンとPrefs Save用Input Actionから`Prefs.Save`を呼ぶ。

### 現在の制約

- APC mini mk2のTrackボタンは8個で赤単色LED、ShiftはLEDなし、Master Fader専用ボタンはない。
- 現在のBindingキーは`MidiControl.Type + Channel(Page) + Number`である。
- 自動テスト用asmdef／テストAssemblyは存在しない。Commonコードは`Assembly-CSharp`でコンパイルされる。
- 既存のステージ済み変更はユーザー作業であり、実装時に無関係な巻き戻しや再生成をしてはいけない。

## Problem

現行`IMidiAction`は入力イベントにしか反応できないため、次フレームでOFFにする`momentary`とbeatで進行する`sequence`を表現できない。LEDはboolの`IsActive`と単一色だけなので、`state`と`sequence`の複数色も表現できない。

また、BindingManagerは1 Binding対1 Actionを前提としており、複数Bindingが1つの値を共有する`radio`と`sequence`を登録・解除・更新する所有モデルがない。Actionの状態をPrefsへ保持する責務も未定義である。

Faderは物理値しか保持しておらず、Trackボタン、beat、Prefs、実効出力の関係がない。単に`GetFaderValue`だけを書き換えると、既存`FloatMidiAction`へ送る値とAPIから読む値が不一致になるため、物理値と実効値の単一経路が必要である。

## Requirements

### Functional

1. Issueで定義した6種類のActionをC#登録APIから作成できること。
2. Action登録時に安定した`actionId`を必須とし、PrefsキーとグループBinding IDの基点にすること。
3. `radio`と`sequence`は複数Bindingを1つのActionグループとして所有し、各項目を個別にLearnできること。
4. `momentary`は押下フレームより後の最初の更新でOFFになること。
5. `sequence`は`floor(Beat)`から現在位置を決め、beatが複数進んだ場合も現在値から直接位置を算出すること。
6. Action状態はPrefsキャッシュへ反映するが、`Prefs.Save`を内部から呼ばないこと。
7. Fader 0〜7の割り当てとアクティブ状態をUIで操作し、Prefsキャッシュへ反映すること。
8. FaderのAPI、MIDI BindingへのCC配送、シミュレーター表示が同じ実効値を使うこと。
9. Master Faderは常に物理値を実効値として使うこと。
10. TrackボタンはFader専用、Shiftはページ非依存の通常Bindingとすること。
11. 実機とシミュレーターのLED表現をIssueどおりにすること。
12. 既存Toggle／Float、MIDI Learn、ページ切替、Prefs Saveを後方互換で維持すること。

### Non-Functional

- 毎フレームの更新でLINQ、配列生成、文字列生成を行わない。
- beat更新時に必要なFader／Sequenceだけを更新する。
- LED送信の15Hz制限と差分送信を維持する。
- Action、グループ、BPMイベント、Prefs callbackの購読を所有者破棄時に解除する。
- 保存キーは安定IDから決定し、表示名や現在ページに依存させない。
- 不正な登録引数は登録時に例外とし、壊れた保存値は安全な既定値へ正規化する。

## Non-Goals

- MIDI Action `random`
- Master Faderモード
- Action構造のランタイム編集
- Sequenceパターン全体の公開API
- 自動保存
- `momentary` LEDの視認用ラッチ
- Track LEDの多色化／点滅化
- Shiftの物理LED
- 外部MIDI Clock
- asmdef再編と自動テスト基盤導入
- APC mini mk2以外のデバイス固有モード

## Proposed Architecture

### 1. Action契約

`IMidiAction`を次の責務へ拡張する。

```csharp
public interface IMidiAction
{
    void Execute(MidiMessage message);
    Color ResolveLedColor(Color configuredColor);
    IMidiActionRuntime Runtime { get; }
}

public interface IMidiActionRuntime
{
    void Update(in MidiActionUpdateContext context);
}

public readonly struct MidiActionUpdateContext
{
    public int FrameCount { get; }
    public long BeatIndex { get; }
}
```

- 時間更新が不要なActionは`Runtime == null`を返す。
- `momentary`は自身をRuntimeとして返す。
- `sequence`の各Step Actionは共有Sequence Runtimeを返す。
- BindingManagerはRuntimeを参照同一性で重複排除して1フレーム1回だけ更新する。
- `ResolveLedColor`は最終的なRGB色を返す。既定の非アクティブ色は`configuredColor * 0.25`とし、alphaは維持する。

### 2. Action登録と所有

`MidiBindingManager`へ型付き登録APIを追加する。

```csharp
ToggleMidiAction RegisterToggle(string actionId, Param<bool> value);
FloatMidiAction RegisterFloat(string actionId, Param<float> value);
OneShotMidiAction RegisterOneShot(string actionId, Param<bool> value);
MomentaryMidiAction RegisterMomentary(string actionId, Param<bool> value);
StateMidiAction RegisterState(
    string actionId,
    Param<int> value,
    int stateCount,
    IReadOnlyList<Color> stateColors = null);
RadioMidiAction RegisterRadio(
    string actionId,
    Param<int> selectedIndex,
    int optionCount);
SequenceMidiAction RegisterSequence(
    string actionId,
    int stepCount,
    SequenceLedPalette palette = default);
void UnregisterAction(string actionId);
```

- 単一Binding Actionでは`bindingId == actionId`とする。
- RadioのBinding IDは`{actionId}.Option.{index}`とする。
- SequenceのBinding IDは`{actionId}.Step.{index}`とする。
- `RadioMidiAction.GetBindingId(index)`と`SequenceMidiAction.GetBindingId(index)`をUI構築用に公開する。
- Managerは`actionId → Action registration`と`bindingId → MidiBinding`を別に管理する。
- `UnregisterAction`はグループ内Binding、Runtime参照、Param購読をまとめて解除する。
- 既存の低レベル`Register(bindingId, action)`はFloat等の互換用に残してよいが、新しい利用コードは型付きAPIを使用する。

### 3. Action状態Prefs

`MidiActionStateRepository`を新設し、ActionからPrefsGUIの詳細を分離する。

| State | Key | Type | Default |
| --- | --- | --- | --- |
| Toggle | `Midi.Action.{actionId}.Toggle` | `PrefsBool` | 登録時Param値 |
| Radio | `Midi.Action.{actionId}.Radio.SelectedIndex` | `PrefsInt` | `0` |
| State | `Midi.Action.{actionId}.State.Index` | `PrefsInt` | 登録時Param値を正規化 |
| Sequence | `Midi.Action.{actionId}.Sequence.Pattern` | `PrefsString` | 全て`0`のbit文字列 |

- Repositoryの`Set`はPrefsキャッシュだけを更新する。
- RepositoryとActionは`Prefs.Save`を呼ばない。
- Toggle／Radio／Stateは保存値を正規化してからParamへ反映し、その後`Param.Changed`を購読する。
- 外部UIやコードがParamを変更した場合もRepositoryのキャッシュへ反映する。
- Sequence文字列は`0`と`1`だけを受理する。長すぎる場合は末尾を切り、短い場合は`0`で補い、不正文字は`0`とする。
- OneShot／Momentaryには状態Prefsを作らない。

### 4. Actionデータフロー

```text
Device / Simulator
  -> ApcMiniMk2InputAdapter
  -> MidiBindingManager.Receive
  -> MidiBinding.Action.Execute
  -> Param / group state
  -> MidiActionStateRepository.Set (persistent actions only)
  -> Prefs memory cache

Prefs Save button / Input Action
  -> Prefs.Save
  -> Prefs.json
```

```text
MidiInputManager.Update
  -> floor(IBPMManager.Beat) + Time.frameCount
  -> MidiBindingManager.UpdateActions
  -> distinct IMidiActionRuntime.Update
  -> Momentary reset / Sequence current step refresh
```

### 5. Radio

- `RadioMidiAction`が選択値、選択肢数、Prefs、Param購読を所有する。
- 各Bindingにはindexを保持する内部`RadioOptionMidiAction`を登録する。
- Option Actionは押下時だけ親の`Select(index)`を呼ぶ。
- 保存値・外部Param値が範囲外なら0へ正規化する。
- Option ActionのLEDは選択中なら設定色、未選択なら25%色を返す。

### 6. Sequence

- `SequenceMidiAction`が`bool[]`パターン、現在beat index、Prefsを所有する。
- 外部公開は`CurrentStepIndex`と`IsCurrentStepEnabled`のread-onlyプロパティだけとする。
- 各Bindingにはindexを保持する内部`SequenceStepMidiAction`を登録する。
- Step Actionは押下時だけ親の`ToggleStep(index)`を呼ぶ。
- Runtime更新では`BeatIndex % stepCount`を計算し、同じ位置なら何もしない。
- LED優先順位は現在位置、Enabled、Disabledとする。現在位置はステップの有効・無効より常に優先する。
- `SequenceLedPalette`の既定はCurrent=`Color.yellow`、Enabled=Binding設定色、Disabled=Binding設定色の25%とする。

### 7. APC mini mk2 Faderモード

`ApcMiniMk2FaderModeController`と`ApcMiniMk2FaderModeRepository`を新設する。

```csharp
public enum FaderModeAssignment
{
    Mute = 0,
    Random = 1
}

public float GetRawValue(int index);
public float GetValue(int index); // effective value
public FaderModeAssignment GetAssignment(int index); // 0..7 only
public bool IsAssignedModeActive(int index);          // 0..7 only
public void SetAssignment(int index, FaderModeAssignment assignment);
public void ToggleAssignedMode(int index);
public void SetRawValue(int index, float value);
public void SetBeat(long beatIndex);
```

所有と依存方向は次のとおりとする。

```text
MidiInputManager
  owns ApcMiniMk2InputAdapter
  owns ApcMiniMk2FaderModeController
  reads IBPMManager

ApcMiniMk2InputAdapter
  routes Pad / Shift / Page to BindingManager
  routes Track 0..7 to FaderModeController
  routes Fader CC raw values to FaderModeController

ApcMiniMk2FaderModeController
  owns raw[9]
  owns assignment[8]
  owns active[8]
  calculates effective values
  asks InputAdapter/BindingManager bridge to publish effective CC changes
```

- index 0〜7だけがAssignmentとActiveを持つ。
- index 8は`GetValue(8) == GetRawValue(8)`を常に保証する。
- Assignment変更中にActiveなら、Activeを維持したまま新しい割り当ての実効値を即時発行する。
- Muteは0を返す。
- Randomは`Pcg.Hash2D(new UInt2((uint)beatIndex, (uint)index)).X`の最下位bitから0／1を決める。
- 物理入力、モード切替、Assignment変更、beat変更、ページ変更で実効値が変わり得る場合は、現在ページのFader Bindingへ実効CCを発行する。
- 同じ実効値の再発行は許容するが、`Param.SetValue`が同値通知を抑止する現在の契約を維持する。
- `GetFaderValue`は実効値へ意味を変更し、シミュレーターのSlider位置用に`GetRawFaderValue`を追加する。

### 8. Fader Prefs

| State | Key | Type | Default |
| --- | --- | --- | --- |
| Assignment | `Midi.ApcMiniMk2.Fader.{index}.Assignment` | `PrefsInt` | 全index=`Mute` |
| Active | `Midi.ApcMiniMk2.Fader.{index}.Active` | `PrefsBool` | `false` |

- キーを作るのはindex 0〜7だけとする。
- 不正なAssignment整数はindex既定値へ戻す。
- Master用キーは作らない。
- 変更時はPrefsキャッシュへSetするが、`Prefs.Save`は呼ばない。
- Raw Fader値は物理状態であり、永続化しない。

### 9. ShiftのグローバルControl

- `ApcMiniMk2.GlobalPage = -1`を追加する。
- ShiftのNote On／Offは常に`new MidiControl(Note, GlobalPage, ShiftButton)`としてBindingManagerへ渡す。
- PadとFaderのページ付きControlは変更しない。
- Learn UIの表示は`Shift (global)`とする。
- RepositoryにはChannel=`-1`として保存し、既存のControl比較をそのまま利用する。
- SimulatorはGlobal Shift Bindingの`ResolveLedColor`を表示するが、OutputAdapterはShiftへMIDI LEDメッセージを送らない。

### 10. LEDデータフロー

```text
MidiInputManager.UpdateDesiredLeds
  RGB Pad -> binding.Action.ResolveLedColor(binding.LedColor)
  Track 0..7 -> faderController.IsAssignedModeActive(index)
  Page 0..7 -> current page (existing behavior)

Simulator
  RGB Pad -> same resolved color
  Track active + Mute -> red
  Track active + Random -> pink (#F38DB2)
  Track normal -> existing off color
  Shift -> global binding resolved color
```

実機TrackはAssignmentに関係なくActiveなら赤ONを送る。RGB Padの15Hz差分送信、最大32 Pad/flushは維持する。

## Design Decisions

### 採用

- Action構造はC#登録時固定とする。型と出力Paramの不整合をランタイム設定で発生させないため。
- Action状態はPrefsへ随時SetするがSaveしない。既存の明示保存操作を唯一のコミット境界にするため。
- Group ActionはManagerが一括所有・解除する。複数Bindingの一部だけが残る状態を防ぐため。
- RuntimeはBindingとは別に重複排除して更新する。Sequenceの全Stepから同じRuntimeが参照されるため。
- FaderモードはデバイスAdapter層に置く。Actionの`random`と混在させず、物理Control固有の責務に限定するため。
- Fader APIとBinding配送の両方に実効値を使用する。読み取り経路による結果の不一致を防ぐため。
- ShiftはChannel/Page=`-1`でグローバル性を表す。既存`MidiControl`比較・保存形式を壊さず、ページ0とも区別できるため。
- Master Faderはモード配列から完全に除外する。利用できない設定をUIやPrefsに残さないため。

### 採用しない

- `random`を汎用MidiActionとして残す案: ユーザー合意により廃止。
- ShiftをMasterモードボタンにする案: Shiftを汎用グローバルBindingに使うため不採用。
- RGB PadをMasterモード用に予約する案: Masterは常時normalと決定したため不要。
- Action種別をUI保存する案: 出力型・グループ構造・利用側契約が動的になり複雑化するため不採用。
- Action変更ごとの`Prefs.Save`: 明示保存だけを保存境界とする合意に反するため不採用。
- Sequenceパターン配列の公開: 利用側から不変条件を壊せるため不採用。
- Momentary LEDの時間延長: 値とLEDの状態期間を一致させる合意に反するため不採用。

## Files to Change

### New files（提案パス）

| Path | Responsibility |
| --- | --- |
| `Assets/Krab/Common/Scripts/Midi/Actions/MidiActionUpdateContext.cs` | Frame／beat更新値と`IMidiActionRuntime`契約 |
| `Assets/Krab/Common/Scripts/Midi/Actions/OneShotMidiAction.cs` | 押下中だけONの一時Action |
| `Assets/Krab/Common/Scripts/Midi/Actions/MomentaryMidiAction.cs` | 1フレームpulse Action |
| `Assets/Krab/Common/Scripts/Midi/Actions/StateMidiAction.cs` | 複数状態巡回と状態色 |
| `Assets/Krab/Common/Scripts/Midi/Actions/RadioMidiAction.cs` | 排他選択グループとOption Action |
| `Assets/Krab/Common/Scripts/Midi/Actions/SequenceMidiAction.cs` | beat同期Stepグループ、pattern、LED palette |
| `Assets/Krab/Common/Scripts/Midi/Actions/SequenceLedPalette.cs` | SequenceのC#固定LED設定 |
| `Assets/Krab/Common/Scripts/Midi/Actions/MidiActionStateRepository.cs` | Action状態のPrefs key、load、normalize、set |
| `Assets/Krab/Common/Scripts/Midi/Devices/ApcMiniMk2/FaderModeAssignment.cs` | `Mute`／`Random` enum |
| `Assets/Krab/Common/Scripts/Midi/Devices/ApcMiniMk2/ApcMiniMk2FaderModeController.cs` | raw／effective値、mode、beat更新 |
| `Assets/Krab/Common/Scripts/Midi/Devices/ApcMiniMk2/ApcMiniMk2FaderModeRepository.cs` | Fader 0〜7のPrefs連携 |

各新規`.cs`にはUnityが生成する`.meta`を含める。GUIDを既存ファイルから複製してはいけない。

### Modified files

| Path | Change | Must not change |
| --- | --- | --- |
| `Assets/Krab/Common/Scripts/Midi/Actions/IMidiAction.cs` | LED解決とRuntime参照を追加 | `Execute(MidiMessage)`によるイベント入力契約 |
| `Assets/Krab/Common/Scripts/Midi/Actions/ToggleMidiAction.cs` | LED、Prefs同期、購読解除に対応 | 押下時だけ反転する挙動 |
| `Assets/Krab/Common/Scripts/Midi/Actions/FloatMidiAction.cs` | 新Action契約へ適合 | CC値を0〜1へClampする挙動 |
| `Assets/Krab/Common/Scripts/Midi/Bindings/MidiBinding.cs` | ActionからLED色を解決する構造へ変更 | Controlと安定Binding IDの保持 |
| `Assets/Krab/Common/Scripts/Midi/Bindings/MidiBindingManager.cs` | 型付き登録、Group所有、Runtime更新、一括解除 | 物理Controlの1対1制約、LearnのNote Off無視 |
| `Assets/Krab/Common/Scripts/Midi/Bindings/MidiBindingRepository.cs` | 必要ならGlobalPage=-1表示／読込を検証 | 既存キー名と保存済みBindingの互換性 |
| `Assets/Krab/Common/Scripts/Midi/Devices/ApcMiniMk2/ApcMiniMk2.cs` | `GlobalPage`、Fader／Track count定数追加 | 既存Note／CC番号 |
| `Assets/Krab/Common/Scripts/Midi/Devices/ApcMiniMk2/ApcMiniMk2InputAdapter.cs` | Track専用routing、Shift global、effective CC配送 | Pad押下ページをNote Offまで保持する挙動 |
| `Assets/Krab/Common/Scripts/Midi/Devices/ApcMiniMk2/IApcMiniMk2Surface.cs` | raw/effective Fader、assignment、mode、Shift色API | Adapterの共有入口 |
| `Assets/Krab/Common/Scripts/Midi/Devices/ApcMiniMk2/ApcMiniMk2OutputAdapter.cs` | Track Activeを赤ON/OFFで維持。必要最小限の整理 | TrackへRGB/SysExを送らないこと |
| `Assets/Krab/Common/Scripts/Midi/Devices/ApcMiniMk2/ApcMiniMk2Simulator.cs` | Assignment UI、raw slider、effective表示、mode色、global Shift色 | UI入力を実機と同じAdapterへ通すこと |
| `Assets/Krab/Common/Scripts/Midi/Input/MidiInputManager.cs` | `IBPMManager`注入、Action/Fader更新、LED解決 | Input System購読と15Hz差分送信 |
| `Assets/Krab/Common/Scripts/Midi/Views/MidiBindingUi.cs` | Action種別表示、Shift global表示、Group項目との互換 | Learn／Cancel／Clear／明示Save |
| `Assets/Krab/Common/Scripts/Sample/ParamSampleComponent.cs` | 全Actionの登録・操作・表示サンプル追加 | 既存Toggle／Floatサンプルの回帰確認用途 |
| `Assets/Krab/Docs/Html/midi.html` | 新Action、保存、Fader、Shift、ownership、edge caseを反映 | 現在実装と将来案を混同しないこと |

Sceneに新しいMonoBehaviourを追加しない設計とし、`Assets/Krab/Scenes/2612_shirt-meets-lausbub.unity`は原則変更しない。既存ComponentのSerialize field追加によりUnityがSceneを変更した場合は差分を確認し、不要なScene再保存を避ける。

## Interfaces / Data Structures

### Stable IDs

- `actionId`はPrefs状態とGroup所有のIDであり、リネームすると旧状態を引き継がない。
- `bindingId`はMIDI Control割り当てとLED基本色のIDである。
- Group内IDはManagerが規則から生成し、呼び出し側が別形式を組み立てない。
- 同一Manager内で`actionId`と`bindingId`は重複登録不可とし、登録時に`InvalidOperationException`を投げる。

### Validation

- null Param／空actionId: `ArgumentNullException`または`ArgumentException`。
- `stateCount < 2`: `ArgumentOutOfRangeException`。
- `optionCount < 1`、`stepCount < 1`: `ArgumentOutOfRangeException`。
- stateColorsを指定した場合に`Count != stateCount`: `ArgumentException`。
- Fader indexが0〜8外: `ArgumentOutOfRangeException`。
- Assignment APIへindex 8を渡す: `ArgumentOutOfRangeException`。Masterにモードがあるように見せない。
- 壊れたPrefs値は例外にせず既定値へ正規化し、Prefsキャッシュも正規化後の値へ更新する。

### Fader effective output

```text
index == 8                     -> raw[index]
index 0..7 && active == false  -> raw[index]
assignment == Mute             -> 0
assignment == Random           -> deterministicBit(beatIndex, index)
```

### Persistence boundary

全Repositoryの`Set`はメモリキャッシュ更新である。保存ファイルへ反映されたとみなしてはいけない。永続化完了条件は外部から`Prefs.Save`が呼ばれたこととする。

## Implementation Steps

### Step 1: Core Action runtimeとLED契約

対象:

- `IMidiAction.cs`
- 新規`MidiActionUpdateContext.cs`
- `MidiBinding.cs`
- `MidiBindingManager.cs`

作業:

1. `ResolveLedColor`、`Runtime`、`IMidiActionRuntime`、更新Contextを追加する。
2. ManagerへRuntime参照カウント／重複排除Collectionを追加する。
3. `UpdateActions(frameCount, beatIndex)`を追加する。
4. LED側が`IsActive`ではなくActionの最終色を読むようにする。
5. 既存Toggle／Floatを新契約へ適合させる。

完了条件:

- 既存Toggle／Floatの入力挙動が変わらない。
- 同じRuntimeを複数Bindingが参照しても1更新につき1回だけ呼ばれる。
- 登録解除後にRuntimeが更新されない。

検証:

- Unity compile。
- Sampleの既存Toggle／Float LearnとLEDを手動確認。

### Step 2: Action state Repositoryと単一Binding Action

対象:

- 新規`MidiActionStateRepository.cs`
- `ToggleMidiAction.cs`
- 新規`OneShotMidiAction.cs`
- 新規`MomentaryMidiAction.cs`
- 新規`StateMidiAction.cs`
- `MidiBindingManager.cs`

作業:

1. Prefsキー生成、load、set、正規化をRepositoryへ実装する。
2. Toggleを永続状態へ接続する。
3. OneShot、Momentary、Stateを実装する。
4. Managerへ型付き登録と`UnregisterAction`を追加する。
5. Param変更購読をAction所有にし、解除時に必ずunsubscribeする。

完了条件:

- 押下／解放／次フレーム／状態巡回がIssueどおり。
- OneShot／Momentary用Prefsキーを作らない。
- `Prefs.Save`をAction／Repositoryから呼ばない。

検証:

- Simulatorで各Actionを操作。
- Save前後でPlay Modeを入り直し、保存対象と非対象を確認。

### Step 3: RadioとSequenceグループ

対象:

- 新規`RadioMidiAction.cs`
- 新規`SequenceMidiAction.cs`
- 新規`SequenceLedPalette.cs`
- `MidiBindingManager.cs`
- `MidiBindingUi.cs`

作業:

1. Manager生成規則でOption／Step Bindingを登録する。
2. Radioの常時1選択、範囲正規化、LEDを実装する。
3. Sequenceの内部pattern、Prefs文字列、beat位置、LED優先順位を実装する。
4. GroupをactionId単位で一括解除できるようにする。
5. Binding UIが生成されたGroup IDを通常Bindingと同様にLearn／Clearできることを保証する。

完了条件:

- Groupの各項目が個別Learn可能。
- Controlの1対1制約がGroup内外で維持される。
- Sequence pattern配列がpublic APIから取得・設定できない。
- `CurrentStepIndex`と`IsCurrentStepEnabled`だけがread-only公開される。

検証:

- Radioの全Option選択、再選択、保存復元。
- Sequence全OFF／全ON／交互pattern、beat巡回、末尾から先頭、保存復元。

### Step 4: BPM／Frame更新の接続

対象:

- `MidiInputManager.cs`
- `MidiBindingManager.cs`

作業:

1. `IBPMManager`を`MidiInputManager`へ注入する。
2. 毎Updateで`Time.frameCount`と`floor(Beat)`をManagerへ渡す。
3. OnEnable／Inject／OnDisable／OnDestroyの順序に依存せずBPM購読を重複させない。
4. Beatが1以上飛んでもSequenceが現在beatから直接位置を求めるようにする。

完了条件:

- Momentaryが押下フレーム中だけONになる。
- SequenceがBPM変更後も整数beat境界で進む。
- Component破棄後にイベントcallbackが残らない。

検証:

- Play ModeでPause／Stepを使いMomentaryをフレーム単位確認。
- BPMを変更しSequence位置と`IsCurrentStepEnabled`を確認。

### Step 5: Fader mode ControllerとPrefs

対象:

- 新規`FaderModeAssignment.cs`
- 新規`ApcMiniMk2FaderModeController.cs`
- 新規`ApcMiniMk2FaderModeRepository.cs`
- `ApcMiniMk2InputAdapter.cs`
- `ApcMiniMk2.cs`

作業:

1. raw[9]、assignment[8]、active[8]、beatをControllerへ実装する。
2. Fader 0〜7の既定AssignmentをすべてMuteにする。
3. `GetFaderValue`を実効値、`GetRawFaderValue`を物理値として分離する。
4. 実効値変更を現在ページのBindingへCCとして発行するbridgeを実装する。
5. index 8を全mode APIから除外する。
6. Prefs Setは行うがSaveは呼ばない。

完了条件:

- 各modeの値、切替復帰、mid-beat random、同beat再現性が仕様どおり。
- Mute／Random中の物理操作がrawだけを更新し、normal復帰時に最新rawが出る。
- Masterは常にrawを返す。

検証:

- 0、0.5、1のraw値で各modeを確認。
- 同一beat／indexでPlay再実行後もrandom値が一致することを確認。
- Save前とSave後のAssignment／Active復元を確認。

### Step 6: Track専用routingとGlobal Shift

対象:

- `ApcMiniMk2.cs`
- `ApcMiniMk2InputAdapter.cs`
- `MidiBindingUi.cs`

作業:

1. Track Note 100〜107をページ付きBindingへ送らずControllerへ送る。
2. TrackはNote Onだけでtoggleし、Note Offを無視する。
3. Shiftを`GlobalPage=-1`でNote On／Off配送する。
4. Learn表示を`Shift (global)`へ変更する。
5. ページ変更時に新しいページのFader Bindingへ全9本の実効値を同期する。

完了条件:

- TrackをLearnしようとしても通常Bindingにならない。
- Shiftをどのページで押しても同じControlとして学習・実行する。
- Shift押下後にページ変更してから解放しても同じglobal ControlへNote Offが届く。

検証:

- Page 0とPage 7で同じShift Bindingを操作。
- Track押下でFader modeだけが変わることを確認。
- ページごとのFloat Bindingがページ切替時に実効値へ同期されることを確認。

### Step 7: 実機LED、Surface、Simulator、設定UI

対象:

- `IApcMiniMk2Surface.cs`
- `MidiInputManager.cs`
- `ApcMiniMk2OutputAdapter.cs`
- `ApcMiniMk2Simulator.cs`

作業:

1. Surfaceへraw/effective値、Assignment、Active、設定変更APIを追加する。
2. 実機Track LEDをActive boolだけで赤ON/OFFする。
3. SimulatorのTrack色をMute=red、Random=`#F38DB2`、Normal=offにする。
4. Simulator Sliderはraw値を操作し、別Labelでeffective値とmodeを表示する。
5. Fader 0〜7にAssignment選択UIを追加する。
6. MasterはAssignment入力を出さず`normal (fixed)`と表示する。
7. Shiftはglobal Bindingの解決済みLED色をSimulatorだけに表示する。

完了条件:

- 実機とSimulatorの入力が同じController経路を使う。
- 実機Trackは両Assignmentで赤、Simulatorだけ色分けする。
- Masterに誤操作可能なmode UIが存在しない。

検証:

- Simulator全8 Trackと9 Faderを操作。
- 実機接続時にTrack LED、Pad LED、Page LEDの回帰確認。

### Step 8: Sampleを全Action対応にする

対象:

- `ParamSampleComponent.cs`

作業:

1. Toggle／Floatの登録を型付きAPIへ移行する。
2. OneShot、Momentary、State、Radio、Sequenceの小さなサンプルを追加する。
3. Radio／Sequence各BindingのMIDIランチャーを表示する。
4. Sequenceは現在step indexと現在step enabledだけを表示し、pattern編集UIはMIDIボタン以外に作らない。
5. OnDestroyでactionId単位に登録解除する。

完了条件:

- 1つのScene／Simulatorで全受け入れ条件を観測できる。
- Sample固有コードがAction内部状態へ直接アクセスしない。

検証:

- 全Actionの手動チェックリストを一巡する。

### Step 9: 技術資料を現在実装へ同期する

対象:

- `Assets/Krab/Docs/Html/midi.html`
- 必要な場合のみ`Assets/Krab/Docs/Html/retro-brutal.css`

作業:

1. Action表、登録API、状態遷移、Group ID、Prefs keysを更新する。
2. Fader raw/effectiveフロー、Track、Master、Shiftの役割を図示する。
3. 明示Saveだけが永続化境界であることを記載する。
4. ownership、破棄、edge case、Scene接続を更新する。
5. 未実装の将来案としてではなく、実装完了後の実コードを再確認して記述する。

完了条件:

- HTML内の型名、パス、シグネチャ、Prefs keyがコードと一致する。
- 相対リンク切れがない。

検証:

- ブラウザでdesktop／narrow幅を確認。
- `rg`で旧説明（Toggle／Floatのみ、Trackが通常Binding等）が残っていないことを確認。

### Step 10: 最終回帰確認

対象:

- 全変更ファイル
- `Assets/Krab/Scenes/2612_shirt-meets-lausbub.unity`

作業:

1. 差分を確認し、無関係なScene／Prefab／meta変更を除外する。
2. Unity compile、Console、Play Modeを確認する。
3. Save前／Save後の再起動相当確認を行う。
4. 実機が利用可能ならAPC mini mk2でLEDと入力を確認する。利用不可ならSimulator確認済み・実機未確認を明記する。

完了条件:

- Issue Acceptance Criteriaに対応する確認結果が全て記録される。
- 既存のステージ済みユーザー変更を欠落・巻き戻ししていない。

## Edge Cases

- Note Onの重複受信: Toggle／State／Radio／Sequenceは受信ごとに1操作とする。デバイスが重複送信する場合のdebounceは本スコープ外。
- Note Off欠落: OneShotはONのままになり得る。デバイス切断時に全一時ActionをOFFへ戻す`ReleaseTransientActions`をManagerへ追加し、`MidiInputManager.OnDisable`とデバイス切断時に呼ぶ。
- Momentaryを同一フレームで複数回押す: ONを維持し、最後の押下フレームの次フレームでOFF。
- ページを押下中に変更: Padは既存どおり押下開始ページへNote Offを返す。Shiftは常にglobal。
- Radio保存値が範囲外: 0へ補正。
- State保存値が範囲外: 0へ補正。
- Sequence stepCount変更: 先頭から共通長を維持し、追加分OFF、超過分破棄。
- beatが複数進む: SequenceとRandomはイベント回数ではなく現在の整数beatから直接算出。
- beatが同じ値へ戻る: 同じbeat／indexから同じRandom値を再現し、Sequenceも対応位置へ戻る。
- Assignment変更中にmode Active: Activeを維持し、新Assignmentの出力とSimulator色へ即時変更。
- Mute／Random中のFader操作: rawを更新し、effectiveはmode値のまま。
- MasterへAssignment API: 例外。黙って無視しない。
- Shift Bindingの旧保存値: 現行コードはShiftをページ付きで保存し得るため、自動移行は行わない。新しいglobal Shiftは再Learnが必要。旧キーを推測で移行しない。
- 未割当てBinding: 入力を無視し、LEDは消灯／Simulator fallback色。
- Prefs Save未実行終了: 変更消失を仕様どおり許容。
- Prefs JSON破損: PrefsGUI既存のparse error処理に従い、Action側は受け取った値の範囲正規化だけを担当。
- デバイス切断／再接続: transient Actionを解除し、再接続時に全LED refresh。永続ActionとFader modeはメモリ状態を保持。

## Verification

### Planned automated/static checks

```sh
git diff --check
uloop clear-console --add-confirmation-message
uloop compile --timeout-seconds 600
uloop get-logs --log-type Error --include-stack-trace --max-count 100
```

現リポジトリにはテストasmdefがなく、`Assembly-CSharp`のMIDI型を直接参照するEditModeテストを追加するにはAssembly再編が必要になる。本Issueではasmdef導入を行わず、Sample＋SimulatorによるPlay Mode確認を必須とする。テスト基盤導入は別Issueとする。

### Planned Play Mode checks

```sh
uloop control-play-mode --action Play --timeout-seconds 180
uloop control-play-mode --action Stop --timeout-seconds 180
```

Play中は次を手動確認する。

1. Toggle: 2回押してON／OFF、解放で変化なし。
2. Radio: 全Optionを選択し、常に1つだけactive。
3. OneShot: 押下中ON、解放OFF。
4. Momentary: Pause／Stepを併用し1フレームだけON。
5. State: 全状態を巡回し末尾から0へ戻る。個別色とfallback色を確認。
6. Sequence: pattern toggle、beat進行、wrap、現在位置優先色を確認。
7. Fader Normal: rawとeffectiveが一致。
8. Fader Random: mid-beat有効化、beat更新、0/1、物理入力無視、normal復帰。
9. Fader Mute: effective 0、物理入力無視、normal復帰。
10. Master: 常時raw、設定UIなし。
11. Track: Pageを変更しても同じFader modeを操作。
12. Shift: 複数Pageで同じBindingを実行し、Simulator色だけ変化。
13. Save: 状態を変えてSaveせず再Playし旧保存値、明示Save後は新しい値を復元。
14. MIDI Learn: Pad、Shift、Fader CCをLearn／Clearし、Control重複時は旧所有者解除。
15. LED: 実機Track赤ON/OFF、Simulator Randomピンク／Mute赤、Pad複数状態色、Page緑を確認。

### Verification status

- 実施済み: Unity 6000.3.12f1のRoslyn応答ファイルを使用した`Assembly-CSharp`コンパイル（エラー・警告なし）、`git diff --check`。
- 未実施: Play Mode操作確認、APC mini mk2実機でのLED・入力確認。Unity CLI Loopのproject runner pinが未生成のため、上記のPlay Mode手順は利用できなかった。

## Risks

### Prefs key互換

Action ID変更で状態が失われる。登録IDを定数化し、表示名から生成しない。既存Bindingキーは変更しない。

### ParamとPrefsの循環更新

Prefs load → Param.Set → Changed → Prefs Setが循環し得る。初期load後に購読するか、同値抑止を使い、購読解除を所有Actionに閉じる。

### Unity Update順序

BPMManagerとMidiInputManagerのUpdate順でbeat反映が1フレームずれる可能性がある。ActionとFaderは毎フレーム`IBPMManager.Beat`の現在値を読み、イベント購読の順序には依存しない。必要ならScript Execution Orderを後続で固定する。

### Group解除漏れ

Step／Option単位の解除だけではRuntimeや購読が残る。公開利用側は`UnregisterAction(actionId)`を使い、Managerが参照カウントと全子Bindingを所有する。

### LED帯域

State／Sequenceで色変化が増えるが、既存15Hz／32 Pad制限を維持する。Momentaryの1フレーム色が実機で見えないことは合意済み仕様。

### 実機未接続

Simulatorだけでは単色Track LEDとShift LEDなしを完全検証できない。実機未確認の場合は完了報告で明示し、後続確認項目として残す。

### Rollback

変更はAction契約、Fader Controller、UIの単位でコミットを分ける。問題時は対象コミットをrevertし、既存Toggle／Floatのみの経路へ戻せるようにする。Prefsの新規キーは旧実装から参照されないため、残っても既存動作を壊さない。既存Bindingキーの削除・移行は行わない。

## Assumptions

- Beatは`IBPMManager.Beat`を唯一の時間源とする。
- Beatは通常0以上だが、計算は負値を受けてもindexが0以上になる正規化modを使う。
- Channel Faderはindex 0〜7、Masterはindex 8とする。
- Track 0〜7はFader 0〜7へ固定対応する。
- Fader 0〜7の既定Assignmentはすべて`Mute`とする。
- UI上のピンクは`#F38DB2`を使用する。
- Action状態、Fader Assignment、Fader ActiveはPrefsキャッシュへ即時反映するが、自動Saveしない。
- Action種別、state count、state colors、sequence paletteはC#登録で固定する。Radio optionとSequence step countは後続合意により動的変更を許可する。
- Radio／Sequenceの各項目は個別にLearnする。
- Sequence patternは外部から読み書き不要である。
- Shiftは1つのglobal Controlであり、ページ別の複数割り当てを持たない。
- Masterのmode要求と元の9要素default表は、今回の合意で「Masterは常時normal」に置き換えられた。

## Open Questions

なし。実装に必要なプロダクト判断とアーキテクチャ判断は本計画内で確定済み。

## Follow-up Implementation: Action表示とSample variation

1. `IMidiAction`へ機械判定用の`MidiActionKind`とUI表示用の安定したAction名を追加する。具象型名の文字列加工には依存せず、各Actionが自身の意味を返す。
2. `radio`／`sequence`の子Actionは`Radio (option 1/4)`、`Sequence (step 1/8)`のように位置情報を返す。
3. `MidiBindingUi`の小ウインドウへ`Action:`行を追加する。未割当てでもC#登録済みActionは表示する。
4. `ParamSampleComponent`へ次の利用例を追加する。
   - state + 名前付き状態表示（dropdownは使用しない）
   - radio + 追加・削除可能なPrefs list item selection
   - normalized float + application range変換
5. 既存Action例、保存境界、sequence外部API制限は変更しない。
6. Unity Roslyn compileと`git diff --check`を実施する。Play ModeではAction表示と双方向UI同期を確認する。
7. Sampleの内容全体を高さ600pxの縦ScrollViewへ入れる。Toggleと直接0〜1 Floatは各1件に集約し、変換方法が異なるState、Radio、mapped Float、および複数Binding自体が仕様であるSequenceは残す。
8. 動的Radio optionには永続IDを持たせ、`{actionId}.Option.{itemId}`をBinding IDとする。追加・削除・並び替え時はManagerが子Bindingを再調整し、選択中IDを新しいindexへ変換する。削除したBindingのPrefs値は消さない。
9. SampleのRadioを丸い選択記号付きFold、Sequenceを横1列のステップFoldにして、グループの意味を視覚化する。
10. `SetSequenceStepCount`で1〜16ステップへ変更可能にする。縮小時はpatternを切り捨て、拡張分はOFFで初期化する。子Bindingは実行中だけ再調整し、削除stepのPrefs mappingは消さない。Sampleのstep countもPrefsキャッシュへ反映し、明示Save時だけ永続化する。
11. `SequenceMidiAction`へstep単位の参照・toggle APIを追加する。Sampleのstep番号ボタンでpatternを編集し、暗色／シアン／黄色の3状態をボタン背景で示す。pattern配列全体は公開しない。
