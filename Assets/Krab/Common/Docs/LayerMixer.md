# Layer Mixer

TypeLayout・ShapeSynth・ImageLayerの3つの描画レイヤーを、設定UIの「Layers」タブ1か所で表示/非表示・重なり順を操作する。

## 方式

- レイヤーごとにOverlay Cameraを1台持ち、出力Camera（`OutputTextureProvider`）をBaseとするURP Camera Stackに積む。同じCameraに複数のp5.Unite Canvasを載せると前後が決まらないため（`Modules/ImageLayer/Docs/ImageLayerImplementationPlan.md` のPhase 0）。
- 重なり順は、Stack内の並び＝`OverlayLayerCamera.Order` の昇順（小さいほど奥）。`Order` を変えると、その場でStackを並べ直す（安定な挿入ソート。レイヤーでないOverlay Cameraは最も奥へ）。
- 出力Cameraは黒でクリアするだけで、何も描かない。出力RTとPreview・Syphon/Spoutの経路は変えていない。
- 不透明度・ブレンドモードは未実装。ブレンドモードが要るときは、レイヤーごとのRTと合成シェーダーへの移行を検討する。

## 構成

| パス | 役割 |
| --- | --- |
| `Common/Runtime/Output/Layers/OverlayLayerCamera.cs` | Overlay CameraをBaseのStackへ `Order` 順に入れる。無効にするとCameraを止めてStackから外す（＝非表示） |
| `Common/Runtime/Output/Layers/LayerMixer.cs` | `[UiTarget("Layers", "Layers")]`。各レイヤーの名前と `OverlayLayerCamera` を持ち、順番と表示をPrefsへ保存して反映する |
| `Shows/2612_shirt-meets-lausbub/Editor/LayerSceneSetup.cs` | メニュー `Krab/Layers/...`。検証Sceneの作成と、開いているSceneへのレイヤー追加 |
| `Shows/2612_shirt-meets-lausbub/Samples/LayerMixer.unity` | 検証Scene |

各レイヤーは1つのGameObjectにまとまっている：Camera＋`OverlayLayerCamera`＋描画Component（`TypeLayoutPerformance` / `ShapeSynthController`＋`ShapeSynthPerformance` / `ImageLayerCanvas`＋`ImageLayerPerformance`）。描画サイズはそのCameraから取り、`OverlayLayerCamera` が出力RTを同期するので出力解像度になる。

## UIと保存

- 1行に1レイヤー。上の行ほど手前。`Up` / `Down` で入れ替え、トグルで表示/非表示を切り替える。
- `Layers Order`（PrefsString）：レイヤー名をカンマ区切りで手前から並べる。未保存・不明な名前は無視し、足りないレイヤーはInspectorの並び（初期値：Type Layout → Shape Synth → Image Layer）で末尾に補う。
- `Layers <名前> Visible`（PrefsBool）：レイヤーごとの表示。非表示でも描画スクリプトは動き続けるので、ShapeSynthのVoiceやLooperはBPMとずれない。
- `LayerMixer` はPrefsの値を毎フレーム見て反映するので、PrefsGUIでの読み込み・リセットにも追従する。
- ImageLayer・TypeLayoutにあった個別の `Visible` 設定は削除し、表示はMixerに一本化した。

## 検証（2026-11-27）

- `Shows/2612_shirt-meets-lausbub/Samples/LayerMixer.unity` と本番Sceneで、Play直後のStackが奥から Image → Shape → Type、3台とも1920×1080で描画されることを確認した。Consoleのエラー・警告は0件。
- 検証Sceneで、Image LayerをUpで2回動かし、Shape Synthを非表示にしたとき、Stackが奥から Type → Image になり、Shapeが外れることを確認した。戻すと元の順に戻る。
- 見た目（実際の重なり・UIの操作感）は未確認。
