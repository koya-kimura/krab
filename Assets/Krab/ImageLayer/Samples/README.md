# ImageLayer Samples

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
