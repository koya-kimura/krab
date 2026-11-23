# ImageLayer：Layoutプリセットと画像のラジオ選択

2026-11-23。演奏中に「どう並べるか（Layout）」と「何を並べるか（Image）」を別々のラジオで選ぶ。VJでは、MIDIのパッド列の1つをLayout、もう1つをImageに割り当て、配置を保ったまま素材だけを差し替えたり、素材を保ったまま配置だけを変えたりする。

これまでの「18パターンそれぞれにON/OFF・Set・Look・パラメータ」（パターンごとに1つだけ、Prefsに保存）は、この仕組みに置き換える。

## 用語

| 名前 | 中身 | 保存先 |
|---|---|---|
| Layoutプリセット | ID、名前、配置の種類（18パターンのどれか）、パラメータの値 | `StreamingAssets/Krab/ImageLayerPresets/Layouts.json` |
| Imageエントリ | ID、名前、Set、Clip（全Clip／番号）、Look、Frames Per Beat | `StreamingAssets/Krab/ImageLayerPresets/Images/<ライブラリ名>.json` |
| Deck | Layoutラジオ1つ＋Imageラジオ1つ。A・Bの2つ。BがAの手前 | 選択中のIDはPrefs |

- 配置の種類（March、Tunnelなど）は今までどおりコードで書く。同じ種類から、パラメータの違うプリセットをいくつでも作れる。
- Layoutは画像を持たない。Look（Original／Duotone／Threshold）とFrames Per Beatは素材ごとに変えたいので、Image側に置く。
- パターンの「Clip」パラメータは、ImageエントリのClip一覧の中での番号になる。エントリが特定のClipに絞られていれば、どの番号でもそのClipになる。
- プリセットは公演の素材として扱い、Gitで管理する。`StreamingAssets/Krab/ImageLayer/` はライブラリの置き場（フォルダ＝ライブラリ）なので、その外に置く。
- Imageエントリはライブラリごと。ファイルがなければ、読み込んだライブラリのSetごとに1エントリ（全Clip、Original）を作る。

## Deckとラジオ

```text
Deck A  Layout: [Off] [March] [Lineup] [Tunnel] …    Image: [animal] [shirt] [shirt 03] …
Deck B  Layout: [Off] [Center] …                      Image: [walk] …
描画 = Deck A（Layout × Image） → Deck B（Layout × Image）
```

- Layoutラジオの先頭は「Off」（そのDeckを描かない）。
- 押した瞬間に切り替える。拍の位置（BPMの拍数）はDeckをまたいで共通なので、Imageだけを替えても動きの位相は続く。
- MIDIは `MidiBindingManager.RegisterRadio` を使う。選択肢のキーはプリセットIDとエントリIDなので、並べ替え・名前の変更・追加・削除でも割り当ては外れない。選択肢の増減は `SetRadioOptions` で反映する。LEDは選択中だけ明るい（`RadioMidiAction`）。
- アクションIDは `ImageLayer.Deck A.Layout` などで、選択肢のバインドIDは `ImageLayer.Deck A.Layout.Option.<ID>`。
- MIDIの部品（`IMidiBindingProvider` / `IMidiBindingUi`）は任意。無いSceneでもUIのラジオで操作できる（`[Nullable]` で注入）。
- 選択中のIDはPrefs（`ImageLayer Deck A Layout` など）に保存する。プリセットが消えていた場合は先頭（Layoutは「Off」）に戻す。

## UI（設定UIの「ImageLayer」→ Performance → Image Layer）

| タブ | 内容 |
|---|---|
| Decks | Deck A・Bそれぞれに、Layoutの選択肢とImageの選択肢を縦に並べる。各行は選択ボタンとMIDIボタン |
| Layouts | プリセット一覧。追加（種類を選んでAdd）、Edit（名前とパラメータ）、Duplicate、Delete、上下移動。Save／Revert |
| Images | エントリ一覧。追加、Edit（名前、Set、Clip、Look、Frames Per Beat）、Duplicate、Delete、上下移動、Setごとのエントリを補う。Save／Revert |
| Library / Display | 今までどおり |

- 編集は即座に描画へ反映する。ファイルへの保存はSaveボタンで行い、Revertで最後に保存した内容へ戻す。Prefsの保存（Cmd+S）とは別。
- 行は、プリセットの増減・並べ替え・名前の変更のときだけ作り直す（行の中にMIDI割り当てのウインドウがあるため）。

## 構成

| 場所 | 型 | 役割 |
|---|---|---|
| Core（`Krab.ImageLayer.Core.Presets`、Unity非依存） | `PatternParameter`、`ParameterValues` | パラメータの定義（種類・既定値・範囲）と値。範囲外は丸める |
| | `LayoutPreset`、`ImageEntry`、`PresetNaming` | プリセットとエントリ、IDと名前の重複回避 |
| Performance | `ImagePattern`（18種） | 状態を持たない描画。パラメータの定義を公開し、値を受け取って描く |
| | `ImageLayerPresetStore` | JSONの読み書き（Newtonsoft.Json） |
| | `ImageDeck` | Layout・Imageの選択とラジオのMIDI |
| | `ImageLayerPerformance` | 読み込み、Deckの描画、UI |

## 検証

- Core：パラメータの既定値・丸め、値の独立性、IDの重複回避をEditModeテストで確かめる。
- `ImageLayerPerformance.unity` にMIDIの部品（`MidiInputManager`、`MidiOutput`、`ApcMiniMk2Simulator`、`MidiBindingUi`）を加える。Playして、プリセットの保存・読み込み、ラジオの切り替え、Consoleを確認する。
- 見た目とMIDIの操作感はユーザーが確認する。

### 記録（2026-11-23）

- EditModeテスト：ImageLayerの42件すべて成功（新規のPresetTests 10件を含む）。
- Performance SceneをPlay：Layoutプリセット18件（パターンごとに1つ、既定値）とImageエントリ12件（Setごと）を作って保存した。Deck AをMarch×shirt、Deck BをTunnel×walkにして、Bが手前に重なることを出力（`Reports/ImageLayerDeckAB.png`）で確認。ラジオの選択肢（Offを含む）がMIDIのバインドとして登録されることを確認。Consoleのエラー・警告0件。
- 以前の各パターンの既定のSetとLook（例：March＝animal・Threshold）は引き継いでいない。Imageエントリはすべて Original・8 Frames Per Beat から始まる。
- 未確認：UIの操作感、実機のAPC mini mk2でのラジオとLED、見た目。
