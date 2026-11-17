# Phase 8：5プリセットの演奏とStroke

## 操作

`Assets/Krab/ShapeSynth/Samples/ShapeSynthPerformance.unity` を開いてPlayする。

| キー | Preset ID | 表現 |
|---|---|---|
| Q | fx01 | 従来の赤/水色150円 |
| W | pad01 | 中央のオレンジ輪郭円5個 |
| E | snare01 | 放射状に広がる緑の正三角形5個 |
| A | lead01 | Seed付き10頂点の青い星形輪郭 |
| S | lead03 | 茶色の5×5矩形とSeedで選ばれる赤い円 |

連打は別Seedの独立Voiceを重ねる。押し続けても自動連打しない。Dの共通UI表示切替は維持。UIのTextField等へフォーカスがある間は演奏入力を抑止するので、演奏時はGame ViewのUI外へフォーカスを戻す。

階層化されたSettingsのShapeSynthタブ → Performanceカテゴリ → Performanceウィンドウに5つのTriggerボタンがある。ModulatorsにはFX01の2設定に加え、Pad01/Snare01/Lead01/Lead03のEnvelope、Pad01/Lead01のLFOを表示する。Seedはプリセット別に管理し、編集した設定は次のTriggerから適用する。保存は従来のSave ShapeSynth / Reload ShapeSynth。共通PrefsのSaveとは別。

## 構成と所有

`PresetCatalog` の読み取り専用5エントリがPreset ID・名前・キーを定義する。Preset IDとModulator IDは別の識別子。キーボードとUIはどちらも単一の `ShapeSynthPerformance.TriggerPreset(presetId)` へ入り、Libraryの不変スナップショットから生成関数を呼び、既存ShapeSynthControllerへSpawnする。`TriggerFx01()` は互換用として残す。

FX01は従来の `Fx01Preset.Build` を再利用。4つの追加生成関数は `AdditionalPresets` にまとめた。Definition生成関数と入力Controllerを分離し、プリセット別のMonoBehaviourやFactory継承階層は作らない。Group/Voice/Binding/Variation/Rendererと既存Camera→RT→Preview/Syphonを再利用する。

各プリセットはModulator IDを参照し、生成関数内にADSR/LFOの設定値を持たない。同一Voice内ではRoot scopeの信号を共有し、別VoiceのInstanceは独立する。各EnvelopeがLifetimeを兼ねるため自然終了する。編集・名前変更・Reloadで既存Voiceの設定/位相/BPMは変わらない。

## 表現の再構成

旧synth-machineの [Pad01](https://github.com/koya-kimura/synth-machine/blob/main/src/synth/presets/pad01.ts)、[Snare01](https://github.com/koya-kimura/synth-machine/blob/main/src/synth/presets/snare01.ts)、[Lead01](https://github.com/koya-kimura/synth-machine/blob/main/src/synth/presets/lead01.ts)、[Lead03](https://github.com/koya-kimura/synth-machine/blob/main/src/synth/presets/lead03.ts) を調査した。

- Pad01：`#FC4622` の5本の同心輪郭。DiameterとOpacityへ共有ADSR、リング別Depthで弱いLFOを加える。元の円ごとのAttack時間差は共通開始時刻へ簡略化した。新しい時間遅延やADSR設定Variationは追加しない。
- Snare01：`#0E551D` の正三角形。Seedで開始角を決め、72度刻みの方向を生成時にsin/cosでX/Yへ変換する。ADSRが半径・回転・Scale・Opacityを変える。Release中は半径も戻りながら薄くなる簡略表現。
- Lead01：`#4C00D2` の10頂点。角度順を保つ小さなjitterと交互の半径で単純な凹Polygonを作る。Seedは頂点単位へ分岐し、生成順に依存しない。ADSR/LFOで回転、ADSRでScale/Opacity。動的Geometryや生成グラフは作らない。
- Lead03：`#B46600` の25矩形を専用ループで5×5へ配置し、選ばれたセルに `#C7190C` の円を**追加**する。矩形は常に25個。円の選択はSeed/row/columnから決定する。汎用Layoutは導入しない。

## ShapeStyleとStroke

Coreの `ShapeStyle(fill=true, stroke=false, strokeWidth=1, strokeColor=null)` は不変の描画属性。ShapeDefinitionの各factory末尾に任意styleを追加し、DrawItemへ渡す。未指定は従来のFillのみ。色は従来のShape Colorを使用し、StrokeColor指定時のみ輪郭色を分ける。Fill/Strokeとも `Color Alpha × Opacity` を描画時にClampする。Stroke Widthは有限・非負。0幅は輪郭を描かない。

幅はローカル描画単位として扱う。Circle/Rectangleはp5.UniteのSDFと完全なWorldTransformによって線幅も変形する。Polygonは公開BeginShape/Vertex/EndShapeが線分を画面上で展開するため、Adapterの `ResolveStrokeWidth` で `localWidth × sqrt(abs(det(WorldTransform)))` へ換算してStrokeWeightへ渡す。等方Scale・反転では幅が揃い、非等方Scale/shearでは面積Scaleの幾何平均を使った**近似**となる。正確な異方的Polygon Strokeや一定画面px幅を保証する機能ではない。

Lead01は約100単位の頂点半径とMinDimension相対Scaleを使用し、過大な輪郭幅を避ける。角のJoinは導入済みp5.Uniteの丸い継ぎ目に従う。Miter/Bevelの正確な再現や半透明のJoin重なりの補修はしない。PackageCache/Shaderの変更、新しいMesh生成機構は追加していない。

## JSON互換性

ファイル名 `Performance.v1.json` とVersion=1を維持した加算的な拡張。Phase 7の `Fx01` / `Modulators` は変更しない。任意の `Presets` リストに4つの追加設定（Preset ID、Envelope ID、任意LFO ID、Seed）を保持する。

旧JSONでPresetsがない/空の場合、Decodeは既存データを検証してから4設定と不足する既定Modulatorを補完し、再検証する。既存のID/名前/数値/FX01個数/Seedは上書きしない。新しいIDが既に存在すればその設定を保持し、型不一致はエラーにする。重複ID・未知Preset・参照切れを拒否する。読み込みだけではファイルを書き換えず、明示Save時に拡張済みDTOを安全に保存する。

Coreの `CreateDefault()` は互換用に従来のFX01・2 Modulatorを維持し、新規Performanceは `CreateForPerformance()` で5プリセット・8 Modulatorを作る。古いアプリへ戻して保存すると追加フィールドが失われ得るため、旧アプリからの再保存まで保証する形式ではない。

## 検証と残課題

2026-11-11：Unityコンパイル成功、EditMode179件成功・失敗0（既存157件を含む）。Playで全Preset/同一Presetの同時Spawn、自然終了、5つの実UIボタン、Pad01の新旧Voice設定分離、実UIのSave/Reloadを確認した。保存検証にはReports内の別ファイルを使用し、ユーザーの既存保存ファイルは変更していない。

Input Systemイベントと明示Updateによる入力経路で、Q/W/E/A/Sの対応、保持で連打しないこと、TextFieldフォーカス時の抑止を検証した。物理キーによる通常操作、映像の最終的な見た目、外部Syphon受信はユーザー確認対象。RenderTexture画像は `Reports/Phase8Combined.png`。

今後は本来の円別タイミング、Polygon Strokeの異方的変形、プリセットID/ルーティングの編集UI、保存形式の版管理を検討する。MIDI、シーケンサー、Pooling、汎用Layout、全30プリセットの移植は今回の対象外。

## 追補: MIDIでユーザープリセットをTrigger（2026-11-17）

- Performanceの「User Presets」の各行に「MIDI」ボタンを追加した。MIDIの割り当て方はMIDI Sample（`ParamSampleComponent`）と同じで、ボタンから割り当てたNoteやボタンを押すと、そのプリセットがTriggerされる。Releaseは送らない（押したらTriggerするだけ）。
- 保存済みプリセット1つにつき、Momentaryのアクション `ShapeSynth.UserPreset.<プリセットID>` を1つ登録する。割り当てはプリセットIDで保存されるので、名前を変えても外れない。割り当ての保存は、ほかのMIDI設定と同じくPrefsの保存（Settingsの「Save」かCmd+S）。
- 起動時（Start）に保存済みプリセットを読み込んで登録するので、UIを開かなくてもMIDIで演奏できる。「Refresh User Presets」で、増えたプリセットを登録し、消えたプリセットの登録を外す。
- Trigger時にはファイルを読み直してCompileする（従来のTriggerボタンと同じ）。Preset Editorで保存した変更は、次のTriggerから反映される。
- 制限：Play中にスクリプトを再読み込みすると、MIDIの登録が消える（MIDI Sampleと同じ。DIの注入が再実行されないため）。Playし直すと戻る。
- 検証：Performance SceneのPlayで、5つのプリセットそれぞれにアクションが登録されることを確認した。テスト用のNote（ch15 / 127）を一時的に割り当て、`MidiBindingManager.Receive` で押下を送ると Voice が0→1、離しではTriggerされず、別フレームの再押下で1→2になった。確認後に割り当ては外した。User Presetsの各行に「Trigger | MIDI」が並び、エラーは0件だった。EditMode 319件成功。実機のMIDIコントローラーでの確認は未実施。
