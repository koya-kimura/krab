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
| `{persistentDataPath}/ShapeSynth/Presets/{ID}.json` | プリセット（SchemaVersion 6。`Values` を持たない） |

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

## 移行（2026-11-25実施）

保存済み31個（SchemaVersion 5）を変換した。変換前のフォルダは `{persistentDataPath}/ShapeSynth/Presets-backup-schema5-20261125/` に残してある。

- 延べ64個のValueを、名前ごとに36個へまとめた。同じ名前のValueは全て中身（Component）が同一だったので、名前の付け替えはない。バインドは各名前の最初のValueのIDへ付け替えた（28個のIDを付け替え）。
- 並び順は、ADSRの代表形16個（Rise〜Hold）を先頭に、残りをプリセット名順の初出順にした。
- 変換は一度だけのスクリプトで行い、製品コードには移行処理を置いていない。別の環境の古いプリセットは読み込めない。

## 検証

- Unity Editorでコンパイル成功。EditModeテスト388件成功・失敗0（ShapeSynthは250件。共通Valueの保存・共有・編集の反映・欠けたValue・読めないライブラリ・不正なValueを保存しないこと、未使用Valueがモジュレーターにならないこと、Newの既定ADSRを追加したテストを含む）。
- 移行後の31個は、全て読み込み・Compileに成功した。各プリセットがバインドするValueの中身とバインド数が、変換前と一致することをスクリプトで確認した。
- Preset Editor・Performance・GalleryのUI操作と見た目は未確認。
