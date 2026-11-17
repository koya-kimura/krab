# HSBによる色（Fill / Stroke共通）

2026-11-17に、To Color / Color Mix / Hue Shift による色の変化を廃止した。代わりに、色を **HSBのParameterだけで表す1つの仕組み** にした。FillとStrokeは同じ仕様。

## 仕様

1つの色は次の4項目で決まる。FillとStrokeがそれぞれ別に持つ。

| 項目 | 種類 | 範囲 | 備考 |
| --- | --- | --- | --- |
| Hue | Parameter（角度、deg / turn） | 一周で戻る | 評価後に [0, 360) へ折り返す |
| Saturation | Parameter（Ratio） | 0〜1 | 評価後に0〜1へClamp |
| Brightness | Parameter（Ratio） | 0〜1 | 評価後に0〜1へClamp |
| Alpha | 固定値 | — | Parameterではない。最終的な不透明度は Alpha × Opacity（描画時に0〜1へClamp） |

- 3つのParameterは、ほかのParameterと同じく `Base + Σ Depth × Weight × signal`。BindできてRandomとFlipも効く。
- 色の値の出どころはこの3つのParameterだけ。Inspectorのカラーピッカーは、3つのBaseを読み書きする入力補助。
  - 灰色を選んだときはHueのBaseを、黒を選んだときはSaturationのBaseを変えない。あとで明るい色を選んでも、色相が赤に戻らない。
- 変換はHSV（= HSB）の標準式。`ColorDefinition.ToHsb` / `FromHsb`。

## Core

- `ColorDefinition(hue, saturation, brightness, alpha)`：Fill・Stroke共通の色定義。省略時は白（H 0 / S 0 / B 1）。
- `ColorDefinition.Constant(RgbaColor)`：固定色。HSBへの往復による誤差を出さないよう、元のRGBAをそのまま返す（`ConstantColor`）。
- `ShapeDefinition.FillColor` / `StrokeColor`：`strokeColor` を省略するとFillと同じインスタンスを使う（コードで書く既存プリセットの挙動を維持）。
- `ShapeStyle` は Fill / Stroke のON・OFFとStroke Widthだけを持つ（`StrokeColor` を削除）。
- `DrawItem.FillColor` / `StrokeColor`：評価済みの2色。描画Adapterはそれぞれをそのまま使う。
- Randomのキー：Fill Hue 10（旧Hue Shiftと同じ）、Fill Saturation 11、Fill Brightness 12、Stroke 13〜15。旧Color Mixの9は使わない。

## 保存データ（SchemaVersion 3）

`PresetNode` の色フィールドは次のとおり。

- `FillHue` / `FillSaturation` / `FillBrightness`（Parameter）、`FillAlpha`
- `StrokeHue` / `StrokeSaturation` / `StrokeBrightness`（Parameter）、`StrokeAlpha`

v2のJSONは読み込み時に `PresetJsonMigration` がv3へ変換する。

- 静止時のFill色を求める：Color と To Color を、Color Mix の Base（0〜1にClamp）で補間する。Alphaも補間する。これをHSBに変換する。
- Hue Shift は、Binding・Random・Flip・単位ごと Fill Hue に引き継ぐ。Baseには元の色の色相を足す。そのため、Hue Shiftを含めた色相は以前と同じになる。
- Stroke色（RGBA）はHSBに変換する。
- **失われるもの**：Color Mix の Binding と Random（To Colorへ向かう動き）。HDR（1を超える）RGBの明るさは1にClampされる。
- 保存し直すとv3として書き出される。

## Stroke Width（2026-11-17追加）

Stroke WidthもBindできるParameterにした。

- Core：`ShapeDefinition.StrokeWidth`（ParameterDefinition、省略時は固定値1）。`ShapeStyle` はFill / StrokeのON・OFFだけを持つ。`DrawItem.StrokeWidth` に評価済みの値が入る。Randomのキーは16。
- 値はShapeのローカル座標での太さ（px基準。画面基準の単位も選べる）。描画時の変形の扱いは従来と同じ（[描画Adapter](RenderingAdapter.md)のStroke拡張）。
- Coreは値をClampしない。描画Adapterは、評価した太さが0以下ならStrokeを描かない（サイズが0以下の図形を描かないのと同じ扱い）。
- 保存データ：`StrokeWidth` が数値からParameterになり、SchemaVersion 4に上げた。v3の数値は読み込み時に、同じ値をBaseに持つParameterへ移行する。
- Preset Editor：Stroke ONのとき、表の先頭に Width 行（Base / Rand / Depth / Unit / Bind）を出す。

## Preset Editor

- Fill ON：`Fill Color`（ピッカー）、Hue / Saturation / Brightness の行。
- Stroke ON：`Stroke Color`（ピッカー）、Width / Hue / Saturation / Brightness の行。
- HueのBind時の既定Depthは360（Valueが0→1で一周）。

## 検証

2026-11-17：Unity Editor経由で再コンパイルに成功。EditModeテスト322件成功・失敗0。内訳は `ColorDefinitionTests` 19件、`ShapeStyleTests` 7件、`PresetRepositoryTests` 14件（v2→v3移行テストを含む）など。Play Mode中は2回ともテストが始まらずにタイムアウトし、Play Modeを止めてから実行した。Preset Editor UIの操作と見た目は未確認。
