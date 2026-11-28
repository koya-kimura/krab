# Shape Synth Smoke Test

`ShapeSynthSmokeTest.unity` を開いてPlayする。Game Viewの上段左にシアンのADSR円、上段右にオレンジのLFO円が現れる。左は直径が拡大・減衰・保持・縮小し、右は一定の直径で左右に往復する。

- Space：再Trigger（Game Viewにキーボードフォーカスが必要）
- R：ADSRを現在値から外部Release
- L：LFOのみ再開始し、その時点のBPMを取得
- D：共通設定UIの表示切替（既存UiBuilderの操作）
- Play停止：終了
- `ShapeSynth/Modulator Comparison` のInspectorで拍単位のADSR、最大直径（出力の短辺に対する割合）、色、Auto Repeatを変更できる。
- LFOのCycles Per Beat / Phase Offset / WaveformをInspectorから変更できる。初期値はSine、0.25 cycles/beat（4拍で1周期）。0は停止、負の周波数は未対応。Play中の設定変更は開始時刻/BPMを保持して再評価するため、位相が飛ぶ場合がある。
- BPMは既存BPMManagerのPrefs値を使用する。Trigger時の有効BPMを保持する。

初期値はAttack=1拍、Decay=0.5拍、SustainLevel=0.35、SustainTime=1拍、Release=1拍、反復前の休止=1拍。BPM120なら1周期2.25秒。

描画はUCanvasのScreenOverlay/Indirect、ピクセル単位。検証用の画面配置は出力Cameraの寸法から計算する。このRT/Preview経路では小さいYが画面上側に見えるため、表示用の配置をそれに合わせている。正式Adapterでの中央原点・X右/Y上の座標変換は [描画Adapter](../../Docs/ShapeSynth/RenderingAdapter.md) を参照。

出力経路：UCanvas → Main Camera → 既存OutputTextureProviderのRT → 既存OutputPreviewDisplay/RawImage → Game View。既存SyphonController/Serverも同じRTにTextureモードで接続する。Syphonクライアントでの受信は別途確認が必要。

## 共通Scene構成

`2612_shirt-meets-lausbub.unity` の構成をEditorでコピーし、次のHierarchyにしている。本番Sceneと共通Prefab自体は変更しない。

```text
Main Camera                 OutputTextureProvider、AudioListener、URP設定
Directional Light
Common                      MIDI入出力、Binding UI、APC Simulator、ParamSampleComponent
  SceneContainer            UnitySimpleContainerのScene DI
  UiBuilder                 RosettaUI / Prefs設定UI
  OutputSender              SyphonController / SyphonServer（macOS）, SpoutController / SpoutSender（Windows）
  OutputPreview             Preview用Camera / Canvas / RawImage
  [Graphy]                  パフォーマンス表示
  ApplicationFrameSetting
  BPMManager                BPMManager / TapTempoController
EventSystem                 InputSystemUIInputModule
ShapeSynth
  Modulator Comparison      UCanvas / ShapeSynthSmokeTest（ADSR + LFO）
```

デモは `[Inject] Construct(IBPMManager)` で既存SceneContainerからBPMを受け取る。PreviewとSyphonも既存DIに任せ、デモからの手動Construct呼出しは行わない。参考Sceneに重複していたCommon本体のBPMManager/TapTempoControllerはコピー先で取り除き、子のBPMManager Prefabに一本化している。

デモ用にMain Cameraの背景を暗色のSolidColorとし、HDR/MSAAを無効化している。それ以外の共通UI、出力Prefab、入力設定やURP設定は参考Sceneを引き継ぐ。UCanvasは各Cameraで描画するため、サイズはUCanvas.Widthではなく出力Cameraから取得する。Preview用Cameraは既存Prefabと同じUIレイヤーを描画し、円はMain CameraのRTを通して表示される。

`ShapeSynthSmokeTest.cs` は正式なShape/Binding/Voice管理を代替するものではない。Coreをそのまま使用し、ADSRから直径、LFOから左右座標への変換だけをサンプル内で行う。最初は同じVoiceTimeで開始するが、ADSRの自動反復/SpaceはLFOをリセットしない。

SceneはEditorで作成・保存済み。再生成が必要な場合は既存の検証Sceneを退避してから `Krab > Shape Synth > Create Smoke Test Scene` を実行する。作成ツールは既存ファイルの上書きを拒否する。参考Sceneを一時コピーして構築し、保存後に元のSceneを復元する。意図的な再構築に限りEditorから `ShapeSynthSmokeSceneBuilder.BuildFromReference(true)` を呼び出せる。参考Sceneまたは既存の検証Sceneに未保存変更がある場合は上書きを拒否する。


## Phase 2bのBinding表示

下段左の紫の円は、ADSRとLFOを同じDiameter Parameterへ接続して加算する。下段右の緑の円は、紫の円と**同じADSR Instance**をOpacity Parameterへ接続して明滅する。直径は一定。上段の従来表示は比較用として維持する。

Inspectorの「Parameter / Binding comparison」でBase・ADSR Depth・LFO Depth・短辺に対するReference係数を変更可能。ParameterのCore出力はClampせず、OpacityのClampは描画時のみ。詳細APIは `Assets/Krab/Modules/ShapeSynth/Docs/ParameterAndBinding.md` を参照。


## Phase 3のGroup表示

従来の4つの円は左パネルへまとめ、右側にGroupデモを追加している。`ShapeSynth/Group Hierarchy` にUCanvasとHierarchySmokeTestがあり、図形ごとのGameObjectは不要。

- 右上A：2図形が同一Pivotの周りに回転。
- 右中央B：親子GroupのTransform合成。Rectangleと三角形に非等方/負Scaleを含む。
- 右下C：LFOでGroup Rotation、共有ADSRでCircle DiameterとRectangle Position Xを変調。

灰色の点はPivot。Space/R/L/Dは従来通りで、新デモもSpace/R/Lを受け取る。Core座標はCanvas中央原点・Y上向き。新AdapterはこのRT/Preview出力向けにY反転を明示設定し、合成済み行列を一度だけ適用する。詳細は `Assets/Krab/Modules/ShapeSynth/Docs/ShapeGroupTransform.md`。

## Phase 4の描画確認

上端の3図形と下端の非等方/負Scale・半透明の重なりを追加。既存の変調/階層デモは維持する。各デモはTargetCameraCanvasでOutput Cameraだけへ送信する。Sceneを開いてPlayすれば動作する。詳細は `Assets/Krab/Modules/ShapeSynth/Docs/RenderingAdapter.md`。

## Phase 5のVoice / Lifetime

`ShapeSynth/Voice Lifetime` にTargetCameraCanvas、ShapeSynthController、VoiceSmokeTestを配置済み。Main Cameraと既存RT出力を使い、以前のデモを維持する。Play開始時に青のTimer-only CircleとピンクのADSR+Timer Groupを1つずつSpawnする。追加デモは上端3図形と既存デモの間の行に表示する。

- **Z**：青CircleをSpawn。LFOで移動し、初期設定8拍のTimerで終了。
- **X**：ピンクGroupをSpawn。ADSRは8拍、Timerは2拍。Timerが先に終了してもADSR終了まで残る。
- **V**：Xの演奏対象だけをRestart。青と黄色のVoiceには影響しない。
- **C**：黄色の保持ADSR+Timer GroupをSpawn。Timerが終了しても残る。
- **H**：Cから生まれたVoiceのADSRをRelease。即時削除せずRelease完了で終了する。
- **D**：既存BPM UIを表示。BPMを変更し、次拍で有効になってからZ/X/Cで再Spawnすると、新旧Voiceはそれぞれの開始時BPMで動く。

Game View左上にVoice数・現在BPM・最後のTrigger BPMと操作キーを表示する。これはサンプルのOnGUIであり、SyphonのRTには描かない。従来のSpace/R/Lは従来デモだけに作用する。検証用のBPM変更は保存せず、確認後に元へ戻す。Voice Pooling・自動反復・Voice数制限は実装していない。詳しいAPIとscopeは `Assets/Krab/Modules/ShapeSynth/Docs/VoiceAndLifetime.md` を参照。

## Phase 6のVariation（削除済み）

旧 `ShapeSynth/Variation` は2026-11-17に削除した。後継のRandom / Delay / Flipは Preset Editor で確認する（`Assets/Krab/Modules/ShapeSynth/Docs/Variation.md`）。

## 演奏とPreset Editor

`ShapeSynthPresetEditor.unity` を開いてPlayし、Settings → ShapeSynth → Authoring → Preset Editorから編集する。操作は [Preset Editor](../../Docs/ShapeSynth/PresetEditorWorkspace.md) を参照。

保存後、`ShapeSynthPerformance.unity` を開いてPlayする。SettingsのShapeSynth → Performanceから、保存済みプリセットのTriggerまたはMIDIを操作する。一覧は自動更新される。組み込みプリセット、Q/W/E/A/SのTrigger、Performance.v1.jsonは廃止済み。詳細は [ユーザープリセットの演奏UI](../../Docs/ShapeSynth/UserPresetPerformance.md) を参照。

## 8スロットルーパー

`ShapeSynthLooper.unity` を開いてPlayする。Settings → ShapeSynth → Performance → PerformanceのLoopersを展開し、Record → プリセットをTrigger → Recording → Playで録音を反復する。次の状態ボタン操作かClearで消去。8スロット独立、秒数固定（BPM変更に追従しない）、クオンタイズなし。詳細は [ルーパー](../../Docs/ShapeSynth/Looper.md)。

## ディレクトリ構成

Commonと同様にC#コードを`Runtime/`へ集約する。名前空間には`Modules`・`Runtime`を含めない。

- `../Runtime/Core/`: Unity非依存の数値処理と定義（`Krab.ShapeSynth.Core`）。専用asmdefの`noEngineReferences`を維持する。
- `../Runtime/Unity/`: Controller、描画アダプター、設定保存（`Krab.ShapeSynth.Unity`）。Coreとは別アセンブリ。
- `Settings/`: サンプル用UI設定資産。
- `Scripts/`: 検証用MonoBehaviour（`Krab.ShapeSynth.Samples`）。
- `../Runtime/Performance/`: 正式な演奏操作・設定UI・ユーザープリセット（`Krab.ShapeSynth.Performance`）。
- `../Runtime/PresetEditor/`: 正式なランタイム編集UI（`Krab.ShapeSynth.PresetEditor`）。検証SceneとScene構築コードはSamplesに置く。
- `../Editor/Scenes/`: Scene構築用Editorツール（`Krab.ShapeSynth.Editor.Scenes`）。
- `../Tests/Editor/`: CoreのEditModeテスト（`Krab.ShapeSynth.Tests.Editor`）。`Adapter/`はUnity連携テストとその子名前空間。

検証Sceneはこのディレクトリ直下に置く。資産移動時は`.meta`のGUIDとアセンブリ名を維持し、既存Sceneからの参照を保持する。

### 責務ごとの名前空間

ディレクトリ名と名前空間の末尾を一致させる。アセンブリの境界は変更しない。

| ディレクトリ（Scriptsからの相対パス） | 名前空間 | 責務 |
| --- | --- | --- |
| `Core/Geometry/` | `Krab.ShapeSynth.Core.Geometry` | 座標・行列・色 |
| `Core/Definitions/` | `Krab.ShapeSynth.Core.Definitions` | ノード・パラメーター・モジュレーターの定義 |
| `Core/Evaluation/` | `Krab.ShapeSynth.Core.Evaluation` | 階層・パラメーター・変動の評価と参照コンテキスト |
| `Core/Modulation/` | `Krab.ShapeSynth.Core.Modulation` | モジュレーターの実行状態とライブラリー |
| `Core/Runtime/` | `Krab.ShapeSynth.Core.Voices` | Voiceの時間・生成・寿命・描画データ |
| `Core/Performance/` | `Krab.ShapeSynth.Core.Performance` | 演奏設定と保存用設定モデル |
| `Core/Presets/` | `Krab.ShapeSynth.Core.Presets` | プリセット文書・コンパイル・標準カタログ |
| `Unity/Rendering/` | `Krab.ShapeSynth.Unity.Rendering` | 描画アダプターと対象CameraのCanvas |
| `Unity/Persistence/` | `Krab.ShapeSynth.Unity.Persistence` | 設定・プリセットの永続化 |
| `Unity/Runtime/` | `Krab.ShapeSynth.Unity` | Unityの時計とCoreを接続するController |

Samplesのスクリプトは検証Scene専用の`*SmokeTest`のみ。正式機能は`Krab.ShapeSynth.Performance`・`Krab.ShapeSynth.PresetEditor`、Scene構築ツールは`Krab.ShapeSynth.Editor.Scenes`、テストは`Editor`・`Editor.Adapter`の分類を維持する。名前空間を変更したMonoBehaviourには`MovedFrom`を付け、以前のSceneの型参照を移行できるようにする。
