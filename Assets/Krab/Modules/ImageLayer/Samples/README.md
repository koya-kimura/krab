# ImageLayer Samples

## ImageLayerPerformance.unity（演奏用）

開いてPlayする。`StreamingAssets/Krab/ImageLayer/Libraries/Sequences` を読み込み、Deck A・Bの画像を描く。その手前のCameraにTypeLayoutの文字を重ねる。設定UIの「ImageLayer」タブ → Performance → Image Layer から操作する（仕組みは `Assets/Krab/Modules/ImageLayer/Docs/LayoutPresets.md`）。

- Decks：Deck A・Bそれぞれで、Layout（先頭はOff）とImageをラジオで選ぶ。各行のMIDIボタンでパッドを割り当てる（割り当てと選択の保存はPrefs、Cmd+S）。BがAの手前。All Offで両方を消す。
- Layouts：Layoutプリセットの追加（種類を選んでAdd）、Edit（名前とパラメータ）、Copy、↑↓、Delete。Saveで `StreamingAssets/Krab/ImageLayer/Presets/Layouts.json` に保存、Revertで戻す。
- Images：Imageエントリ（Set、Clip、Look、Frames Per Beat）の追加・編集。Add Missing Setsで、エントリの無いSetを補う。Saveで `StreamingAssets/Krab/ImageLayer/Presets/Images/<ライブラリ名>.json` に保存。
- Library：ライブラリの選択、圧縮のON/OFF、Reload、読み込み状況、問題の一覧。
- Display：Duotone／Thresholdで使うDark／Lightの色、Thresholdの明度。

MIDIの部品（`MIDI` GameObject：MidiOutput、MidiInputManager、ApcMiniMk2Simulator、MidiBindingUi）はShapeSynthのSceneと同じ構成。APC mini mk2が無くても、設定UIのシミュレーターで操作できる。

| Camera | Order | 内容 |
|---|---|---|
| Main Camera（出力、Base） | — | 黒でクリアするだけ（背景はImageLayerの対象外） |
| Image Layer Camera（Overlay） | 0 | `ImageLayerPerformance` の画像 |
| Foreground Camera（Overlay） | 10 | `TypeLayoutPerformance` の文字 |

再構築：メニュー Krab > Image Layer > Create Performance Scene（既存のSceneを削除してから）。

## ImageLayerSmokeTest.unity

開いてPlayする。`StreamingAssets/Krab/ImageLayer/Libraries/Sequences` を読み込み（約21秒、中央にプログレスバー）、次の3層を重ねて描く。

| 層 | Camera | 内容 |
|---|---|---|
| 背景 | Main Camera（出力、Base） | 黄色の地とグリッド（p5.Unite） |
| 画像 | Image Layer Camera（Overlay、Order 0） | 上段：ペンギンの列（白黒の2値化、拍に合わせて右へ流れる）。中段：walkの4クリップ（元の色、足元を揃える）。下段：lifeの静止画（紺とピンクのDuotone、拍ごとに入れ替わる） |
| 前景 | Foreground Camera（Overlay、Order 10） | ピンクの円（p5.Unite） |

読み込み結果（セット数、フレーム数、メモリ、時間、問題の一覧）はConsoleに出る。`ImageLayerSmokeTest` の `Orientation Check` をONにすると、各層の左上に目印だけを描く（背景＝赤、画像＝緑、前景＝青）。3つが同じ左上に重なれば、層ごとの座標の向きが一致している。

再構築：メニュー Krab > Image Layer > Create Smoke Test Scene（既存のSceneを削除してから）。

## ImageLayerLayerOrder.unity

Phase 0の重ね順の検証。Playすると、Camera Stackと単一Cameraでの重なり順をConsoleに出す。
