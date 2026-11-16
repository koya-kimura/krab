# TypeLayout 検証Scene

## TypeLayoutSmokeTest

`Assets/Krab/TypeLayout/Samples/TypeLayoutSmokeTest.unity` を開いてPlayする。出力RT（OutputTextureProvider）とPreview・Syphonの経路はCommonのPrefabを使う。

| キー | 内容 | 確認すること |
|---|---|---|
| 1 | Grid：10列のストライプと10×6の文字グリッド | 各文字がストライプの中央にある（横は字形中心）。行ごとにベースラインが揃う |
| 2 | Frame：セーフエリア外周の繰り返し文字列、内側の縦・横の文字列 | 外周は継ぎ目なく閉じ、角で重ならない。下辺は逆さまに右から左へ読む。横の行は両端の字形が線の端に接する |
| 3 | Circle：外向き・内向きのリング、正立した時計の数字 | リングの継ぎ目の字間がほかと同じ。数字の字形中心が円周上の12等分点にある |
| 4 | Alignment：セーフエリアの各辺に揃えたラベル、縦の基準点の比較 | 字形がセーフエリアの線に接する。Hxgは左から Baseline / CapCenter / EmCenter / InkCenter |
| G | ガイド線の表示切替 | 水色＝セーフエリア、黄＝パス、マゼンタ＝文字の基準点 |

長さは高さ1080の基準ピクセルで書き、出力の高さに合わせて拡大縮小する。Sceneは `Krab/Type Layout/Create Smoke Test Scene` で再生成できる（既存ファイルがある場合は失敗する）。
