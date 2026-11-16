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
| 2 | 文字幅を考慮した流し込み、閉じたパスの継ぎ目合わせ、基準点（Baseline/Em/Ink）、回転モード | 未着手 |
| 3 | p5.Unite Textでの描画Adapter、フォント読み込み、検証Scene、ガイド線表示 | 未着手 |
| 4 | 文字列の繰り返し・Seed付きランダム、拍単位のスクロールと入れ替え | 未着手 |
| 5–6 | UI・保存、MIDI、性能計測 | 未着手 |

## Phase 0の記録

- `Packages/manifest.json` は `#0.2.1` タグで `net.sbm.unite` と `net.sbm.unite.text` を参照する（PackageCache `net.sbm.unite@9304386ff376`、`net.sbm.unite.text@2750d0f0c429`）。このリポジトリでは `Packages/` が `.gitignore` 対象のため、manifestの変更はコミットに含まれない。
- 0.2系のUCanvasは `CreateCanvas(targetCamera)` で1つのCameraに描画し、`LateUpdate` でText拡張のアトラス更新（`FrameCapturing`）を発行する。派生クラスでライフサイクルを置き換えるとTextが動かないため、置き換えない。
- Editorでは、Scene Viewにフォーカスすると描画先がScene View Cameraへ切り替わる。出力用Canvasではシリアライズ専用の `_drawInSceneView` をfalseにする。
- 確認：EditModeテスト225件成功。ShapeSynthSmokeTestをPlayし、出力RT（1920×1080）への図形描画とエラーなしを確認。
