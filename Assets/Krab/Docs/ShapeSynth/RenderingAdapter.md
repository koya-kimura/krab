# Phase 4：p5.Unite描画境界

## データと責務

`HierarchyEvaluator.Evaluate(now, context)` → `IReadOnlyList<DrawItem>` → `UniteShapeAdapter.Draw` → `TargetCameraCanvas` → 指定CameraのRenderTexture。

Coreは階層・Parameter・行列合成を評価する。DrawItemはKind、ローカルGeometry、Color、Opacity、WorldTransformの不変スナップショットで、ShapeDefinitionを公開しない。Core asmdefの `noEngineReferences` は維持する。AdapterはDefinitionを参照せず、Coreへ描画都合のClampを戻さない。

`Krab.ShapeSynth.Unity` asmdefはCoreとUnite.Runtimeだけを参照する。サンプルだけがCommonのDI/BPMを利用する。Voiceと寿命の仕様は [Voiceと寿命](VoiceAndLifetime.md) を参照。

## APIと座標

- `UniteShapeAdapter.Draw(UCanvas canvas, IReadOnlyList<DrawItem> items, float width, float height, bool invertTargetY = false)`：リスト順で描画命令を発行する。毎フレーム呼び出す。
- `CreateTargetMatrix(Affine2D transform, float width, float height, bool invertTargetY = false)`：中心への平行移動と出力Y反転を含む行列。親子合成済み行列を再分解せず適用する。
- `TargetCameraCanvas.Initialize(Camera camera)`：`CreateCanvas(ScreenOverlay, targetCamera: camera)` でコンテキストを作成し、Scene Viewへの描画切り替えを無効にする。ライフサイクルはUCanvas本体に任せる。

Coreの原点はCanvas中央、X右/Y上、角度は度。Adapterはラジアンへの変換やTRS再分解を行わない。非等方Scale、反転、親子合成で生じるshearを行列のまま維持する。Circle/Rectangleは中心基準、Polygonの頂点は呼出側が指定したローカル原点からの座標で、自動的にbounds中央へ寄せない。

幅・高さは毎フレーム対象CameraのpixelWidth/pixelHeightから取得し、同じ値をReferenceContextとAdapterに渡す。UCanvas.Width/Heightは最後に描画したCameraを参照するため、この経路では使用しない。RT/Previewの既存経路に合わせ、サンプルのInvert Target Yはtrue。別出力ではこの明示設定を調整する。

## 描画制約

非正のCircle Diameter、Rectangle Width/Heightは描画省略。Opacity×Color Alphaは描画境界で0〜1に制限する。Coreの値は変更しない。有限floatへ変換できない行列・色・頂点は例外で検出し、黙って0に置き換えない。Polygonは頂点を検証してからBeginShapeする。Coreの最低3頂点という制約を維持する。自己交差や退化Polygonの補修・穴・複数輪郭には対応しない。単純Polygonを使う。

Push/Popをtry/finallyで対にし、外部の描画スタイルと行列を復元する。Groupの子を深さ優先で列挙した順に描画する。p5.Uniteはpremultiplied alpha（One/OneMinusSrcAlpha）とsubmission順のsort distanceを使い、Overlayでは深度テスト/書込を行わない。同一Canvas内の順序を契約とし、複数Canvas間の重なり順は契約しない。独立Z-indexは追加しない。

## Cameraの限定と依存条件

2026-11-17にp5.Unite 0.2.1（tag `0.2.1`、PackageCache `net.sbm.unite@9304386ff376`）へ更新した。0.2.0以降のUCanvasは `SetCamera` / `CreateCanvas(targetCamera)` で1つのCameraだけに描画する。TargetCameraCanvasはこの公開APIを使う薄い派生クラスに変更し、UCanvasのライフサイクル（Text拡張の `FrameCapturing` を含む）をそのまま使う。0.2系のUCanvasはEditorでScene Viewにフォーカスすると描画先をScene View Cameraへ切り替えるため、出力RTが変わらないようシリアライズ専用の `_drawInSceneView` を `JsonUtility.FromJsonOverwrite` でfalseにする。reflectionやPackageCacheの編集は行わない。パッケージ更新時はこのフィールド名と複数Camera検証を再確認する。

OutputTextureProviderがRT生成・再生成・解放を所有する。PreviewとSyphonは従来のTextureChanged経由で同じTextureを参照する。新しい出力Managerは作らない。Syphon受信側で映像が届くことの確認は別途必要。

## 検証Scene

`Assets/Krab/ShapeSynth/Samples/ShapeSynthSmokeTest.unity` を開きPlayする。

- 既存：左のADSR/LFO/Binding、右の回転Group/ネストGroup/共有変調を維持。
- 追加 `ShapeSynth/Renderer Checks`：上端にCircle・Rectangle・Polygon。下端左は反転/非等方Scaleとshear、下端中央はcyan→orange→pinkの順で半透明の3図形を重ねる。
- 3つのデモはそれぞれTargetCameraCanvasを使用し、同じOutput Cameraを指定する。
- Space=ADSR再開始、R=Release、L=LFO再開始、D=既存UI切替。新しい静的描画チェックに操作は不要。

再構築用の `ShapeSynthSmokeSceneBuilder` も同じ構成を作る。本番Sceneは変更しない。

数値テストとCamera送信確認はGPUの最終映像や演出の目視判断を代替しない。実行ごとの結果はgitignore対象のReportsへ記録する。

2026-11-10の実行：Unityコンパイル成功、EditMode 94件成功・失敗0。Playで3Canvasの送信、別Cameraへの描画漏れなし、1920×1080→800×600の再生成とPreview/Syphon参照更新を確認し、元の解像度へ復元した。外部Syphon受信と最終的な目視判断は未実施。詳細は `Reports/Phase4Validation-2026-11-10.md`。

## Phase 8のStroke拡張

DrawItemに不変ShapeStyleを追加。Fill/Stroke/Stroke Width/HSB色を公開APIで描画する。Circle/Rectangleはローカル線幅を行列で変形し、Polygonは画面上の線分へ展開されるため面積Scaleの平方根で幅を換算する。等方Scaleでは一致、非等方Scale/shearでは近似。色と線幅は [HSBとStroke Width](HsbColor.md) を参照。既存Fill-onlyは維持する。

## Play中のスクリプト再読み込み（2026-11-17）

p5.UniteのUCanvasは、スクリプトの再読み込みの直前に描画コンテキストを破棄する（Play中の再読み込みも含む）。再読み込み後にAwakeは再実行されないため、Awakeでだけ `Initialize` していると、毎フレーム `Canvas not initialized` の例外が出ていた。

`TargetCameraCanvas` は初期化済みフラグを `[NonSerialized]` で持ち、再読み込みでコンテキストと一緒にリセットされるようにした。描画する側（`ShapeSynthController` と各Sampleの `Update`）は、描画の前に `EnsureInitialized()` を呼ぶ。同じCameraでCanvasを作り直す。

検証：Play中に一時スクリプトの追加と削除で再読み込みを2回起こし、どちらの後もコンソールのエラーが0件で、描画コンテキストがあり、フレームの送信が続くことを確認した。EditMode 319件成功。

