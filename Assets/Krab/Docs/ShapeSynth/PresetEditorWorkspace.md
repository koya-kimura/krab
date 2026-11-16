# Phase 10: Preset Editor Workspace

## 現在のUI（2026-11-14、素のRosettaUI）

独自テーマ・3ペインの可変レイアウト・UI倍率・Zoom/Panなどを削除し、RosettaUI標準のWindow / Tabs / Fold / Field / Dropdown / Buttonだけで組み直した（PresetEditorのスクリプトは約3200行から約1300行）。説明文・ヒント・ステータスバーは置かない。表示するのは処理に失敗したときのエラー文だけ。

`Assets/Krab/ShapeSynth/Samples/ShapeSynthPresetEditor.unity` を開いてPlayすると、共通Settingsウィンドウの右にPreset Editorウィンドウが開く。閉じた場合は Settings → ShapeSynth → Authoring → Preset Editor → Open Preset Editor。

- 上部: Preset名、New、Save、保存済みPresetのDropdownとOpen。未保存の確認はない（タイトルの `*` が未保存を示す）。
- Visualタブ
  - Hierarchy: Nodeをボタンで選択する（選択中は青）。種類は記号で示す（■ Group、○ Circle、◻ Rectangle、▲ Polygon）。`▼` / `►` でGroupを開閉する。Drag & Dropで並べ替え・入れ子を変更する（端は前後、Groupの中央は子）。ドラッグ中はドロップできる行を暗い緑、今ドロップする行を明るい緑、ドラッグ中のNodeを灰色で示す。ShapeのDropdownと `+ Shape` / `+ Group` / `Delete`。
  - Preview: 出力RTを枠に収まるように表示する（Zoomなし）。`Play` / `Stop`、Beatスライダー、BPM、Seed。ModeはBeat（BPMクロック）だけ。各Valueを現在のBeatで評価して静止Hierarchyを描画し、Playでは最長ADSRの寿命（上限6拍）でループする。編集は0.3秒後に自動でCompileする。
  - Inspector（幅620）: Name、Transform / Shape / Layout / StyleのFold（GroupはName/Transformのみ）。Parameterは表形式の1行「名前 | Base | Rand | Depth | Unit | Now | Flip | Bind」（Rand/Flipは `Variation.md`）で、列見出しは各ブロックの先頭に1回。名前とBindプルダウンはValue接続中に緑。Depthは未接続なら灰色の「-」。Nowは接続中・画面基準の単位・Randありのときだけ、プレビューBeat時点の Base＋Depth×信号 の出力（px / deg）を出す。Rand / Flip / Delayで複製ごとに値が変わる場合は先頭に「~」を付ける（それらは反映しない基準値）。アイコンはRosettaUI既定のopen-in-new画像。
  - 単位: Position / Size / Spacing / Radius は `px / xW / xH / xMin / xMax`（画面基準は倍率。0.1 xW = 画面幅 × 0.1。保存値をそのまま表示・入力）、Rotation / Hue Shift は `deg / turn`、Scaleは `x`、Opacity / Color Mixは `0-1` 固定。単位を変えるとBaseとDepthを換算し、出力を変えない。保存形式は変更せず、`PresetUnits` が既存のReference / Multiplier / AbsoluteValueへ対応付ける（画面基準→Canvas系Reference、turn→Absolute×Multiplier 360、AbsoluteValueは常に1）。Reference / Multiplier / Absolute ValueはUIに出さない。
  - Bind列: `None` と全Valueのプルダウン（RosettaUI Dropdown）。Noneで解除、Valueを選ぶとDepthは種類ごとの既定値（Position 100、Rotation 90deg/0.25turn、Hue 360deg/1turnなど）で接続。別のValueへ切り替えたときはDepthを維持する。Value追加・削除・名前変更で選択肢を作り直す。Bind用の別ウインドウは廃止。
  - 必要な項目だけを出す: Fill ON時のみColor / Color Mix / Hue Shift、Color MixのBaseが0以外かValue接続中のみTo Color、Stroke ON時のみStroke Width / Stroke Color、LayoutはKindに応じた項目のみ。
- Valuesタブ: Value一覧（選択中は青）と `+ Value`。Valueは複数のComponent（ADSR / LFO、Weight付き）の合成で、信号は `Σ Weight × Component`。出力グラフ（0–6 beat）には選択ValueのComponentを細線（ADSRは青、LFOは橙、Weight込み）、合計を白の太線で描き、Beat位置に縦線を引く。縦軸は±1を基本に、合計がはみ出すと自動で広げる。その下にName、各ComponentのFold（Weight、ADSR 5項目 / LFO 3項目、Remove Component）、`+ ADSR` / `+ LFO`、Remove Value（どのParameterにも接続されていないときだけ有効）。
- 保存形式はSchemaVersion 2（`Values[].Components[]`、Bindingは `ValueId`）。v1のファイルは読み込み時に、各Modulatorを「Weight 1のComponentを1つ持つValue」へ変換する（`PresetJsonMigration`）。ファイルは保存し直すまでv1のまま。CompilerはBound Valueを「Componentごとに Depth × Weight のBinding」へ展開するので、Runtime（`ParameterDefinition` の合算）は変更していない。
- Visual側でValueを新規作成する機能、Static / Animatedモード、Preview Zoom、UI倍率、右クリックメニュー、未保存確認、Open一覧ブラウザ、ステータスバーは削除した。
- 各ペインの先頭に Hierarchy / Preview / Inspector / Values / Output の見出しだけを置く。状態を示す色は `PresetEditorPalette` にまとめ、RosettaUIの `Style.Color` / `Style.BackgroundColor` だけで付ける。記号はRosettaUI既定フォントにあるもの（○ ◻ ▲ ■ ● ◆ ▼ ►）に限る。
- Play中にスクリプトが再読み込みされるとDocumentは消える（シリアライズしないため）。そのときは新規Documentに置き換えてエラー欄に表示する。保存していない編集は失われる。
- `Samples/Settings/WorkspacePanelSettings.asset` はScale With Screen Size（1600×900、Match 0.5）。このSceneだけが使う。
- RosettaUIのDropdownメニューはUnity 6000.3で削除された内部APIに依存するため、`Choice` で `createMenuCallback` を外してUnity標準のメニューを使う互換処理だけ残した。
- 検証: コンパイル成功、EditMode 206成功。Play中にVisual（停止・再生中）、Values、ParameterのFoldを展開した状態をスクリーンショットで確認し、Console Errorは0件だった（`Reports/PlainUI/`）。色分けは選択状態とドラッグ状態（内部状態を直接設定）を `color-select.png` / `color-drag.png` / `color-values.png` で確認した。Drag & Drop、Dropdown、ボタンを実際にクリックする操作は自動では送っていない。

以下はこれ以前のUIの記録。

2026-11-12。Phase 8/9の未コミット変更を保持してUIを改善した。今回もコミットしていない。Core、Compiler、Repository、SchemaVersion=1、Performance.v1.json、共通描画/出力の仕様は変更していない。

## 操作

`Assets/Krab/ShapeSynth/Samples/ShapeSynthPresetEditor.unity` を開いてPlayすると専用Workspaceが自動で開く。閉じた場合は Settings → ShapeSynth → Authoring → Preset Editor → Open Preset Workspace。Dの共通UI切替も維持する。

- 上部: Preset名、未保存印、New / Open / Save Preset、Preview / Restart、Stop、Static / Animated、Auto Update、BPM、Seed。New/Openで未保存なら、画面内の確認バーでDiscard & ContinueまたはCancelを選ぶ。OpenはSaved Preset一覧とOpen Selectedから読み込む。保存先は従来どおりpersistentDataPath/ShapeSynth/Presets。
- 左上: Hierarchy。Nodeをクリックして選択、矢印でGroupを開閉する。種類とネストを表示し、選択行を青で強調する。追加先Groupを選び、種類Dropdownと+ Add Childを押す。Shape選択中の追加とRoot削除は無効。Delete Nodeで選択した子孫も削除する。右クリックメニューのSelect/Add Group/Add Circle/Deleteもコード上で用意している。
- 左下: Modulator一覧。+ ADSR / + LFOで追加し、行を選んで右側Inspectorで詳細を編集する。Visual階層には入れない。使用中Modulatorの削除は無効で、Binding解除の理由を表示する。
- 右: 選択NodeまたはModulatorだけを表示する。NodeはTransform / Geometry / Styleを分離し、Position X/YとScale X/Yは同じ行。Colorは色付きスウォッチで確認する。
- Parameterの横のBindボタンで行内のBinding詳細を開く。1 Parameterに接続できるValueは1つだけ（下記「追補: 1 Parameter = 1 Value」）。式は `Reference × (Base + Depth × Value)`。
- 中央: 既存CameraのRenderTextureをUI.Imageで直接表示する。Fitは表示領域に収め、Wheelはカーソル位置を基準にZoom、中央ボタンドラッグはPan。ZoomはFitに対する倍率。画像の生成・所有は既存出力側のまま。
- Staticは全Modulator信号を1に固定する配置確認。Animatedは既存Voiceの寿命に従って自然終了する。Staticの意味はStatusにも表示する。Preview / Restartは新しくCompileして古いPreview Voiceを置き換える。Stopで消す。

1920×1080、1280×720では3領域を同時表示する。境界を左ドラッグして幅を変更できる。1100px未満で左右の既定幅を縮小し、850px未満はNavigation / Inspector切替ボタンで片側だけを表示する。Previewは常時残る。Window端のリサイズも使える。最小設計幅は640px。極端に低い高さや640px未満は対象外。

## 変更前の問題

- Settings配下の長いフォームで、Preview・Hierarchy・Inspectorを同時に見ることが難しかった。
- 共通RevisionがHierarchy、Inspector、Modulator全体の再構築を引き起こし、Binding追加や名前変更でFoldやフォーカスを保持しにくかった。
- Modulatorの全パラメータを一覧の下へ展開し、一覧と詳細が混在していた。
- 選択Group以外への追加やRoot削除がボタン押下後のエラーだった。
- 中央の専用RT Previewがなく、Zoom/Panや未保存確認がなかった。

## 構成と更新設計

- `ShapeSynthPresetEditor.cs`: 編集操作、選択ID、未保存状態、確認操作、Previewのデバウンス。UIを組み立てるコードはpartialの各Viewファイルへ分離する。
- `.UI.cs`: 専用Window、Toolbar、Open Browser、Status、共通Fieldヘルパー。IUiTargetはWorkspaceを開く導線だけを提供し、CommonのUiBuilderは変更していない。
- `.Hierarchy.cs`: NodeのツリーとModulator一覧、選択ハイライト、無効操作と理由。Group展開状態はNode IDで保持する。
- `.Inspector.cs`: 選択NodeのTransform/Geometry/StyleとParameter/Binding。`.Modulators.cs`: Modulator操作と選択ModulatorのInspector。
- `PresetPreviewView.cs`: UI.Image、Fit / Zoom / Panと入力Capture。RenderTextureを生成・破棄しない。
- `PresetWorkspaceLayout.cs`: 3領域、境界ドラッグ、狭画面切替、Windowサイズ制約、Rosetta ElementとUI Toolkit Viewの接続・解除。
- `WorkspacePanelSettings.asset`: 専用Sceneだけで使うConstantPixelSizeのPanelSettings。CommonのDPI設定では1920px画面が約723論理pxになり、Workspaceが過剰に拡大されたため専用アセットにした。既存共通アセットは変更しない。
- `ShapeSynthPresetEditorSceneBuilder.cs`: 上記PanelSettings参照と診断Graphyの非表示を設定し、専用Sceneを再現する。既存Editor SceneはUnity経由で保存した。CommonのRT/SyphonとPerformance/本番Sceneは変更していない。

DynamicElementの監視は次のように限定する。

| 操作 | 更新する領域 |
| --- | --- |
| New / Open | Document RevisionでHierarchy/一覧/Inspectorを入れ替える |
| Node追加・削除 | 親Group IDごとのChildren Revisionで該当枝を入れ替える |
| Node/Modulator選択 | Document Revisionと選択IDの組だけでInspectorを切り替える。選択色は既存行のStyleを更新する |
| Modulator追加・削除 | Modulator一覧と必要なBinding選択肢を更新する |
| Binding追加・削除 | PresetParameterごとのRevisionで、そのBinding一覧だけを入れ替える |
| 名前・数値・色・Depth | 既存Fieldの値Bindingを更新。Inspector全体は再構築しない |
| Modulator名 | 一覧のLabelとBinding選択肢だけを更新する |

外側のScrollView、Workspace、Previewは編集のたびに作り直さない。Fold開閉状態は選択ID単位で保持し、Groupを閉じても選択は変わらない。値変更は300msの編集停止後にAuto Updateし、不正値はStatusへ一度表示して以前のPreviewを保持する。Saveや名前変更だけで不要なVoiceを生成しない。

## RosettaUIとの互換性・技術的制約

導入済みRosettaUIは2.0.0（解決済みPackageCacheを確認）。参考の `SplitViewHorizontal` / `RegisterVisualElementAttachedCallback` は存在しなかった。アップグレードせず、Row/Column/ScrollView/DynamicElement/Image/Fieldを使い、境界操作とWindow調整をUI Toolkitで補助している。

`BindView` は軽いView参照取得で一度だけUI Toolkitハンドラーを接続する。再Bind/破棄時はGetViewBridge().onUnsubscribeでGeometry/Pointer/Panelイベントを解除し、Pointer Captureも解放する。毎フレームツリー検索やUI全体の再構築はしない。軽いLabel/選択色更新とRTサイズ確認のみ継続する。

このRosettaUIのDropdown拡張はUnity 6000.3で削除された内部interfaceをReflection.Emitで参照し、実際のメニューOpenで例外になった。WorkspaceのDropdownに限り、Rosettaの値Bindingを残してcreateMenuCallbackを解除し、Unity標準ランタイムメニューを使う互換処理を設けた。PackageCacheや依存バージョンは変更していない。将来RosettaUIを更新する際はこの限定した互換処理を再評価する。

また、標準Windowは内容の推奨幅で拡大するため、RTの実サイズへ広がってInspectorが画面外へ出た。Workspace内の内容リサイズ要求を抑止し、画面の最大幅/高さで制限する。ユーザーのWindow/境界リサイズは残す。PreviewのZoom/PanはCompileで保持し、RTの解像度が変わった場合はFitへ戻す。

## 検証

既存krab Editor（Unity 6000.3.12f1）へPipeline接続し、再起動・前面表示は行っていない。実行ログ/スクリーンショット/実UI操作スクリプトはgitignore対象 `Reports/Phase10/`。保存先は専用検証フォルダで、既存ユーザーデータは変更しない。

- C#コンパイル成功。最終EditMode **201成功・失敗0**。UI改善のためCore/保存Schemaの変更はしていない。不正JSON生成用の既存テストfixtureではJsonUtilityの再帰深度警告が出るため、実UIのConsoleとは区別する。
- Play中の実RosettaUIのButton/Field/Dropdown/Pointerイベントを使い、Group追加、子Circle追加、Node選択、Position/Diameter/Color/名前編集を確認。
- ADSRとLFOの選択連動、Attack/Cycles per beatと名前の編集、使用中Modulatorの削除無効化を確認。
- ParameterのBinding追加、Modulator ID選択、Depth編集、保存してNew/Openで再読込を確認。名前、4 Nodeの階層、2 Modulator、Depth=81が復元された。
- 入力中のFocusが保持され、数値/名前/色編集の前後でInspectorBuildCountが変わらないことを確認。
- Preview再生成後もInspectorスクロール、Zoom=1.331、Pan=(36,24)が保持された。Fitは倍率1、Pan=0。Animatedを連続Restartしても所有Voiceは1つ。
- Native DropdownをNavigationSubmitで開き、表示されたAnimatedメニュー項目をPointerイベントで選択して、モード変更を確認。
- Newの未保存確認でCancelすると編集を維持し、Discard & Continueすると切替。Openも同じ確認経路で既存JSONを復元。
- Groupを折りたたんでも選択Nodeは維持。境界ドラッグでNavigation幅240→272pxを確認。
- Workspaceを4回Close/Openし、Window数2→2、RenderTexture数45→45で増加なし。
- Game Viewに一時的な固定解像度を設定して、実1920×1080と1280×720の3領域配置を確認。800×600ではNavigation/Inspectorの排他切替と常時Previewを確認。単なるスクリーンショットの拡大縮小とは区別している。検証後は一時解像度を除去して元の選択へ戻す。
- PerformanceのUser Presets UIから保存済みデータを選択してTriggerし、Pad01と2 Voice / 7 DrawItemで重ね描画と自然終了を確認。Q/W/E/A/SとDは一時Input System Keyboardで既存Update経路を再確認した。
- 最終の新しいPlayセッションではCamera/RT/Canvas参照を確認。Workspace由来の例外は検出されなかった。一方、Play開始時にUnityの音声出力切替で `FMOD failed to switch back to normal output ... Cannot call this command after System::init (32)` が1件発生した。UIコードを指すStackTraceはなく音声設定を変更していないため、環境側の未解決事項として記録する。最終Consoleの全体Error数は0ではない。検証後はPreset Editor Sceneを開いたEdit Mode、dirty=falseで終了。

最終スクリーンショット: `Reports/Phase10/workspace-1920-final.png`、`workspace-1280-final.png`、狭画面参考 `workspace-800-final.png`。

未検証/制約: 物理マウス・キーボードによる全ジェスチャ、長時間のメモリプロファイリング、Playerビルド、Syphon受信先、極小画面。右クリックメニューは実マウスでの最終確認対象。本格的Undo/Redo、並べ替え、多重選択、Profiler/Compareは今回の対象外。演出・操作感の最終承認はユーザーが行う。


## 追補: 表示サイズとHierarchyドラッグ（2026-11-13）

- UI初期倍率を140%に拡大した。ToolbarのUIから100/125/140/160/180/200%を選べる。4K以上は出力ピクセル幅に応じてさらに補正し、Game Viewを縮小表示しても操作部が小さくなりすぎないようにする。倍率はセッション内の設定で、Preset JSONには保存しない。
- PanelSettingsはPlay中だけ複製して使用し、Sceneの元アセットは変更しない。終了時に元へ戻して複製を破棄する。`PresetWorkspaceScale.cs`が担当する。
- 左右の既定パネル幅を260/400論理px（狭幅では220/320）に広げ、中央の画像はFit時も25%の余白を取る。Wheel Zoom/Panは継続して使える。
- Hierarchy行を左ボタンでドラッグすると移動できる。行の上端/下端へ落とすと前/後へ並べ替え、Group中央へ落とすとそのGroupの末尾へ移す。ドロップ位置は青い線/面とラベルで表示する。Escまたは無効な領域で離すとキャンセルする。Rootは移動できない。
- `PresetHierarchyDrag.cs`は入力Capture/ターゲット判定/表示のみを担当し、Document操作はEditorを通す。移動後も選択IDとNode IDを維持し、移動元/移動先の枝だけを更新する。
- `PresetDocument.MoveNode(nodeId, parentId, insertionIndex)`を追加。indexは削除前の対象Children基準。同一親なら削除によるずれを補正する。Root、Shapeへの子追加、自分/子孫への移動、不正Index、最大深さ64を超える移動を変更前に拒否する。ローカルTransformと子の順序/IDは保持する。Schemaは変更していない。
- 追加テストを含むUnity EditMode **205件成功・失敗0**。実Pointerイベントで、子孫への無効ドロップ、子GroupからRootへの移動、兄弟並べ替え、別Groupへの移動を確認。移動後のJSON往復とCompileも確認した。
- UI倍率140→160%の切替と安定したレイアウトを確認。4Kでは実Panel倍率2.8→3.2となり、見かけの操作密度を保つ。画像は `Reports/PresetUiFollowup/ui-140.png`。更新前の未保存Draftは同フォルダへ退避した。

更新後は退避した3 Nodeの未保存DraftをJSON構造の完全一致を確認して復元し、未保存状態のままPlay中で引き渡した。既存ユーザー保存先は上書きしていない。


## 追補: ADSR Bindingの操作

> 旧手順。現在の操作は「追補: 1 Parameter = 1 Value」を参照。

PreviewのWheel/Middle dragの案内文字列を削除した。Zoom/Panの機能は維持する。

ADSRをOpacityへ接続する基本手順:

1. HierarchyでShapeを選ぶ。
2. Opacityの横の **Bind** を押す（右側InspectorがPARAMETER BINDINGへスクロールする）。ParameterはDropdownでも切り替えられる。
3. **Modulator** から接続したいADSRを選ぶ。まだなければ **New ADSR** で追加できる。この操作ではNode選択を維持する。
4. **New Depth = 1** にして **Bind ADSR from 0** を押す。この明示的な操作はBaseを0にし、選んだADSRのBindingを追加/更新する。同じModulator IDを再適用しても重複を増やさない。他のBindingは解除しない。
5. ToolbarのModeを **Animated** にして **Preview / Restart** を押す。Staticは信号を1に固定するので、ADSRの時間変化を見る場合はAnimatedを使う。

DiameterはDepthがReferenceに対する割合になる。例: Ref=MinDimension、Depth=0.2、Bind ADSR from 0なら、直径が0から短辺の20%まで変化する。通常の **Bind** はBaseを変更せずに信号を足す。OpacityのBaseが1のまま正のADSRを足すと0→1のフェードにはならないため、上記の専用操作を用意した。既存のSignal/Depth行は接続済みの設定、Modulator/New Depthは次に適用する接続設定である。

画面実装は `.Bindings.cs` に分離した。Parameter選択と追加フォームだけをDynamicElementで切り替え、Inspector全体は再構築しない。選択肢には一覧番号とADSR/LFOの種類を表示し、同じ表示名でもIDで区別する。Core、式、JSON Schemaは変更していない。

検証: Unityコンパイル成功、既存205件成功・失敗0。実UIで新規CircleのOpacityをBase=0 / Depth=1へ接続し、Compile後の評価で開始時0・Attack完了時1を確認。DiameterはBase=0 / Depth=0.2、再適用してもBinding 1件。BindボタンのParameter切替とスクロール、Inspectorの再構築なし、New ADSR時のNode選択保持、JSON保存/再読込/Compileも確認。案内文字列がUIツリーに存在しないことを確認した。検証成果物は `Reports/AdsrBinding/`。

## UI v0.5: 参考HTMLに合わせた再構成

2026-11-14。参考モック `ShapeSynth_PresetEditor_UI_v05.html` のレイアウトと操作感をRosettaUIで再現した。Core、Compiler、Repository、保存Schema、Scene構成は変更していない。コミットはしていない。

- 上部Header: `ShapeSynth / Preset Editor`、Visual / Valuesタブ、Preset名、未保存印、UI倍率、New / Open / Save。未保存確認とOpen BrowserはHeader直下の帯に表示する。
- Visualタブ: 左Hierarchy（アイコン付きツリー、選択行ハイライト、`+ Group` / `+ Shape`、Shape種類、Delete）、中央Preview（Preview / Stop、モード、Beatスライダー、Fit、下部にAuto / BPM / Seed / 解像度・Zoom）、右Inspector。
- 追加先: Group選択中はその子、Shape選択中は同じ親Groupへ兄弟として追加する（モックと同じ）。Drag & Drop、右クリックメニュー、Rootの削除不可は従来どおり。
- Inspector: パラメータごとに「名前・現在の出力値 / Base・Reference・Bindボタン」。Bindボタンは接続中なら緑でValue名を表示し、押すと行内にBinding詳細を開く（現在の構成は「追補: 1 Parameter = 1 Value」）。出力値は `Reference × (Base + Depth × Value)` をStatic=1またはBeat時点の信号で計算する。
- Valuesタブ: 左Value Library（既存Modulator一覧、Value名編集、`+ ADSR` / `+ LFO`）、右上Outputグラフ（0–6 beat、選択Valueを強調、他を薄く表示、Beat位置の縦線）、右下Signal Components（ADSR 5項目 / LFO 3項目、使用数、未使用時のみRemove）。
- Previewモード: `Static · signals = 1`（従来のStatic）、`Beat Scrub`（新規）、`Animated`（従来）。Beatスライダーを動かすとStaticはBeat Scrubへ切り替わる。Beat Scrubは各信号をCoreの `AdsrInstance` / `LfoInstance` で指定beatの値に固定し、スクラブ中はCompileせず値だけ更新する。

### モックとの差分（意図的）

- モックの「Value＝複数Signal（ADSR+LFO、Weight付き）の合成」は保存Schemaの変更が必要なため実装していない。既存どおり1 Value = 1 Modulatorとする。Parameter側の複数Bindingによる合成も廃止した（1 Parameter = 1 Value）。合成Valueが必要なら別途仕様合意のうえCore / Schemaを拡張する。
- Bindは1 Parameterに1 Valueまで（2026-11-14に変更）。ボタンには接続中のValue名を表示する。
- フォントに `⌁` / `◯` が無いため、Value・Binding表示は `●`、Circleは `○` を使う。

### 構成

- `PresetEditorTheme.cs`: モックの配色トークンと、RosettaUI Elementへ一度だけインラインStyleを当てるヘルパー（`Styled`、PaneTitle、SectionTitle、ActionStrip、Button）。
- `PresetSignalGraph.cs`: `PresetSignalSampler`（Coreインスタンスで1 beat = 1 sの評価）と、Painter2Dで描くOutputグラフ。入力が変わった時だけ再描画する。
- `.UI.cs` Header / Status / Open Browser、`.Hierarchy.cs` 左ペイン、`.Preview.cs` 中央ペイン、`.Inspector.cs` + `.Bindings.cs` 右ペイン、`.Modulators.cs` Valuesタブ。`PresetWorkspaceLayout.Create` はHeader・Visual 3ペイン・Values・Statusを受け取り、タブで表示を切り替える（Values表示中は狭幅切替ボタンを出さない）。

### 検証（2026-11-14）

既存krab Editorへ接続し、再起動・前面表示はしていない。Reportsは `Reports/PresetEditorUI/`（gitignore対象）。

- C#コンパイル成功。EditMode **205成功・失敗0**。
- Play中の実UI ToolkitボタンへNavigationSubmitを送り確認: Bind詳細の開閉、Position XへのBinding追加（Depth 100）、`+ Shape`（Shape選択中は兄弟追加）、`+ Group`、Delete、Values/Visualタブ切替、`+ LFO`、未使用LFOのRemove、Save → New（未保存確認のDiscard & Continue）→ Open Selectedで名前・3 Node・2 Modulatorの復元。保存先は検証用ディレクトリへ差し替えた。
- Beatスライダー1.5でOpacity出力0.64（ADSR Release中）と暗いCircleを確認。0.8→3.2のスクラブでCompile回数が増えないことを確認。LFO値の編集でグラフが再描画されることを確認。
- Animatedへ切替（Dropdownの値変更）後、Preview連打でVoice 1、StopでVoice 0。
- Console Error 0。検証後はEdit Mode、Scene dirty=false。
- 未確認: Hierarchy Drag & Dropと境界ドラッグのPointer操作の再確認、1920×1080 / 850px未満での目視、Native Dropdownメニューのクリック操作。見た目の最終評価はユーザー確認待ち。

スクリーンショット: `Reports/PresetEditorUI/v2-visual.png`、`v3-bind-details.png`、`v4-beat.png`、`v5-values.png`。

### ボタン状態の明示（2026-11-14 追補）

- `PresetButtonStyle.cs` でボタンの見た目を状態から決める: 通常（灰）、ホバー（明るい灰）、押下中（暗色＋青枠）、Primary（青: 実行を促す）、Active（緑: 現在オン）、無効（暗く文字も減光）。インラインStyleはUSSの:hover/:activeより優先されるため、Pointerイベントで状態を管理し、変化した時だけ塗り直す。
- Preview: 表示中は緑の `● Previewing`。Animatedは既存Voiceが自然終了すると自動で `▶ Preview` へ戻る。Stopは表示中だけ有効。
- Save: 未保存時のみ青（Primary）。Header/Statusの未保存印は橙 `● Unsaved`、保存済みは `✓ Saved`。Openは保存一覧を開いている間は緑。
- Bind: 接続済みは緑、詳細を開いた未接続は青で `▾ Bind`。Hierarchy / Value Libraryの行はホバーで薄くハイライトし、選択行は青。
- Beat表示: Beat Scrub中のみ値を強調し、Animated中は減光して `—`。Statusのエラー（`⚠`）は橙で表示し、右側に `● Preview on` / `○ Preview off` を表示する。
- 確認: コンパイル成功、EditMode 205成功。Playで Preview on/off、Stopの無効化、Bind編集中の青、Open表示中の緑をスクリーンショットで確認（`Reports/PresetEditorUI/s1-preview-on.png`、`s2-preview-off.png`、`s3-states.png`）。ホバー・押下中の色はPointer入力を自動送信できないため未確認。

### Preview再生（2026-11-14 追補）

- Previewボタンは手動スクラブではなく再生になった。押すとbeat 0から、BPM欄の値で拍が進む（`beat += 経過秒 × BPM / 60`）。Beatスライダーと出力グラフの縦線も自動で動く。もう一度押すと先頭から再生し直す。
- ループ長は、ADSRの寿命（Attack + Decay + Sustain Time + Release）の最大値。上限はスライダー範囲の6拍。トリガー1回分を繰り返し見られる。
- モード: `Static · signals = 1`（従来どおり静止。Previewを押すとBeatへ切り替えて再生）、`Beat · BPM clock`（信号をCoreのADSR/LFOで現在拍の値に固定し、Compileせずに毎フレーム更新）、`Animated · Voice`（実Voiceを生成。Voiceが自然終了したら再トリガーしてループ）。
- 再生中にスライダーを動かすと、その拍から再生を続ける（シーク）。Stopで時間が止まる。BeatではStop時の拍の絵を残してスクラブでき、AnimatedではVoiceを消す。編集による自動Compileでは再生位置を保つ（AnimatedはVoiceを作り直すので0拍に戻る）。
- 確認: 120 BPMで約2拍/秒、60 BPMで約1拍/秒の進行、2.30拍でのループ、Stopで拍が止まること、AnimatedのVoice数が常に1で再トリガーされることを確認した。Console Error 0、EditMode 205成功。スクリーンショット: `Reports/PresetEditorUI/p1-playing.png`、`p2-stopped.png`、`p3-60bpm.png`。

## 追補: 1 Parameter = 1 Value

2026-11-14。Value Libraryの値変化（Modulator）は、1 Parameterに1つまでしか接続しない仕様にした。複数Valueの足し合わせは廃止。

- Core: `PresetParameter.Bindings` はJSON Schema互換のためListのまま残す。`GetBinding()` / `Bind(id, depth)`（既存を置き換え）/ `Unbind()` を追加した。`Validate` は2件以上のBindingを `A Parameter can bind at most one Value.` で拒否するので、該当するJSONは保存も読み込みもできない（既存の保存Presetに該当なしを確認済み）。Runtime側の `ParameterDefinition` は変更していない。
- Binding詳細（Parameter行のBindボタンで開閉）:
  - **VALUE**: Dropdownで `None · stays at Base` または Value を選ぶと即座に接続・差し替え・解除する。差し替え時はDepthを保つ。未接続から接続した場合のDepthは既定値（Opacity 1、Position 100、Rotation 90、他はBase、Base=0なら1）。`+ ADSR` / `+ LFO` は新しいValueを作ってそのまま接続する。
  - **Depth / Output range**: 接続中だけ表示する。Output rangeはValueの振れ幅（ADSR 0→1、LFO −1→+1）を当てたときの出力値で、Reference込みの実際の値を示す。
  - **Start from 0 (Base = 0)**: Baseを0にする。OpacityのフェードインなどADSRで0から動かしたいときに使う（旧 `Bind ADSR from 0`）。Baseが0のときは無効。
  - **Unbind** / **Done**、Reference用のRef. multiplier / Absolute value、現在の式（未接続 `Reference × Base`、接続中 `Reference × (Base + Depth × Value)`）。
  - 旧UIの「接続済み一覧 + Add Value + New Depth + + Bind」の2段構成と、未適用の保留状態を廃止した。
- Value LibraryのRemove不可時の案内は「すべてのParameterからUnbindする」に変更した。
- 検証: コンパイル成功、EditMode 206成功（`ParameterBindsAtMostOneValue` を追加）。Play中にPosition Xで未接続表示、Default ADSRの接続（同じValueの再選択でもBinding 1件、Depth 100、Output range 0→100）を確認。スクリーンショット: `Reports/BindingUI/unbound.png`、`bound.png`。Dropdown・ボタンの実クリックは自動送信していない。

## 追補: ShapeのLayout（2026-11-16）

各ShapeのInspectorに **Layout** Foldを追加。1つのShapeを **Count** 個複製して並べる。同じGroupに「円形に並べたCircle」と「横並びのRectangle」を置ける。Hierarchyの行には `x個数` を表示する。

- Kind: `None`（1個、従来どおり）/ `Row` / `Column` / `Grid` / `Circle`。None以外でCountを指定（1〜2000）。
- Row: 左から右へ Spacing X 間隔。Column: 上から下へ Spacing Y 間隔。Grid: Columns列で左→右、上→下に折り返し、全体をブロックとして中央揃え。
- Circle: Radiusの円周上に等間隔。最初の1個を Start Angle（度、+Xから反時計回り。既定90=真上）に置き、残りは時計回り。Face Outward をONにすると各個のローカル+Yが外向きになる。
- Spacing X / Y、Radius は通常のParameter（Reference・Base・Bind可）。既定は MinDimension × 0.25（Spacing）/ 0.3（Radius）。ADSR/LFOをBindすれば間隔・半径をアニメーションできる。
- ShapeのTransform: Position X/Y は並びをずらす（各複製に同じだけ足す）。Rotation / Scale は各個に、それぞれの位置を中心として適用する。複製ごとの差はDelay / Rand / Flipで付ける（`Variation.md`）。
- データ: `PresetNode.Layout`（`PresetLayout`: Kind, Count, Columns, SpacingX, SpacingY, Radius, StartAngle, FaceOutward）。旧JSONはLayoutなし→Noneとして読める。SchemaVersionは2のまま。GroupにNone以外を設定すると検証エラー。1 Presetで描画するShapeの総数は20000まで。
- Compile: Layout付きShapeを「scopeなしGroup」→「各スロットへ平行移動するscopeなしGroup × Count」→「複製ごとにFlip/Delayを反映したShapeDefinition（Transform込み）」に展開する。スロット座標 k は `Reference × (k·Base + Σ k·Depth × signal)` としてParameterに畳み込む。
- 検証: EditMode 218件成功（`PresetLayoutTests` 10件）。Preset Editor SceneのPlayで、1つのGroup内にCircle×12（Circle配置）とRectangle×5（Row）を描画できることを画像で確認（`Reports/PresetLayout-shapes.png`）。InspectorのLayout UIの実クリック操作は未確認。

## 追補: 色のBinding（2026-11-16）

ShapeのInspectorのStyleに **To Color**、**Color Mix**、**Hue Shift** を追加。塗り色は毎フレーム次の順で決まる。

1. `Color` と `To Color` をRGBAで線形補間する。比率は Color Mix（0=Color、1=To Color、範囲外は0〜1にClamp）。
2. その色の色相をHSVで Hue Shift 度だけ回す（彩度・明度・Alphaは維持。灰色/白/黒は変化しない）。

- Color Mix / Hue Shift は通常のParameterなので、Bindボタンで ADSR / LFO を接続できる。Hue ShiftのBind時の既定Depthは360（Value 0→1で1周）。
- 既定は Mix=0、Hue=0、To Color=白なので、既存Presetの見た目は変わらない。旧JSONはこの3項目なしで読める（SchemaVersion 2のまま）。To Colorの非有限値は検証エラー。
- Core: `ColorDefinition(From, To, Mix, HueShiftDegrees)` と `ShapeDefinition.WithFillColor`。`HierarchyEvaluator` が出現ごとにMix/Hueを評価して `DrawItem.Color` を作る（Variation parameterKey 9/10）。Coreは引き続きUnity非依存。
- 対象は塗り色のみ。Stroke Colorは固定のまま。
- 検証: EditMode 225件成功（`ColorDefinitionTests` 7件追加）。Preset Editor SceneのPlayで、ADSRにHue Shift(Depth 240)をBindした赤いCircle×5と、Color MixをBindした橙→青のRectangle×5が、Beat 0で基準色、Beat 1で変化した色になることを数値と画像で確認（`Reports/PresetColor-beat0.png` / `beat1.png`）。InspectorのUI実クリック操作は未確認。

