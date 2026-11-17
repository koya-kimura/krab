# 図形シンセ：調査・段階的実装計画

2026-11-08。現在のリポジトリと解決済みPackageCacheを調査した。初回はPhase 1を実装。後続のPhase 2aではLFO Coreと比較描画を追加済み。Phase 2bでParameter/BindingとReference評価も実装。Phase 3でShape/Group/Transformと最小描画Adapterも実装。Phase 4で既存Adapterを正式化し、Camera限定と描画検証を追加（`RenderingAdapter.md`）。Phase 5でVoice/Trigger/Lifetimeを実装（`VoiceAndLifetime.md`）。Phase 6でIndex/RandomのBase/Depth Variationを実装（`Variation.md`）、LayoutやADSR/LFO設定へのVariationは後続。Phase 7でFX01演奏Scene、Modulator ID/編集UI/専用JSON保存を実装（`PerformanceAndModulators.md`）。Phase 8で5プリセットとStroke、v1 JSONの補完読み込みを追加（`MultiPresetPerformance.md`）。最新APIは `ShapeGroupTransform.md` と `ParameterAndBinding.md` を参照し、Phase 2a時点の検討案は `LfoAndPhase2bPlan.md` に残す。

## 既存構成と接続点

- `Common/Scripts/Timing/BPMManager.cs` は `IBPMManager`、`IUiTarget` を実装。`Time.deltaTime` で `Beat` を積算し、PrefsFloatからのBPM変更を次の拍境界で適用する。`OnBeat` はフレーム内で複数拍を跨いでも一回。新シンセの経過時間にはこのイベントや累積Beatを使わない。
- 今回追加した `IBPMManager.CurrentBpm` は予約値でなく現在有効なBPM。既存の変更タイミング・積算・MIDI動作は変更していない。実装クラスは現在BPMManagerのみ。将来のテスト用実装にもプロパティが必要。
- 時間源はゲーム時間 `UnityEngine.Time.timeAsDouble` を選ぶ。既存の `deltaTime` と同じtimeScaleの影響を受け、timeScale=0で止まる。アプリ非アクティブ時はUnityの更新設定に従う。unscaled/壁時計とは混ぜない。停止・再開の製品仕様を変える場合はUnity側の時刻供給箇所で変更する。
- `Common/Scripts/Midi/Input/MidiInputManager.cs` は `[UnitySimpleContainer.Inject] Construct(IBPMManager)` で注入し、既存のAction/Bindingへ渡す。`Midi/Core/Param<T>` はMIDI用の値と変更通知。新シンセのParameterをここへ直結せず、後続のTriggerアダプターで接続する。別パスの未追跡 `Midi/MidiInputManager.cs` 等の作業中変更は今回触れていない。
- `Common/Scripts/Views/IUiTarget.cs` の `CreateElement()` を `UiBuilder` が `IEnumerable<IUiTarget>` 注入で集約。RosettaUIのウィンドウを構築しボタン・入力から `Prefs.Save()`。既存のPrefsFloat/PrefsInt/PrefsBool等の利用方式に合わせる。
- `Common/Scripts/Output/OutputTextureProvider.cs` はCameraのRenderTextureを作成し、`IOutputTextureContainer.Texture` と `TextureChanged` を公開。`SyphonController` は注入された出力を消費。新しい出力Managerを作らずこのCameraへ描画する。
- 既存は `Assets/Krab/Common`、`Performances`、`Docs`、namespace `Krab.Common.*` 等。既存Krabコードにはasmdefなし。新機能は `Assets/Krab/ShapeSynth` / `Krab.ShapeSynth.*` に配置し、Coreだけ `noEngineReferences` のasmdefで隔離した。Assembly-CSharp側からCoreを利用可能。CoreからCommonを参照しない。
- Test Framework 1.6.0はmanifestに導入済み。既存Krabテスト/テストasmdefは見つからない。今回Editor専用のNUnitテストアセンブリを追加。
- p5.Uniteは解決済み `net.sbm.unite@81559d3daad3`、package.json version 0.1.0。`Runtime/Canvas/Rectangle.cs` にRect、`Ellipse.cs` にCircle、`ShapePath.cs` にBeginShape、`Transform.cs` にPush/Pop・Translate・Rotate（ラジアン）・Scaleがある。UCanvasはpartial class。Coreから参照せずAdapterだけで使用。Krab内に描画実装はまだない。既存のReports内の調査資料は今回上書きしていない。

## Phase 1：時間とADSR（今回）

追加ファイル：

- `ShapeSynth/Scripts/Core/Runtime/VoiceTime.cs`：開始秒・開始BPMの不変スナップショット。`max(0, (now-start)*bpm/60)`。
- `ShapeSynth/Scripts/Core/Definitions/AdsrDefinition.cs`：共有可能な不変設定。時間単位は拍、SustainLevelのみ無次元。
- `ShapeSynth/Scripts/Core/Modulation/AdsrInstance.cs`：時刻指定の純粋な評価と終了判定、Instanceごとの外部Release履歴。Update不要。
- `ShapeSynth/Scripts/Core/Krab.ShapeSynth.Core.asmdef`：Unity/UI/描画依存を禁止。
- `ShapeSynth/Tests/Editor/AdsrTests.cs` とテストasmdef：NUnit EditModeテスト。
- 既存TimingのBPMManager/IBPMManagerにはCurrentBpmの読み取り口だけ追加。

将来のTrigger処理からの接続例（今回MonoBehaviourやVoice管理は追加しない）：

```csharp
// IBPMManagerを既存DIから受け取る。Awake完了後にTriggerする。
var time = new VoiceTime(UnityEngine.Time.timeAsDouble, bpm.CurrentBpm);
var envelope = new AdsrInstance(definition, time);
var value = envelope.Evaluate(UnityEngine.Time.timeAsDouble);
// 同じGroupの全Modulatorへ同じtimeを渡す。
// 外部NoteOff時：envelope.Release(UnityEngine.Time.timeAsDouble);
```

`autoRelease: false` を生成時に指定するとSustainTimeによる自動Releaseを抑制し、外部Releaseを待つ。モード列挙型は不要。DefinitionはInstanceと分離し、後のUI編集は新しいDefinitionに置き換える。既存Instanceには生成時設定が残る。

値は線形補間。0拍の区間は即座に通過し、境界は後続区間に属する。全区間0ならTrigger時点で値0・終了。Release時はその時点のAttack/Decay/Sustain値から0へ進む。外部Releaseは最初のイベントのみ記録し、自然Releaseの方が早ければ自然Releaseを優先する。Release中の再イベントで延長しない。評価は状態を進めないため、飛び越し・同時刻再評価・過去時刻評価が可能。外部イベントは時系列順で渡す契約（イベントログの編集/巻き戻しは今回の対象外）。

不正値は補正せず例外：時間設定は有限かつ非負、SustainLevelは有限の0〜1、合計時間も有限、BPMは有限で正、時刻は有限。負の絶対時刻も同じ時計なら許容。開始前の評価値/経過拍は0、終了はfalse。開始前のReleaseと未初期化VoiceTimeは拒否。Parameter全般へのClamp方針とは別にADSRの設定を検証する。

完了条件：境界・0拍・外部Release・BPM固定・Instance独立性を数値テストし、CoreにUnity依存がないこと。Unity側の実接続・シーン変更は後続Phase。

## 後続フェーズ

候補ファイルはすべて `Assets/Krab/ShapeSynth` 配下。必要になった時点で追加する。

| 段階 | 責務と候補ファイル | 依存・既存接続 | テストと完了条件 |
|---|---|---|---|
| 2 LFO / Parameter / Binding | Core/LfoDefinition, LfoInstance, ParameterDefinition, BindingDefinition, ReferenceContext | Phase 1のVoiceTimeを共有。`Reference × (Base + ΣDepth×Signal)`。Modulator状態はBindingごとに複製しない | 位相・周波数単位、複数Binding、同一信号の分岐、リサイズ時Referenceを数値テスト。Clampせず期待式通り |
| 3 Shape / Group / Transform | Core/ShapeDefinition, GroupDefinition, Transform2D, DrawItem | Phase 2。Group再帰と中心原点・右X/上Y。親子行列を合成。Unityから独立した数値表現を選ぶ | ネスト、Pivot、回転/Scale/Position合成を検証。描画なしでワールド形状データを評価可能 |
| 4 最小描画 | Unity/UniteShapeAdapter, TargetCameraCanvas | Phase 3。UCanvasへDrawItemを変換。既存Camera/OutputTextureProviderを利用。Adapter専用asmdefでCore/Unite.Runtimeへ接続 | Circle/Rectangle/Polygonとネストを画面で確認。中心/座標方向/RT解像度/カメラ/URP設定を検証。ShapeごとのGameObject不要 |
| 5 Voice / Trigger / 寿命 | Core/ILifetime, TimerDefinition, TimerInstance, VoiceInstance, VoiceCollection、Unity/ShapeSynthController | Phase 1〜3。DIのIBPMManagerとUnity時計をTriggerでスナップショット。Rendererは4。寿命仕様は下記 | 同時Spawn、BPM固定、階層全体のLifetime必須・全終了で削除、Restartを検証。共有するModulatorは同じInstanceを参照し、子とTimerへ同じVoiceTimeを渡す |
| 6 Variation（2026-11-17にRandom/Delay/Flipへ置換、`Variation.md`） | Core/VariationContext, VariationDefinition, VariationResolver | Phase 2/3/5。Definitionを変えず生成時に個体設定を解決 | Indexの偶奇反転、Seed/配置パス/Parameterごとの再現性、Base/Depthへの適用。共有Definitionの汚染なし。Position由来・ADSR設定・Layoutは対象外 |
| 7 FX01演奏 / Modulator編集 / 保存 | Core/ModulatorLibrary, PerformanceSettings, Fx01Preset、Scripts/Performance、Unity/PerformanceSettingsStore | Coreを独立維持。編集/保存DTO→Trigger時の不変Definition、IUiTarget/RosettaUI、専用JSON保存。汎用Visual編集は後続 | ID/参照検証、旧Voice不変、編集後Trigger、JSON保存/復元を検証。汎用Visual編集とInspector Applyは後続 |
| 8 複数Preset / Stroke | Core/PresetCatalog, AdditionalPresets, ShapeStyle、Performance UI | Phase 4〜7。既存Voice/Renderer/UI/保存へ追加 | 5キー/ボタンの演奏、自然終了、Stroke、旧JSON補完を検証 |
| 9 Preset Editor | Core/PresetDocument, PresetCompiler、Unity/PresetRepository、Scripts/PresetEditor | 既存Core/UI/Voice/描画を再利用。ランタイム編集→不変Definition、JSONでScene間共有 | 階層/Parameter/Binding/Modulator編集、Preview、保存とPerformanceからの読込。詳細は `PresetEditor.md` |
| 10 Preset Editor UI | Scripts/PresetEditorの専用Workspace/View | Phase 9のCore/Compiler/Repositoryを維持。RosettaUI主体の3領域とRT Preview | フォーカス/Scroll維持、Zoom/Pan/Fit、実UI操作、1920/1280配置。`PresetEditorWorkspace.md` |
| 11 MIDI / 出力 / 性能（後続） | Unity/ShapeSynthMidiAdapter、検証用Performanceシーン | Phase 4/5/7。既存MIDI ActionからTrigger/Release、既存Camera→RT→Syphon | MIDI押下/解放、Note重複、解像度変更、複数Voiceの負荷とGCを測定。想定最大数/FPSに合意して達成 |

Phase 5のVoice計算テストはPhase 4と独立して先行可能。描画・編集・MIDIの各AdapterだけがCoreと既存サービスをつなぐ。Coreに新しいManager/汎用グラフ/シーケンサーは作らない。

## Voiceの寿命仕様（Phase 5で実装済み）

2026-11-10更新。ADSRをVoiceの必須要素とする方針は採用しない。値の生成と終了判定を独立した小さなインターフェースで表す。

| 要素 | 値の生成：IModulator | 終了判定：ILifetime | Bindingの接続対象 |
|---|---|---|---|
| ADSR | あり | あり | 対象 |
| LFO | あり | なし | 対象 |
| Timer | なし | あり | 対象外 |

- 既存の `IModulator.Evaluate(double nowSeconds)` は維持する。`ILifetime` は既存ADSRに合わせた `bool IsFinished(double nowSeconds)` を採用した。ADSRは両インターフェース、LFOはIModulatorのみ、TimerはILifetimeのみを実装する。
- Trigger対象のGroupとその全子孫を含む階層全体に、ADSRまたはTimerが少なくとも1つ必要。各Groupに個別のADSRを要求しない。Timerだけでも有効だが、LFOだけの階層はTrigger条件を満たさない。
- Voiceは配下のすべてのLifetime要素が終了したときに破棄する。信号値が0かどうかでは判定しない。Timerの終了は他のLifetimeを強制終了せず、ADSRのReleaseも発火しない。外部Releaseを待つADSRが残っていれば、Timer終了後もVoiceは存続する。
- TimerのDurationは拍数で指定する。Voice開始時のBPMを固定した `VoiceTime` で評価し、Trigger後のグローバルBPM変更には追従しない。値を生成せず、Bindingの信号スロットには登録しない。
- Phase 5では階層全体のLifetime有無、Timerの拍境界とBPM固定、ADSRとTimerの終了順が異なる場合の全終了判定、Timer終了後も保持中ADSRが存続することを数値テストする。Durationの不正値・0拍・開始前評価も既存Coreの規約に合わせて仕様化・検証する。

Phase 3では計画のみを更新し、Phase 5でTimer、ILifetime、Voice寿命管理を実装した。既存ADSR・LFO・Parameter・Binding・Transformの数値挙動は維持する。GroupRuntimeDefinitionがGroup出現ごとの信号scopeとTimer配置を定義し、Voiceが状態を所有する。詳細は `VoiceAndLifetime.md`。

## 今後合意が必要な判断

1. **演奏中のDefinition編集**：現時点は生成時スナップショット。既存Voiceへライブ反映を求めるなら、時間継続やRelease値の保持を含めて合意する。
2. **MIDI NoteOffの対象**：RestartはTriggerTarget参照で対象を識別する仕様をPhase 5で実装済み。MIDIの同音重複やNoteOffとVoiceの対応はPhase 8前に決める。
3. **停止時間の扱い**：今回は既存同様のゲーム時間。実時間追従や明示トランスポートを求める場合は時刻供給境界で設計変更する。BPM変更と同フレームのTriggerは呼出順の有効BPMを取得するため、厳密な拍同期が必要なら呼出順を定義する。

Referenceの単位、LFOの波形/位相、Polygonの表現、加算後の描画制約、最大Voice数は該当Phaseで決める。描画Adapterが負のサイズ等をどう扱うかを定義し、Coreの一般Parameterには一律Clampを加えない。

## 検証結果

後述の実行結果を参照。Unity Test Runnerは既存Editorが同プロジェクトを開いているためバッチ実行不可。既存EditorのTest Runnerから `Krab.ShapeSynth.Tests` を実行する必要がある。PlayMode・実MIDI・描画・Syphon・timeScale実動作は今回未検証。


- .NET 10の一時コンソールハーネスでCoreと同じ `AdsrTests.cs` をコンパイルし、Unity同梱の `nunit.framework.dll` の実Assertを使ってTest/TestCaseを呼び出した結果：**25成功、0失敗**。NuGetへの依存なし。これはUnity Test Runnerの検出・アセンブリロード検証を代替しない。
- 対象：ADSR全境界、SustainTime=0、各0拍区間/全0拍、Attack/Decay/Sustain中の外部Release、再評価と終了、大きな時間ジャンプ、60/120BPMの独立Instance、仮想グローバルBPM変更、手動保持、重複/遅延Release、不正設定。
- BPMManager自体を動かした統合テストは未実施。BPM変更テストは生成に渡すBPMを変更する純粋数値テスト。
- Unity 6000.3.12f1の `-batchmode -nographics -runTests -testPlatform EditMode -testFilter Krab.ShapeSynth.Tests` は「another Unity instance is running with this project open」で中止。テスト失敗として数えていない。
- 変更したTimingファイルへの `git diff --check` は成功。作業開始時から存在する `Midi.meta` の空白警告は今回の変更対象外。

2026-11-11: Phase 9としてランタイムPreset Editorを追加。従来のPhase 9候補だったMIDI/出力/性能はPhase 10へ移動。今回MIDI等は実装していない。

2026-11-12: Phase 10はPreset Editor UI改善。MIDI/出力/性能の後続候補はPhase 11へ移動し、今回は実装していない。

2026-11-17: 計画外の追加として、signalの種類ごとのクラスの土台、Ramp、ADSRの区間ごとのカーブを実装（[RampとADSRのカーブ](RampAndCurves.md)）。ADSRの積分値、Triggerをまたいで積み重なる値は未実装。

2026-11-17: 色をHSBのParameterに一本化し、Fill / Strokeを同じ仕様にした（[HSBによる色](HsbColor.md)）。To Color / Color Mix / Hue Shiftは廃止し、SchemaVersion 3へ移行した。
