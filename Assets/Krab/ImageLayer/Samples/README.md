# ImageLayer Samples

## ImageLayerPerformance.unity（演奏用）

開いてPlayする。`StreamingAssets/Krab/ImageLayer/Maltine20` を読み込み、画像のパターンを描く。その手前のCameraにTypeLayoutの文字を重ねる。設定UIの「ImageLayer」タブ → Performance → Image Layer から操作し、Saveボタン（⌘S）でPrefsに保存する。

- Patterns：8パターンのON/OFFとEdit（Set、Look、Frames Per Beat、各パターンのパラメータ）。既定ではMarchだけがON。
- Library：ライブラリの選択、圧縮のON/OFF、Reload、読み込み状況（セット数・フレーム数・メモリ・時間）、問題の一覧。
- Display：表示、Duotone／Thresholdで使うDark／Lightの色、Thresholdの明度。

| Camera | Order | 内容 |
|---|---|---|
| Main Camera（出力、Base） | — | 黒でクリアするだけ（背景はImageLayerの対象外） |
| Image Layer Camera（Overlay） | 0 | `ImageLayerPerformance` の画像 |
| Foreground Camera（Overlay） | 10 | `TypeLayoutPerformance` の文字 |

再構築：メニュー Krab > Image Layer > Create Performance Scene（既存のSceneを削除してから）。

## ImageLayerSmokeTest.unity

開いてPlayする。`StreamingAssets/Krab/ImageLayer/Maltine20` を読み込み（約11秒、中央にプログレスバー）、次の3層を重ねて描く。

| 層 | Camera | 内容 |
|---|---|---|
| 背景 | Main Camera（出力、Base） | 黄色の地とグリッド（p5.Unite） |
| 画像 | Image Layer Camera（Overlay、Order 0） | 上段：ペンギンの列（白黒の2値化、拍に合わせて右へ流れる）。中段：walkの4クリップ（元の色、足元を揃える）。下段：lifeの静止画（紺とピンクのDuotone、拍ごとに入れ替わる） |
| 前景 | Foreground Camera（Overlay、Order 10） | ピンクの円（p5.Unite） |

読み込み結果（セット数、フレーム数、メモリ、時間、問題の一覧）はConsoleに出る。`ImageLayerSmokeTest` の `Orientation Check` をONにすると、各層の左上に目印だけを描く（背景＝赤、画像＝緑、前景＝青）。3つが同じ左上に重なれば、層ごとの座標の向きが一致している。

再構築：メニュー Krab > Image Layer > Create Smoke Test Scene（既存のSceneを削除してから）。

## ImageLayerLayerOrder.unity

Phase 0の重ね順の検証。Playすると、Camera Stackと単一Cameraでの重なり順をConsoleに出す。
