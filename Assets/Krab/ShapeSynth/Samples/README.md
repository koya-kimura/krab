# Shape Synth Smoke Test

`ShapeSynthSmokeTest.unity` を開いてPlayする。Game Viewの中央にシアンの円が現れ、ADSRに沿って拡大・減衰・保持・縮小を反復する。

- Space：再Trigger（Game Viewにキーボードフォーカスが必要）
- R：現在値から外部Release
- D：共通設定UIの表示切替（既存UiBuilderの操作）
- Play停止：終了
- `ADSR Circle` のInspectorで拍単位のADSR、最大直径（出力の短辺に対する割合）、色、Auto Repeatを変更できる。
- BPMは既存BPMManagerのPrefs値を使用する。Trigger時の有効BPMを保持する。

初期値はAttack=1拍、Decay=0.5拍、SustainLevel=0.35、SustainTime=1拍、Release=1拍、反復前の休止=1拍。BPM120なら1周期2.25秒。

描画はUCanvasのScreenOverlay/Indirect、ピクセル単位。左下原点・X右・Y上から出力Cameraの半幅/半高へ配置する。一般的なシンセ座標系への変換は将来の正式Adapterで実装する。

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
  ADSR Circle               UCanvas / ShapeSynthSmokeTest
```

デモは `[Inject] Construct(IBPMManager)` で既存SceneContainerからBPMを受け取る。PreviewとSyphonも既存DIに任せ、デモからの手動Construct呼出しは行わない。参考Sceneに重複していたCommon本体のBPMManager/TapTempoControllerはコピー先で取り除き、子のBPMManager Prefabに一本化している。

デモ用にMain Cameraの背景を暗色のSolidColorとし、HDR/MSAAを無効化している。それ以外の共通UI、出力Prefab、入力設定やURP設定は参考Sceneを引き継ぐ。UCanvasは各Cameraで描画するため、サイズはUCanvas.Widthではなく出力Cameraから取得する。Preview用Cameraは既存Prefabと同じUIレイヤーを描画し、円はMain CameraのRTを通して表示される。

`ShapeSynthSmokeTest.cs` は正式なShape/Binding/Voice管理を代替するものではない。Coreをそのまま使用し、直径への変換だけをサンプル内で行う。

SceneはEditorで作成・保存済み。再生成が必要な場合は既存の検証Sceneを退避してから `Krab > Shape Synth > Create Smoke Test Scene` を実行する。作成ツールは既存ファイルの上書きを拒否する。参考Sceneを一時コピーして構築し、保存後に元のSceneを復元する。意図的な再構築に限りEditorから `ShapeSynthSmokeSceneBuilder.BuildFromReference(true)` を呼び出せる。参考Sceneまたは既存の検証Sceneに未保存変更がある場合は上書きを拒否する。
