# ShapeSynth

図形シンセ。プリセット（図形・グループ・ADSR/LFO・共通Value）をMIDIやボタンでTriggerするとVoiceとして発音し、p5.Uniteで出力Cameraへ描く。

| フォルダ | 内容 |
| --- | --- |
| `Runtime/Core/` | Unity非依存の数値Core（`Krab.ShapeSynth.Core`、asmdef・`noEngineReferences`） |
| `Runtime/Unity/` | `ShapeSynthController`、描画Adapter、`PresetRepository`（`Krab.ShapeSynth.Unity`、asmdef） |
| `Runtime/Performance/` | 演奏UI、ユーザープリセット、ルーパー（`Krab.ShapeSynth.Performance`） |
| `Runtime/PresetEditor/` | プリセット編集UI、Gallery（`Krab.ShapeSynth.PresetEditor`） |
| `Editor/Scenes/` | 検証Sceneの構築ツール |
| `Tests/Editor/` | CoreとAdapterのEditModeテスト |
| `Samples/` | 検証Scene（Performance、PresetEditor、Looper、SmokeTestなど）。操作は [Samples/README.md](Samples/README.md) |
| `Docs/` | 仕様と計画。入口は [Docs/README.md](Docs/README.md) |

- プリセット・共通Value・評価は `Assets/StreamingAssets/Krab/ShapeSynth/`（Git管理）。
- `Performance` と `PresetEditor` はCommonのBPM・MIDI・UI・Prefsに依存する（Assembly-CSharp）。
