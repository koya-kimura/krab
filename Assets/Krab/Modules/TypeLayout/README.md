# TypeLayout

文字のレイヤー。スクリプトで書いたパターン（ティッカー、四隅、渦巻きなど）で文字を配置し、BPMに合わせて動かす。UIから変えるのは各パターンのON/OFFとパラメータ、表示する言葉。

| フォルダ | 内容 |
| --- | --- |
| `Runtime/Core/` | 配置計算（`Krab.TypeLayout.Core`、asmdef・`noEngineReferences`） |
| `Runtime/Unity/` | `TypeLayoutCanvas`、フォント読み込み、描画（`Krab.TypeLayout.Unity`、asmdef） |
| `Runtime/Performance/` | `TypeLayoutPerformance`、パターン、言葉の管理（`Krab.TypeLayout.Performance`） |
| `Tests/Editor/` | CoreのEditModeテスト |
| `Samples/` | 検証Scene（Performance、SmokeTest）。操作は [Samples/README.md](Samples/README.md) |
| `Docs/` | [TypeLayoutImplementationPlan.md](Docs/TypeLayoutImplementationPlan.md) |

- フォントは `Assets/StreamingAssets/Krab/TypeLayout/Fonts/`。
- 新しいパターンは `TypePattern` を継承し、`TypeLayoutPerformance` の一覧に加える。
