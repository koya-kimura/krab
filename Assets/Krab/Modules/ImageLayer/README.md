# ImageLayer

切り抜き画像と連番アニメーションのレイヤー。Deck A・Bがそれぞれ配置（Layoutプリセット）と画像（Imageエントリ）を選んで描き、MIDIパッドで切り替える。

| フォルダ | 内容 |
| --- | --- |
| `Runtime/Core/` | 自然順、セット/クリップ、切り出し座標、フィット、コマ選択、プリセット（`Krab.ImageLayer.Core`、asmdef・`noEngineReferences`） |
| `Runtime/Unity/` | 非同期読み込み、`ImageCanvas`、シェーダー（`Krab.ImageLayer.Unity`、asmdef） |
| `Runtime/Performance/` | `ImageLayerPerformance`、Deck、パターン、プリセット保存（`Krab.ImageLayer.Performance`） |
| `Tests/Editor/` | CoreのEditModeテスト |
| `Samples/` | 検証Scene（Performance、SmokeTest、LayerOrder）。操作は [Samples/README.md](Samples/README.md) |
| `Docs/` | [実装計画](Docs/ImageLayerImplementationPlan.md)、[Layoutプリセット](Docs/LayoutPresets.md) |
| `Tools~/` | グリーンバック動画を透過PNG連番にする `convert_greenscreen.sh`（Unityには読み込まれない） |

- 画像ライブラリは `Assets/StreamingAssets/Krab/ImageLayer/Libraries/<ライブラリ名>/`、プリセットは `.../ImageLayer/Presets/`（Git管理）。
