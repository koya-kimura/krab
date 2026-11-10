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

描画はUCanvasのScreenOverlay/Indirect、ピクセル単位。検証用の画面配置は出力Cameraの寸法から計算する。このRT/Preview経路では小さいYが画面上側に見えるため、表示用の配置をそれに合わせている。中央原点・X右/Y上というCore側の座標方針と、一般的な座標変換は将来の正式Adapterで扱う。

出力経路：UCanvas → Main Camera → 既存OutputTextureProviderのRT → 既存OutputPreviewDisplay/RawImage → Game View。既存SyphonController/Serverも同じRTにTextureモードで接続する。Syphonクライアントでの受信は別途確認が必要。

## 共通Scene構成

`2612_shirt-meets-lausbub.unity` の構成をEditorでコピーし、次のHierarchyにしている。本番Sceneと共通Prefab自体は変更しない。

```text
Main Camera                 OutputTextureProvider、AudioListener、URP設定
Directional Light
Common                      MIDI入出力、Binding UI、APC Simulator、ParamSampleComponent
  SceneContainer            UnitySimpleContainerのScene DI
  UiBuilder                 RosettaUI / Prefs設定UI
  OutputSender              SyphonController / SyphonServer
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

Inspectorの「Parameter / Binding comparison」でBase・ADSR Depth・LFO Depth・短辺に対するReference係数を変更可能。ParameterのCore出力はClampせず、OpacityのClampは描画時のみ。詳細APIは `Assets/Krab/Docs/ShapeSynth/ParameterAndBinding.md` を参照。
