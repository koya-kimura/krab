# 作業方針

簡単で独立した作業は、軽量モデルのサブエージェントへ積極的に委任する。目的・背景・期待する出力を簡潔に伝え、親エージェントが結果を確認して統合する。トークン消費とコンテキスト使用量を抑える。

## プロジェクト概要

KrabはUnity製の映像制作・ライブパフォーマンス用プロジェクト。MIDI操作、BPM同期、設定UI、RenderTextureとSyphon / Spoutによる映像出力を共通基盤として持ち、図形シンセ（ShapeSynth）を段階的に開発している。

- Unityのバージョンは `ProjectSettings/ProjectVersion.txt` を正とする（現在 `6000.3.12f1`）。
- パッケージは `Packages/manifest.json` と `Packages/packages-lock.json` を確認する。現在URP `17.3.0`、Unity Pipeline `0.9.0-exp.1`、Test Framework `1.6.0` を使用。
- 主な実装は `Assets/Krab/` 配下。Rector用のパス・Installer・専用コマンドを前提にしない。

## 構成と既存基盤

| パス | 役割 |
| --- | --- |
| `Assets/Krab/Common/Scripts/Timing/` | `IBPMManager` / `BPMManager`、Tap Tempo、フレームレート設定 |
| `Assets/Krab/Common/Scripts/Midi/` | MIDI入出力、Action、Binding、APC Mini MK2対応 |
| `Assets/Krab/Common/Scripts/Views/` | `IUiTarget` と `UiBuilder` による設定UIの集約 |
| `Assets/Krab/Common/Scripts/Output/` | Camera→RenderTexture、プレビュー、Syphon（macOS）/ Spout（Windows）出力 |
| `Assets/Krab/Common/Prefabs/` | 共通コンポーネントとSceneContainerなどのPrefab |
| `Assets/Krab/Resources/ProjectContainer.prefab` | プロジェクト共通のDI構成 |
| `Assets/Krab/Scenes/` / `Assets/Krab/Performances/` | Sceneと公演ごとの資産 |
| `Assets/Krab/ShapeSynth/Scripts/Core/` | Unity非依存の数値Core（VoiceTime、ADSRなど） |
| `Assets/Krab/ShapeSynth/Tests/Editor/` | CoreのNUnit EditModeテスト |
| `Assets/Krab/ShapeSynth/Scripts/Performance/` / `Scripts/PresetEditor/` | 正式な演奏操作UIとプリセット編集UI |
| `Assets/Krab/ShapeSynth/Scripts/Editor/Scenes/` | Scene構築・設定用Editorツール |
| `Assets/Krab/ShapeSynth/Samples/` | 専用の検証SceneとSmokeTestコード |
| `Assets/Krab/Docs/` | MIDI・ShapeSynthの設計資料と実装計画 |

- DIはUnitySimpleContainerを使用。既存のContainer・`[Inject]` による注入方式を確認して接続する。
- UIはRosettaUI、設定保存はPrefsGUIを使用。`IUiTarget.CreateElement()` を `UiBuilder` が集約する。
- MIDI関連はMinis、映像出力はKlak Syphon（macOS）とKlak Spout（Windows）（`Assets/Krab/Docs/WindowsSupport.md`）、描画にはp5.Uniteが利用可能。実装前に実際の利用箇所とAPIを確認する。
- 出力は `OutputTextureProvider` / `IOutputTextureContainer` を再利用し、`TextureChanged` の通知と購読解除を維持する。
- 設計資料の計画と実装済み機能を区別する。ShapeSynthの後続フェーズは `Assets/Krab/Docs/ShapeSynth/ShapeSynthImplementationPlan.md` を参照し、今回の依頼範囲を越えて実装しない。

## コード変更のルール

- 命名・メンバー順・書式・Unity接続の書き方は `Assets/Krab/Docs/CodingStyle.md` に従う。`Common/Scripts/` のMidi以外がオーナーの手本コード。
- 既存のnamespace（`Krab.Common.*`、`Krab.ShapeSynth.*`）と周辺コードの書式に合わせる。機能と無関係な一括整形・リファクタリングは避ける。
- 継承を想定しないクラスは `sealed` を検討し、単純なプロパティにはauto-propertyや式形式を使う。UnityのInspector表示・シリアライズが必要なフィールドは維持する。
- ファイルが300行を超える場合は責務ごとの分割を検討する。分割自体を目的にしない。
- MonoBehaviourのライフサイクルに合わせ、イベント購読、InputAction、RenderTextureなどの解放を管理する。
- 非同期処理には原則 `Async` 接尾辞と末尾の `CancellationToken` を使い、所有者の寿命に合わせてキャンセル・破棄する。`async void` は必須のイベントハンドラー以外で避ける。
- UniTask・R3など参考文書だけに登場する依存を前提にしない。非同期の戻り値やfire-and-forgetの方式は実際に導入済みのライブラリに合わせる。
- 整形が必要な場合は対象の生成済み `.csproj` を確認し、`dotnet format <対象.csproj> --include <変更ファイル>` などで変更範囲に限定する。`Rector.csproj` は使用しない。
- コンパイル確認に `dotnet build` を使わず、Unity Editorで確認する。生成された `.csproj` / `.sln` を手編集しない。
- 作業開始時に `git status` を確認し、既存の未コミット変更・未追跡ファイルを保護する。関連しない変更の取り消しやコミットをしない。
- Unity資産の移動・削除では `.meta` とGUID参照を維持する。`Library/`、`Temp/`、`Logs/` などの生成物をソースとして編集しない。

## Unity CLI / Pipeline

- 実際の `unity --help`、`unity command --help` とコマンド一覧から利用可能な操作・引数を確認する。参考文書の古いコマンド名をそのまま使わない。
- `unity status` と利用可能な接続オプションで、起動中の対象プロジェクトへ接続する。他プロジェクトのEditorを操作しない。
- group/subcommandは別引数で渡す（例：`unity command editor status`）。zshで複数の引数を一つの文字列にまとめて渡さない。
- コンパイル、Test Runner、Console、Scene操作、Play Mode、画像取得は、その環境で確認できたコマンドを使う。利用できない操作は未実施と報告する。
- 確認のためだけにEditorを前面表示・再起動しない。モーダルダイアログによる待機を、処理の完了と扱わない。
- CLIとPackageの不整合を見つけても、無関係なアップグレードを実施しない。必要な変更は明示して扱う。
- ビルド用のPipeline設定やDevelopment Build設定を無断で変更しない。Player接続は実際の設定と対応コマンドを確認して行う。
- Editorが保持しているScene・Prefabなどは原則Editor経由で変更・保存し、ディスク上の直接編集との競合を避ける。

## 図形シンセの開発・検証

- 数値Coreは可能な限りUnity非依存に保つ。`Krab.ShapeSynth.Core.asmdef` の `noEngineReferences` を維持し、CoreからCommon・UI・描画へ依存させない。
- 数値Coreの変更には境界条件や状態の独立性などを確かめる意味のある単体テストを追加し、Unityで実行できるテストは実際に実行する。
- Unityとの接続では既存のShapeSynth CoreとBPM・DI・UI・Prefs・映像出力を再利用し、検証用コードを正式なRendererやVoice管理へ拡張し過ぎない。
- 時間単位・時計・BPMの適用タイミングは既存設計に合わせる。`IBPMManager.CurrentBpm` は現在有効なBPMであり、予約値と混同しない。
- Unity連携や描画の変更には専用の検証Sceneを用意する。CodexがGameObject・Component・参照・Inspector値を設定し、Unity Editorを通してSceneを保存する。ユーザーにHierarchyの組み立てを任せない。
- 本番Sceneや既存設定へ不用意に変更を加えない。開いているSceneのdirty状態とPlay Modeを確認し、未保存の作業を保護する。必要がなければEditorを再起動しない。
- コンパイル、Test Runner、Scene保存、参照、Play Mode、Consoleなど自動確認できる項目はCodexが実行する。テスト成功・失敗・未実施、画像取得・目視未確認を区別する。
- 最終的な見た目や演出の評価はユーザーが行う。完了報告にはSceneのパス、操作方法、確認済み項目と未検証項目を記載し、Sceneを開いてPlayするだけで確認できる状態にする。
- 新しく実装したPhaseは、ユーザーの目視確認とコミットの指示があるまで自動コミットしない。大きな仕様変更はユーザーと合意してから行う。
- 文書のみの変更ではUnityのコンパイルやScene作成は不要。変更内容に応じた確認を行い、実行していない検証を成功として報告しない。

## ドキュメントと検証成果物の配置

- ShapeSynthは `Assets/Krab/ShapeSynth/` に置き、`Common/` と同階層を維持する。
- 設計・調査・計画は `Assets/Krab/Docs/ShapeSynth/` にまとめ、Git管理する。
- `Tests/` のテストコード・asmdef・meta、および `Samples/` の検証Scene・デモは、再現可能な検証に必要なソースとしてGit管理する。
- 実行ごとのレポート・ログ・JSON/XML・画像はプロジェクト直下の `Reports/` に出力し、gitignore対象とする。長期参照する仕様や手順はDocsまたはSamplesのREADMEへ残す。
