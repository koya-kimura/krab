# ImageLayer：切り抜き画像・連番の描画システムの実装計画

2026-11-22。切り抜き画像（透過PNG）とPNG連番を、背景の図形や文字と重ねて演奏するための仕組み。参考は #マルチネ20 のVJ（p5.js、`~/Documents/develop/p5/Maltine-Records`）。TypeLayoutの計画で「写真の切り抜きは別パッケージ」とした部分にあたる。

## 前提

- p5.Unite 0.2.1は画像描画を持たない。上流のAPI一覧（`references/api-checklist/image.md`）でもImageは0/39。内部のテクスチャ描画はText拡張のグリフ用で、公開APIではない。そのため描画はKrab側で行う。
- 素材は事前に切り抜いた透過PNG。実行時の背景除去は対象外。クロマキーは将来の候補。
- GIF・動画はPNG連番に変換して使う。VideoPlayerは必要になってから検討する。
- 素材はStreamingAssetsから実行時に読み込む（TypeLayoutのフォントと同じ）。公演ごとにUnityのインポートなしで入れ替えられる。

## 旧VJの問題と対応

| 旧VJ | 問題 | 対応 |
|---|---|---|
| 枚数・コマ数をコードに直書き（`{name, animationCount, framesPerAnimation}`） | ファイルの増減でコード修正が必要。ずれるとwarnだけで黙ってコマが欠ける | フォルダ構成を定義とし、起動時に走査する。欠番・サイズ不一致・読み込み失敗は読み込み完了時に一覧で報告する |
| 呼び出し側が毎フレーム文字列と番号で指定（`getImage("walk", 2, t)`） | 綴り違いが本番中の例外になる | 準備時にハンドルを取得し、存在しない名前をそこで検出する。UIは走査結果からセットを選び、Prefsには名前で保存する |
| 全フレームが全画面サイズ（1280×720×732枚、静止画1920×1080） | 展開すると約2.7GB。実際の絵はwalkで約22〜24%、handで約64% | 読み込み時に不透明範囲だけを切り出し、元サイズと切り出し矩形を保持する |

全画面PNGには「キャンバス上の位置や足元の基準が焼き込まれ、コマ間でぶれない」という利点がある。描画時に元の座標へ補正し、元のPNGをそのまま置いた場合と同じ見た目にして、この利点を残す。連番では全フレームの外接矩形も持ち、「絵の範囲に合わせる」配置でコマごとに大きさが揺れないようにする。

## 素材の置き方

```text
Assets/StreamingAssets/Krab/ImageLayer/
  <ライブラリ>/        公演や素材群の単位（例：Maltine20）。ライブラリ単位で読み込む
    animal/            PNGが直接並ぶ：静止画のセット（1枚＝1クリップ）
      01.png 02.png …
    walk/              サブフォルダが並ぶ：連番のセット（サブフォルダ1つ＝1クリップ）
      01/ 001.png … 040.png
      02/ …
```

- ファイル名・フォルダ名はゼロ詰めの連番にし、どのツールでも名前順＝再生順になるようにする。読み込み側は自然順（1, 2, 10）でも並べる。
- 静止画は1フレームのクリップとして扱い、呼び出し側は静止画も連番も同じAPIで描く。
- PNGはGit LFSで管理する（`.gitattributes`）。`.meta` は通常のGitで管理する。
- 縮小はしない。最大は1920×1088（hand/03〜05）で、1080p出力で画面いっぱいに使えるサイズのため。

### Maltine20（2026-11-22に旧VJから移行）

`~/Documents/develop/p5/Maltine-Records/public` の `image/`（静止画）と `animation/`（連番）を、上の構成へ並べ替えてコピーした。中身は無変更（md5一致）。754枚、約204MB。

| セット | 種類 | クリップ数 × コマ数 | 解像度 |
|---|---|---|---|
| animal / building / human / life / noface | 静止画 | 3 / 2 / 5 / 8 / 4 | 1920×1080 |
| car | 連番 | 1 × 100 | 1280×720 |
| dance | 連番 | 4 × 40 | 1280×720 |
| dothand | 連番 | 3 × 24 | 960×960（アルファは2値） |
| hand | 連番 | 5 × 40 | 01〜02：1280×720、03〜05：1920×1088 |
| step | 連番 | 1 × 40 | 1280×720 |
| walk | 連番 | 4 × 40 | 1280×720 |

## image()風の描画API

座標はTypeLayoutと同じく出力ピクセル、左上原点、X右・Y下。

```csharp
img.ImageMode(ImageMode.Center);
img.Push();
img.Translate(x, y);
img.Scale(1.3f);
img.Duotone(palette.Dark, palette.Light);   // tint()の拡張：白黒化して2色を割り当てる
img.Image(walk[clipIndex], 0, 0, frame: walk.FrameAt(progress));
img.Pop();
```

- `FrameAt(progress)`（0〜1）と、拍に合わせる `FrameAtBeat(beat, framesPerBeat)` でコマを選ぶ。
- 内部では1フレーム分の描画命令を集め、インスタンス描画する。利用側はMesh・Materialを扱わない。
- p5.Uniteに `image()` が入った場合、この層の中身を差し替える。
- クロマキーは、Duotoneと同じシェーダーに後から追加できるようにしておく。

## 構成（予定）

```text
Assets/Krab/ImageLayer/
  Scripts/Core/         Krab.ImageLayer.Core（noEngineReferences）自然順、セット/クリップ構造、切り出し→元座標、フィット、コマ選択
  Scripts/Unity/        走査・非同期読み込み、トリミング、ImageCanvas、シェーダー
  Scripts/Performance/  演奏用パターン、RosettaUI、Prefs、BPM（Assembly-CSharp）
  Samples/ / Tests/Editor/
```

配置の計算は、Performance層からTypeLayout Coreの `GridDistributor`、`ILayoutPath`、`BeatSteps` を再利用する。CoreとUnity層は `Krab.Common` やTypeLayout、ShapeSynthを参照しない。

## 段階

| Phase | 内容 | 状態 |
|---|---|---|
| 0 | 重ね順の方式（Camera Stack／Render Queue）の検証。旧VJ素材の一部で読み込み時間とメモリを実測 | 完了 |
| 1 | Core：自然順、セット/クリップ、切り出し矩形→元座標、フィット、コマ選択。EditModeテスト | 完了 |
| 2 | Unity層：走査と非同期読み込み、トリミング、検証結果の一覧、`ImageCanvas`、Duotoneシェーダー、検証Scene | 完了 |
| 3 | 演奏用パターン（列で行進、敷き詰め、散らす、中央に大きく、パスに沿う）、UI/Prefs/BPM | 完了 |
| 4 | 共通パレット、背景パターンとの統合 | 取り下げ（2026-11-22、ImageLayerは画像とアニメーションの管理に絞る） |
| 5 | MIDI割り当て、クロマキー | 未着手 |

## Phase 0の記録（2026-11-22）

### 重ね順：レイヤーごとにCameraを分け、URPのCamera Stackで重ねる

p5.UniteはCanvasごとに描画のsort distanceを `1000 - 0.01 × 描画順` で振り直す（`DrawingContext.CreateRenderParams`）。同じCameraに複数のCanvasや別の描画を載せると、距離が重なって前後が決まらない。Render Queueも、UCanvasのMaterialが非公開のため変えられない。

検証Scene `Assets/Krab/ImageLayer/Samples/ImageLayerLayerOrder.unity`（`LayerOrderProbe`）で、背景（UCanvasの赤い矩形）→画像（クアッドの緑）→前景（UCanvasの青い円）を描き、出力RTの3点を読み戻して判定した。

| 方式 | 結果 |
|---|---|
| Base Camera（背景）＋Overlay Camera（画像）＋Overlay Camera（前景）のCamera Stack | 30/30フレームで正しい順 |
| すべて1つのCamera | 0/30。画像が前景の円を覆う |

採用：レイヤーごとにCameraを分け、出力Camera（`OutputTextureProvider` のCamera）をBaseとし、画像や文字のレイヤーをOverlayとしてStackに積む。UCanvasは `CreateCanvas(targetCamera)` で各レイヤーのCameraへ描く。Overlay Cameraは出力先RTをBaseから引き継ぐので、Preview・Syphon/Spoutの経路は変えない。Consoleのエラー・警告は0件。

### 読み込み時間とメモリ

旧VJ素材（754枚）をPythonで計測した結果、RGBA8展開時で全画面3.20GB、クリップごとの外接矩形で1.64GB、フレームごとの切り出しで0.97GB。外接矩形はcar（大きく移動するクリップ）でまったく縮まないため、フレームごとの切り出しを採用する。

Editor（Metal、Apple Silicon）で `ImageConversion.LoadImage` → 不透明範囲の切り出し（4の倍数に揃える）→ `Texture2D.Compress`（DXT5）を計測した。1フレームあたり：

| 素材 | 読み込み | デコード | 切り出し | 圧縮 | 切り出し後 | 圧縮後 |
|---|---|---|---|---|---|---|
| walk/1（40枚、1280×720） | 0.4ms | 4.8ms | 1.6ms | 1.1ms | 31MB | 8MB |
| hand/3（40枚、大きい解像度） | 0.4ms | 8.8ms | 2.8ms | 2.1ms | 56MB | 14MB |
| car/1（100枚） | 0.4ms | 5.0ms | 1.2ms | 2.2ms | 143MB | 36MB |
| image/human（5枚、1920×1080） | 0.4ms | 15.8ms | 3.4ms | 3.6ms | 11MB | 3MB |

1フレーム約10msで、754枚では8秒前後の見込み。圧縮後は切り出し時の約1/4（全体で約0.25GBの見込み）。デコードはメインスレッドで行うため、Phase 2では複数フレームに分けて読み込み、進み具合を表示する。DXT5圧縮による画質の劣化は目視未確認。圧縮のON/OFFをセット単位で選べるようにする。

## Core API（Phase 1、2026-11-22）

`Krab.ImageLayer.Core`（`noEngineReferences`）。座標は、元画像では左上原点のピクセル（`PixelRect`、int）、出力では左上原点のピクセル（`DrawRect`、double）。

| 型 | 役割 |
|---|---|
| `NaturalOrder` | 数字の並びを値で比べる（2 < 10）。値が同じならゼロ詰めの多い方を後にし、全順序にする |
| `ManifestBuilder.Build(name, relativePaths)` | ライブラリ直下からの相対パス一覧から `LibraryManifest` を作る。ファイルの走査はUnity層が行い、Coreはパスだけを扱う |
| `LibraryManifest` / `SetManifest` / `ClipManifest` | セット（`SetKind.Stills`／`Sequences`）とクリップ、フレームのパス。`TryGetSet(name)` は大文字小文字を区別する |
| `ManifestIssue` | 読み込み時の問題。`Warning`：読み込むが意図と違いそう（PNG以外、置き場所の誤り、番号の欠け・重複）。`Error`：その部分を読み込まない（1つのセットに静止画とクリップが混在） |
| `AlphaBounds.TryFind(rgba32, w, h, rowsBottomUp, out rect, threshold)` | 不透明範囲を探す。Unityの下から上の行順は `rowsBottomUp` で左上原点へ直す |
| `TrimmedFrame` | 元のキャンバスサイズと切り出し範囲。空なら描画しない |
| `ImagePlacement.Resolve(mode, a, b, c, d)` | p5の `imageMode`（Corner／Center／Corners）で、元キャンバス全体の描画矩形を求める |
| `ImagePlacement.MapContent(canvasRect, frame)` | 元キャンバスを canvasRect に描いたときの、切り出し部分の矩形 |
| `ImagePlacement.Fit(box, srcW, srcH, reference, Contain/Cover, alignX, alignY)` | reference（元キャンバス全体か、クリップの外接矩形）を box に収める元キャンバスの矩形。外接矩形を渡すと、コマごとに大きさが揺れない |
| `FrameSelector.FromPosition(frames, count, wrap)` / `FromProgress(progress, count, wrap)` | コマ番号。`Clamp`／`Loop`／`PingPong`（両端を繰り返さない）。progressは1で1往路。拍に合わせるときは beat × framesPerBeat を位置として渡す |

ゼロ詰めでない名前（1, 2, 10）も自然順で読む。セットの構成判断（静止画か連番か）はフォルダの深さだけで決める。

検証：EditModeテスト342件成功（ImageLayer 32件）。境界（Clamp・Loop・PingPongの負の位置と端、1フレームのクリップ、完全透明・完全不透明の画像、行順の反転、空の参照範囲）を含む。

## Unity層（Phase 2、2026-11-22）

`Krab.ImageLayer.Unity`（Core＋URP Runtime）。

### 読み込み

`ImageLibraryLoader.LoadAsync(libraryName, options, onProgress, cancellationToken)`（`Awaitable`）。`StreamingAssets/Krab/ImageLayer/<libraryName>` を走査し、`ManifestBuilder` で構成を作り、1フレームずつデコードする。`FrameBudgetMilliseconds`（既定12ms）を使い切るたびに次のUnityフレームへ譲るので、読み込み中も出力は止まらない。

1フレームの処理（`FrameDecoder`）：PNGを読み、不透明範囲を探し、その範囲だけを4の倍数に揃えたテクスチャへ写す。このとき色をアルファで乗算済みにする（拡大縮小時に透明部分の黒がにじまないように）。既定でミップマップを作り、DXT5に圧縮する。完全に透明なフレームはテクスチャを持たず、描画もしない。

読めないPNGはError、クリップ内でサイズが異なるフレームはWarningとして `ImageLibrary.Issues` に加え、読み込みは続ける。キャンセル時は読み込み済みのテクスチャを破棄する。

| 型 | 役割 |
|---|---|
| `ImageLibrary` | セットの一覧と問題、メモリ量、読み込み時間。`Get(name)` は無い名前で例外（準備時に気づくため）、`TryGet` もある。`Dispose` で全テクスチャを破棄 |
| `ImageSet` | `ClipCount`、添字、`Wrap(index)`（範囲外も折り返す。カウンターや乱数でそのまま選べる） |
| `ImageClip` | `Frames`、`ContentBounds`（全フレームの外接矩形）、`AtProgress(progress, wrap)`、`AtPosition(frames, wrap)` |
| `ImageFrame` | テクスチャと `TrimmedFrame`、UVの範囲 |

### 描画

`ImageCanvas`（p5風、出力ピクセル・左上原点・Y下、角度は度で時計回り）。毎フレームUpdateから呼ぶ。

- 状態：`Push` / `Pop` / `Reset`、`Translate` / `Rotate` / `Scale`、`Mode`（`ImageMode`）、`Tint`、`Duotone(dark, light, amount)`、`Threshold(level, softness)`（Duotoneの2色を明度で切り分ける）。
- 描画：`Image(frame, x, y)`（元のサイズ）、`Image(frame, a, b, c, d)`（Modeに従う矩形に元キャンバス全体を描く）、`ImageFit(clip, frame, a, b, c, d, fit, alignX, alignY)`（クリップの外接矩形を箱に収める。コマで大きさが揺れない）。
- 呼んだ順に重なる。Transparentのソートは描画の境界で決まるため、p5.Uniteと同じく呼び出しごとに少しずつCameraへ近づける。

`ImageLayerCanvas`（Cameraに付けるComponent）が `ImageCanvas` を持つ。シェーダー参照をシリアライズしてビルドに含める。`OverlayLayerCamera` は自分のCameraを出力CameraのStackに `Order` 順で挿入する（GameObjectのOnEnable順に依存しない）。

### 向き

p5.Uniteのスクリーン描画は、出力テクスチャ上で「y=0が上」になる。画像のシェーダーもこれに合わせ、ピクセル座標（Y下）をそのままクリップ座標へ写す（反転しない）。検証Sceneの `Orientation Check` で、3つの層の目印が同じ左上に重なることを読み戻しで確認した。

### 検証（2026-11-22）

`Assets/Krab/ImageLayer/Samples/ImageLayerSmokeTest.unity` をPlay（構成はSamplesのREADME）。

- Maltine20：11セット、754フレーム、問題0件、テクスチャ約331MB（DXT5＋ミップマップ）、読み込み約11秒。読み込み中も60fpsで描画が続く。
- 重ね順：背景（p5.Unite）→画像→前景（p5.Unite）の順で重なる。
- 向き：Orientation Checkで、背景・画像・前景の目印が同じ左上に出る。
- Consoleのエラー・警告0件。出力の画像を `Reports/ImageLayerSmoke.png` に保存した。
- 未確認：見た目の最終評価、DXT5圧縮による画質（目視）、Syphon/Spoutの受信側。

## 演奏用コンポーネント（Phase 3、2026-11-22）

`Assets/Krab/ImageLayer/Samples/ImageLayerPerformance.unity` を開いてPlayする（操作はSamplesのREADME）。`ImageLayerPerformance`（Assembly-CSharp、`Scripts/Performance`）はTypeLayoutPerformanceと同じ作りで、配置と動きはパターンごとにコードで書き、UIからはON/OFFとパラメータだけを変える。BPM（Common/Timing）に同期し、注入がなければ120BPMで動く。

全パターン共通のパラメータ：Set（読み込んだライブラリのセットから選ぶドロップダウン。再読み込みで選択肢を作り直す）、Look（Original／Duotone／Threshold。色はDisplayタブのDark／Light）、Frames Per Beat（連番の再生速度。拍に同期）。Setが見つからない、または値が範囲外の場合は、そのパターンだけを描かずにEditウインドウへ理由を出す。

| パターン | 既定のセット | 配置と動き | パラメータ |
|---|---|---|---|
| March（既定でON） | animal | 1つのクリップを横に並べた列が行進する。列ごとに向きを交互にできる | Clip, Size, Rows, Cells Per Beat, Position, Alternate |
| Lineup | walk | セットのクリップを横に並べ、1本の基準線に立たせて連番を再生する | Count, Size, Baseline, Stagger |
| Tile | noface | グリッドのセルごとにseed付きでランダムなクリップ。拍で選び直す | Columns, Rows, Beats Per Change, Fill, Seed |
| Scatter | life | ランダムな位置と角度に散らす。拍で選び直す | Count, Size, Beats Per Change, Spin, Seed |
| Fall | life | 回転しながら落ちる（または昇る）。通過ごとに位置とクリップを選び直す | Count, Size, Speed, Spin, Rise, Seed |
| Center | human | 中央に大きく1つ。足元を支点に揺れ、拍で次のクリップへ | Size, Beats Per Change, Swing, Beats Per Swing |
| Mirror | animal | 中央線をはさんで鏡像と向かい合う。拍で呼吸する | Clip, Size, Gap, Breathe |
| Orbit | life | 中心の周りの円に等間隔で並べ、拍で回る（画像は正立） | Count, Radius, Size, Degrees Per Beat |
| Trail | hand | 1つのクリップが8の字を描き、遅れたコピーが小さく薄くなりながら後を追う | Clip, Size, Copies, Lag, Beats Per Loop, Fade |
| Tunnel | noface | 中心から入れ子に並べたコピーが外へズームし続ける。層ごとにクリップは保たれる | Layers, Size, Ratio, Beats Per Layer, Twist, Seed |
| Wave | dance | 横一列のクリップが順番に上下し、ウェーブになる | Count, Size, Amplitude, Beats Per Wave, Phase Step, Position |
| Spiral | animal | ひまわりの種の配置（黄金角）で中心から並べ、外ほど大きく、拍で回る | Count, Size, Spread, Degrees Between, Degrees Per Beat, Grow |
| Flash | hand | ステップごとに位置・大きさ・クリップ・左右反転が切り替わる。短くするとストロボ | Count, Size, Size Spread, Beats Per Change, Random Flip, Seed |
| Pile | life | ステップごとに1つずつランダムな位置と角度で積み重なり、満杯で消えて最初から | Max Count, Size, Beats Per Item, Tilt, Seed |
| Columns | building | 縦の列が交互に上下へスクロールする。セルごとにクリップは保たれる | Columns, Cell Height, Cells Per Beat, Fill, Seed |
| Bounce | human | 横一列のクリップが拍で跳ね、着地でつぶれる。Staggerで跳ねるタイミングをずらす | Count, Size, Jump, Beats Per Bounce, Stagger, Floor |
| Radial | dothand | 1つのクリップを中心の周りに外向きに並べる。輪が回り、拍で膨らむ | Clip, Count, Radius, Size, Degrees Per Beat, Pulse |
| Parade | walk | 奥行きの違う群衆が横切る。遠いほど小さく高く遅い。左右両方向に歩ける | Count, Size, Speed, Horizon, Both Ways, Clips Face Left, Seed |

2026-11-22に10パターン（Trail〜Parade）を追加し、計18パターン。Performance SceneのPlay中に18パターンを1つずつONにして出力を保存し（`Reports/ImageLayerPattern-*.png`）、全パターンでError無し、Consoleのエラー・警告0件を確認した。見た目の評価と各パラメータの操作感は未確認。

乱数はTypeLayoutの `TextPicker.Hash`（seed・スロット・ステップ）を、ステップは `BeatSteps.Step` を使う。

### Overlay Cameraのサイズ

Overlay Cameraは自分の出力先を持たないため、`pixelWidth/Height` がGame Viewのサイズ（Retinaでは出力の2倍）を返していた。描画はRTのサイズで行われるので、Overlay Cameraからサイズを取るTypeLayoutや画像が、左上だけを拡大した絵になった。`OverlayLayerCamera` は出力Cameraの `targetTexture` を毎フレーム（`DefaultExecutionOrder(-1000)`）自分にも設定し、サイズを出力に揃える。描画先はStackのBaseのまま。

### 検証（2026-11-22）

- 8パターンを1つずつONにして出力を保存した（`Reports/ImageLayerPattern-*.png`）。全パターンでError無し、Consoleのエラー・警告0件。
- 画像（Order 0）の手前にTypeLayoutの文字（Order 10）が重なる。
- 修正後、Overlay CameraとMain Cameraのサイズがともに1920×1080になることを確認した。SmokeTestのOrientation Checkも引き続き一致する。
- 検証後、パターンのON/OFFを既定（MarchだけON）に戻した。
- 未確認：UIの操作感（ドロップダウンの作り直し、Editウインドウ）、Prefsの保存と復元、MIDIでの操作、見た目の最終評価。

## パレットと背景（Phase 4、取り下げ）

2026-11-22にパレット（`PaletteController`／`ColorPalette`）と背景（`BackgroundPerformance`）を実装したが、ユーザーの判断で削除した。ImageLayerは画像と連番アニメーションの管理・描画だけを受け持ち、背景は扱わない。DuotoneとThresholdの色は `ImageLayerPerformance` のDisplayタブ（Dark／Light）で決める。実装は `e9c7e2b` に残っている。

### p5.Uniteの色が明るく出る問題

プロジェクトはLinear色空間で、出力RTはsRGB（`R8G8B8A8_SRGB`）。p5.Uniteは `Fill` / `Stroke` の色をリニアへ変換せずに書き込むため、出力では指定よりも明るくなる（例：(0.85, 0.22, 0.20) が (0.93, 0.51, 0.49) になる）。`color.linear` を渡せば指定どおりになる。画像のシェーダーは `MaterialPropertyBlock.SetColor` 経由でUnityが変換するので影響しない。TypeLayout・ShapeSynthは見た目を確認のうえ、現状のまま（2026-11-22、ユーザー確認済み）。

## 確認・相談事項（2026-11-22時点）

1. 起動時の読み込み（Maltine20で約11秒）。今は全セットを起動時に読む。セット単位の遅延読み込みや、切り出し・圧縮結果のキャッシュは未実装。
2. Phase 5（MIDI割り当て）はTypeLayoutのPhase 6と同じ横断的な仕組みにする想定で未着手。クロマキーも未着手（2026-11-22の方針どおり）。
