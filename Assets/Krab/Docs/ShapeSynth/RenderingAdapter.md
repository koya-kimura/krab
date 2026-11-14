# Phase 4：p5.Unite描画境界

## データと責務

`HierarchyEvaluator.Evaluate(now, context)` → `IReadOnlyList<DrawItem>` → `UniteShapeAdapter.Draw` → `TargetCameraCanvas` → 指定CameraのRenderTexture。

Coreは階層・Parameter・行列合成を評価する。DrawItemはKind、ローカルGeometry、Color、Opacity、WorldTransformの不変スナップショットで、ShapeDefinitionを公開しない。Core asmdefの `noEngineReferences` は維持する。AdapterはDefinitionを参照せず、Coreへ描画都合のClampを戻さない。

`Krab.ShapeSynth.Unity` asmdefはCoreとUnite.Runtimeだけを参照する。サンプルだけがCommonのDI/BPMを利用する。正式なVoice/Trigger/Timer/ILifetime/Variationは今回実装しない。

## APIと座標

- `UniteShapeAdapter.Draw(UCanvas canvas, IReadOnlyList<DrawItem> items, float width, float height, bool invertTargetY = false)`：リスト順で描画命令を発行する。毎フレーム呼び出す。
- `CreateTargetMatrix(Affine2D transform, float width, float height, bool invertTargetY = false)`：中心への平行移動と出力Y反転を含む行列。親子合成済み行列を再分解せず適用する。
- `TargetCameraCanvas.Initialize(Camera camera)`：ScreenOverlay/Indirectのコンテキストを作成する。指定CameraのSRP beginCameraRenderingだけでDrawする。OnDisableで購読解除と保留命令の破棄、OnDestroyでコンテキストを解放する。

Coreの原点はCanvas中央、X右/Y上、角度は度。Adapterはラジアンへの変換やTRS再分解を行わない。非等方Scale、反転、親子合成で生じるshearを行列のまま維持する。Circle/Rectangleは中心基準、Polygonの頂点は呼出側が指定したローカル原点からの座標で、自動的にbounds中央へ寄せない。

幅・高さは毎フレーム対象CameraのpixelWidth/pixelHeightから取得し、同じ値をReferenceContextとAdapterに渡す。UCanvas.Width/Heightは最後に描画したCameraを参照するため、この経路では使用しない。RT/Previewの既存経路に合わせ、サンプルのInvert Target Yはtrue。別出力ではこの明示設定を調整する。

## 描画制約

非正のCircle Diameter、Rectangle Width/Heightは描画省略。Opacity×Color Alphaは描画境界で0〜1に制限する。Coreの値は変更しない。有限floatへ変換できない行列・色・頂点は例外で検出し、黙って0に置き換えない。Polygonは頂点を検証してからBeginShapeする。Coreの最低3頂点という制約を維持する。自己交差や退化Polygonの補修・穴・複数輪郭には対応しない。単純Polygonを使う。

Push/Popをtry/finallyで対にし、外部の描画スタイルと行列を復元する。Groupの子を深さ優先で列挙した順に描画する。p5.Uniteはpremultiplied alpha（One/OneMinusSrcAlpha）とsubmission順のsort distanceを使い、Overlayでは深度テスト/書込を行わない。同一Canvas内の順序を契約とし、複数Canvas間の重なり順は契約しない。独立Z-indexは追加しない。

## Cameraの限定と依存条件

導入済みp5.Unite 0.1.0（81559d3daad3）の通常UCanvasはSRPの全Cameraへ送信する。TargetCameraCanvasはUCanvasのprivate Unityライフサイクルメッセージを派生クラスで置き換え、公開DrawingContext APIによってcaptureと対象Cameraへの送信を行う。PackageCacheの編集・reflectionによる内部アクセスは行わない。パッケージ更新時はこのライフサイクルと複数Camera検証を再実行する。Text等のoptional package向け内部FrameCapturingイベントは発行しないため、このクラスは図形描画専用とする。

OutputTextureProviderがRT生成・再生成・解放を所有する。PreviewとSyphonは従来のTextureChanged経由で同じTextureを参照する。新しい出力Managerは作らない。Syphon受信側で映像が届くことの確認は別途必要。

## 検証Scene

`Assets/Krab/ShapeSynth/Samples/ShapeSynthSmokeTest.unity` を開きPlayする。

- 既存：左のADSR/LFO/Binding、右の回転Group/ネストGroup/共有変調を維持。
- 追加 `ShapeSynth/Renderer Checks`：上端にCircle・Rectangle・Polygon。下端左は反転/非等方Scaleとshear、下端中央はcyan→orange→pinkの順で半透明の3図形を重ねる。
- 3つのデモはそれぞれTargetCameraCanvasを使用し、同じOutput Cameraを指定する。
- Space=ADSR再開始、R=Release、L=LFO再開始、D=既存UI切替。新しい静的描画チェックに操作は不要。

既存Sceneの移行はEditorの `RendererSmokeSceneSetup.Upgrade()` を使う。保存済みSmokeTestのEditModeのみ受け付ける。再構築用Builderも同じ構成を作る。本番Sceneは変更しない。

数値テストとCamera送信確認はGPUの最終映像や演出の目視判断を代替しない。実行ごとの結果はgitignore対象のReportsへ記録する。

2026-11-10の実行：Unityコンパイル成功、EditMode 94件成功・失敗0。Playで3Canvasの送信、別Cameraへの描画漏れなし、1920×1080→800×600の再生成とPreview/Syphon参照更新を確認し、元の解像度へ復元した。外部Syphon受信と最終的な目視判断は未実施。詳細は `Reports/Phase4Validation-2026-11-10.md`。

## Phase 8のStroke拡張

DrawItemに不変ShapeStyleを追加。Fill/Stroke/Stroke Width/任意StrokeColorを公開APIで描画する。Circle/Rectangleはローカル線幅を行列で変形し、Polygonは画面上の線分へ展開されるため面積Scaleの平方根で幅を換算する。等方Scaleでは一致、非等方Scale/shearでは近似。詳細は [複数プリセットとStroke](MultiPresetPerformance.md)。既存Fill-onlyは維持する。
