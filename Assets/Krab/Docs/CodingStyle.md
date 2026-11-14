# Krab コーディングスタイル

`Assets/Krab/Common/Scripts/` のうち `Midi/` 以外（`Timing/`、`Output/`、`Views/`、`Utils/`）はプロジェクトオーナーが書いたコードで、本プロジェクトの書き方の手本とする。新規コード・リファクタはこの文書と手本に合わせる。迷ったら `BPMManager`、`TapTempoController`、`OutputTextureProvider`、`SyphonController`、`OutputPreviewDisplay` を参照する。

## 命名

| 対象 | 規則 | 例 |
| --- | --- | --- |
| namespace | フォルダに対応した `Krab.Common.*` / `Krab.ShapeSynth.*` | `Krab.Common.Output` |
| 型・メソッド・プロパティ・イベント | PascalCase | `OutputTextureProvider`, `ApplyResolution()` |
| interface | `I` + PascalCase。メンバーには明示的に `public` を付ける | `public RenderTexture Texture { get; }` |
| private フィールド | `_camelCase` | `_textureContainer`, `_subscribed` |
| 定数・static readonly | PascalCase | `MinTargetFrameRate`, `TapTimeoutSeconds` |
| ローカル変数・引数 | camelCase。省略しない | `resolution`, `tapCount` |
| イベント | 過去分詞の名詞句（`TextureChanged`） | 既存の `OnBeat` は互換のため維持 |
| イベントハンドラー | `On` + イベント名 | `OnTextureChanged()` |
| DI受け取りメソッド | `[Inject] public void Construct(...)` | |

- 単位や意味が曖昧な数値は名前に含める（`TapTimeoutSeconds`）。
- PrefsのキーはUIに出る名前と揃え、意味を変えるときはキー移行を検討する（保存値が消えるため）。

## ファイルとクラスの構成

- 1ファイル1型を基本とする。ファイル名と型名を一致させる。密接な小さな型（`readonly struct` のスナップショット、private な入れ子クラス）は所有クラスの末尾に置く。
- MonoBehaviour のメンバー順は手本に合わせる。
  1. `const` / `static readonly`
  2. `[SerializeField] private` フィールド
  3. `private readonly` フィールド（`PrefsXxx`、コレクション）
  4. その他の private フィールド
  5. Unity ライフサイクル（`Awake` → `Start` → `Update` → `OnEnable` → `OnDisable` → `OnDestroy`）
  6. public プロパティ・イベント
  7. public メソッド（`CreateElement()`、`[Inject] Construct()` など）
  8. private メソッド（呼び出し順に近い順）
  9. 入れ子型
- 純粋なMonoBehaviour以外のクラス（Core、Repository、Controllerなど）は `const` → フィールド → コンストラクター → イベント・プロパティ → public メソッド → private メソッド → 入れ子型の順。
- 継承を想定しない非MonoBehaviourクラスは `sealed` にする。
- 300行を超えたら責務ごとの分割を検討する。`partial` での分割は1ファイルが責務単位（`.UI.cs` など）になる場合に使う。

## 書式

- 中括弧は Allman（改行して `{`）。インデントは4スペース。1行は120文字程度まで。
- 1行に1文。`{ a; b; }` のように複数の文を1行に詰めない。`try` / `catch` も複数行で書く。
- ガード節は1行の `if (...) return;` を許容し、その後に空行を入れて本処理と分ける。

  ```csharp
  if (_subscribed || _textureContainer == null) return;

  _textureContainer.TextureChanged += OnTextureChanged;
  ```

- 本体が1文の `if` / `foreach` は中括弧を省略してよいが、条件か本体が複数行にまたがる場合は改行してインデントする。ネストした中括弧なしループは避ける。
- 論理的なまとまりの間に空行を入れる。メンバー間は1行空ける（同種のフィールド宣言の並びは詰めてよい）。
- メソッドはブロック本体を基本とする。式本体（`=>`）は単純なプロパティ、1行で収まるgetter的メソッドに限る。
- `using` は `System.*` を先頭に、残りをアルファベット順に並べる（`Krab.*`、`PrefsGUI`、`RosettaUI`、`UnityEngine`、`UnitySimpleContainer`）。未使用のものは削除し、型名の完全修飾より `using` を使う。

## 言語機能

- 型が右辺から明らかなローカルは `var`。フィールド初期化は target-typed `new()` を使う（`private readonly List<double> _tapTimes = new();`）。
- 単純なプロパティは auto-property か式形式（`public int TargetFrameRate => _targetFrameRate;`）。
- イベント発火は `TextureChanged?.Invoke();`。
- 状態を持たない計算は `private static` にする。
- 値の比較・スナップショットには `readonly struct` を使う（`ApplicationFrameSettings.AppliedSnapshot`）。
- LINQは初期化やUI構築など頻度の低い処理で使ってよい。毎フレームの処理では割り当てを避ける。
- 例外は呼び出し側の契約違反（引数不正）に限る。Unityの設定不足は `Debug.LogError` / `LogWarning` で知らせて処理をスキップする。

## Unity との接続

- 必須Componentは `[RequireComponent]` を付け、`Awake` で `GetComponent` する。参照が欠けていれば `Debug.LogError($"{nameof(A)} requires {nameof(B)}.", this);` の形でcontext付きで報告する。
- ログ文言は英語で、型名は文字列リテラルではなく `nameof` を使う。
- イベント購読は `OnEnable` / `OnDisable` で対にし、`_subscribed` フラグで二重購読・二重解除を防ぐ（`OutputPreviewDisplay`）。
- `InputAction` は `OnEnable` で `Enable()`、`OnDisable` で `Disable()` する。
- `RenderTexture` など生成したリソースは `OnDestroy` で `Release()` と `Destroy()` をする。
- 設定UIは `IUiTarget.CreateElement()` を実装し、`[UiTarget("Tab/Category", "Title", order)]` を付ける。`CreateElement()` は `UI.Column(...)` などを返すだけにし、状態変更はボタンのコールバックへ分ける。
- 設定保存は `PrefsXxx` を `private readonly` フィールドで持つ。
- DIは `[Inject] public void Construct(...)`。複数登録があり得るものは `IEnumerable<T>` で受け、選択ロジックは共通のResolverに置く。

## コメント

- コメントは英語で、コードから読み取れない「なぜ」だけを書く（`// Effective tempo; pending edits take effect at the next beat boundary.`）。
- 公開APIで意図が名前だけで伝わらない場合に `/// <summary>` を1〜2文で付ける。処理を逐一説明するコメント、区切り線コメントは書かない。

## コミット

- プレフィックスは `[add]` / `[change]` / `[adjust]` / `[update]` / `[fix]` / `[hotfix]` / `[clean]` / `[wip]`。リファクタは `[clean]`。
- 本文は日本語で何を変えたかを簡潔に。リファクタと機能変更を同じコミットに混ぜない。
