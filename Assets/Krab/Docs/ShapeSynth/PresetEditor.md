# Phase 9: Runtime Preset Editor

> Phase 10でUIを専用Workspaceへ更新した。現在の操作方法・更新設計・検証は [Preset Editor Workspace](PresetEditorWorkspace.md) を参照。以下はPhase 9のデータ/APIと当時のUI記録。

## 目的と構成

`Assets/Krab/ShapeSynth/Samples/ShapeSynthPresetEditor.unity` を開いてPlayする。CommonのSettingsで **ShapeSynth → Authoring → Preset Editor** を開く。GameObject、Camera、TargetCameraCanvas、ShapeSynthController、CommonのDI/UI/RT出力はSceneに設定済み。Scene構築の再現コードは `Scripts/Editor/Scenes/ShapeSynthPresetEditorSceneBuilder.cs`。既存Sceneがある場合は上書きしない。

Phase 8の作業ツリーを引き継いで追加した。Phase 8、Phase 9とも目視確認・コミット指示までコミットしない。既存のPerformance.v1.json、5つのBuilt-inとキー設定は維持する。

## 操作

1. **New** は新しいPreset ID、Root Group、Default ADSR、OpacityにADSRをBindingしたCircleを作る。**Preset Name** で表示名を編集する。
2. **Hierarchy** を開いてNodeを選ぶ。Groupを選び **+ Group / + Circle / + Rectangle / + Polygon** で子を追加する。子Groupを選べば入れ子になる。**Delete Selected** で子孫ごと削除する。Rootは削除できず、Shape選択中の子追加はエラーになる。兄弟は追加順に描画する。
3. **Inspector** は選択Nodeを編集する。Node Name、Position X/Y、Rotation（度）、Scale X/Yを持つ。CircleはDiameter、RectangleはWidth/Height、Shape共通でOpacity、Color、Fill、Stroke、Stroke Width、Stroke Colorを持つ。Polygonはローカル座標半径100pxの三角形で開始し、Scale/Rotationで調整する。頂点の高度な編集は対象外。
4. 各ParameterのFoldを開き、Reference / Base / Reference multiplier / Absolute valueを設定する。式は `Reference × (Base + Depth × Value)`。既定サイズはMinDimension比率、PositionはAbsolute（px）。Scale/Opacityは無次元。
5. **+ ADSR / + LFO** で独立した信号源を追加する。Modulatorsを開き名前・ADSRの拍数とSustain level、LFOのWaveform/Cycles per beat/Phase offsetを編集する。1 Parameterに接続できるModulator（Value）は1つだけ。Parameterの **Bind** でValueを選んでDepthを編集し、**Unbind** で解除する（詳細は `PresetEditorWorkspace.md`）。参照中Modulatorの削除は拒否する。先にすべてのParameterからUnbindする。
6. **Preview / Restart Preview** は新しくCompileする。**Static Preview** がONなら全信号を1に固定し、Voiceの寿命に影響されず配置を表示する。実際のアニメーションの途中時刻ではない。OFFならADSR/LFOを実行し、自然終了する。BPMとSeedを指定できる。**Auto update Preview** がONなら編集後に再CompileしてPreviewを置き換える。以前のPreview Voiceを停止し、重なり続けない。**Stop** で消す。
7. **Save Preset** は構造と参照が正しいDocumentを保存する。ADSRがなく実行不能な下書きも保存できる。Preview/TriggerはADSR Lifetime不足を明示して拒否する。**Refresh / Open → Saved Presets → Open <name>** で読込。読込失敗時は現在のDocumentを維持する。New/OpenにはUndoや未保存確認はないため、必要な編集を先に保存する。

階層・Binding・Modulator一覧の構造変更ではDynamicElementを再構築する。Inspector等のFoldが閉じた場合は再度開く。各WindowのPageはスクロールできる。UIにフォーカスがある間、従来どおり演奏キーは抑止する。

## APIとデータの境界

### PresetDocument（Core、Unity非依存）

- `CreateNew()` / `NewId()`、`Validate()`、`AddNode(parentId, kind)`、`DeleteNode(id)`、`DeleteModulator(id)`、`Nodes()`。
- SchemaVersion=1、安定したId、変更可能なName、既存の `ModulatorSettings` 一覧、必須RootGroupを持つ。
- `PresetNode` は明示的なKind（Group/Circle/Rectangle/Polygon）、Id、Name、順序付きChildren、Transform/Geometry/Opacityの `PresetParameter`、Color/Style、Polygon頂点を持つ。
- `PresetParameter` はReference/Multiplier/AbsoluteValue/Baseと `PresetBinding(ModulatorId, Depth)`。BindingはList形式で保存するが、1 Parameterにつき最大1件。編集モデルはPrefsGUIへ変換しない。
- IDは先頭英数字、以降英数字・underscore・hyphen、1〜80文字。重複Node ID・重複Modulator ID、未知のBinding参照、1 Parameterに2件以上のBinding、null、非有限値、無効なenum/ADSR/LFO/Stroke設定を拒否する。IDと兄弟Indexは別概念。最大64階層・2000 Node、Modulator/Polygon頂点も上限2000。Root以外のShapeに子は持てない。

### PresetCompiler（Core）

- `CompileGroup(document)` は構造検証済みの不変GroupDefinitionスナップショットを返す。Lifetimeを要求しない。
- `Compile(document)` はさらにADSR Lifetimeを要求し、既存 `TriggerTarget` を返す。
- DocumentのModulator順をRoot scopeのslotへ変換する。子GroupはRuntime=nullで既存のscope継承を使う。Bindingの安定IDをslotへ解決する。
- Definition、頂点、Binding、Modulator設定はコピーする。Documentを後から変更しても既存Voiceには影響しない。保存、UI、Unity時計は担当しない。

### PresetRepository（Unity境界）

- `new PresetRepository(directoryPath = null)`、`DirectoryPath`、`Save(document)`、`Load(id)`、`Exists(id)`、`List()`、`List(out errors)`。
- 既定保存先：`Application.persistentDataPath/ShapeSynth/Presets/<preset-id>.json`。Nameを変えてもファイル名は変わらない。既存Performance.v1.jsonとは別の形式。
- JSONはSchemaVersion、Id/Name、Modulators、再帰的RootGroup/Children、Node ID、Parameter/Binding、Color/Style、Geometryを保存する。Voice、進行状態、GameObject、UI、RTは保存しない。
- Unityに既に導入されている `com.unity.nuget.newtonsoft-json` 3.2.2を使う。JsonUtilityは深い再帰でNodeを省略することを実検証で確認したため採用していない。CoreにJSON依存を追加していない。型メタデータの実行は無効。SchemaVersionの必須・整数・対応値、重複JSONキー、Document構造、ファイル名とIDの一致を検証する。
- 保存は検証とシリアライズ後、同じフォルダの一時ファイルへ書いてMove/Replaceする。失敗時は既存ファイルを保持する。一覧取得は壊れたファイルをerrorsへ返し、UIに表示する。黙って参照やIDを補修しない。

### Previewと既存描画

`ShapeSynthPresetEditor` はIUiTarget。`ShapeSynthController.StaticPreview` に任意のHierarchyEvaluatorを設定し、既存のUniteShapeAdapter/TargetCameraCanvas/OutputTextureProviderで描画する。静的表示のためにLifetime仕様を変えていない。アニメーションPreviewは既存Controller.Spawnを使い、追加した `VoiceCollection.Stop(voice)` で所有する1 Voiceだけを停止する。Compile失敗時は以前のPreviewを保持してエラーを表示する。

## Performance Scene

`Assets/Krab/ShapeSynth/Samples/ShapeSynthPerformance.unity` を開いてPlayし、**Settings → ShapeSynth → Performance → Performance** を開く。**User Presets → Refresh User Presets** でRepositoryを再取得する。名前で選択し **Trigger Selected User Preset**、または各行の **Trigger** を押す。押すたびにLoad/Compileし、その時点のCurrentBpmとUser seedでSpawnする。複数User Preset、同じPresetの複数Voice、Built-inとの重ね演奏が可能。SeedはTrigger成功時に増分する。Q/W/E/A/SとDの共通UI切替は維持する。自由なキー割当は対象外。

Scene間で共有するのはJSONのみ。RosettaUI、GameObject、実行中Voiceを共有しない。Repository/CompilerはSamplesのPerformanceに依存せず、将来別SceneやMIDIから利用できる。

## 検証

実行ごとのJSON、画像、UI操作用スクリプトはgitignore対象の `Reports/Phase9/`。保存検証は専用フォルダまたはOS一時フォルダを使用し、ユーザーのプリセットを上書きしない。

- Unity 6000.3.12f1の既存krab EditorへPipelineで接続。Editorを前面表示・再起動していない。
- Editor経由で新しいSceneを構築・保存。SmokeTest/Performance Scene資産を直接編集していない。
- Play中の実RosettaUIのUI Toolkitボタン・入力フィールドにクリック/Changeを送り、Groupの入れ子、Circle追加、名前/直径/色、LFO追加、Bindingの選択/Depth、静的Preview、アニメーションPreviewの置換、Save、New、Openを確認。InspectorからDocumentを組み立てる代替検証ではない。
- 読込失敗時のDocument維持、Performance UIでの一覧・選択・Trigger、UserとBuilt-inの同時描画、別User Presetの同時Spawn、自然終了を確認。9 Voice / 205 DrawItemの重ね描画を取得した。
- Q/W/E/A/Sは一時的なInput System Keyboardから各キーイベントを供給して既存Update経路を確認。Pipelineのsimulate_keyだけではUIフォーカス/Editor入力のためTriggerしなかったので、物理キーボード操作の確認とは区別する。
- 保存方式をNewtonsoft.Jsonへ切替後も、実UIでOpen → Save → Open → Previewを再確認。最終ランタイムConsoleはError/Warningとも0。Camera/RenderTexture/TargetCameraCanvasの参照一致を確認。終了時はPreset Editor Sceneを開いたEdit Modeでdirty=false。
- 静的表示と重ね描画のGame View画像を取得・確認。最終的な演出の目視承認はユーザーが行う。
- 新規テストはDocument/Compiler、scopeとslot、参照とID、数値、既存Voiceの独立性、停止・重ね演奏・自然終了、JSON往復・深い階層・不正JSON/Version/ID・下書き・既存ファイル保護を対象とする。Unity EditMode最終結果は **201件成功、失敗0**（既存179件＋追加22件）。

未対象：Undo/Redo、ドラッグ並べ替え、高度なPolygon/Variation編集、MIDI、Built-inからの自動Document変換。実機Playerビルド、Syphon受信先、物理キー/マウスの全操作、演出品質の最終評価は未検証。

## Phase 9の変更ファイル

以下に付随する新規.metaも含む。開始時点のPhase 8変更は保持した。

- 新規Core: `Core/PresetDocument.cs`、`Core/PresetCompiler.cs`。
- 新規永続化: `Unity/PresetRepository.cs`。
- 新規ランタイムUI: `Scripts/PresetEditor/ShapeSynthPresetEditor.cs`、`.UI.cs`、`.Modulators.cs`。
- 新規Scene/構築: `Samples/ShapeSynthPresetEditor.unity`、`Scripts/Editor/Scenes/ShapeSynthPresetEditorSceneBuilder.cs`。
- 新規Performance拡張: `Scripts/Performance/ShapeSynthPerformance.UserPresets.cs`。既存 `.UI.cs` にUser Presets表示の1行を追加（同ファイルの他の未コミット変更はPhase 8由来）。
- 既存Core/Unityの最小追加: `Core/VoiceCollection.cs` のStop、`Unity/ShapeSynthController.cs` のStaticPreview。
- 新規テスト: `Tests/Editor/PresetDocumentTests.cs`、`Tests/Editor/Adapter/PresetRepositoryTests.cs`。
- 文書: 本文書、Docs/ShapeSynth/README、ShapeSynthImplementationPlan、Samples/README。

初回テストは191件中190成功（追加テストの例外型期待値を修正）、次回は199件中198成功（追加テストのBinding配列Indexを修正）。その後、深い階層/JSON検証を追加した最終201件がすべて成功した。Play中のテストコード更新に伴うdomain reload時には既存Graphy/ParamSampleComponentの例外が発生したため、停止・再コンパイル後の新しいPlayセッションで再検証し、最終Consoleが0件であることを別途確認した。
