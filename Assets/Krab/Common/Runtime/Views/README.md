# Settings UI hierarchy

`IUiTarget` のクラスに `Krab.Common.Views.UiTargetAttribute` を付けると、
`UiBuilder` が注入された対象を自動的に分類します。RosettaUI本体の変更は不要です。

```csharp
[UiTarget("Common/Timing", "BPM", 0)]
public class BPMManager : MonoBehaviour, IBPMManager, IUiTarget
```

- `CategoryPath`: `/` 区切り。最初はタブ、それ以降は折りたたみカテゴリ。
- `Title`: 設定を開くボタンとウィンドウの表示名。
- `Order`: 小さい順。同値なら表示名のOrdinal順。カテゴリの順序は配下の最小値。
- 属性は継承可能。未指定・空のパスは `Other`、空のタイトルは型名。
  パスの各要素の前後の空白と空要素は除去します。
- 実際にDIから渡された対象のみ表示します。`Save` ボタンと ⌘S は同じ処理で、Prefsを保存したあと、`IUiSaveTarget` を実装した対象の `Save()` も呼びます（例：Preset Editorは未保存の変更があるときだけPresetとValueを保存）。UIの表示切り替えキー（D）は、入力欄に文字を入力している間は効きません。
- `IUiWindowsTarget` を実装した対象は、1つのウインドウの代わりに `CreateWindows()` の各ウインドウを「タイトル - 名前」のランチャーとして並べます（例：Preset Editor - Visual / Values）。

現在の分類:

| タブ | カテゴリ | 設定 |
| --- | --- | --- |
| Common | Timing | BPM, Tap Tempo |
| Common | Application | Frame Rate |
| Common | Output | Output Texture, Syphon, Spout |
| MIDI | Devices | APC mini mk2 Simulator, MPD218 Simulator |
| ShapeSynth | Performance | Performance |
| TypeLayout | Performance | Type Layout |
| Samples | MIDI | Parameters |

## 確認Scene

`Assets/Krab/Modules/ShapeSynth/Samples/UiHierarchyValidation.unity`

Performance SceneをEditor経由でコピーした専用Sceneです。開いてPlayすると、
全8対象が4タブに表示されます。カテゴリを展開し、ボタンから設定ウィンドウを開けます。
`D` でUI表示を切り替えます。サンプル操作・Saveは実際の設定に作用します。
