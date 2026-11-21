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
| 1 | Core：自然順、セット/クリップ、切り出し矩形→元座標、フィット、コマ選択。EditModeテスト | 未着手 |
| 2 | Unity層：走査と非同期読み込み、トリミング、検証結果の一覧、`ImageCanvas`、Duotoneシェーダー、検証Scene | 未着手 |
| 3 | 演奏用パターン（列で行進、敷き詰め、散らす、中央に大きく、パスに沿う）、UI/Prefs/BPM | 未着手 |
| 4 | 共通パレット、背景パターンとの統合 | 未着手 |
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
