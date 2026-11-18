# Phase 10: Preset Editor Workspace

## 現在のUI（2026-11-14、素のRosettaUI）

独自テーマ・3ペインの可変レイアウト・UI倍率・Zoom/Panなどを削除し、RosettaUI標準のWindow / Tabs / Fold / Field / Dropdown / Buttonだけで組み直した（PresetEditorのスクリプトは約3200行から約1300行）。説明文・ヒント・ステータスバーは置かない。表示するのは処理に失敗したときのエラー文だけ。

`Assets/Krab/ShapeSynth/Samples/ShapeSynthPresetEditor.unity` を開いてPlayし、共通Settingsウィンドウの **ShapeSynth → Authoring → Preset Editor** を押すとPreset Editorウィンドウが開く（Commonの他のUIと同じ扱い。自動では開かない）。編集中のPresetは常に背景の出力へ描画される。

- 上部: Preset名、New、Save、保存済みPresetのDropdownとOpen。未保存の確認はない（Saveボタンの `Save *` が未保存を示す）。
- 再生（タブの外、Visual / Values共通）: `Play` / `Stop`、`Loop`、BPM、Seed、Beatスライダー。各Valueを現在のBeatで評価して静止Hierarchyを背景へ描画し、Playでは最長ADSRの寿命＋最長Delay（上限8拍）でループする。編集は0.3秒後に自動でCompileする。
- Visualタブ
  - Hierarchy: Nodeをボタンで選択する（選択中は青）。種類は記号で示す（■ Group、○ Circle、◻ Rectangle、▲ Polygon）。`▼` / `►` でGroupを開閉する。Drag & Dropで並べ替え・入れ子を変更する（端は前後、Groupの中央は子）。ドラッグ中はドロップできる行を暗い緑、今ドロップする行を明るい緑、ドラッグ中のNodeを灰色で示す。ShapeのDropdownと `+ Shape` / `+ Group` / `Delete`。
  - Inspector（幅620）: Name、Transform / Shape / Layout / StyleのFold（GroupはName/Transformのみ）。Parameterは表形式の1行「名前 | Base | Rand | Depth | Unit | Now | Flip | Bind」（Rand/Flipは `Variation.md`）で、列見出しは各ブロックの先頭に1回。名前とBindプルダウンはValue接続中に緑。Depthは未接続なら灰色の「-」。Nowは接続中・画面基準の単位・Randありのときだけ、プレビューBeat時点の Base＋Depth×信号 の出力（px / deg）を出す。Rand / Flip / Delayで複製ごとに値が変わる場合は先頭に「~」を付ける（それらは反映しない基準値）。アイコンはRosettaUI既定のopen-in-new画像。
  - 単位: Position / Distance / Size / Spacing / Radius は `px / xW / xH / xMin / xMax`（画面基準は倍率。0.1 xW = 画面幅 × 0.1。保存値をそのまま表示・入力）、Rotation / Direction / Hue は `deg / turn`、Scaleは `x`、Opacity / Color Mixは `0-1` 固定。単位を変えるとBaseとDepthを換算し、出力を変えない。保存形式は変更せず、`PresetUnits` が既存のReference / Multiplier / AbsoluteValueへ対応付ける（画面基準→Canvas系Reference、turn→Absolute×Multiplier 360、AbsoluteValueは常に1）。Reference / Multiplier / Absolute ValueはUIに出さない。
  - Bind列: `None` と全Valueのプルダウン（RosettaUI Dropdown）。Noneで解除、Valueを選ぶとDepthは種類ごとの既定値（Position 100、Rotation 90deg/0.25turn、Hue 360deg/1turnなど）で接続。別のValueへ切り替えたときはDepthを維持する。Value追加・削除・名前変更で選択肢を作り直す。Bind用の別ウインドウは廃止。
  - 必要な項目だけを出す: Fill ON時のみColor / Color Mix / Hue Shift、Color MixのBaseが0以外かValue接続中のみTo Color、Stroke ON時のみStroke Width / Stroke Color、LayoutはKindに応じた項目のみ。
- Valuesタブ: Value一覧（選択中は青）と `+ Value`。Valueは複数のComponent（ADSR / LFO、Weight付き）の合成で、信号は `Σ Weight × Component`。出力グラフ（0–6 beat）には選択ValueのComponentを細線（ADSRは青、LFOは橙、Weight込み）、合計を白の太線で描き、Beat位置に縦線を引く。縦軸は±1を基本に、合計がはみ出すと自動で広げる。その下にName、各ComponentのFold（Weight、ADSR 5項目 / LFO 3項目、Remove Component）、`+ ADSR` / `+ LFO`、Remove Value（どのParameterにも接続されていないときだけ有効）。
- 保存形式はSchemaVersion 5（`Values[].Components[]`、Bindingは `ValueId`）。旧版からの移行はしない。CompilerはBound Valueを「Componentごとに Depth × Weight のBinding」へ展開するので、Runtime（`ParameterDefinition` の合算）は変更していない。
- Visual側でValueを新規作成する機能、Static / Animatedモード、Preview Zoom、UI倍率、右クリックメニュー、未保存確認、Open一覧ブラウザ、ステータスバーは削除した。
- 各ペインの先頭に Hierarchy / Inspector / Values / Output の見出しだけを置く。状態を示す色は `PresetEditorPalette` にまとめ、RosettaUIの `Style.Color` / `Style.BackgroundColor` だけで付ける。記号はRosettaUI既定フォントにあるもの（○ ◻ ▲ ■ ● ◆ ▼ ►）に限る。
- Play中にスクリプトが再読み込みされるとDocumentは消える（シリアライズしないため）。そのときは新規Documentに置き換えてエラー欄に表示する。保存していない編集は失われる。
- `Samples/Settings/WorkspacePanelSettings.asset` はScale With Screen Size（1600×900、Match 0.5）。このSceneだけが使う。
- RosettaUIのDropdownメニューはUnity 6000.3で削除された内部APIに依存するため、`Choice` で `createMenuCallback` を外してUnity標準のメニューを使う互換処理だけ残した。
- 検証: コンパイル成功、EditMode 206成功。Play中にVisual（停止・再生中）、Values、ParameterのFoldを展開した状態をスクリーンショットで確認し、Console Errorは0件だった（`Reports/PlainUI/`）。色分けは選択状態とドラッグ状態（内部状態を直接設定）を `color-select.png` / `color-drag.png` / `color-values.png` で確認した。Drag & Drop、Dropdown、ボタンを実際にクリックする操作は自動では送っていない。
