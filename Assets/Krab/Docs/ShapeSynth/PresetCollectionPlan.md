# ShapeSynth：演奏用プリセット30個の追加計画

2026-11-22作成。同日に30個のデータ追加・登録・Unity内検証を実施した。製品コードとSceneは変更していない。以下は作成時の計画と初期値。

2026-11-25：Visualを作り直すため、30個のプリセット（`Samples/Presets/` の原本と実行時の保存先）を削除した。共通Value（`Values.json`）は残している。原本はGit履歴にある。実行時のファイルとバックアップは残していない。

## 目的と範囲

好きな2Dの造形と動きを作り込み、音楽を聴いて選び、叩き、重ねられる30個を追加する。音色を作る感覚で、硬い一発・減衰・溜め・移動・周期運動を揃える。曲に対して使い方を固定しないため、Kick／Snare等の楽器名ではなく見た目で命名する。

- 既存のPreset Editorで編集・保存・再読込できるSchemaVersion 5のデータのみ追加。
- 新機能、保存形式変更、組み込みプリセット、共有Valueライブラリ、インポーター、ルーパー、描画機能の追加は行わない。
- 既存の `group/name` 規則でPerformanceのタブを5つに分け、各6個とする。Preset Editor内に新しいタブは作らない。
- 現在のPreset EditorはVisual／Valuesの別ウィンドウ。古い説明文書より現行コードを正とする。
- 参考：`synth-machine`（ローカル絶対パスは下記）。ユーザーが対象を指定済み。
- 思想の背景は [VJの制作姿勢](../VJ/ArtisticIntent.md)。今回の計画とは分ける。

## 参照したp5と対応方針

参照元：`/Users/kimurakoya/Documents/develop/p5/synth-machine`。ソース読み取りのみで、今回p5の映像を実行・鑑賞したわけではない。

| p5のファイル（src/synth/presets/配下） | 着想 | 今回の対応 |
|---|---|---|
| kick01.ts | 中央の円と短い立ち上がり・減衰 | Hitの円、同一Valueのサイズ／不透明度への共有 |
| snare03.ts | 左右の六角形とSquare LFO | Organicの左右の三角形ペアへ置き換える。LFOは負値を取る前提でBaseを設定 |
| hihat02.ts | 細長い矩形が右から左へ移動 | Lineの水平移動。64Hz相当の細かな震えはそのまま移植しない |
| snare01.ts | 円周上の三角形が外へ広がる | Radial。Circle配置のRadiusを変調して放射移動を作る |
| percussion01.ts | 疎らな多角形グリッド | Arrayの固定した疎配置。ランダムな頂点数や出現選別は追加しない |

30ファイルを1対1移植する計画ではない。p5とUnityでは信号の合成や時間の定義が異なるので、数値を機械的にコピーしない。高頻度LFO、画像、ブレンドモード、ノイズ、頂点アニメーションは今回の必須要素にしない。

## Valueの共通化

### 共通化する範囲

- 同じプリセット内では、同じ信号に従わせたい全Parameterに同じValueIdをBindする。
- 大きさの違い、反転、方向の違いは、まずBase／Depth／Flip／Group Transformで表す。これだけのためにValueを複製しない。
- 複製の時間差はLayoutのDelayで表す。
- 位相差、時間形状、周波数が本当に異なる場合だけ別Valueにする。
- 複数プリセットで共通のレシピと名前を使う。ただし保存上のValueは各Documentが所有する。プリセットをまたぐライブ共有は行わない。
- 一つのParameterにBindできるValueは一つ。複数の信号が必要な場合はValue内のComponentを加算する。Value同士の乗算や、ADSRによるLFOの振幅包絡はできないので要求しない。

### 基本レシピ（初期値）

時間は全て拍。ADSR表記は `(Attack, Decay, SustainLevel, SustainTime, Release)`。Weightは特記以外1、未指定CurveはLinear。数値は実装時の初期案で、目視調整して確定する。

| 名前 | 定義 | 意図 |
|---|---|---|
| Hit | ADSR (0.02, 0.18, 0.25, 0.05, 0.25)、Decay=OutQuad、Release=OutSine | 0.5拍の立ち上がりと尾 |
| Tick | ADSR (0, 0, 1, 0.0625, 0.0625)、Release=Linear | 0.125拍の硬い短音 |
| Bloom | ADSR (0.5, 0, 1, 0, 0.5)、Attack=InOutSine、Release=InOutSine | 1拍で膨らんで戻る |
| Spring | ADSR (0.25, 0, 1, 0.125, 0.625)、Attack=OutBack、Release=OutCubic | 1拍の弾性。Opacityには使わない |
| Swell | ADSR (0.5, 0.25, 0.65, 0.75, 0.75)、Attack=InOutSine、Decay=OutSine、Release=InOutSine | 2.25拍の柔らかい持続 |
| Travel(t, curve) | ADSR (t, 0, 1, 0, 0)、Attack=指定Curve | 0→1の一方向の進行。終了時の0戻りは消灯と合わせる |
| Gate(t) | ADSR (0.01, 0, 1, t−0.06, 0.05) | t拍で必ず消灯。tは0.5以上 |
| Sine(f, phase) | Sine LFO、CyclesPerBeat=f、PhaseOffset=phase | ±1の滑らかな往復 |
| Triangle(f, phase) | Triangle LFO、CyclesPerBeat=f、PhaseOffset=phase | ±1の均等な往復 |
| Square(f, phase) | Square LFO、CyclesPerBeat=f、PhaseOffset=phase | ±1の硬い切替 |
| Flutter | Weight 0.8のHit ADSR ＋ Weight 0.2のTriangle LFO(f=2, phase=0) | 加算合成の例。Scale／Rotationに使い、Gate(0.5)で消灯 |

全プリセットにADSRを含め、有限寿命を与える。LFOだけのプリセットは作らない。SpringやFlutterをOpacityに使ってClampに依存する画面は避ける。Sine(f,0.25)を別Valueとして置くのは、XYの位相差が必要な場合のみ。

## 共通の造形・色・命名

- 名前は `01_Hit/01 Solid Pulse` の形式。既存の名前順ソートでタブ順／項目順を安定させる。
- 中央原点、X右・Y上。M=画面短辺、W=幅、H=高さ。距離／大きさはxMin／xW／xHで設定し、解像度に依存しすぎない。
- 下表の `Scale=0.2+0.8×Hit` は、ScaleのBase=0.2、Depth=0.8、Bind=Hitを意味する。単位は表記に従う。
- CircleはWidthを直径として使い、等方ScaleはScaleXとScaleYへ同じValueを接続。
- 線はRectangle、枠はStroke、Polygonは新規作成時の標準三角形だけを使う。星は三角形2つのGroupで作る。現行Editorに頂点編集がないため、任意頂点を外から投入しない。三角形は半径100pxの固定頂点なので、表の外径は基準解像度1920×1080に合わせた固定Scaleで整える（外径dなら基準倍率=d×1080÷200）。動的Scaleと分けたい場合は外側Groupに固定倍率を置く。Polygonまで自動で解像度追従するとは扱わない。
- 色は白／シアン／コーラル／黄／ライムを出発点に、1プリセット1〜2色。色違いだけで別プリセットにしない。p5のPalette依存は持ち込まない。
- 原則1〜3 Values、XY位相差＋Gateが必要なら3 Values以上を許す。共通化の数だけを目的に動きを無理に同期させない。
- 原則1〜32描画Shape、最大48を目安。全面を埋めるものと、余白を残して重ねやすいものを混在させる。

## 30個の具体案

表の寸法・色・時間は登録前の初期案。Opacityは特記しない限り記載したADSRのValueに接続し、Base=0／Depth=1。LFOを用いる場合はGateで消灯する。サイズ・Opacity以外が固定の行も、色違いの水増しではなく、輪郭と時間形状の違いとして扱う。

### 01_Hit — 一発の形（6個）

| 名前（/以降） | 見た目・構成 | Valueと主なBind |
|---|---|---|
| 01 Solid Pulse | 中央のコーラル円、直径0.30M | Hit共有：ScaleXY=0.2+0.8×Hit、Opacity=Hit |
| 02 Ring Bloom | シアンの輪、直径0.32M、Stroke 3px | Bloom共有：ScaleXY=0.35+0.65×Bloom、Opacity=Bloom |
| 03 Diamond Snap | 白の矩形を45度回転、辺0.22M | Tick共有：ScaleXY=0.85+0.15×Tick、Opacity=Tick |
| 04 Cross Punch | 黄の縦横2矩形、長さ0.34M・太さ0.025M | Hitを2つのShapeで共有：長辺=0.10M+0.24M×Hit、Opacity=Hit |
| 05 Star Spring | ライムの標準三角形2つを0／180度に重ねた六芒星、外径0.30M | Spring：ScaleXY=0.25+0.75×Spring、Rotation=−15+30×Spring。Gate(1)→Opacity |
| 06 Facet Flutter | シアンの三角形、外径0.27M | Flutter共有：ScaleXY=0.65+0.35×Flutter、Rotation=−8+16×Flutter。Gate(0.5)→Opacity |

### 02_Line — 線と移動（6個）

| 名前 | 見た目・構成 | Valueと主なBind |
|---|---|---|
| 01 Horizontal Sweep | コーラルの縦線、幅0.012W・高さ0.65H | Travel(0.5,Linear)→X=0.45W−0.9W×Travel。Gate(0.5)→Opacity |
| 02 Rising Bar | 白の横線、幅0.55W・高さ0.015H | Travel(1,OutCubic)→Y=−0.35H+0.70H×Travel。Gate(1)→Opacity |
| 03 Opposed Slashes | ±35度の細線2本、左右から寄って戻る | Bloomを共有：X=±(0.28W−0.20W×Bloom)、ScaleY=0.6+0.4×Bloom、Opacity=Bloom |
| 04 Corner Brackets | 四隅にL字を構成する矩形8個 | Hit共有：各角GroupのX／Yを画面端方向へ0.025W／H変位、各ShapeのOpacity=Hit |
| 05 Ribbon Stretch | 中央のシアン横帯、幅0.30W・高さ0.025H | Spring共有：ScaleX=0.4+1.6×Spring、ScaleY=1−0.55×Spring。Gate(1)→Opacity |
| 06 Rotating Needle | 中央から伸びる黄の細線、子を0.16MずらしたGroup | Travel(1,Linear)→親Rotation=−90+180×Travel。Gate(1)→Opacity |

### 03_Array — 配列と時間差（6個）

| 名前 | 見た目・構成 | Valueと主なBind |
|---|---|---|
| 01 Dot Row | 白い円8個、Row、間隔0.09W、直径0.045M | Hit共有：ScaleXY=0.25+0.75×Hit、Opacity=Hit。Delay=0.0625、Index |
| 02 Tile Wave | シアン矩形24個、Grid 6列、辺0.065M | Bloom共有：ScaleXY=0.2+0.8×Bloom、Opacity=Bloom。Delay=0.125、Column |
| 03 Checker Split | コーラル矩形24個、Grid 6列、辺0.055M | Bloom共有：XのDepth=0.02WにChecker Flip、RotationのDepth=20度にChecker Flip、Opacity=Bloom |
| 04 Sparse Confetti | 円・矩形・三角形の12個を固定した疎な配置、ピンクと白 | Tick共有：全ShapeのOpacity。Rotation Rand=35度、X/Y Rand各0.015M。選別処理は作らない |
| 05 Center Fan | 黄の縦線9個、Row、間隔0.07W、高さ0.28H | Hit共有：ScaleY=0.15+0.85×Hit、Opacity=Hit。Delay=0.0625、CenterOut |
| 06 Alternating Bounce | ライム円12個、Grid 4列、直径0.055M | Spring→YのDepth=0.035HにRow Flip、ScaleXY=0.5+0.5×Spring。Gate(1)→Opacity |

### 04_Radial — 円周・放射・回転（6個）

| 名前 | 見た目・構成 | Valueと主なBind |
|---|---|---|
| 01 Triangle Burst | シアン三角形8個、Circle、外径0.08M、FaceOutward | Travel(0.5,OutQuad)→Radius=0.08M+0.28M×Travel、各Rotation=15×Travel。Gate(0.5)→Opacity |
| 02 Orbit Dots | 白い円12個、Circle、直径0.035M | Sine(0.5,0)共有：Radius=0.22M+0.045M×Sine、親Rotation=20×Sine。Gate(2)→Opacity |
| 03 Ring Chase | 黄の円16個、Circle、Radius=0.25M、直径0.045M | Tick共有：Opacity、ScaleXY=0.6+0.4×Tick。Delay=0.0625、Index |
| 04 Petal Breath | コーラルの矩形8個、Circle、0.035M×0.18M、FaceOutward | Bloom共有：Radius=0.15M+0.08M×Bloom、ScaleY=0.4+0.6×Bloom、Opacity=Bloom |
| 05 Iris Close | シアンの輪3個、直径0.20／0.34／0.48M | Travel(1,InOutSine)共有：各ScaleXY=1−0.85×Travel、各Stroke Width=2px+3px×Travel。Gate(1)→Opacity |
| 06 Radial Ticks | ライムの細矩形16個、Circle、Radius=0.27M、FaceOutward | Square(2,0)共有：ScaleY=0.8+0.2×Square、親Rotation=5×Square。Gate(1)→Opacity |

### 05_Organic — 有機性と周期（6個）

| 名前 | 見た目・構成 | Valueと主なBind |
|---|---|---|
| 01 Floating Ellipse | シアンの楕円、直径0.18MのCircleを縦倍率0.25/0.18の固定Groupに置く | Sine(0.5,0)共有：Y=0.06H×Sine、ScaleX=1+0.12×Sine、ScaleY=1−0.12×Sine。Swell→Opacity |
| 02 Breathing Frame | 白い矩形枠、0.55W×0.45H | Swell共有：ScaleX=0.8+0.2×Swell、ScaleY=0.9+0.1×Swell、Opacity=Swell |
| 03 Facet Pair | 左右のシアン三角形2個、外径0.17M | Square(2,0)共有：ScaleXY=0.85+0.15×Square。Hitを共有：各ShapeのOpacityとXの内向き変位0.025W |
| 04 Lissajous Trio | コーラルの円3個を同じGroupに固定配置 | Sine(0.5,0)→親X=0.10W×Sine、親Rotation=12×Sine。Sine(1,0.25)→親Y=0.08H×Sine。Gate(2)→Opacity |
| 05 Elastic Stack | 黄の矩形3段、異なる幅・高さを固定 | Spring共有：各ScaleY=0.6+0.4×Spring、各Y=固定値±0.025H×Spring、Rotation=±8×Spring。Gate(1)→Opacity |
| 06 Soft Orbit | ライムの輪と内側の小円、子Groupで構成 | Sine(0.25,0)→小円X=0.12M×Sineと輪ScaleX=1+0.05×Sine。Sine(0.25,0.25)→小円Y=0.12M×Sineと輪ScaleY=1+0.05×Sine。Gate(4)→両Opacity |

Opacity／Stroke WidthはShapeにBindし、GroupにはTransformだけをBindする。Circle配置のRadiusは全スロットに共通で、Layout DelayはRadiusには適用されないため、Radius変調と時間差を組み合わせた放射ウェーブは今回作らない。

## 登録手順（後続の実装時）

1. 対象Unityへの接続、Play Mode、開いているSceneのdirty状態、編集中のDocumentを確認。未保存の編集は保護する。実際のCLI helpとコマンド一覧を確認して操作方法を決める。
2. 保存済みプリセットのID／名前／件数と保存先を確認し、既存ファイルを退避する。同名があれば自動上書きしない。今回用のID対応表を作る。
3. 各タブから代表を1つずつ、既存Preset EditorのDocument／SaveDocument経由で作成・保存・再Openする。自動操作が必要でも一時的な作業用手段に留め、製品コードへ登録機能を追加しない。
4. 代表5個で信号、単位、共有Binding、色・サイズを確認後、残り25個へ展開する。
5. パラメータJSONの再現可能な原本を `Assets/Krab/ShapeSynth/Samples/Presets/` に保存する案とする。実際の登録先は既存Repositoryの `Application.persistentDataPath/ShapeSynth/Presets`。起動時自動コピーは追加しない。原本と登録ファイルが一致することを確認する。
6. 新規30個だけのID・ファイル一覧を記録する。再登録時もIDを維持し、既存MIDI割り当てや他のプリセットを保護する。今回の計画ではMIDI割り当て変更を要求しない。
7. 既存の `ShapeSynthPresetEditor.unity` と `ShapeSynthPerformance.unity` で確認する。プリセットデータだけの変更なので、新しい検証Sceneや新しいUIは作らない。

## 検証と完了条件

- データ：新規30個、各タブ6個、重複IDなし、Schema 5、全Value参照が有効。未接続Value・同一レシピの不要な重複がない。既存PresetのID・内容に変更なし。
- Compile：Unity上で全30個のValidate／CompileとRepository再読込が成功する。CoreやRepositoryの変更はしない。追加の恒久的テストコードは原則不要。
- Editor：全30個をOpenし、Visual／Valuesで編集・保存できる。共有すべきParameterが同一ValueIdを参照する。既存Editorにない隠し設定を使わない。
- 時間：ADSRの開始・ピーク・終了、最大Delay後の消灯、Voice破棄を確認。期待する終了拍はDot Row=0.9375、Tile Wave=1.625、Center Fan=0.75、Ring Chase=1.0625。Travel終了の0戻りが見えないことを確認する。
- LFO：負の信号で幅・高さが負にならず、想定外の反転や透明度Clampを起こさない。Flutterは加算のままで期待する画面になるかを確認する。
- 性能：120 BPMを基準に、同一Presetを続けて発火・異なるPresetを重ねた状態を確認。80／160 BPMでも拍単位の振る舞いを確認。新しい性能保証や負荷制御機能は追加しない。
- 表示：5タブ、各6行、名前と順番を確認。Randを含むものは複数Seedで破綻しないことを確認。
- 視覚記録：全30個について代表Beatの静止画、必要なものは複数時刻。`Reports/ShapeSynthPresetCollection/` に保存。静止画だけで演奏感を検証済みとはしない。
- 保存：原本と登録先の一致、Unity Consoleの新規Errorなし。ログ・画像・一時作業コードはReports等のgitignore領域へ置く。
- ユーザー確認：Sceneを開いてPlayし、Triggerだけで確認可能にする。見た目・使い心地の最終評価はユーザー。自動コミットしない。

## 確認した実装箇所

- `Scripts/Core/Presets/PresetDocument.cs`：ValuesはDocument単位、一Parameter一Binding、形状と階層。
- `Scripts/Core/Presets/PresetComponent.cs`：ADSR／LFO／Weight／Curve。
- `Scripts/Core/Presets/PresetCompiler.cs`：Value内Componentは加算、TriggerにはADSR必須。
- `Scripts/Core/Presets/PresetLayout.cs`：Row／Column／Grid／Circle、Radius、Delay。
- `Scripts/Core/Presets/PresetPath.cs` と `Scripts/Performance/ShapeSynthPerformance.UserPresets.cs`：group/nameでPerformanceタブを分類。
- `Scripts/PresetEditor/ShapeSynthPresetEditor.UI.cs`：Name (group/name)、Visual／Valuesウィンドウ。
- `Scripts/Unity/Persistence/PresetRepository.cs`：保存先、JSON検証と保存。既存の未コミット変更があるため手を加えない。

これらのパスは `Assets/Krab/ShapeSynth/` を基準とする。旧文書のColor MixやSchemaVersion等と現行コードに差がある箇所は、今回のデータに持ち込まない。
