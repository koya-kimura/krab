# p5.Unite API・図形シンセ適用調査

調査日：2026-11-08（JST）。対象は **krabに実際に解決されているp5.Unite 0.1.0**。既存コード・設定・PackageCacheは変更せず、このレポートのみ追加した。

**結論：入れ子GroupのTransform描画は実現できる。ShapeごとのGameObjectは不要。Group / Pivot / Voice / Modulator / 保存はシンセ側で管理し、p5.Uniteには毎フレームの描画命令を渡す構成が適している。** ただし、画面座標、Pivotの意味、複数Cameraへの描画、半透明の重なりは先に設計・実機検証が必要。

以下では「確認済み」はローカルソースで確認した仕様、「推奨」は設計案、「未検証」は描画実行・測定をしていない項目を指す。コード例は公開シグネチャと照合したが、Unityへの追加・コンパイル・PlayMode実行はしていない。性能測定、既存テスト実行もしていない。

参照リンクはローカルの絶対パスと行番号。共有先では `Library/PackageCache/net.sbm.unite@81559d3daad3/` 以下のパスをパッケージ内パスとして読み替えられる。上流参照用：[公式リポジトリ](https://github.com/popsy-y/p5.Unite)、[lock記載コミットのパッケージ](https://github.com/popsy-y/p5.Unite/tree/4921c865e318428b084bf48f615b6b6debc563e9/Packages/net.sbm.unite)。オンラインではリポジトリの存在を確認した。本レポートのAPI判断はローカル実装を根拠とし、上流最新版との差分は未調査。

## A. p5.Uniteの概要

### 導入状況と同一性

| 項目 | 確認結果 |
| --- | --- |
| 正式表示名 / package名 | `p5.Unite` / `net.sbm.unite` |
| package.jsonのversion | `0.1.0`。最新リリース番号を示すという意味ではない |
| 導入元 | `https://github.com/popsy-y/p5.Unite.git?path=Packages/net.sbm.unite#package` |
| 解決済みGit hash | `4921c865e318428b084bf48f615b6b6debc563e9` |
| 実体 | `Library/PackageCache/net.sbm.unite@81559d3daad3`。末尾はキャッシュ識別子でGit commit hashではない |
| 作者 / ライセンス | `popsy-y` / MIT（package.json記載） |
| 実装 | C#、ShaderLab / HLSL。p5.jsをそのまま実行するJavaScript環境ではない |
| namespace | `Unite.Canvas`、`Unite.Drawing`、`Unite.Coordinate` |
| Runtime assembly | `Unite.Runtime`。unsafe許可、Burst / Mathematics / RenderPipelines.Core.Runtime参照 |
| 要求環境 | package.json Unity `6000.2`、READMEは6000.2以降・URP必須 |
| krab | Unity `6000.3.12f1`、manifestのURP `17.3.0` |

根拠：[Packages/manifest.json:11-20](/Users/kimurakoya/Documents/develop/unity/krab/Packages/manifest.json:11)、[Packages/packages-lock.json:283-296](/Users/kimurakoya/Documents/develop/unity/krab/Packages/packages-lock.json:283)、[package.json:1-36](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/package.json:1)、[Runtime/Unite.Runtime.asmdef:1-18](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Unite.Runtime.asmdef:1)、[README.md:7-12](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/README.md:7)、[ProjectSettings/ProjectVersion.txt:1-2](/Users/kimurakoya/Documents/develop/unity/krab/ProjectSettings/ProjectVersion.txt:1)。

package.jsonの依存宣言は Burst `1.8.19`、Mathematics `1.3.2`、RP Core / URP `17.0.0`。プロジェクトの実解決版すべてをこの宣言値と同一とは扱わない。導入参照は`#package`であり、READMEの例の`#v0.1.0`とは異なる。再現性のため、今後更新する際はversion文字列だけでなくGit hashも記録することを推奨する。

### 構成・資料・Runtime / Editor

- `Runtime/Canvas/`：`UCanvas : MonoBehaviour`と図形・Style・Transformのpartial実装。
- `Runtime/Coordinate/`：行列演算、`CanvasTransformStack`。
- `Runtime/Drawing/`：命令キュー、GPU用データ生成、Indirect / Sequential送信。
- `Runtime/Shaders/`：Screen / World / CanvasLocalのSDF描画シェーダー。
- `Runtime/Resources/UniteShaderLibrary.asset`：ビルドにシェーダー参照を保持するライブラリ。[Runtime/Drawing/UniteShaderLibrary.cs:8-43](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/UniteShaderLibrary.cs:8)
- ローカル資料：`README.md`、`CHANGELOG.md`、`Samples~/BasicDrawing/README.md`。サンプルは`Samples~/BasicDrawing/Scripts/BasicDrawing.cs`とUnityシーン。Package ManagerのSamplesからImportする構成。[package.json:27-35](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/package.json:27)
- `Tests/EditMode/`：`TransformApiTests`、`MatrixMathTests`、`CurveApiTests`、`ShaderLibraryTests`。`Unite.Runtime.Tests.asmdef`はEditor限定。Transform・Style復元やネストのテストはあるが、VJ用途の実機性能を保証するテストではない。[Tests/EditMode/Unite.Runtime.Tests.asmdef:1-21](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Tests/EditMode/Unite.Runtime.Tests.asmdef:1)、[Tests/EditMode/TransformApiTests.cs:89-178](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Tests/EditMode/TransformApiTests.cs:89)

描画APIはRuntime向け。独立したEditor用assemblyは同梱パッケージ内では確認できない。`UCanvas.Reset` / `OnValidate`のAssetDatabaseによるシェーダー自動割り当ては`#if UNITY_EDITOR`。実行時にはResourcesからの既定ライブラリ取得経路がある。`DrawingContext.Dispose()`はEditorとPlayerで破棄関数を切り替える。[Runtime/Canvas/UCanvas.cs:199-223](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:199)、[Runtime/Drawing/DrawingContext.cs:122-153](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:122)

p5.UniteはUnityの`MeshRenderer`を図形ごとに生成せず、`Graphics.RenderMeshIndirect` / `Graphics.RenderMesh`を使う独自の描画層である。`UnityEngine.Canvas`（uGUI）とも別物。

## B. APIリファレンス

以下は今回の用途に関係する公開API。`Vector2/3/4`、`Color`、`Matrix4x4`はUnityEngine型。図形・Style・Transformの変更メソッドは特記以外すべて`public void`。

### B.1 初期化・描画ライフサイクル

```csharp
public DrawingContext CreateCanvas(
    DrawingMode mode = DrawingMode.ScreenOverlay,
    DrawingExecutionMode executionMode = DrawingExecutionMode.Indirect,
    bool enableDepth = false);
public DrawingMode DrawingMode { get; }
public DrawingExecutionMode ExecutionMode { get; set; }
public bool EnableDepth { get; set; }
public bool DebugShapeBounds { get; set; }
public UniteShaderLibrary ShaderLibrary { get; set; }
public int Width { get; }
public int Height { get; }
```

根拠：[Runtime/Canvas/UCanvas.cs:64-137](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:64)、[Runtime/Canvas/UCanvas.cs:293-353](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:293)。`CreateCanvas()`は既存contextをDisposeして再生成する。毎フレーム呼ぶ初期化関数ではない。未初期化の図形呼び出しは例外になる。描画モードを変える場合、`UCanvas.DrawingMode`のsetterはないため再生成を検討する。

処理順序は次の通り。

1. アプリの`Update`などでStyle / Transform設定と図形API呼び出し。
2. APIが現在値を`DrawCommand`にコピーしてキューへ追加。呼び出し直後にGPU描画するわけではない。
3. `UCanvas.LateUpdate()`が`CaptureCommands()`で記録用・描画用バッファを交換し、新しい記録側をクリア。
4. 既定`Indirect`はSRPの`beginCameraRendering`でCameraごとに描画送信。診断用`Sequential`は`LateUpdate`内でアクティブCamera群へ送信。
5. 次フレームも表示したければ再び命令を出す。図形オブジェクトを保持して自動再描画するAPIではない。

根拠：[Runtime/Canvas/Shape.cs:71-96](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Shape.cs:71)、[Runtime/Canvas/UCanvas.cs:168-192](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:168)、[Runtime/Drawing/DrawingContext.cs:176-205](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:176)、[Runtime/Drawing/DrawCommandBuffer.cs:53-67](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawCommandBuffer.cs:53)。

**推奨：シンセ更新と描画命令生成は`Update`で順序を明示して実行する。** 別MonoBehaviourの`LateUpdate`から描画すると、UCanvasのcapture前後が実行順に依存する。`CaptureCommands()` / `Draw(Camera)`自体は`DrawingContext`の公開関数だが、通常のUCanvasの自動処理と二重に動かす使い方は避ける。p5.jsの`setup()` / `draw()`相当の自動呼び出しは、この実装にはない。

### B.2 座標・単位

| DrawingMode | XYの意味 | Unity Transformとの関係 |
| --- | --- | --- |
| `ScreenOverlay` / `Screen` | ピクセル、左下(0,0)、右が+X、上が+Y | UCanvasのGameObject Transformは基本図形位置に使われない |
| `NormalizedScreenOverlay` / `NormalizedScreen` | XY各軸0..1、左下(0,0)、右上(1,1) | 同上。縦横は別々の画面寸法で伸びる |
| `World` | Unityワールド単位 | 図形ローカル行列の結果をワールド位置として投影 |
| `CanvasLocal` | UCanvasのローカル単位 | `UCanvas.transform.localToWorldMatrix`を追加で適用 |

根拠：[Runtime/Drawing/DrawingMode.cs:3-33](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingMode.cs:3)、[Runtime/Shaders/ScreenDrawing.shader:77-106](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Shaders/ScreenDrawing.shader:77)、[Runtime/Shaders/WorldDrawing.shader:66-71](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Shaders/WorldDrawing.shader:66)、[Runtime/Shaders/CanvasLocalDrawing.shader:90-96](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Shaders/CanvasLocalDrawing.shader:90)、[Runtime/Drawing/DrawingContext.cs:397-400](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:397)。

Screenの式は`screenPosition * 2 - 1`でY反転なし。したがってp5.jsの左上原点・下向きYを仮定しない。`Corner`を「左上」と呼ぶのもこの実装の既定Screen座標では不正確。正のサイズなら左下側の角になる。

回転APIは**ラジアン**。単引数`Rotate`はZ回転。XY平面では+Xから+Yへ回る。`Rotate(Vector3)`は各成分を度へ変換してUnityの`Quaternion.Euler`へ渡す。`GetRotationAndScale()`の角度戻り値だけは度。

幅・高さ・通常のStrokeWeightは描画座標の単位。非正方形出力のNormalized座標では、同じXY値のCircleも画面上の真円とは限らない。Screen系の非OverlayのZはワールド距離ではなく0..1の線形深度としてシェーダーで変換される。通常面図形はアンカー深度を使うため、ScreenモードのX/Y回転をWorldモード同様の3D投影とは扱わない。[Runtime/Coordinate/CanvasTransformStack.cs:61-81](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Coordinate/CanvasTransformStack.cs:61)、[Runtime/Shaders/ScreenDrawing.shader:88-106](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Shaders/ScreenDrawing.shader:88)

READMEにはScreenを「深度を使う」と記載するが、**実装は`enableDepth = false`が既定**。Screenを選ぶだけでは深度テストは有効にならない。OverlayはEnableDepthをtrueにしても深度を無視する。[Runtime/Canvas/UCanvas.cs:115-118](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:115)、[Runtime/Drawing/DrawingContext.cs:55-68](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:55)。

`Width/Height`は最後のCameraのpixel寸法（Normalizedでも1ではない）。World/CanvasLocalは通常-1だが、最初のCamera描画前にはCamera.mainのpixel寸法または1を返す分岐が先にある。固定の論理キャンバス寸法として使わない。[Runtime/Canvas/UCanvas.cs:293-353](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:293)

### B.3 Rectangle

```csharp
void Rect(float x, float y, float width, float height);
void Rect(float x, float y, float width, float height, float radius);
void Rect(float x, float y, float width, float height,
          float topLeft, float topRight, float bottomRight, float bottomLeft);
void Rect(Vector2 position, Vector2 size);
void Rect(Vector3 position, Vector2 size);
void Rect(Vector3 position, Vector2 size, float radius);
void Rect(Vector3 position, Vector2 size, Vector4 cornerRadii);
public ShapeMode RectMode { get; set; }
```

`radius`は矩形の角丸半径。4要素はコード上の引数名が上記順。ただし画面Y方向との見た目の対応はp5.js準拠と断定しない。`Rect`自身にRotation/Pivot引数はない。`Square`も同ファイルにある。[Runtime/Canvas/Rectangle.cs:7-79](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Rectangle.cs:7)。

### B.4 Circle / Ellipse

```csharp
void Circle(float x, float y, float diameter);
void Circle(Vector2 position, float diameter);
void Circle(float x, float y, float z, float diameter);
void Circle(Vector3 position, float diameter);
void Ellipse(float x, float y, float width);
void Ellipse(float x, float y, float width, float height);
void Ellipse(Vector2 position, Vector2 size);
void Ellipse(float x, float y, float z, float width, float height);
void Ellipse(Vector3 position, Vector2 size);
public ShapeMode EllipseMode { get; set; }
```

根拠：[Runtime/Canvas/Ellipse.cs:7-49](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Ellipse.cs:7)。既定CenterではCircle引数は**直径**。ただし実装はCircleも`EllipseMode`を通る。`Radius`なら引数を2倍してサイズにするため、名前がdiameterでも実効半径として扱われる。`Corners`なら引数値が対角の座標として解釈される。このためRenderer側で`EllipseMode = ShapeMode.Center`を明示して直径仕様を固定するのを推奨する。

### B.5 ShapeModeと原点

`ShapeMode { Corner, Center, Corners, Radius }`。Rectの既定はCorner、EllipseはCenter。[Runtime/Canvas/UCanvas.cs:24-35](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:24)、[Runtime/Canvas/Shape.cs:179-201](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Shape.cs:179)。

| Mode | position / sizeの解釈 |
| --- | --- |
| Center | positionが中心、sizeが全幅・全高 |
| Corner | 中心=`position + size/2`、サイズ=`abs(size)` |
| Corners | positionと**size引数そのもの**が対角2点。中心は平均、寸法は差の絶対値 |
| Radius | positionが中心、sizeは半幅・半高、全寸法=`2*abs(size)` |

Cornersの`width/height`はpositionからの差ではなく、もう一方の角のX/Y座標になる。負のWidth/Heightは最終的に絶対値になるため、鏡像は負のScaleで表現する。Cornerでは負サイズを含めた中心計算が先に行われるので、単純に入力全体が同じになるわけではない。

これは**形状の配置規約**であり、Transformの回転中心をそのpositionへ設定するPivot APIではない。

### B.6 Line・Polygon・任意頂点

```csharp
void Line(float x1, float y1, float x2, float y2);
void Line(float x1, float y1, float x2, float y2, float z);
void Line(Vector2 start, Vector2 end);
void Line(Vector2 start, Vector2 end, float z);
void Line(float x1, float y1, float z1, float x2, float y2, float z2);
void Line(Vector3 start, Vector3 end);

void Triangle(float x1, float y1, float x2, float y2, float x3, float y3);
void Triangle(float x1, float y1, float x2, float y2, float x3, float y3, float z);
void Triangle(Vector2 point1, Vector2 point2, Vector2 point3);
void Triangle(Vector2 point1, Vector2 point2, Vector2 point3, float z);
void Quad(float x1, float y1, float x2, float y2,
          float x3, float y3, float x4, float y4);
void Quad(float x1, float y1, float x2, float y2,
          float x3, float y3, float x4, float y4, float z);
void Quad(Vector2 point1, Vector2 point2, Vector2 point3, Vector2 point4);
void Quad(Vector2 point1, Vector2 point2, Vector2 point3, Vector2 point4, float z);

void BeginShape(ShapeKind kind = ShapeKind.Path);
void Vertex(float x, float y);
void Vertex(float x, float y, float z);
void Vertex(Vector2 position);
void Vertex(Vector3 position);
void BeginContour();
void EndContour();
void EndShape(ShapeCloseMode closeMode = ShapeCloseMode.Open);
```

根拠：[Runtime/Canvas/Line.cs:10-37](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Line.cs:10)、[Runtime/Canvas/Polygon.cs:10-109](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Polygon.cs:10)、[Runtime/Canvas/ShapePath.cs:80-177](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/ShapePath.cs:80)。**`Polygon(...)`という公開関数は確認できない**。任意多角形は`BeginShape`→`Vertex`列→`EndShape(Close)`で描く。

`ShapeKind`は`Path / Points / Lines / Triangles / TriangleFan / TriangleStrip / Quads / QuadStrip`。`ShapeCloseMode`は`Open / Close`。`BezierVertex`と`SplineVertex`もVertexと同じ4種類の位置引数を持つ。`BeginContour`は新しい輪郭へ切り替えるが、`EndContour()`は実装上空の関数。[Runtime/Canvas/ShapePath.cs:10-29](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/ShapePath.cs:10)、[Runtime/Canvas/ShapePath.cs:103-153](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/ShapePath.cs:103)。

LineはFillを使わずStrokeを使う。2D Line overloadとVector3 Lineは異なる内部経路。後者とPathのStrokeはLine3D経路を使う。`LinePerspective(false)`はWorld系Line3Dの線幅を画面ピクセル一定にする設定であり、全図形のStrokeを一律ピクセル固定にする機能ではない。[Runtime/Canvas/Line.cs:19-37](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Line.cs:19)、[Runtime/Canvas/Stroke.cs:32-80](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Stroke.cs:32)。

Path頂点は追加時には座標だけ格納し、EndShape時のTransform / Styleで命令化する。BeginShape中のTransform変更を頂点ごとの変換として使わない。Path fillはXYへ落とし、Z平均を面位置にする。任意の非平面3Dポリゴン描画APIではない。[Runtime/Canvas/ShapePath.cs:179-194](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/ShapePath.cs:179)、[Runtime/Canvas/ShapePath.cs:512-573](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/ShapePath.cs:512)。

### B.7 Style

```csharp
void Fill(Color color);
void NoFill();
void Stroke(Color color);
void NoStroke();
void StrokeWeight(float weight);
void StrokeCap(StrokeCap cap);
public StrokeCap GetStrokeCap();
void StrokeJoin(StrokeJoin join);
public StrokeJoin GetStrokeJoin();
void LinePerspective(bool enable);
public bool GetLinePerspective();
```

根拠：[Runtime/Canvas/Color.cs:7-23](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Color.cs:7)、[Runtime/Canvas/Stroke.cs:10-43](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Stroke.cs:10)。`Color`を直接受け取り、0..255への自動変換APIはここにはない。Unityの通常の0..1色値で例を記載する。Fill/Stroke呼び出しは各機能を再有効化する。Weightは命令化時に非負化する。

既定は白Fill、黒Stroke、Weight=1、両方有効。Capは`Round / Square / Project`。Joinは`Miter / Bevel / Round`を設定可能だが、**現描画はRoundのみ**。[Runtime/Canvas/UCanvas.cs:24-35](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:24)、[Runtime/Canvas/Stroke.cs:17-25](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Stroke.cs:17)。

Styleは永続するCanvas状態で、毎フレーム初期値へリセットされる実装ではない。Push/Popで局所化できる。Shape内部にキャプチャしたStyleは、その後Fillなどを変えても遡及変更されない。

### B.8 Transform

```csharp
void Push();
void Pop();
void ResetMatrix();
void ResetTransform(); // ResetMatrixの互換alias
void ApplyMatrix(Matrix4x4 matrix);
void ApplyMatrix(float[] values); // 6 / 16要素
void ApplyMatrix(float a, float b, float c, float d, float e, float f);
void ApplyMatrix(float a, float b, float c, float d,
                 float e, float f, float g, float h,
                 float i, float j, float k, float l,
                 float m, float n, float o, float p);
void Translate(Vector2 translation);
void Translate(Vector3 translation);
void Translate(float x, float y, float z = 0f);
void Rotate(float radians);
void Rotate(float radians, Vector3 axis);
void Rotate(Vector3 anglesRadians);
void RotateX(float radians);
void RotateY(float radians);
void RotateZ(float radians);
void Scale(float uniform);
void Scale(float x, float y);
void Scale(Vector2 scale);
void Scale(Vector3 scale);
void Scale(float x, float y, float z);
void ShearX(float radians);
void ShearY(float radians);
public Vector3 TransformPoint(Vector3 point);
public (Vector3 angleDeg, Vector2 scale) GetRotationAndScale();
```

根拠：[Runtime/Canvas/Transform.cs:19-150](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Transform.cs:19)。`Scale(float)`はZも含め一様Scale、2成分版はZ=1。負Scaleを保持する。ApplyMatrixは現在行列への右乗算。6値版は`x'=a*x+c*y+e, y'=b*x+d*y+f`、16値版は4値ずつ列に格納。float配列はnullまたは長さ不正で例外。行列型overloadなら毎回配列を生成する必要はない。

## C. 実際のコードサンプル

以下は**レポート内のみの提案例**。p5.Uniteの公開APIだけを呼び、実在しないGroup / Pivot / Polygon関数は使っていない。サンプル用の独自関数名は説明用。プロジェクトに実装していない。

### C.1 Rectangle・Circle・Ellipse・Line・任意Polygon

```csharp
using Unite.Canvas;
using Unite.Drawing;
using UnityEngine;

[RequireComponent(typeof(UCanvas))]
public sealed class ShapeApiExample : MonoBehaviour
{
    private UCanvas canvas;

    private void Start()
    {
        canvas = GetComponent<UCanvas>();
        canvas.CreateCanvas(DrawingMode.ScreenOverlay);
    }

    private void Update()
    {
        canvas.Push();
        try
        {
            canvas.ResetMatrix();
            canvas.RectMode = ShapeMode.Center;
            canvas.EllipseMode = ShapeMode.Center;
            canvas.Fill(new Color(0.2f, 0.7f, 1f, 1f));
            canvas.Stroke(Color.white);
            canvas.StrokeWeight(2f);

            canvas.Rect(120f, 100f, 80f, 40f);
            canvas.Circle(240f, 100f, 60f); // 直径60
            canvas.Ellipse(340f, 100f, 80f, 40f);
            canvas.Line(80f, 180f, 160f, 220f);

            canvas.BeginShape();
            canvas.Vertex(220f, 180f);
            canvas.Vertex(290f, 180f);
            canvas.Vertex(310f, 230f);
            canvas.Vertex(250f, 260f);
            canvas.Vertex(210f, 220f);
            canvas.EndShape(ShapeCloseMode.Close);
        }
        finally { canvas.Pop(); }
    }
}
```

Rectangleの回転・Scaleも、`Translate` / `Rotate` / `Scale`を設定して`Rect(0,0,...)`を描く。Circleを非一様Scaleすると画面上の形状も伸びる。RadiusをParameterとして持つなら、Center mode固定のadapterで`Circle(..., radius * 2f)`へ変換する。

### C.2 Group A / Bと兄弟の分離

次の関数を初期化済みUCanvasに対しUpdate内から呼ぶ。親のPosition=(100,100), Rotation=30度、子Group BのPosition=(50,0), Rotation=45度を表現している。

```csharp
private static void DrawGroups(UCanvas c)
{
    c.Push();
    try
    {
        c.ResetMatrix();
        c.RectMode = ShapeMode.Center;
        c.EllipseMode = ShapeMode.Center;
        c.NoStroke();

        c.Push(); // Group A
        try
        {
            c.Translate(100f, 100f);
            c.Rotate(30f * Mathf.Deg2Rad);
            c.Scale(1f, 1f);
            c.Fill(Color.white);
            c.Rect(0f, 0f, 60f, 20f);
            c.Circle(-40f, 0f, 20f);

            c.Push(); // Group B、Aの変換を継承
            try
            {
                c.Translate(50f, 0f);
                c.Rotate(45f * Mathf.Deg2Rad);
                c.Fill(Color.cyan);
                c.Rect(0f, 0f, 30f, 10f);
                c.BeginShape();
                c.Vertex(0f, 20f);
                c.Vertex(20f, 20f);
                c.Vertex(10f, 40f);
                c.EndShape(ShapeCloseMode.Close);
            }
            finally { c.Pop(); } // AのTransform / Styleへ戻る
        }
        finally { c.Pop(); } // Group Aの外へ戻る

        c.Push(); // Aと兄弟のGroup C
        try
        {
            c.Translate(300f, 100f);
            c.Fill(Color.magenta);
            c.Rect(0f, 0f, 40f, 40f);
        }
        finally { c.Pop(); }
    }
    finally { c.Pop(); }
}
```

Bの原点は`(100,100)+R(30°)*(50,0)`≒`(143.30,125)`、BのローカルX軸の角度はこの一様Scale例では75度。これは行列式からの計算例でありGPU実測ではない。Group CにはA/BのTransformもFillも漏れない。例外発生時の状態復元のためtry/finallyを使っている。

### C.3 独立Pivotの適用例

ここでは「Position = 親空間におけるPivotの到達位置」「Pivot = 子が定義されるローカル座標での回転中心」と定義する。

```csharp
private static void DrawPivotGroup(
    UCanvas c, Vector2 position, Vector2 pivot,
    float rotationRadians, Vector2 scale)
{
    c.Push();
    try
    {
        c.Translate(position);
        c.Rotate(rotationRadians);
        c.Scale(scale);
        c.Translate(-pivot);
        c.RectMode = ShapeMode.Center;
        c.Rect(0f, 0f, 80f, 40f); // 子Shapeの配置例
    }
    finally { c.Pop(); }
}
```

この契約は`L=T(position)*R*S*T(-pivot)`。もしPositionを「Pivot補正前のローカル原点の平行移動量」と定義するなら、必要な式は`T(position)*T(pivot)*R*S*T(-pivot)`になり、コードにもRotate前の`Translate(pivot)`を追加する。**どちらを採用するかは未決定**。両方式を混在させない。

## D. Pivot・Groupの詳細分析

### D.1 行列順序とネスト

確認済み：Canvasの現在行列はidentityから開始し、変換は右乗算される。`Translate → Rotate → Scale`は`M=T*R*S`。点への実作用は右側から、Scale→Rotate→Translateになる。[Runtime/Coordinate/CanvasTransformStack.cs:15-65](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Coordinate/CanvasTransformStack.cs:15)、[Runtime/Coordinate/MatrixMath.cs:13-52](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Coordinate/MatrixMath.cs:13)。

例えばTranslate(100,0)→Rotate(π/2)なら原点は(100,0)、点(1,0)は(100,1)。Rotate→Translateなら原点は(0,100)。呼び出し順序は配置の意味を変える。既存テストでもTranslate(10,20)→RotateZ(π/2)でVector3.rightが(10,21)になることを検証している。[Tests/EditMode/TransformApiTests.cs:89-110](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Tests/EditMode/TransformApiTests.cs:89)。

親子の合成は`Mworld = Mparent * Mlocal`。p5.UniteのPush/Popで現在行列を保持しながら再帰描画できる。`UCanvas.Push()`が保存するのは行列とStyleの両方で、Popで両方を復元する。低レベル`CanvasTransformStack.Push`の行列のみの挙動と区別する。[Runtime/Canvas/Transform.cs:43-63](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Transform.cs:43)。

ただし、**永続的な親子Node・Group識別子・子リスト・Trigger伝播はない**。Push/Popは描画中のスコープであり、保存可能なScene Graphではない。シンセ側のGroupからこの描画スコープを生成する。

矩形を`Rect(100,100,...)`で描き、手前にRotateだけ呼ぶと、回転中心は(100,100)ではなく現在ローカル原点である。中心回転には`Translate(center); Rotate(...); Rect(0,0,...)`を使う。内部でbounding box中心に頂点を置き換えていても、APIの回転中心が自動変更されるわけではない。[Runtime/Canvas/Transform.cs:15-17](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Transform.cs:15)、[Runtime/Canvas/Shape.cs:84-96](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Shape.cs:84)。

### D.2 方式AとBの比較

| 観点 | A：ローカル原点がPivot | B：Pivotを独立Parameter化 |
| --- | --- | --- |
| ローカル式 | `T(P)*R*S` | 例：`T(P)*R*S*T(-Q)`。Pの意味を先に決める |
| 子の配置 | Pivotからの相対位置として持つ | 任意の図形座標とPivot座標を別々に持てる |
| 実装 | 最小。Push→TRS→子→Pop | Translate追加か行列合成。特別なp5 APIは不要 |
| 編集 | Pivot変更を子のLocalPosition変更として表す必要がある | UIからPivotを直接編集・変調しやすい |
| ADSR/LFO | 原点のTRS変調は容易。Pivot移動だけは表現が間接的 | Pivot自体の変調を明示的に表現可能 |
| 性能構造 | 階層を1度評価しShapeごとに命令化 | 行列演算が増えるが図形数・GPU instance数は通常同じ |
| 注意 | 子を共有Definitionから再利用する場合、子座標の補正・複製に注意 | Pivot変更時の見た目維持、単位、正規化、Scaleとの関係の仕様が増える |
| p5.Uniteとの親和性 | 高い。既定行列操作そのもの | 高い。既存Translate/ApplyMatrixだけで実現可能 |

性能差の実測は未実施。Bを採用するとGameObjectや別draw callが必ず増えるという構造ではない。CPUコストはGroup数・深さ・行列評価頻度による。

**推奨案：基本仕様はAを明確に定め、Bを必要に応じて追加できるデータ境界にする。** ただし、演奏中のPivot変調が中心的な表現要件なら、最初からBを採用する合理性がある。これはユーザーが今後決める項目であり、本調査で確定しない。

Bを採用する場合は次を先に決める。

- Pivotをローカル座標で持つか、boundsに対する0..1のAnchorで持つか。
- Group boundsを子のDefinitionから求めるか、変調後のInstanceから毎フレーム求めるか。
- Pivotを変更した際、見た目の位置を維持するか。前者ならPosition補償が必要。
- Groupの一部だけ発音中の場合にboundsを変えるか。自動中央Pivotが演奏で動く可能性がある。

p5.Uniteに自動bounds中心・Anchor・Group Pivot管理機構は確認できない。RectMode / EllipseModeはこれらの代替ではない。

### D.3 Core評価かp5スタックか

| 案 | 利点 | 制約 |
| --- | --- | --- |
| 自前ツリーをp5のPush/Popで再帰描画 | APIが素直。親子Style継承も容易 | ツリー評価がRendererに寄りやすい。描画順変更や別backendで再評価が必要 |
| Coreで最終行列を評価し、adapterでApplyMatrix | backend非依存、結果の検査・単体検証・描画順ソートが容易 | 数学型と座標規約をCore側で定める必要 |
| Unity GameObject階層をShape/Groupとして使う | Scene操作、Gizmo、既存Componentとの連携が容易 | 大量Node、保存モデル、Voice複製とUnity lifecycleの管理が増える |

**推奨はCoreで合成行列とStyleを評価し、p5 adapterで描画する構成。** adapterは各draw itemごとに`Push(); ResetMatrix(); ApplyMatrix(matrix);`→全Style設定→図形→`Pop()`とすれば、最終行列を親変換と二重適用せずに使える。CanvasLocalのときは、このmatrixがUCanvasローカル基準かWorld基準かも固定する。World行列をCanvasLocalへそのまま渡すとGameObjectの変換を二重適用し得る。

親の非一様Scaleと子の回転の合成にはshearが含まれ得る。最終行列をPosition/Rotation/Scaleへ毎回分解して戻す方式は避ける。p5.Uniteは命令に4x4行列を保持しGPUへ渡しているため、行列のまま接続できる。[Runtime/Drawing/ShapeDataBuilder.cs:28-86](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/ShapeDataBuilder.cs:28)。`GetRotationAndScale`はEuler角とXY lossyScaleだけなので、一般的な階層の可逆な保存表現には使わない。[Runtime/Coordinate/CanvasTransformStack.cs:107-111](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Coordinate/CanvasTransformStack.cs:107)

### D.4 毎フレーム変更時

Translate/Rotate/Scaleは累積する。毎フレーム同じ値を追加し続けないよう、描画入口のResetMatrixと均衡したPush/Popを使う。カメラコールバックでは現在行列がResetされるが、カメラ有無に依存した暗黙リセットをシンセの契約にしない。ResetMatrixは保存済みPush状態を消さず、Styleもリセットしない。`_stateStack`のクリアもここでは行わない。[Runtime/Canvas/UCanvas.cs:183-192](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:183)、[Runtime/Canvas/Transform.cs:19-63](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/Transform.cs:19)。

既にenqueueした命令のTransformは値のコピー。変調後の値は次の命令生成で反映する。UnityEngine API・共有Canvas状態・キューを使うため、p5の描画APIを並列スレッドから呼ぶ前提にはしない。Coreの変調計算を別スレッド化するかは独立して検討できる。

## E. Unityとの統合

### Camera / RenderTexture / 既存出力

確認済み：UCanvasはSRPのカメライベントを購読する。Cameraの指定・選別を行う公開プロパティは確認できず、`CheckCurrentCamera`は渡されたCameraを保持するだけ。**UCanvasを出力Cameraの同じGameObjectに付けても、そのCamera限定になる実装ではない。** Scene View、プレビュー、複数のGame Cameraで同じ命令が描かれ得る。[Runtime/Canvas/UCanvas.cs:141-148](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:141)、[Runtime/Canvas/UCanvas.cs:183-241](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:183)。

`RenderParams.camera = cam`でCameraへ描画送信する。UCanvasにRTを直接受け取って作成・管理する公開APIは確認できない。Camera.targetTextureを使う経路が自然だが、本調査では実機表示を確認していない。[Runtime/Drawing/DrawingContext.cs:373-415](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:373)。

krab側では既に以下の経路がある。

```text
UCanvasの命令 → SRPで描くCamera
                    ↓ targetTexture
OutputTextureProvider → IOutputTextureContainer
                    ├→ Preview
                    └→ SyphonController → SyphonServer.SourceTexture
```

`OutputTextureProvider`はCameraを要求し、Prefsの解像度でARGB32・深度24のRenderTextureを生成してtargetTextureへ設定する。解像度変更時にTextureChangedを通知する。`SyphonController`は同供給textureをSourceTextureへ渡す。[Assets/Krab/Common/Scripts/Output/OutputTextureProvider.cs:10-46](/Users/kimurakoya/Documents/develop/unity/krab/Assets/Krab/Common/Scripts/Output/OutputTextureProvider.cs:10)、[Assets/Krab/Common/Scripts/Output/OutputTextureProvider.cs:64-92](/Users/kimurakoya/Documents/develop/unity/krab/Assets/Krab/Common/Scripts/Output/OutputTextureProvider.cs:64)、[Assets/Krab/Common/Scripts/Output/SyphonController.cs:48-85](/Users/kimurakoya/Documents/develop/unity/krab/Assets/Krab/Common/Scripts/Output/SyphonController.cs:48)。

**推奨：まずこの既存Camera→RT→出力経路にp5描画を合流させる。** UCanvas側でRTを二重管理しない。RTの上下反転、alpha、解像度変更、Camera stacking、UI混入、描画先Camera選別は接続試験で確認する。現APIだけでCamera限定を保証できるとは報告しない。

### GameObject / MonoBehaviour

必要なのは描画ホストの`UCanvas : MonoBehaviour`と描画するCameraなど。Shape / Group / VoiceごとのGameObjectは不要。普通のC#オブジェクトから、ホストが所有するUCanvasへ描画命令を出せる。CanvasLocalではホストGameObjectのUnity親子Transformも合成されるが、それを全シンセGroupの保存・実行モデルとして使う必然性はない。

Inspectorで扱うDefinitionを通常C#オブジェクトにするなら、Unity Serializationに適したDTO、必要に応じたSerializeReferenceなどは別途設計する。GameObjectが不要なことと、任意のC#グラフが自動保存されることは別問題。

### Inspector / Runtime Definition / RosettaUI / Prefs

p5.UniteにRosettaUI / Prefs専用の連携APIは確認できない。次の接続を推奨する。

| 層 | 所有するもの・操作 |
| --- | --- |
| Inspector default | 保存済みの初期Definition。実行時状態とは別コピー |
| Runtime Definition | 実行時の編集対象。Shape / Group / Binding / Variation設定 |
| RosettaUI | Runtime Definitionの基底Parameter値や接続を編集 |
| Prefs adapter | Runtime Definitionをversion付き保存データへ変換して保存・復元 |
| Instance / Voice | 発音時刻、ADSR進行、LFO位相、乱数状態、評価結果 |
| p5 adapter | 評価結果をFill / Stroke / Transform / 図形引数へ変換 |
| Apply to Inspector | Runtime Definitionを明示的にdefaultへ反映するEditor処理 |

RosettaUIが表示する基底値と、Modulator加算・乗算後の実効値は分ける。描画のたびに基底値へ変調結果を書き戻すと、保存値の汚染や累積変調が起きる。PrefsにVoiceの経過時間などを保存するかは要件として別途決める。既存Prefs型がネストしたDefinitionツリーをそのまま保存できるかは未調査で、専用serializerが必要となる可能性がある。

既存`UiBuilder`は`IUiTarget`群からUIを組み立て、Saveボタン・入力アクションで`Prefs.Save()`を呼ぶ。シンセ編集用adapterをこの入口へ接続するのは自然。[Assets/Krab/Common/Scripts/Views/UiBuilder.cs:27-62](/Users/kimurakoya/Documents/develop/unity/krab/Assets/Krab/Common/Scripts/Views/UiBuilder.cs:27)。Apply to Inspectorはp5 APIではなく、Editor専用assembly側でUndo / 永続化対象を決めて実装する設計項目。

## F. 制約と注意事項

### F.1 大量描画：確認できた内部構造

| 項目 | コードから確認した構造 | 根拠 |
| --- | --- | --- |
| GameObject | 図形ごとには生成しない。値型DrawCommandを配列へ追加 | [Runtime/Drawing/DrawCommandBuffer.cs:13-32](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawCommandBuffer.cs:13) |
| Mesh | DrawingContext生成時にUnitQuadを1つ作り、各図形で共用。Canvas間のglobal singleton共有ではない | [Runtime/Drawing/DrawingContext.cs:95-117](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:95)、[Runtime/Drawing/DrawingHelper.cs:7-35](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingHelper.cs:7) |
| Material | contextごと通常材質とdepth prepass材質を生成。色はshape dataに格納し図形ごと材質を生成しない | [Runtime/Drawing/DrawingContext.cs:107-117](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:107)、[Runtime/Drawing/ShapeDataBuilder.cs:84-86](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/ShapeDataBuilder.cs:84) |
| CPU変換 | DrawCommandからShapeDataへBurst対応kernelで変換。ループはcommand数に比例 | [Runtime/Drawing/ShapeDataBuilder.cs:12-23](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/ShapeDataBuilder.cs:12) |
| GPU転送 | shape dataとpolygon point bufferをSetData。capture世代とAA paddingが同じCamera間では再利用可能 | [Runtime/Drawing/DrawingContext.cs:186-233](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:186) |
| 既定Indirect | 通常図形のみの単一runなら全commandを1回のRenderMeshIndirectで送信。depth有効時はprepass追加 | [Runtime/Drawing/DrawingContext.cs:236-255](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:236) |
| Sequential | 各commandにつきRenderMesh。depth有効ならprepassも増える。診断用 | [Runtime/Drawing/DrawingContext.cs:284-308](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:284) |
| 描画単位 | 1 Shape = 必ず1 commandとは限らない。PathはStroke各segmentとFillが別命令 | [Runtime/Canvas/ShapePath.cs:303-343](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/ShapePath.cs:303) |

「1回のIndirect呼び出し」はCPU側API送信回数の構造であって、全環境で常に1 GPU draw / 一定fpsを保証する主張ではない。テキストatlasが交じる場合は連続atlasごとにrunを分割する。Group単位の新規UCanvasはcontext / material / bufferも増やすため、Group数とCanvas数は結び付けない。

### F.2 Allocation・CPU・GPU

確認できた再利用：記録/描画バッファ交換、command配列の容量維持、CPU scratch、GPU buffer、MaterialPropertyBlock、Path contour/polylineのpool。初回・最大数更新時は確保がある。[Runtime/Drawing/DrawCommandBuffer.cs:27-67](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawCommandBuffer.cs:27)、[Runtime/Drawing/DrawingContext.cs:208-234](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:208)、[Runtime/Drawing/DrawingContext.cs:417-425](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:417)、[Runtime/Drawing/DrawingContext.cs:468-498](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:468)、[Runtime/Canvas/ShapePath.cs:49-74](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/ShapePath.cs:49)。

Pushは`Stack<CanvasState>`に値型を保存するため、毎回新しいGroupオブジェクトを作る構造ではない。ただしスタック容量拡張時には内部配列の確保があり得る。深さに応じて最大使用量が変わる。Unmatched Popの警告や例外を通常ループに混ぜない。`ApplyMatrix(new float[]{...})`のようなアプリ側配列生成も避けられる。

GPUはquad範囲でSDF評価する。大きな半透明Shapeの多重重なりはfill rate / overdrawに影響する。Polygon fillのfragmentは頂点数ぶん辺を走査するため、「同数のCircle」と「高頂点Polygon」は同コストではない。[Runtime/Shaders/SDFs.hlsl:251-295](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Shaders/SDFs.hlsl:251)。CPUでは曲線展開、階層評価、変調、命令数、GPU upload、Camera数が負荷要因になる。ソースコメントの「数万command」等は測定結果として採用しない。

実測が必要な項目：

- 100 / 1,000 / 将来の最大数でのCPU/GPU frame time、GC Alloc、draw/instance数。
- Circle / Rect中心、Stroke付き、頂点数の多いPolygon、深いGroup、半透明重畳を別条件にする。
- 1920×1080など実出力、目標fps、使用GPU/API、EditorとPlayer、Syphon接続あり/なし。
- `Indirect / Sequential`、depthあり/なし、複数Camera、解像度変更、最大数急増時のspike。
- `Unite.Draw.Capture / Layout / Upload / Submit`のProfilerMarkerでp5コストを分離。[Runtime/Drawing/DrawingContext.cs:164-167](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:164)

定常GCゼロ、数千図形でのfps、Push/Popの所要時間は**未測定**。

### F.3 描画順・深度・透過

キューは呼び出し順に追加され、同一Canvas内のpainter's orderを保持する意図で実装されている。Sequentialやatlas run間は透明物sort用boundsを調整し、シェーダーはcommand indexに由来するdepth biasも使う。Shapeに`sortingOrder`やGroup layerを指定する専用APIは確認できない。シンセ側が兄弟順・Voice順を決め、その順で命令生成する。[Runtime/Drawing/DrawCommandBuffer.cs:27-32](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawCommandBuffer.cs:27)、[Runtime/Drawing/DrawingContext.cs:402-414](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:402)、[Runtime/Shaders/UniteCommon.hlsl:226-231](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Shaders/UniteCommon.hlsl:226)。

同一Canvasの深度無効2D描画に対する順序保持意図と、複数Canvas / 他のUnity透明Rendererとの完全な順序保証は区別する。Indirect instance間の半透明合成、Scene透明物との順序、Camera stackingは実機検証対象。

深度有効時は面のdepth prepassが追加されるが、Line strokeはdepthを書かずテストのみ。半透明でもdepthを記録するため、単純なアルファ合成と同じ結果とは限らない。通常の色合成はpremultiplied alphaを内部生成して`One / OneMinusSrcAlpha`でblendする。[Runtime/Canvas/UCanvas.cs:90-101](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:90)、[Runtime/Shaders/ScreenDrawing.shader:145-152](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Shaders/ScreenDrawing.shader:145)、[Runtime/Shaders/SDFs.hlsl:208-230](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Shaders/SDFs.hlsl:208)。

### F.4 任意Pathの注意と未検証箇所

- `BeginShape`はCanvas内の単一作業領域。BeginShapeをネストすると現在の収集状態を上書きする。Push/PopはPath収集状態を保存しない。[Runtime/Canvas/ShapePath.cs:46-87](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/ShapePath.cs:46)、[Runtime/Canvas/UCanvas.cs:39-49](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/UCanvas.cs:39)
- Open/Closeは主にStrokeを閉じるかの指定で、Fill無効化ではない。線だけにしたい場合はNoFillを明示する。[Runtime/Canvas/ShapePath.cs:303-324](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/ShapePath.cs:303)
- PathではStrokeをenqueueした後にFillをenqueueしている。通常Rectの同一SDF内Fill+Stroke合成と見た目が同一とは断定しない。半透明Strokeや接合を確認する。
- 複数Contourはコメント上even-oddによる穴を意図するが、現CPUコードは複数輪郭の頂点を区切り情報なしで連結して1つのpoint列へ送り、Shaderも1つの閉路として走査する。輪郭間の不要な接続辺・穴のAAに影響する可能性があるため、穴あき多角形を仕様保証しない。単一輪郭の例だけを掲載した。[Runtime/Canvas/ShapePath.cs:519-557](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Canvas/ShapePath.cs:519)、[Runtime/Shaders/SDFs.hlsl:251-295](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Shaders/SDFs.hlsl:251)。これは静的読解上の懸念で、描画不具合の再現確認はしていない。
- Fillの頂点Zは平均化される。3D Transformがあることと、任意3Dメッシュを表現できることは同一ではない。
- Shader Model 4.5、StructuredBuffer、IndirectArgumentsを使う。対象Mac/GPU・Graphics API・ビルドで対応確認が必要。WebGLやモバイルへの互換性は未確認。[Runtime/Shaders/ScreenDrawing.shader:29-40](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Shaders/ScreenDrawing.shader:29)、[Runtime/Drawing/DrawingContext.cs:468-498](/Users/kimurakoya/Documents/develop/unity/krab/Library/PackageCache/net.sbm.unite@81559d3daad3/Runtime/Drawing/DrawingContext.cs:468)

## G. 図形シンセへの推奨アーキテクチャ

以下は**推奨設計**であり、p5.Uniteに組み込まれた機能ではない。

```text
Inspector default ─copy─→ Runtime Definition ← RosettaUI
                              ↕ Prefs保存adapter
Trigger(timestamp) → Voice / Node Instance
                              ↓ Update(time)
                 Parameter + Binding + Modulator + Variation
                              ↓
               Evaluated draw items（形状・行列・Style・順序）
                              ↓ p5 adapter
                UCanvas → Camera → RenderTexture → 出力
```

### 責務

| 概念 | シンセCoreの責務 | p5.Uniteへ渡すもの |
| --- | --- | --- |
| Shape | 種類、寸法、頂点、基底StyleのDefinition | Rect / Circle / BeginShape等の引数 |
| Group | 子Node参照、ローカルTRS、任意Pivot、子順、Trigger規約 | 評価済み行列、またはPush/Popによるスコープ |
| Parameter | 基底値、範囲、単位、変調合成ルール | 実効値 |
| Modulator | ADSR / LFO / 時間評価 | 直接渡さずParameter値へ反映 |
| Binding | 対象ID、深さ、加算/乗算、合成順 | なし |
| Voice | 発音単位の開始時刻、release、phase、乱数、Node Instance | そのフレームのdraw items |
| Variation | seed、Index、Position参照の意味と評価時点 | 評価済みの個体値 |
| Definition | 保存・共有・編集できる構造 | 直接描画状態にしない |
| Instance | 演奏中の状態と評価キャッシュ | そのフレームの描画結果 |

Shape / Groupを共通Nodeとして扱い、Groupは順序付き子Nodeリスト、Shapeはgeometryを持つ通常C#データとする案が自然。Definition IDとInstance IDを分け、同一Definitionから複数Voiceを生成してもADSRやLFO状態を共有しない。

GroupをTriggerしたとき、**1回取得した開始timestampをすべての子へ伝える**。子の列挙中に時刻を再取得しない。timestampはInstance側のデータで、p5.Uniteの時刻APIを必要としない。release時刻と再trigger / Voice stealingは別途規約を定める。

毎フレームの処理を次に分ける。

1. Triggerなど入力イベントを確定する。
2. 共通時刻でInstance / Modulatorを更新する。
3. 基底値とBindingから実効Parameterを評価する。
4. 親子行列と描画順を評価してdraw itemを作る。
5. p5 adapterがdraw itemを順にUCanvasへenqueueする。
6. UCanvasがLateUpdate以降にcapture・描画送信する。

更新と描画の分離は可能。ただしp5.Unite側は毎フレーム再enqueueが必要なので、演奏状態が変わらないフレームも描画命令生成は行う。draw itemバッファを再利用し、更新頻度と描画頻度を別にしても良い。

Coreをp5.Unite非依存にできる。Coreが`Unite.Canvas.Shape` enumやUCanvasを保持せず、独自のgeometry / Style / 数学型を使えばよい。UnityEngineからも独立させたい場合はVector / Color / Matrix型をCore側で別途選ぶ。Unity依存を許容しながらp5依存だけ除く案もあり、これは別々の判断。

p5 adapter側で座標、度→ラジアン、半径→直径、RectMode / EllipseMode、Shape enum変換を一か所にまとめる。GroupやVoiceはp5オブジェクトへ変換せず、最終的な描画命令へ変換する。

## H. ChatGPTと今後決めるべき設計論点

重要度順。以下は未決定事項であり、本調査では固定しない。

1. **論理座標系**：ピクセル / 固定論理解像度 / Normalized / WorldのどれをCoreに採用するか。Y方向、アスペクト維持、解像度変更時のScale、出力Camera限定の要件を決める。
2. **TransformとPivotの契約**：A/B、Positionが原点位置かPivot到達位置か、`TRS`の順、角度のCore単位、Pivot変更時の位置維持、bounds由来Anchorの要否。
3. **Rendererとの境界**：Core評価済み行列をp5へ渡すか、adapterがツリーを走査するか。CanvasLocalを使う場合のUnityホストTransformとの合成規約。
4. **実描画の受け入れ条件**：同一Canvasの半透明順、Stroke/Fill、複数Camera、RT上下/alpha、Scene View、Syphon出力をどの最小シーンで検証するか。
5. **Voice / Group Trigger**：親子の同時開始、release伝播、再trigger、polyphony、Voice stealing、親Groupの変調がVoiceごとか共有か。
6. **Parameterの合成規則**：ADSR/LFOの加算/乗算・順序・Clamp、負Scale、サイズ0、Normalized線幅、基底値と実効値のUI表示。
7. **Definition / Instanceの所有権**：共有Shape Definition、Node ID、実行中編集の既存Voiceへの適用範囲、ツリーの循環禁止と最大深さ。
8. **Variationの基準**：seed固定、Trigger時randomか毎フレームrandomか、Indexの安定性、Positionがlocal/world/変調前後のどれか。
9. **保存・Apply to Inspector**：schema version、ネスト/多態性のserializer、Prefsへの接続、復元時検証、Inspectorへの反映対象とUndo。
10. **性能予算・プラットフォーム**：対象機材とfps、最大Shape / command / 頂点 / Voice数。段階的に測定してから最適化やbackend拡張を判断する。
11. **依存版の固定・更新方針**：`#package`参照とlock hashの扱い、API変更時のadapter修正範囲、Path contourなど疑義のある挙動をどこまで使用するか。

### 調査の到達点

ローカルの導入、公開API、行列合成、Style保存、描画ライフサイクル、mesh/material/buffer構造、既存RT出力との接点を確認した。未確認なのは実機での見た目・性能、上流最新版との差分、複雑Contourの正しさ、Prefsによる新規Definitionグラフの保存適合性。これらを確認済み仕様と混ぜず、次の設計・試作で検証する。
