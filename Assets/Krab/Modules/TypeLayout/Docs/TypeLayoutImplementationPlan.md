# TypeLayout：テキスト配置システムの実装計画

2026-11-17。映像の上に重ねる文字（画面端に沿う文字列、列に揃えた文字グリッド、円周上の文字など）を、ずれなく配置するための仕組み。ShapeSynthと同様に、将来のパッケージ化を見越して層を分ける。写真の切り抜きは別パッケージとし、対象外。

## 構成

```text
Assets/Krab/Modules/TypeLayout/
  Runtime/Core/    Krab.TypeLayout.Core（noEngineReferences）配置計算のみ
  Runtime/Unity/   Krab.TypeLayout.Unity：Core + Unite.Runtime + Unite.Text
  Runtime/Performance/  演奏用コンポーネントとレイヤー（Assembly-CSharp。Krab CommonのBPM・DI・UI・Prefsと接続）
  Samples/         検証Sceneとデモ（Assembly-CSharp）
  Tests/Editor/    CoreのEditModeテスト
Assets/StreamingAssets/Krab/TypeLayout/Fonts/  Jost（SIL OFL 1.1、ライセンス同梱）
```

CoreとUnityの層は `Krab.Common` やShapeSynthを参照しない。BPM・DI・RosettaUI・Prefsとの接続は `Runtime/Performance` とSamples（Assembly-CSharp）に置く。

## 座標と単位

- 出力ピクセル、左上原点、X右・Y下。p5.Unite TextのScreenモードと同じ向き。
- 角度は度。0が右向き、正の値は画面上で時計回り。90度の倍数ではcos/sinを厳密値にし、軸に沿った配置が整数座標から動かないようにする。
- 長さの指定は基準解像度（高さ1080）のピクセルで書き、`LayoutFrame.Unit`（出力の高さ÷基準高さ）を掛ける。アスペクト比が変わると横の余白だけが変わる。
- パスは弧長でパラメータ化する（`ILayoutPath.Sample(distance)`）。閉じたパスは距離を周回させ、開いたパスは端の方向へ直線で延長する。
- 角はそこから始まる辺に属する。

## 段階

| Phase | 内容 | 状態 |
|---|---|---|
| 0 | p5.Unite 0.2.1とText 0.2.0を導入。ShapeSynthの `TargetCameraCanvas` を `SetCamera` ベースへ移行 | 完了 |
| 1 | Frame/Rect/Anchor、Line/Edge/Arc/RectPerimeter、等間隔スロット、グリッド | 完了 |
| 2 | 文字幅を考慮した流し込み、閉じたパスの継ぎ目合わせ、基準点（Baseline/Em/Cap/Ink）、回転モード | 完了 |
| 3 | p5.Unite Textでの描画Adapter、フォント読み込み、検証Scene、ガイド線表示 | 完了 |
| 4 | 文字列の繰り返し・Seed付きランダム、拍単位のスクロールと入れ替え | 完了 |
| 5 | 演奏用コンポーネント、RosettaUIでの操作、Prefsへの保存 | 完了 |
| 6 | サンプルSceneでUI操作をMIDIへBindする機能、性能計測 | 未着手（後回し） |

## Phase 0の記録

- `Packages/manifest.json` は `#0.2.1` タグで `net.sbm.unite` と `net.sbm.unite.text` を参照する（PackageCache `net.sbm.unite@9304386ff376`、`net.sbm.unite.text@2750d0f0c429`）。このリポジトリでは `Packages/` はNuGet用の `.gitignore` 規則で除外されていたため、2026-11-17に `manifest.json` と `packages-lock.json` だけを追跡対象に戻した。
- 0.2系のUCanvasは `CreateCanvas(targetCamera)` で1つのCameraに描画し、`LateUpdate` でText拡張のアトラス更新（`FrameCapturing`）を発行する。派生クラスでライフサイクルを置き換えるとTextが動かないため、置き換えない。
- Editorでは、Scene Viewにフォーカスすると描画先がScene View Cameraへ切り替わる。出力用Canvasではシリアライズ専用の `_drawInSceneView` をfalseにする。
- 確認：EditModeテスト225件成功。ShapeSynthSmokeTestをPlayし、出力RT（1920×1080）への図形描画とエラーなしを確認。

## Core API（Phase 1–2）

1. 配置先を作る：`LayoutFrame.SafeArea(margin)` → `Rect2`。`LinePath.AlongEdge(rect, edge)`、`ArcPath.Circle(center, radius)`、`RectPerimeterPath(rect)`、`GridDistributor.Distribute(rect, columns, rows)`。
2. 文字の位置を決める（どちらか）：
   - スロット方式：`SlotDistributor.Distribute(path, count, SlotSpacing)` で等間隔に1文字ずつ配置する。`Ends` は開いたパスの両端に最初と最後の文字を置く。`Cells` はパスを等分したセルの中央に置く。閉じたパスでは継ぎ目に文字が重ならない。
   - 流し込み方式：`FlowDistributor.Flow(path, advances, FlowOptions, results)` で、各文字の送り幅で並べる。`Justify` は開いたパスの両端に箱を接し、閉じたパスでは継ぎ目の間隔をほかの字間と同じにする。`FlowFill.WholeText` は文字列を丸ごと繰り返し、ループの継ぎ目でも単語が切れない。`InkFlush` は最初と最後の字形（送り幅の箱ではなく描かれた形）をパスの端に合わせる。開いたパスから字形がはみ出す文字は、既定で除く（`KeepOverflow`）。字間カーニングは適用しない。
3. 描画位置にする：`GlyphPlacer.Place(slot, cluster, metrics, PlacementOptions)` → `GlyphPlacement`（ペン原点・回転・サイズ）。描画側は原点へ移動・回転し、左・ベースライン揃えで1文字ずつ描くだけでよい。
   - 横の基準：`AdvanceCenter`（送り幅の中央）／`InkCenter`（字形の中央）。
   - 縦の基準：`Baseline`（下の辺に載せる）／`CapTop`（上の辺から大文字を吊るす）／`EmBottom`（ディセンダーを下の辺に載せる）／`EmCenter`／`CapCenter`（既定。行内でベースラインを共有したまま、大文字の中心を揃える）／`InkCenter`（字ごとに中心を揃える。ベースラインは揃わない）。
   - 回転：`FollowPath`（パスの向き）／`Upright`。`RotationOffsetDegrees` で各字を基準点まわりに回す。
   - `IGlyphMetrics` はサイズ1の値を返す。描画Adapterがフォントから実測し、テストでは固定値を使う。

## Unity層（Phase 3）

- `TypeLayoutCanvas`：1つのCameraへ描くUCanvas（ScreenOverlay、Y下）。Scene Viewへの描画切り替えを無効にする。
- `TypeLayoutFont`：StreamingAssetsのフォントをAwakeで同期読み込みし、OnDestroyでアトラスを解放する。デスクトップ向け。Android/WebGLでは非同期読み込みが必要。
- `UniteGlyphMetrics`：`IGlyphMetrics` の実装。1文字ごとに一度だけ実測してキャッシュする。単一文字の送り幅はフォントの値を直接読む（行幅の計測では末尾の空白が除かれるため）。字形の範囲は `TextBounds`、キャップハイトは "H" の字形の上端で測る。
- `UniteTextRenderer`：`GlyphPlacement` ごとに Push → Translate → Rotate → 左・ベースライン揃えで描画 → Pop。
- `UniteGuideRenderer`：矩形・パス・基準点のガイド線。検証用。
- `RectPerimeterPath` の `cornerGap`：各辺を角の手前で止め、2辺の文字帯が角で重ならないようにする。

### 検証（2026-11-21、Flicker）

- EditModeテスト：`Common/Tests/Editor/RhythmTests` 21件成功。`Rhythm.GetPhase` が `2×周期番号+進み` を返していた不具合、`GetIndex` が1未満の間隔を使えなかった点、長さ0での0除算を修正した（修正前は呼び出し元なし）。
- Performance SceneをPlayし、Flickerだけを表示した出力RT（1920×1080）を時間をずらして3枚保存し、各ブロックがずれた段階で現れ・消えること、ハイライトとマーカーが描かれることを確認した。Console Error・パターンのErrorなし。確認後にパターンのON/OFFを元に戻した。
- 重なり回避の追加後、出力RTを時間をずらして4枚保存し、ブロック同士が重ならないことを確認した。既定の18ブロックのうち、位置が見つかって表示されたのは15〜16個だった。
- 未確認：最終的な見た目の評価、Prefs保存。

### 検証（2026-11-17）

- EditModeテスト：TypeLayout 37件成功。
- `TypeLayoutSmokeTest.unity` をPlayし、4ケースの出力RT（1920×1080）を保存して目視確認。Console Errorなし。
- ガイド線なしのAlignmentで字形の外接矩形を画素から計測した。左上ラベルの左端は40、右上ラベルの右端は1880で、セーフエリア（40〜1880）と一致した。"CENTER" の縦方向の中心は540。上下の1px未満のずれは "O" などのオーバーシュート。
- 未確認：出力解像度やアスペクト比の変更時の見た目、Syphonでの受信、最終的な見た目の評価。

## 動き（Phase 4）

- `TextPicker.Random(clusters, count, seed, step, results)`：(seed, スロット, ステップ) のハッシュ（SplitMix64）で各スロットの文字を選ぶ。同じ入力なら環境によらず同じ結果になり、スロットの結果は後続のスロット数に依存しない。ステップを変えると全スロットを選び直す。
- `BeatSteps.Step(beats, beatsPerStep)`：拍の境界ちょうどで切り替わるステップ番号。`BeatSteps.Scroll(beats, pixelsPerBeat)`：拍あたりの速さによるスクロール量。
- `FlowOptions.Offset`：閉じたパスでは周回する。開いたパスで繰り返し（`FlowFill.Glyphs` / `WholeText`）を使う場合は、1回分の周期で折り返す。前後に1回分ずつ足して両端に隙間が出ないようにし、パスからはみ出した文字はクリップで除く。
- 時計は `IBPMManager.Beat`（累積の拍数）。拍ごとのステップは見た目の切り替えだけに使い、Voiceの寿命のような予約タイミングは扱わない。

## 演奏用コンポーネント（Phase 5）

`Assets/Krab/Modules/TypeLayout/Samples/TypeLayoutPerformance.unity` を開いてPlayする。設定UIの「TypeLayout」タブ → Performance → Type Layout から操作し、Saveボタン（⌘S）でPrefsに保存する。

配置と動きはパターンごとにコードで書く（`Runtime/Performance/Patterns/`、17種類）。UIから変えられるのは各パターンのON/OFFとパラメータ（文字列・サイズ・速さなど）だけで、配置そのものは変えない。新しいパターンは `TypePattern` を継承し、`CreateParameters()` と `DrawPattern()` を書いて `TypeLayoutPerformance` の一覧に加える。

| パターン | 配置と動き | パラメータ |
|---|---|---|
| Frame Loop（既定でON） | セーフエリアの外周を繰り返し文字が内側に吊られて回る。角は空ける | Text, Size, Speed |
| Corners | セーフエリアの四隅に字形を揃えたラベル。`{bpm}` `{beat}` を置き換える | 4か所の文字列, Size |
| Ticker | 横の帯が交互の向きに流れる | Text, Size, Rows, Speed |
| Columns | 縦の列が列ごとに異なる速さで落ちる | Text, Size, Columns, Speed |
| Cross | 中心を通る縦横に単語を均等配置。中心の文字を共有し、ステップごとに1文字ずれる | Text, Size, Beats Per Step |
| Wave | 中央を進む正弦波に沿った文字列（`PolylinePath`） | Text, Size, Amplitude, Wavelength, Cycles Per Beat |
| Orbit | 中心の二重リングが逆向きに回る。外は外向き、内は内向き | Outer/Inner Text, Size, Radius, Speed |
| Letter Grid | 出力の縦横比に合わせたグリッドに1文字ずつ。拍でランダムに入れ替わる | Letters, Size, Rows, Beats Per Change, Seed |
| Spiral | 中心から外へほどける渦巻き（`PolylinePath`）に沿って外向きに流れる | Text, Size, Turns, Speed |
| Diagonal | 画面全体を横切る平行な斜めの帯。交互の向きに流れる。帯は画面の矩形で切り取る | Text, Size, Lines, Angle, Speed |
| Zigzag | 中央を横切るジグザグに沿って流れる。歯の頂点で向きが急に変わる | Text, Size, Teeth, Height, Speed |
| Big Letter | 中央に1文字を大きく表示し、字形の中心を画面中心に合わせる。ステップごとに次の文字 | Text, Size, Beats Per Step |
| Radial | 中心から放射状に外向きに読むスポーク。ゆっくり回転する | Text, Size, Spokes, Inner Radius, Degrees Per Beat |
| Dial | 円周上に等間隔の正立した文字（時計の文字盤）。ステップごとに1つずつ回る | Text, Size, Radius, Beats Per Step |
| Scatter | 単語を4×8のセルへ重複なくランダムに置く（正立か逆さ）。拍で選び直す | Text (words), Size, Count, Beats Per Change, Seed |
| Tunnel | セーフエリア内の入れ子の矩形。隣の輪と逆向きに回る | Text, Size, Rings, Gap, Speed |
| Flicker | 複数行（`\|` 区切り）のブロックを散らし、各ブロックが1文字ずつ現れ・保持し・先頭から消える。ブロックごとの開始のずれは `Rhythm.GetPhase` にseed付きオフセットを足して作る。単語の背景色は `Rhythm.GetIndex` のステップごとにランダムに切り替わる。背景は5段階のグレー（白・80%・60%・40%・20%）、文字色はグレースケール（通常は設定色のグレー、明るい背景では黒、暗い背景では白）。周期が変わったブロックだけ位置を選び直し、他のブロックが使っている位置と重ならない場所をseed付きで探す（`FlickerPlacer`）。空きがなければその周期は表示しない | Text (lines), Size, Blocks, Beats Per Cycle, Hold, Flicker Beats, Highlight Chance, Seed |

UIは「Patterns」「Words」「Display」の3タブに分けた。Patternsタブには、全OFFボタン・ON数の表示と、パターンごとに1行（ON/OFFのトグルとEditボタン）を並べる。Editを押すと、そのパターン専用の小ウインドウ（説明、Enabled、パラメータ、警告）が開く。値が範囲外で描画できないときは、Editボタンが「Edit !」になる。Displayタブには、表示、色、セーフエリアの余白、ガイド線を置く。範囲外の値でレイアウトが作れない場合は、そのパターンだけ描画を省き、ウインドウ内に警告を出す。速さはすべて拍基準で、BPM（Common/Timing）に同期する。

### Words（表示する文字列の一括編集）

各パターンの文字列はテンプレートで、`{main}` などの記号をWordsタブの値に置き換えて表示する。パターン側の文字列を直接書き換えれば、そのパターンだけ固定の文字にもできる。

| 記号 | 値 |
|---|---|
| `{main}` / `{sub}` | 単語リストからMain・Subに選んだ単語（既定：KIMURA / Middle Model） |
| `{word1}` `{word2}` … | リストの番号の単語 |
| `{words}` / `{list}` | 全単語を空白区切り / `|` 区切り（Scatterは `|` で項目に分ける） |
| `{event}` | イベント名 |
| `{time}` / `{date}` | 現在時刻と日付。書式は .NET の日時書式（既定 `HH:mm:ss` / `yyyy.MM.dd`）。不正な書式は既定の書式で表示し、警告を出す |
| `{bpm}` `{bar}` `{beat}` `{position}` | テンポ、4/4での小節・拍、`小節.拍` |

`:upper` / `:lower` を付けると大文字・小文字にする（例 `{main:upper}`）。知らない記号は書いたまま表示する。展開処理はCoreの `TextTemplate`（テストあり）、値の管理は `TypeWords`。値が変わったフレームだけ結果を作り直し、変わらない文字列は同じインスタンスを返して描画側の計測キャッシュを保つ。

| パターン | 既定のテンプレート |
|---|---|
| Frame Loop, Columns, Letter Grid, Tunnel | `{main}` |
| Cross, Diagonal, Big Letter, Radial | `{main:upper}` |
| Orbit | 外 `{sub} ` / 内 `{main} ` |
| Ticker | `{sub:upper} ` |
| Wave, Spiral, Zigzag | `{sub} ` |
| Corners | `{event}` / `{time}` / `{date}` / `{bpm} BPM  {position}` |
| Scatter | `{list:upper}` |
| Dial | `0123456789AB` |
| Flicker | `{main:upper} * {sub:upper}\|{bpm} BPM * {time}\|{date}` |

### 検証（2026-11-17）

- EditModeテスト：TypeLayout 47件成功（Words追加時の `TextTemplateTests` 3件を含む、全体272件）。
- Words：Play中に `{main}` `{sub:upper}` `{event}` `{time}` `{date}` `{bpm} BPM {position}` `{list}` `{word3:lower}` の解決結果を確認し、Frame Loop・Ticker・Cornersの出力RTで表示を目視確認した。WordsタブのUI操作は未確認。
- 16パターンを1つずつ単独で表示した出力RTと、全パターンONの出力を保存して目視確認した。Console Errorなし、各パターンのErrorなし。
- Play中にスクリプトを再コンパイルしても、`TypeLayoutPerformance` と `TypeLayoutFont` が次のフレームで初期化し直すことを確認した（以前はNullReferenceExceptionになった）。
- Performance SceneをPlayし、8パターンを1つずつ単独で表示した出力RTを保存して目視確認した。全パターンON時もConsole Errorなし、各パターンのErrorなし。スクロールとLetter Gridの入れ替えが拍に合わせて動くことを、時間をずらした画像で確認した（パターン化前の版で確認し、同じCoreの動きを使う）。
- 設定UIに「TypeLayout」タブが出ることを画面キャプチャで確認した。UIの実操作（各項目の編集・Save）は未確認。
