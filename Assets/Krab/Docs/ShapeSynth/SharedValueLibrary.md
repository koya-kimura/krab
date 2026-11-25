# 共通Valueライブラリ（2026-11-25）

Valueを全プリセット共通の部品にした。プリセットは「Visual（図形の階層とパラメーター）＋バインド」だけを持ち、バインドは共通Valueをキー（Value ID）で参照する。Valueを直すと、そのValueを使う全プリセットが一緒に変わる。

## 用語

- **プリセット**：1回Triggerすると出る映像1つ分。名前、Visual（Root Group以下の図形の木）、各パラメーターのバインド（Value ID と Depth）を持つ。
- **Value**：ADSR／LFOのComponentを重み付きで足した信号。共通の部品で、どのプリセットからもバインドできる。
- **Visual**：プリセットごとに1つ。部品化していない。

## 保存

保存形式はJSONのまま（Prefsは使わない）。

| ファイル | 中身 |
|---|---|
| `{persistentDataPath}/ShapeSynth/Values.json` | 共通Value（`{"SchemaVersion": 1, "Values": [...]}`） |
| `{persistentDataPath}/ShapeSynth/Presets/{ID}.json` | プリセット（SchemaVersion 7。共有の `Values` は持たず、専用の `LocalValues` を持つ） |

- `PresetDocument.Values` は保存しない（`[NonSerialized]`）。読み込み時に `PresetRepository` が共通Valueを付ける。`List()` で読んだプリセットは同じValue一覧を共有する。
- `Values.json` がない場合は空のライブラリとして扱う。読めない場合は一覧に1件のエラーを出し、プリセットは読み込まない。
- プリセットとValueの保存は、どちらも検証してから一時ファイル経由で置き換える。
- SchemaVersion 5以前のプリセットは読み込まない（従来どおり自動移行はしない）。

## Compile

Compileでモジュレーターになるのは、そのプリセットがバインドしているValueだけ（`PresetDocument.BoundValues()`）。共通ライブラリの未使用ValueでVoiceが長くならないようにするため。以前は「プリセット内の全Value」だったが、移行時点の30個のプリセットは全Valueをバインドしていたので、動きは変わらない。Triggerには、バインドしたValueの中にADSRが1つ以上必要。

## Preset Editor

- Valuesウィンドウには、どのプリセットを開いていても共通Valueが並ぶ。選択中のValueはプリセットを開き直しても保たれる。
- Save（Cmd+S）で共通Value → プリセットの順に書き込む。`Values.json` が読めなかった場合は、空の一覧で上書きしないようSaveを止めてエラーを出す。
- Remove Valueは、開いているプリセットと保存済みの全プリセットのどれにもバインドされていないValueだけ有効。
- Newは、最初にADSRを含む共通Valueへ円のOpacityをバインドする。ADSRを含むValueが1つもないときだけ「Default ADSR」を共通Valueに足す。
- 保存先の変更検知（約1秒ごと）は `Values.json` も対象にする。PerformanceとGalleryは、Saveの後の一打・一覧更新から新しいValueを使う。

## プリセット専用のValue（ローカルValue、SchemaVersion 7、2026-11-25）

共有のValueを「部品」として保ちつつ、そのプリセットだけで値を直せるようにした。

- プリセットのJSONに `LocalValues` を持つ。ローカルValueは `Values.json` には入らず、編集しても他のプリセットは変わらない。IDは共有Valueと重ならない（重なるとValidateで拒否）。バインドはどちらのValueもIDで指す。
- SchemaVersion 6のプリセットは「ローカルValueのない7」としてそのまま読み込む（内容の変換はない）。次にSaveしたときに7で書かれる。
- **Bindウィンドウ**：InspectorのBind列はボタンになり、押すと「Preset Editor - Bind」が開く。ValueをグラフでタブごとにValue Galleryと同じ形で並べ、名前のボタンでバインドする。**None**で外す。この画面では値を編集しない。
- **Copy & Bind**：今バインドしているValueをローカルValueとして複製し、バインドを付け替える（Depthはそのまま）。複製はValuesウィンドウで選択されるので、そこで直す。
- Valuesウィンドウ・Value Gallery・Bindは、ローカルValueがあるとき先頭に **This Preset** タブを出す。This Presetタブで `+ Value` を押すとローカルValueを足す。Duplicateは、共有なら共有に、ローカルならローカルに複製する。
- Valuesウィンドウでは、Nameの下に「This preset only.」か、共有のValueなら「Shared. Editing changes N preset(s): …」（使っているプリセット名）を出す。
- 似たローカルValueを共有ライブラリへまとめる操作は、まだない（今後、AIで候補を出して人が確認する形を検討）。

## 階層（group/name、2026-11-25）

Valueの名前は、プリセットと同じ `group/name` の形で書ける（`PresetPath`）。保存形式は変えていない。名前の最初の `/` までがグループ。

- Valuesウィンドウの一覧とValue Galleryは、グループごとのタブに分かれる。並びはPerformanceのタブと同じで、グループ名の順、`/` のない名前は最後の **Ungrouped**。タブの中は名前の残りの部分の順。並べ替えは `PresetPathGroups.Of`（Core、EditModeテストあり）。
- 一覧とギャラリーには名前の残りの部分だけを出す。Bindのプルダウンには `group/name` の全体を、同じ並びで出す。
- Valuesウィンドウで名前や選択が変わると、選択中のValueのグループのタブを開く。`+ Value` はグループなしの「New Value」（同名があれば「New Value 2」…）を足し、Ungroupedのタブを開く。
- ADSRの代表形16個は、グラフの形を一通り揃えたグループとして **All Shapes** に入れた（例：`All Shapes/Rise`）。

## 移行（2026-11-25実施）

保存済み31個（SchemaVersion 5）を変換した。変換前のフォルダはバックアップとして残していたが、2026-11-25に削除した。

- 延べ64個のValueを、名前ごとに36個へまとめた。同じ名前のValueは全て中身（Component）が同一だったので、名前の付け替えはない。バインドは各名前の最初のValueのIDへ付け替えた（28個のIDを付け替え）。
- 並び順は、ADSRの代表形16個（Rise〜Hold）を先頭に、残りをプリセット名順の初出順にした。
- 変換は一度だけのスクリプトで行い、製品コードには移行処理を置いていない。別の環境の古いプリセットは読み込めない。

## 2026-11-25：ADSRの代表形16個だけにした

Visualの作り直しに合わせて、共通ValueをADSRの代表形16個（Rise、Rise Hold、Rise Step Hold、Rise Fall、Rise Step Fall、Rise Hold Fall、Rise Step Hold Fall、Rise Settle Hold、Rise Two-Step Fall、Rise Settle Hold Fall、Fall、Hold Fall、Fall Hold、Two-Step Fall、Fall Hold Fall、Hold）に絞った。それ以外の20個（Hit、Gate、Travel、LFOなど）は削除した。16個の設定は、代表形の比較資料（手元のHTML、未公開）と一致することを確認した。`Values.json` はGit管理外のため、変更はこの記録だけ残る。

## 検証

- Unity Editorでコンパイル成功。EditModeテスト388件成功・失敗0（ShapeSynthは250件。共通Valueの保存・共有・編集の反映・欠けたValue・読めないライブラリ・不正なValueを保存しないこと、未使用Valueがモジュレーターにならないこと、Newの既定ADSRを追加したテストを含む）。
- 移行後の31個は、全て読み込み・Compileに成功した。各プリセットがバインドするValueの中身とバインド数が、変換前と一致することをスクリプトで確認した。
- Preset Editor・Performance・GalleryのUI操作と見た目は未確認。
