# TypeLayout：テキスト配置システムの実装計画

2026-11-17。映像の上に重ねる文字（画面端に沿う文字列、列に揃えた文字グリッド、円周上の文字など）を、ずれなく配置するための仕組み。ShapeSynthと同様に、将来のパッケージ化を見越して層を分ける。写真の切り抜きは別パッケージとし、対象外。

## 構成

```text
Assets/Krab/TypeLayout/
  Scripts/Core/    Krab.TypeLayout.Core（noEngineReferences）配置計算のみ
  Scripts/Unity/   Krab.TypeLayout.Unity：Core + Unite.Runtime + Unite.Text
  Samples/         検証Sceneとデモ（Assembly-CSharp。Krab Commonとの接続はここだけ）
  Tests/Editor/    CoreのEditModeテスト
Assets/StreamingAssets/Krab/TypeLayout/Fonts/  Jost（SIL OFL 1.1、ライセンス同梱）
```

CoreとUnityの層は `Krab.Common` やShapeSynthを参照しない。BPM・DI・RosettaUI・Prefsとの接続はSamplesまたはAssembly-CSharp側に置く。

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
| 4 | 文字列の繰り返し・Seed付きランダム、拍単位のスクロールと入れ替え | 未着手 |
| 5–6 | UI・保存、MIDI、性能計測 | 未着手 |

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

### 検証（2026-11-17）

- EditModeテスト：TypeLayout 37件成功。
- `TypeLayoutSmokeTest.unity` をPlayし、4ケースの出力RT（1920×1080）を保存して目視確認。Console Errorなし。
- ガイド線なしのAlignmentで字形の外接矩形を画素から計測した。左上ラベルの左端は40、右上ラベルの右端は1880で、セーフエリア（40〜1880）と一致した。"CENTER" の縦方向の中心は540。上下の1px未満のずれは "O" などのオーバーシュート。
- 未確認：出力解像度やアスペクト比の変更時の見た目、Syphonでの受信、最終的な見た目の評価。
