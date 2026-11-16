# Phase 3：Shape / Group / Transform

## Coreの経路

`GroupDefinition / ShapeDefinition → HierarchyEvaluator → IReadOnlyList<DrawItem> → UniteShapeAdapter → UCanvas`。

CoreはUnity/p5.Unite/UI/保存に依存しない。既存のParameterEvaluatorを使用し、Binding式を重複実装しない。

```csharp
var circle = ShapeDefinition.Circle(ParameterDefinition.Constant(40),
    new TransformDefinition(positionX: ParameterDefinition.Constant(50)));
var group = new GroupDefinition(new TransformDefinition(
    positionX: ParameterDefinition.Constant(100),
    rotationDegrees: ParameterDefinition.Constant(90)), circle);
var evaluator = new HierarchyEvaluator(group, Array.Empty<IModulator>());
var items = evaluator.Evaluate(nowSeconds, new ReferenceContext(width, height));
// items[0].WorldTransform.TransformPoint(new Point2(0, 0)) == (100, 50)
```

`ParameterDefinition.Constant(value)` は固定値レシピの生成補助。既存Parameter/Bindingの振る舞いは変更しない。

## Shape / GroupのAPI

- `ShapeDefinition.Circle(diameter, transform?, opacity?, color?)`：中心(0,0)、Width/HeightはいずれもDiameter Parameterを参照する。
- `ShapeDefinition.Rectangle(width, height, transform?, opacity?, color?)`：中心(0,0)。サイズの半分を左右/上下へ持つ。
- `ShapeDefinition.Polygon(vertices, transform?, opacity?, color?)`：3点以上の固定ローカル頂点。配列をコピーし、閉じた単純Polygonを描画する。頂点は作者がローカル原点の周囲へ配置する。Bounds/重心からの自動再中心化はしない。穴/ベジェ/自己交差の描画仕様は未定。
- `GroupDefinition(transform?, params children)`：Shape/Groupの混在した順序付き子リスト。コピーして読み取り専用化する。Pivotはローカル(0,0)で固定し、子の配置/サイズ/Boundsには依存しない。
- `INodeDefinition`：Transformの読み取り口のみ。現在の評価器が扱う実装は上記2つに限定し、独自ノードを受理するプラグイン構造にはしない。
- `RgbaColor`：固定の4成分。有限値のみ。Color用の汎用Parameter型は追加しない。2026-11-16に `ColorDefinition`（From→ToのMixとHue Shiftを各スカラーParameterで評価）を追加し、`ShapeDefinition.WithFillColor` で付けられるようにした。`ShapeDefinition.Color` は基準色（From）。Opacityは既存のスカラーParameterで評価できる。

両Definitionはsealedかつ不変。同じオブジェクトを複数の親から参照できる（DAG）。永続ID/Unity InstanceIDは採用しない。

## Transformの規約

CoreのRoot原点はCanvas中央、X右/Y上、正のRotationは反時計回り。Positionは親Groupのローカル座標。角度はAPI上すべて**度**。

`TransformDefinition(positionX?, positionY?, rotationDegrees?, scaleX?, scaleY?)` は5個のParameterDefinitionを持ち、省略時は0,0,0,1,1。

`Affine2D` はdoubleの2×3アフィン行列で、列ベクトルに作用する。

- `Local = Translation × Rotation × Scale`
- `World = ParentWorld × Local`
- `Affine2D.Trs(...)`、`operator *`、`TransformPoint(Point2)` を提供。
- 回転はCoreで度→ラジアンへ変換してsin/cosを計算する。大きい角度は360度で縮める。
- 非等方/負/ゼロのScaleを許容。非等方Scaleと子Rotationで発生するshearも行列のまま保持し、分解して失わない。
- 入力/演算結果が非有限の場合は既存方針に合わせて例外。サイズやOpacityに一律Clampしない。

Referenceは評価時のCanvas寸法のみを参照し、座標変換は行わない。

## Runtime評価とDraw Item

`HierarchyEvaluator(groupDefinition, modulators)` は階層の**出現箇所ごと**にParameterEvaluatorを生成する。同じDefinitionを複数箇所へ置いても、親行列を取り違えるキャッシュは持たない。既存の外部Modulatorスロットを解決するだけで、Voice/Modulatorを生成・所有しない。

`Evaluate(nowSeconds, context)` は深さ優先の子順でShapeを並べた読み取り専用スナップショットを返す。DrawItemには種類、WorldTransform、評価済みWidth/Height/Opacity、固定Color/Verticesがある。Geometryはローカルに保ち、AdapterがWorldTransformを**一度だけ**適用する。以前返したDraw Itemは後続評価で変わらない。

同じDefinitionから別々の評価器を作り、別のModulator一覧を渡せばADSRのRelease状態なども独立する。1つの評価器内で同じスロットを使えば、明示的に同じ外部Instanceを共有する。Definitionの再利用だけでModulatorの複製/新規Voiceを暗黙に生成しない。

循環は通常の公開コンストラクタでは構築できない。子配列の後書換えも影響しない。評価器構築時には現在の探索パスで循環を検出して例外とし、別の枝で共有するDAGとは区別する。深さの固定上限は設けないが、再帰処理のため実行環境のスタック限界はある。

## p5.Unite境界

`Assets/Krab/ShapeSynth/Scripts/Unity/Rendering/UniteShapeAdapter.cs` は最小の描画Adapter。Draw Itemだけを受け取り、階層やBindingを再評価しない。

`UCanvas.ApplyMatrix(a,b,c,d,e,f)` の列順へ全アフィン行列を渡す。Unity TRSへの分解やp5.Rotateの再呼出しは行わない。RectMode/EllipseModeをCenterに設定し、Push/Popで状態を復元する。PolygonはBeginShape/Vertex/EndShape(Close)。

RootのCanvas中央への移動と出力先Y反転だけをAdapterで処理する。今回のCamera→RT→Previewでは正の画面Yが下に見えることを既存デモで確認済みのため、サンプルのInvert Target YをtrueとしてCoreのY上向きを表示へ対応させる。別出力ではこの出力設定を切り替える。反転はCore行列へ書き戻さない。

0以下のサイズは描画省略、Opacity×Color Alphaは描画時に0〜1へClamp。floatへ変換できない値は明示的に拒否する。Coreはdoubleの有限値を扱い続ける。

Phase 4の正式な描画境界・Camera限定と現在のAPIは [描画Adapter](RenderingAdapter.md) を参照。

## 検証Scene

`Assets/Krab/ShapeSynth/Samples/ShapeSynthSmokeTest.unity` を開いてPlayする。Common/Camera/RT/Preview/Syphonを維持。

- 左パネル：Phase 1〜2bの4つの円（ADSR直径、LFO移動、合成直径、共有ADSRのOpacity）。配置のみ左へまとめる。
- 右上A：CircleとRectangleを同じGroupに置き、LFOで原点の周りに回転。灰色の小さい円がPivot。
- 右中央B：親GroupのRotationと、子GroupのPosition/Rotation/非等方Scale/負Scaleを合成。さらに内側のGroupに三角形を配置する。
- 右下C：Group RotationにLFO、Circle DiameterとRectangle Position Xに同じADSRをBinding。自動で反復する。

追加GameObjectは `ShapeSynth/Group Hierarchy` の1つだけ。TargetCameraCanvas（UCanvas派生）とHierarchySmokeTestを付け、Camera参照とInspector値を保存する。ShapeごとのGameObjectは作らない。

Space=ADSRを再開始、R=外部Release、L=LFOを再開始（旧/新デモそれぞれが入力を受け取る）。D=既存共通UI切替。新デモのRotation Cycles Per Beatは初期0.0625（16拍で1周期）。単純なサンプル用の再生成処理であり、正式なTrigger伝播/Voice管理ではない。

## 後続のVoice / Variation / Prefsへ残す判断

Voiceの寿命方針は [実装計画の寿命仕様](VoiceAndLifetime.md) に更新した。Trigger対象の階層全体にADSRまたはTimerを必須とし、すべてのLifetimeの終了でVoiceを破棄する。TimerとILifetimeの実装は後続Phase 5で行い、Phase 3の挙動は変更しない。

- Definitionの永続的識別・複製と参照のリマップ、出現箇所のIDは未決定。現状は不変オブジェクトの参照と子順だけを使う。
- Group内のModulator所有者/スコープ、Group階層とVoice階層の関係は未決定。現在は呼出側がスロット一覧を渡す。将来は評価器構築の参照解決境界にスコープ/出現箇所ごとのResolverを追加できる。
- Variationは同じコンパイル境界で出現箇所ごとのParameter設定を解決する案。共有Definitionへ状態を書き戻さず、Definition型の全面作り直しを避ける。
- Definitionは不変のため、その場編集は行わない。編集後の新しいDefinitionを既存Instanceへ反映するか、新規Voiceだけに適用するかは未決定。現在のサンプルは評価器を明示的に再生成する。
- 毎評価でDraw Itemスナップショットを確保する。大規模運用のバッファ再利用/キャッシュ/性能保証は後続で検討する。
- Group Trigger、Voice寿命/Spawn/Restart、Variation、Layout、保存DTOや編集UIは今回実装していない。
