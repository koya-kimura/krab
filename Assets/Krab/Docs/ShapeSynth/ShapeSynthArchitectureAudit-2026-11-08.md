# 図形シンセシステム 現行実装調査レポート

調査日：2026-11-08（日本時間）。実装・既存コードの変更は行わず、本レポートのみ作成した。

## 1. 現行実装の概要

### 1.1 調査範囲と証拠の扱い

本レポートの「現状」「実装」はローカルのコードを指す。「提案」「要判断」は将来案であり、実装済みではない。リモートの最新状態との同期、ブラウザでの描画検証、Unityの実行検証は行っていない。コードの読み取りと式の限定的な数値確認に基づく。

| 対象 | ローカルパス | 調査時 HEAD / 状態 |
|---|---|---|
| p5 コア | `/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system` | `29399ba6ac01978e5df14bfa65b0e4039473e237` / clean |
| Generator | `/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator` | `9f2a7093aac65276b1ef9a9ea9ad2b6d3dae62b8` / clean |
| 現在の Unity | `/Users/kimurakoya/Documents/develop/unity/krab` | `717093b` / 既存の Midi.meta 変更と未追跡 MidiInputManager.cs、同 .meta あり |
| 抽出元の補足調査 | `/Users/kimurakoya/Documents/develop/p5/synth-machine` | `b5307c8b91a94b87125b59e19fc517c26f7f2863` / kick01.ts変更、docs/handover.md未追跡あり |

`p5-synth-shape-system/package.json` の name は `@koya-kimura/synth-machine-core`。今回見つかった範囲では、この2つは同じコア資産である。Generator は GitHub 依存名 `p5-synth-shape-system` で利用し、lockfile は上記コア HEAD と同じコミットを指す。根拠：[package.json:1](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/package.json:1)、[package-lock.json:1092](/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator/package-lock.json:1092)。抽出元 synth-machine は系譜と時間・パターン機能の補足に限り調べ、全体を同等には監査していない。

### 1.2 全体構造と処理フロー

現行は「Preset が図形の設定を生成し、図形ごとに包絡・移動・描画を実行する」構成。汎用の信号接続グラフではない。

```text
Generator の UI state / コア demo の JSON
  → 既知 preset key の照合 + 既存 PresetSpec への patch
  → buildPresetFactory(spec)
  → SynthEngine.triggerPreset(p, index, bpm, startTime)
  → resolveLayout → pointOrder → 各点 × shapes/layers × repeat
  → ScalarExpr 評価 → 数値設定を持つ SynthObject を生成
  → Engine.update → 各 Object の ADSR / Position 更新 → DEAD を除去
  → Engine.draw → presetIndex順に Object.display
```

コアは `src/core`（実行）、`src/shapes`（p5描画）、`src/presets/system`（型・式・配置・生成）、`src/presets/library`（既存レシピ）、`src/utils` に分かれる。Generator は `src/main.js` に Editor と Gallery の大部分が集約されている。

| ファイル / クラス | 主要メソッドと責務 |
|---|---|
| [src/core/SynthEngine.ts:12](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/SynthEngine.ts:12) `SynthEngine` | `triggerPreset` で図形追加、`update` で更新・死亡除去、`draw` で整列・描画、`setPresets` で次回生成用Factory差替え |
| [src/core/BaseSynthObject.ts:13](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:13) `BaseSynthObject` | 設定・開始時刻・BPM・ADSR状態・移動・描画共通処理を同一Objectに保持。`updateADSREnvelope`、`calculateLFO`、`beginDraw` |
| [src/shapes/RectSynthObject.ts:14](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/shapes/RectSynthObject.ts:14) `RectSynthObject` | `display`、`calculateDimensions`。aspectRatio と stretchMode から幅・高さを算出 |
| [src/shapes/CircleSynthObject.ts:13](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/shapes/CircleSynthObject.ts:13) `CircleSynthObject` | `display`。sizeを半径相当として楕円描画 |
| [src/shapes/PolygonSynthObject.ts:24](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/shapes/PolygonSynthObject.ts:24) `PolygonSynthObject` | `generateVertices`、`calculateVertexPosition`、`calculateVertexLFO` |
| [src/presets/system/builder.ts:265](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/builder.ts:265) | `buildPresetFactory`。配置解決・各点の式評価・Object生成 |
| [src/presets/system/evaluator.ts:131](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/evaluator.ts:131) | `evaluateScalar`、`evaluateCondition`。生成時の設定式を評価 |
| [src/presets/system/layouts.ts:20](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/layouts.ts:20) | `resolveLayout`。single/line/grid/ring/scatter/waveLine/spiral を座標配列へ変換 |
| [src/core/Looper.ts:7](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/Looper.ts:7) `Looper` | 記録・再生・`getEventsToPlay`。presetIndex と記録時刻を保持 |
| [src/core/LoopBank.ts:10](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/LoopBank.ts:10) `LoopBank` | 複数Looperの選択・操作・イベント結合 |
| [src/presets/library/catalog.ts:89](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/library/catalog.ts:89) | JSON entry のpatchからFactory生成、`validateJsonPresetEntries` |
| [src/main.js:54](/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator/src/main.js:54) `setupEditor` | UI state→JSON生成→検証→Factory再構築。図形、ADSR、LFOなどを編集 |
| [src/main.js:714](/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator/src/main.js:714) Gallery内関数 | 独自の `computeItemLevel` / `computeLfoMul` / `drawItemContents`。コアとは別のプレビュー実装 |

ADSR/LFOの独立クラス、汎用Parameter/Modulation Binding、Voiceクラスはコア内に見つからない。`BaseSynthObject` が「単一図形を伴う独立した一回の演奏状態」に近い。Factory一回は複数図形を返すが、そのまとまりを存続するVoiceとして管理する仕組みはない。

### 1.3 Unity 側の現在地

現在の krab には MIDI入力・Action・割当・UI・BPM・出力基盤がある。一方、今回の図形シンセの Shape、ADSR、LFO、Layout、Voice、SynthEngine に相当する実装は調査したアプリコード内で確認できなかった。描画タスクも [Assets/Krab/Docs/Task.md:24](/Users/kimurakoya/Documents/develop/unity/krab/Assets/Krab/Docs/Task.md:24) では未具体化。p5版をUnityに移植済みとは扱えない。

## 2. 今回の設計との対応表

| 新しい概念 | 現在の実装 | 対応状況 | 主な課題 |
|---|---|---|---|
| Shape | Circle/Rect/PolygonSynthObject | 3種あり | 形状・演奏状態・描画が継承で結合 |
| Parameter | SynthObjectConfig、SynthParams、shape別設定 | 値はあるが汎用Parameterではない | Reference/Base、任意target ID、合成結果の独立管理なし |
| Modulator | BaseSynthObject内ADSR・LFO、移動角LFO、頂点LFO | 特定用途のみ | 信号生成を独立させ、出力と適用先を分離する必要 |
| Binding | サイズやalphaへ直接代入する処理 | 汎用接続なし | 任意の多対多接続・接続別Depthなし |
| Voice | 各BaseSynthObjectが独立startTime/状態を持つ | 部分対応 | 複数Shapeを含むVoice、安定ID、再Trigger/Gateなし |
| Variation | ScalarExprとEvalContext | 生成時分布は相当部分あり | 評価時点、Voice identity、seed方針が明示されていない |
| Layout | LayoutSpec→LayoutPoint[] | 7種あり | 配置点とVoice/Shapeの関係、移動前後座標の区別 |
| Engine | SynthEngine | 生成・更新・描画管理あり | 信号評価順、Shared状態、編集反映、Transport管理なし |
| Definition / Instance | PresetSpec→生成Object | 境界の原型あり | Modulator単位の共有Definition/独立Instanceはない |
| Preset / JSON / DSL | PresetSpec、式AST、既知key+patch | 設定生成あり | 自立した完全patch、version、深い検証、参照IDなし |
| Runtime UI | Generator Editor / Unity RosettaUI | 各環境であり | 生存中Instanceへ状態を保った反映は未対応 |

Unityの `Param<T>` は値とChanged通知、`MidiBinding` はMIDI controlとActionの割当である。名前が似ていても今回のParameter/Modulator Bindingとは異なる。根拠：[Assets/Krab/Common/Scripts/Midi/Core/Param.cs:6](/Users/kimurakoya/Documents/develop/unity/krab/Assets/Krab/Common/Scripts/Midi/Core/Param.cs:6)、[Assets/Krab/Common/Scripts/Midi/Bindings/MidiBindingManager.cs:31](/Users/kimurakoya/Documents/develop/unity/krab/Assets/Krab/Common/Scripts/Midi/Bindings/MidiBindingManager.cs:31)。

## 3. 現在の仕様と制約

### 3.1 Parameter と Modulator の計算

コアの基本サイズは次の式である。

```text
elapsedSeconds = (p.millis() - startTime) / 1000
phase = elapsedSeconds × (生成時BPM / 60) × lfoRate
LFO実寸出力 = waveform(phase) × baseSize × lfoDepth
uniformSize = baseSize × currentLevel + LFO実寸出力
```

根拠：[src/core/BaseSynthObject.ts:180](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:180)（波形）、[src/core/BaseSynthObject.ts:203](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:203)（LFOとサイズ）。`lfoRate` は Hz ではなく1拍あたりの周期数に相当する。`lfoDepth` はbaseSizeに対する比率であり、接続別の係数ではない。

今回の式 `R × (B + Σ d_i M_i)` に似ているが、現状は `R=baseSize、B=0、ADSR係数=1、LFO係数=lfoDepth` の特定ケースに近いだけで、一般化されたAPIはない。ADSRは生成と同時に必ず開始し、Bindingなしの固定Parameterも提供していない。ADSR/LFO設定を省略しても既定ADSRが使われる（[src/core/types.ts:80](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/types.ts:80)）。

| 対象 | 現在の適用方法・制約 |
|---|---|
| Rectangle | baseW=baseSize×aspectRatio、baseH=baseSize。`ADSR + LFO/baseSize` をuniform/horizontal/verticalに適用。最終幅高は各 `Math.max(1, value)` |
| Circle | uniformSize×2を高さ、さらにaspectRatioを幅に適用。コア内でサイズClampなし |
| Polygon | 頂点baseRadius×ADSR + サイズLFO + 任意の頂点sine LFO。頂点ごとの位相offsetあり |
| Opacity | `currentLevel×255` をalphaに直結。サイズと同じADSR値を利用。Opacity専用Base/Depthなし |
| Rotation | rotationAngle固定。共通の回転変調targetはない |
| Position | lifetimeに基づく移動と、任意の移動方向sine LFO。汎用Binding経由ではない |
| 色 | HSB固定値またはPaletteResolver。汎用Modulatorから色へ接続する機構なし |

根拠：[src/shapes/RectSynthObject.ts:36](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/shapes/RectSynthObject.ts:36)、[src/shapes/CircleSynthObject.ts:21](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/shapes/CircleSynthObject.ts:21)、[src/shapes/PolygonSynthObject.ts:92](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/shapes/PolygonSynthObject.ts:92)、[src/core/BaseSynthObject.ts:156](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:156)、[src/core/BaseSynthObject.ts:220](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:220)。

ADSRの通常設定（非負時間・Sustain 0〜1）では0〜1を意図した線形包絡だが、Sustain自体のClampはなく、異常設定で範囲保証されない。LFOのsine/triangle/saw/squareは通常の正方向位相では-1〜1。noiseは実装上例外である。

**noiseの範囲の注意**：式が `(sin(...) * 43758.5453) % 2 - 1`。JSの負の剰余により値はおおむね-3〜1になり得る。seed=123、位相0〜1未満の10,000点を同式で確認すると約-2.9998〜0.9999だった。これは描画テストではなく数式の再評価。根拠：[src/core/BaseSynthObject.ts:196](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:196)。また `waveform` は設定として保持されるものの、この波形選択は `lfoType` を読む。`frequency` getterの440は固定値で、LFO実周波数の観測値ではない（[src/core/BaseSynthObject.ts:271](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:271)）。

**クリッピング方針との違い**：現状はRendererと計算が一体で、Rectangleには下限1、Circle/Polygonには同等の制限なし。合成値の共通Clampはないが、今回の「計算値を保存し、利用層が制約を解釈する」分離にはなっていない。`baseSize=0` のRectangleでは `0/0` が発生し得る。builderのoptional値は非有限数をundefinedにする一方、必須size/x/yなどすべてを検証してはいない（[src/presets/system/builder.ts:50](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/builder.ts:50)、[src/presets/system/builder.ts:142](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/builder.ts:142)）。

### 3.2 Definition / Instance、Shared / Local

`PresetSpec` は保存可能なレシピに近く、Factoryが各設定式を数値に解決し、Objectのconstructorが `resolveSynthParams` で設定を作る。同じSpecから複数Objectを生成し、それぞれ独立のstartTime/currentPhase/currentLevelを持つことはできる。これはLocal状態の原型になる。

一方、ADSR設定は各Object内の数値へ展開され、Modulator DefinitionをIDで共有参照する仕組みではない。設定と実行状態は同一Objectに同居する。複数Parameterが同じADSR実行値を見る例はサイズとalphaにあるが、任意接続ではない。複数Shapeが同じ実行Instanceを参照するShared機構はない。根拠：[src/core/BaseSynthObject.ts:14](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:14)、[src/core/BaseSynthObject.ts:39](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:39)、[src/presets/system/builder.ts:61](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/builder.ts:61)。

### 3.3 ADSR / Time / Trigger / Gate / Looper

- Attack/Decay/Releaseの単位は拍。生成時のBPMで `beats×60000/BPM` に変換して固定する（[src/core/types.ts:99](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/types.ts:99)、[src/core/BaseSynthObject.ts:55](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:55)）。LFOも生成時BPMを保持するため、生存中Objectは後のBPM変更に追従しない。
- `update` はフレーム数ではなく `p.millis()-startTime` を利用。ただしADSRはswitchで1回につき1相しか処理しない。大きな時間飛びやゼロ時間区間でも、複数相の遷移に複数フレームかかる。厳密な評価結果までフレームレート非依存とはいえない（[src/core/BaseSynthObject.ts:72](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:72)、[src/core/BaseSynthObject.ts:84](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:84)）。
- Sustainに保持時間やGate条件がなく、Attack+Decay以降は次の更新でReleaseに進む。totalLifetimeMsもA+D+R。Gate On/Off、保持中のRelease、同一Voice再Trigger、全Voice Gateはない。Triggerは新Object群の生成である。
- LFOはstartTime基準なので新規生成時に位相ゼロから始まる。任意Phase、既存Instanceへの位相リセット、Shared/Free Runの選択はない。Polygon頂点LFOのoffsetは専用実装。
- LooperはpresetIndexとtimestamp、optional beatを記録するが、再生ではtimestampだけを使う。BPM追従する拍ベースのループではない（[src/core/Looper.ts:54](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/Looper.ts:54)、[src/core/Looper.ts:66](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/Looper.ts:66)）。
- Looperのイベント窓は `[現在位置-deltaTime, 現在位置)`。demoはp.deltaTimeを渡すが、長い停止で複数周した回数の再生は復元しない。戻り値もindexだけなので、予定時刻を引き継がず検出フレーム時刻で生成される（[demo/src/main.ts:68](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/demo/src/main.ts:68)）。
- Generator Editorの反復は `now-lastLoopMs >= interval` のとき1回発火してlastLoopMs=nowにする。絶対拍のTransportではなく、フレーム遅延の蓄積や取りこぼしを許す実装（[src/main.js:486](/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator/src/main.js:486)）。
- 補足：抽出元には `PatternPlayer.getEventsToPlay(currentBeat)` があるが、コアのSynthEngineに同梱されたSequencerではない。8拍moduloと許容誤差で範囲照合し、deltaTime引数は未使用。隣接判定窓の重なりや長時間飛びを含め、そのまま精密なschedulerとは扱わない（[src/synth/patterns/patternPlayer.ts:39](/Users/kimurakoya/Documents/develop/p5/synth-machine/src/synth/patterns/patternPlayer.ts:39)）。

Unity `BPMManager.Update` は `Beat += Time.deltaTime * BPM/60`、整数拍を跨ぐとOnBeatを1回通知し予約BPMを適用する。複数拍を飛ばしても通知は1回である。現行の共通clock候補だが、時刻付きイベントの欠落補償まではない。根拠：[Assets/Krab/Common/Scripts/Timing/BPMManager.cs:27](/Users/kimurakoya/Documents/develop/unity/krab/Assets/Krab/Common/Scripts/Timing/BPMManager.cs:27)。

### 3.4 Variation と Layout

**重要：個体差はすべて新規実装というわけではない。** `EnvelopeRecipe.sustain` も旧 `params.sustainLevel` もScalarExprを受け、配置点ごとに評価される。したがって同一のレシピ内でSustainだけを変えることは可能。Depthを変えるだけの実装ではない。根拠：[src/presets/system/types.ts:166](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/types.ts:166)、[src/presets/system/builder.ts:75](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/builder.ts:75)。

| 要件 | 現在できること | 不足・条件 |
|---|---|---|
| Fixed | 数値定数 | 専用Variation型は不要でも表現できる |
| Index | index/totalを使いmapや四則演算でSustain等を分布 | indexは配置順。repeatや複数shapeで同じindexを共有 |
| Random | rand(seed,index,...)で決定的な値を生成 | seedは最大3成分。Voice/項目/Triggerのseed domain管理なし |
| Position | pointX/pointYから設定値を算出 | Layout生成直後の座標であり、移動後/recipe.x上書き後座標ではない |
| Voice生成時 | Factory内で設定を一度数値化 | 既存Voiceが保持されるモデルではなく新Object生成時 |
| Trigger時再評価 | TriggerごとにFactoryを再実行 | 同一Instance再Trigger時の再抽選は未対応 |
| LFO Frequency等 | rate、depth、movementなど既存ScalarExpr項目を分布 | Phase項目や汎用Binding Depth自体がない |

以下は**既存APIから構成できる例（新規実装・実行検証はしていない）**。4点ならSustainを0.2/0.4/0.6/0.8にできる。

```ts
// 既存の mapExpr / ctx / max / sub を用いる EnvelopeRecipe の一部
sustain: mapExpr(ctx("index"), 0, max(1, sub(ctx("total"), 1)), 0.2, 0.8)
```

Count=1なら分母相当を1にすることで0.2になる。現行 `map` 関数自体には入力範囲ゼロの保護がない（[src/utils/math.ts:1](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/utils/math.ts:1)）。この例は「共有ADSR Definitionにoverrideを掛けるAPI」の実装証拠ではなく、共通レシピを各Objectの異なる数値設定に評価できることを示す。

LayoutはShapeを直接複製するクラスではなく、まずLayoutPoint配列を作り、その各点でshapes/layersとrepeatを展開する。点にはindex/i/j/total/x/y/angle/radiusがある。Grid indexは `j*cols+i`。shuffle後はindexが振り直される。総Object数は必ずしもLayoutPoint.totalと一致しない。根拠：[src/presets/system/types.ts:135](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/types.ts:135)、[src/presets/system/layouts.ts:52](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/layouts.ts:52)、[src/presets/system/builder.ts:200](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/builder.ts:200)、[src/presets/system/builder.ts:240](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/builder.ts:240)。

評価順はLayout→並び替え→条件/repeat→shape設定式→Object生成。Layout自身の式を評価している途中のpointX/Yは初期値0であり、自分の完成座標に依存するLayout定義は循環の解決にならない。現行のPosition Variationを引き継ぐなら「配置確定後の基準座標」を入力とするのが自然だが、これは提案であり未確定。

randはsinハッシュの小数部による純関数で乱数状態は進まない。同じseed/contextなら同値になる。startTimeをseedに含めるとTrigger時刻で変わる。scatterとshuffleの既定seedにはstartTimeが含まれる。一方Object内部のnoise/Polygon用randomSeedはMath.randomから作られ、完全な同一preset再現は保証されない。根拠：[src/utils/uniformRandom.ts:1](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/utils/uniformRandom.ts:1)、[src/presets/system/evaluator.ts:161](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/evaluator.ts:161)、[src/presets/system/layouts.ts:117](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/layouts.ts:117)、[src/core/BaseSynthObject.ts:53](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/BaseSynthObject.ts:53)。

### 3.5 Preset / JSON / DSL / Runtime UI

コアのJSON entryは次の形。これは既存presetへのpatchであり、完全に自立した保存形式ではない。

```json
[
  {
    "key": "kick01",
    "params": {
      "layout": { "kind": "single" },
      "layers": [{
        "object": {
          "shape": "rect",
          "size": 100,
          "params": { "sustainLevel": 0.6 }
        }
      }]
    }
  }
]
```

`key` は既存catalogに必須。`params` はPresetSpecのpatch（ShapeのSynthParamsとは別）。objectは再帰merge、arrayは置換。`validateJsonPresetEntries` は配列/entry/key/paramsがobjectかまでの検査で、式ASTやLayout/Shapeの詳細型までは検証しない。version、参照ID、Modulator/Binding配列もない。根拠：[src/presets/library/catalog.ts:62](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/library/catalog.ts:62)、[src/presets/library/catalog.ts:71](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/library/catalog.ts:71)、[src/presets/library/catalog.ts:110](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/library/catalog.ts:110)。

TS DSLの `ctx/mul/rand/mapExpr` などはJSONにできるASTオブジェクトを返し、そのASTを同じevaluatorが読む。「TSの任意コードをJSON化する」方式ではない。ただしEasingRefには関数も許されるため、すべてのTS設定が無条件にJSON往復可能ではない。文字列EasingKeyに限定する必要がある。根拠：[src/presets/system/expr.ts:3](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/expr.ts:3)、[src/presets/system/types.ts:4](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/types.ts:4)、[src/presets/system/builder.ts:40](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/presets/system/builder.ts:40)。

新形式 `shapes/shape` と `envelope/lfo/color`、旧形式 `layers/object` と `params` が併存する。builderは新形式を優先するが、既存catalogプリセットとGenerator出力は主に旧形式。両形式が混在したpatchでは旧項目が残っても新項目に隠され得る。保存仕様の単純化とmigrationが必要。

| UI / API | 実行中の変更の実態 |
|---|---|
| コアdemo | RでJSON再fetch・Factory差替え。生存中Objectの設定/状態は変更しない |
| SynthEngine.setPresets | 配列差替えのみ。次のTriggerに反映 |
| Generator Editor | input/changeごとにJSON再構築・検証・engine.clear・setPresets。既存演奏を消してから新規生成に反映 |
| Editorの編集範囲 | 図形、サイズ/比率/角度/Polygon、ADSR、LFO rate/depth、色/style、4種Layoutなど。UI生成のLFO typeはsine固定 |
| JSON UI | Copy/Downloadあり。textareaはreadonly。任意JSONの入力・ファイルimport機能は調査箇所にない |
| Modulator/Bindingの追加削除 | 汎用リストや接続データ自体がない。UI機能として未対応 |
| Unity UI | IUiTarget→RosettaUI。MIDI Action登録/解除とMIDI割当編集はある。Synth patch編集とは別 |

根拠：[demo/src/presetLoader.ts:15](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/demo/src/presetLoader.ts:15)、[demo/src/main.ts:45](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/demo/src/main.ts:45)、[src/core/SynthEngine.ts:59](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/SynthEngine.ts:59)、[src/main.js:146](/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator/src/main.js:146)、[src/main.js:197](/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator/src/main.js:197)、[src/main.js:227](/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator/src/main.js:227)、[index.html:158](/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator/index.html:158)、[Assets/Krab/Common/Scripts/Views/UiBuilder.cs:45](/Users/kimurakoya/Documents/develop/unity/krab/Assets/Krab/Common/Scripts/Views/UiBuilder.cs:45)。

保存対象はレシピ/patchであり、Objectの経過時間やADSRフェーズはJSON保存していない。ただしModulator設定と状態の明示的な別型があるわけではない。Unity側はPrefsGUIの値・MIDI割当保存とPrefs.Saveがあり、任意Synth patchのJSON入出力は確認できない（[Assets/Krab/Common/Scripts/Midi/Bindings/MidiBindingRepository.cs:41](/Users/kimurakoya/Documents/develop/unity/krab/Assets/Krab/Common/Scripts/Midi/Bindings/MidiBindingRepository.cs:41)、[Assets/Krab/Common/Scripts/Views/UiBuilder.cs:27](/Users/kimurakoya/Documents/develop/unity/krab/Assets/Krab/Common/Scripts/Views/UiBuilder.cs:27)）。

### 3.6 Gallery は別のシンセ実装

Generator Galleryはエンジンの単なる表示窓ではない。`computeItemLevel` は周期内の残り時間をSustain時間として持ち、0〜1にClampする。`computeLfoMul` は絶対アニメーション時刻から `1+sine×depth` を作りサイズへ乗算する。`drawItemContents` はサイズをセル内に収まる上限へ制限する。コアの「生成時刻からのLFO加算」「Sustain保持なし」「Object寿命」と挙動が異なる。根拠：[src/main.js:714](/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator/src/main.js:714)、[src/main.js:738](/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator/src/main.js:738)、[src/main.js:784](/Users/kimurakoya/Documents/develop/p5/p5-synth-shape-generator/src/main.js:784)。

GalleryのMath.randomによる候補生成、独自Layoutと描画は、Voice別Variation APIではない。Gallery→Editor/JSONへ移した時、全挙動が同じように再現されるとはいえない。この相違を新設計の仕様根拠として混同しない。

## 4. 設計上の問題と懸念点

以下は現行コードからの評価・提案である。

1. **演奏寿命と図形寿命が結合している。** ADSR終了でObjectを除去する現状では、固定図形やFree Run LFO、Gate保持、Voiceが複数Shapeを保持する構造に無理が出る。寿命を誰が所有するかを先に決める必要がある。
2. **数値出力と適用先が結合している。** 特定のADSR/LFOをShape継承階層から分離しないままBindingだけ足すと、サイズ・alpha・頂点の隠れた接続が残る。
3. **時間系が統一されていない。** 生成時BPM、Looperのms、Generator反復、Gallery絶対時間、Unity Beatは異なる。BPM変更追従を持つかどうかも明文化が必要。
4. **編集の反映方針が未定義。** 現状は次回生成だけ更新、または全消去。共有Definitionを直接書き換える場合、Attack中のAttack変更やSustain変更をどう補間するか決めないと演奏が飛ぶ。
5. **JSONがcatalogに依存する。** 同じJSONでもライブラリの既定Spec変更で意味が変わる。未知keyで新しいpatchを読み込めない。可搬Presetにはversionと完全定義を持つか、依存catalog versionを固定する必要がある。
6. **再現性とIDが不足する。** 配置点indexはshuffleで変化し、複数shape/repeatで重複する。安定Voice IDと表示順indexを混同すると保存・seed・外部Triggerの対応が崩れる。
7. **非有限値とGeometry制約は別問題。** 合成値の負数/超過を許可しつつ、非有限値は明示エラー/既定値など扱いを統一する。Rendererでの負の幅は反転・絶対値・非表示などが選択肢で、今回は決定しない。
8. **汎用化の範囲に注意。** 全設定へ再帰的なVariationとModulationを同時導入すると、LFO Frequencyを自身が変調する等の依存循環が生じる。現段階では対象の数値項目を限定し、加算Bindingと生成時Variationだけでも主要要件を検証できる。
9. **Geometry/Transform/Appearanceの分類は保存UIの整理に有用だが、必ず3種の継承階層を作る必要はない。** 安定したtarget IDとメタデータによる分類で足りる可能性がある。

性能面ではEngineは毎Triggerにnewし、updateごとfilter、drawごと配列copyとsortを行う。README等にpoolとあっても、この実装は再利用プールではない。maxObjectsは生成前の既存数しか確認しないため、一度の大量生成で上限を超え得る（[src/core/SynthEngine.ts:24](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/SynthEngine.ts:24)、[src/core/SynthEngine.ts:41](/Users/kimurakoya/Documents/develop/npm/p5-synth-shape-system/src/core/SynthEngine.ts:41)）。Unity移植時に5000/8000という数値を性能保証として引き継がない。

## 5. 再利用可能な実装

TS→C#では通常はアルゴリズム/仕様/テスト例の再利用であり、TSファイルをそのままUnity runtimeへ持ち込めるという意味ではない。

| 分類 | ファイル単位の候補 | 判断 |
|---|---|---|
| 現状のまま活用可能（周辺基盤） | Unity `Timing/IBPMManager.cs`、`Views/IUiTarget.cs`、`Views/UiBuilder.cs`、MIDI Devices/Input/Actions/Bindings、Output配下 | 現行UI/MIDI/出力の基盤。Synthとのadapterは新設が必要。時計精度要件は別途確認 |
| 小規模変更・移植 | コア `utils/easing.ts`、`utils/math.ts`、`presets/system/layouts.ts`、`LayoutPoint` | 数式・配置原理。p5寸法引数の置換、finite/count上限・0除算の検証を加える |
| 小規模変更の素地 | `utils/uniformRandom.ts`、Unity `Midi/Core/Param.cs` | seed値生成と値通知の素地。乱数の言語間一致とParamの責務は別途設計 |
| 大きな再構成 | `core/BaseSynthObject.ts`、`shapes/*SynthObject.ts` | Modulator状態・Geometry・描画・寿命を分離。Rectangleの局所ClampはRenderer側仕様へ移す |
| 大きな再構成 | `core/SynthEngine.ts`、`presets/system/builder.ts` | Voice/Shared Instance/Parameter合成を管理。更新順と編集反映を追加 |
| 大きな再構成 | `core/Looper.ts`、`core/LoopBank.ts` | バンク操作は利用可能だが、拍Transport・時刻付きイベント・長時間飛びの方針が必要 |
| 大きな再構成 | `presets/library/catalog.ts`、`presets/system/types.ts`、`evaluator.ts` | catalog patchから独立Presetへ。式AST全移植は必須でなく、まず必要なVariationだけ抽出してもよい |
| 新規実装 | 数値Parameter評価、Modulator Definition/Instance、接続別Depth、Shared/Local、Voice管理 | 対応する汎用実装なし。クラス名や粒度は未確定 |
| 新規実装 | Variationの評価時点/seed方針、Preset version/参照解決、Binding編集UI、Unity Renderer adapter | 既存部品だけでは要件を満たせない |
| 削除・統合検討 | 旧 `layers/params` と新 `shapes/envelope/lfo/color` の二重スキーマ、builderの旧経路 | 保存互換が必要なら一度migrationし、runtime側形式を一つにする |
| 削除・統合検討 | Generator `src/main.js` のGallery独自包絡/LFO/Layout/描画 | 共通runtimeでプレビューする方向。ただし現在の違いを意図した表現として残すか判断が必要 |
| データとして再利用 | `src/presets/library/*.ts`、`demo/public/*.json` | 音色ならぬ図形表現の参考例・移行用比較素材。自動変換で完全同一挙動になるとは限らない |

## 6. Unity（C#）での実装に向けた考察

以下はコード調査から導く**設計提案**であり、Unity版の実装事実ではない。

- Coreは「入力時刻/拍、イベント、設定、Voice context→数値結果」を評価する通常のC#型として扱い、MonoBehaviourはclock供給・入力・Renderer/UIとの接続に限定する案が適する。時計は差替え可能にすると、実時間と任意時刻による検証を両立しやすい。
- 保存用Definition、Voiceごとに解決した設定値、実行状態の3つを区別すると整理しやすい。同じDefinitionを参照しつつSustainだけ異なる場合、Definition自体のSustainを書き換えず、Localに解決済み設定を置く。これにより他Voiceへ値が漏れるのを防げる。
- `R × (B + Σ dM)` の加算リストから開始すればよい。bindingにはsource/targetの安定ID、Depth、必要ならscopeを保持し、IDは起動/変更時に参照へ解決する。毎フレーム文字列検索する設計は避ける案が考えられる。
- Shared Modulatorはフレーム/時刻につき一度評価し、複数targetが同じ結果を読む。LocalはVoiceが状態を所有する。Sharedに対して異なるVoice Sustainを同時適用することは一つの実行値では表せないため、その要件ではLocalへ分ける必要がある。
- Variationは当面 `Fixed/Random/Index/Position` と評価時点を持つ数値設定に限定できる。任意AST全移植、任意プロパティをreflectionで変調する仕組みは初期必須ではない。Count=1、shuffle、複数Shape、seed項目別分離を仕様化する。
- JSONは参照共有をオブジェクトの同一性に頼らずIDで表す案が適する。型タグ・schema version・重複ID/未知target/循環/非有限値の検証をロード段階で行い、検証成功後にまとめて反映する。ScriptableObjectを編集資産にするかどうかは別の判断で、JSONの正本を曖昧にしない。
- Runtime UI/MIDIはDefinition/Base/Depth/Gateなどへ変更要求を送るadapterにする。`Param<float>` をParameter evaluatorそのものに拡張しすぎず、ユーザー入力値と毎フレームの最終変調値を区別する。出力値をUI/MIDIから上書きするとBaseとの関係が崩れる。
- Voice数とGameObject数を直結させず、中央更新と描画用データの一覧を境界にする案がよい。最初は少数の矩形を描ける方式で仕様を確かめ、実測後にMesh共有/バッチ/Instancing等を検討する。GPU化を現時点の必須条件にしない。
- Geometryでは頂点数の整数化・最低頂点数、Transformでは度/ラジアン・座標単位、Appearanceでは色空間/alphaの単位を明文化する。p5のsize=半径相当とRectangleのsize=高さ相当を曖昧なまま移植しない。

## 7. 推奨する実装順序

1. **既存挙動と新仕様を分ける。** A/D/Rは拍か秒か、Gate保持、BPM変更追従、負のGeometry、Galleryとの差を先に判断する。互換再現を目標にする箇所と新仕様へ変える箇所を選ぶ。
2. **描画から独立した最小の数値評価を作る段階を設ける。** 固定Parameter、ADSR、LFO、複数加算Bindingと接続別Depthまで。1つのADSR→幅/Opacity、幅←ADSR+LFOで主要要件を確認する。
3. **Definition/InstanceとGate/clockを固める。** 同じDefinitionから独立発火する2つのLocal状態、1つのShared状態を参照する複数targetを確認。Voiceは必要な所有権だけから始める。
4. **Layout→Variation→Voice生成をつなぐ。** 4点のSustain 0.2/0.4/0.6/0.8、固定seed再現、Count=1、配置座標の基準を確認する。まず生成時評価から始め、同一Voice再Triggerの再評価を後で加える案がある。
5. **Presetの完全保存/復元と編集反映方針を作る。** 未知IDや無効値を扱い、既存演奏を全消去する方式から始めるか状態保持するかを明示する。
6. **UnityのRenderer/UI/MIDIへ接続する。** 既存BPM/UI/MIDIのadapterを利用し、小規模表現で実演する。
7. **拍ベースLooper/Sequencerと性能改善へ進む。** 時刻飛び・同時Trigger・Voice回収を確認した後、描画数とGCを計測して最適化する。

これは依存関係を示す推奨順であり、詳細な実装計画や採用クラスを確定するものではない。

## 8. 追加で判断が必要な設計論点

| 論点 | 未決定事項 / 判断への材料 |
|---|---|
| Voiceの単位 | 配置点単位、演奏単位、複数Shapeグループのどれか。初期はShapeと1:1に限定しても将来の複数Shape要件を妨げない境界にするか |
| 永続Shapeと演奏寿命 | Gate OffでShapeを消すか、ADSR=0でもVoiceを保持するか。固定表示/Free Runの回収条件 |
| ADSR動作 | TriggerとGateを区別するか、再Triggerは0/現在値からか、ReleaseはGate Off時の現在値からか。現行自動終了との互換 |
| 時間単位 | 秒/拍の選択を項目ごとに持つか。BPM変更時に残り時間・LFO位相をどう保つか |
| Clock | timeScaleの影響、停止/再開/seek、外部同期の必要性、フレーム飛びでイベントを追いつかせるか |
| Reference/Base | Rを倍率だけにするか、offset/rangeも必要か。R=0、負数、Rotationの折返し、整数値への適用 |
| 値の保護 | 合成値はClampしない方針を維持しつつ、NaN/Infinityを拒否/既定値/前回値のどれにするか |
| SharedとVariation | 同一Shared実行に異なるSustain/Frequencyを求める場合の扱い。Localへ分岐するのか、対象を禁止するのか |
| Variation対象 | ADSR/LFO/Depth/Base/Layoutのどこまで初期対象にするか。汎用式ASTまで必要か、4種だけで足りるか |
| Positionの基準 | Layout座標、Shape上書き後、移動後、World座標のどれか。Layout依存の循環をどう拒否するか |
| SeedとID | Preset/Voice/項目/Triggerのseed合成、shuffle前後index、言語間で同一分布まで必要か |
| Live Edit | 既存Voiceへ即時反映/次Triggerのみ/全再生成。Instance状態を維持したDefinition差替えが初期必須か |
| 保存形式 | catalog patchを残すか、完全定義へ移るか。既存JSON migration、easing関数の排除、schema version |
| 描画境界 | 負の幅高、0サイズ、Polygon異常値、Opacity範囲外の表示。Mesh/Shader等は計測後に判断可能 |
| Galleryとの互換 | Galleryの周期Sustain・LFO乗算・セル内Clampを表現仕様として残すか、共通runtimeへ統一するか |

調査結果からは、**レシピ・式評価・配置は有用な既存資産であり、演奏状態・汎用接続・描画の分離が再設計の中心**となる。Variationは既存の生成時式評価を活かし、必要な所有権と再現性の仕様を加える形で検討できる。
