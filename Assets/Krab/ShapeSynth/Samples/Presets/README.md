# VJ演奏用プリセット30個

2026-11-22追加。既存SchemaVersion 5のプリセットデータのみ（2026-11-25にSchemaVersion 6へ移行し、Valueは[共通Valueライブラリ](../../../Docs/ShapeSynth/SharedValueLibrary.md)へまとめた。下の表のValues列は追加時の各プリセット内の数）。生成・登録・確認には既存Preset EditorのDocument／Preview／SaveDocument／OpenDocumentとPerformanceのTriggerを使用した。製品コードとSceneに変更なし。

## 使い方

今回の環境では既存の保存先へ登録済み。`Assets/Krab/ShapeSynth/Samples/ShapeSynthPerformance.unity` を開いてPlayする。Settings → ShapeSynth → Performance → Performanceを開き、5タブから各行のTriggerで発火する。MIDI割り当ては各行のMIDIから任意に行う（今回は割り当てていない）。

編集する場合は `Assets/Krab/ShapeSynth/Samples/ShapeSynthPresetEditor.unity` を開いてPlayし、Settings → ShapeSynth → Authoring → Preset EditorのVisual／Valuesを開く。保存済み一覧からOpenする。

名前は `group/name`。5タブはHit（一発）、Line（線）、Array（配列）、Radial（放射）、Organic（周期・有機的な動き）。同じValueを複数Parameterで使い、振幅・符号はBase／Depth、複製の差はDelay／Flip／Randで作っている。

## 原本と再登録

このフォルダのJSONがGit管理する原本。実行時は `Application.persistentDataPath/ShapeSynth/Presets` 内のファイルが読み込まれる。現在のmacOS環境の保存先は `~/Library/Application Support/DefaultCompany/krab/ShapeSynth/Presets/`。

別の環境で使う場合は、既存ファイルをバックアップし、同名ファイル／同じDocument IDがないことを確認して、このフォルダの `vj30-*.json` 30個だけを実行時の保存先へコピーする。`.meta` やREADMEはコピーしない。IDとファイル名は変更しない。自動インポート機能は追加していない。

Preset Editorで編集・Saveした内容は実行時の保存先へ書かれる。この原本は自動更新されないため、確定した変更を原本へ戻す場合は対象IDのJSONを明示的に同期する。

## 実装と確認

- 30個すべてPreset Editor経由で保存・再Open、Unity内でValidate／Compile成功。
- 5タブ×6行と、各タブのTriggerコールバックを確認。
- 80／120／160 BPM × Seed 1234／1235／9001、各60時刻、計270条件・16,200回の数値評価。有限値、正のサイズ、Opacity範囲、非退化行列、終了後のVoice除去を確認。
- 未接続Valueなし。異なるプリセットのレシピ重複なし。登録先と原本のバイト一致。
- 各30個を3時刻ずつ、実際の1920×1080 RenderTextureから90枚取得し、形・配置と時間差を目視確認。
- Performance上で30種類の同時発火と、同一プリセット8回の発火を確認。性能ベンチマークや実機MIDIの演奏評価はしていない。
- Unityスクリプト再コンパイルやTest Runnerは実施していない（製品コード変更なし）。一時作業スクリプトはUnityのscript runでコンパイル・実行済み。
- Consoleにプリセット起因の新規Errorなし。作業中のCLIコマンド名誤り1件は訂正済みで、実行時エラーと区別している。
- 登録時に存在した旧Schema 1／2プリセット5個は、その後保存先から削除された。現在は30個すべて読込成功、旧形式の警告なし。Preset Editorは保存先の追加・変更・削除を約1秒ごとに検知して一覧と警告を更新する。
- 公演での使い心地、最終的な見た目、Syphon／Spoutの外部受信、実機MIDIは未検証。

実行ログ、バックアップ、検証JSON、画像は `Reports/ShapeSynthPresetCollection/`（gitignore）に保存した。新規30個と既存5ファイルの保存前後一致を確認した。自動コミットなし。

## 設計上の補足

参照はローカルp5の `synth-machine`。中央の円、線の移動、放射、疎配置などの着想を借りた。六角形等は標準三角形に置き換え、星は三角形2つで構成した。任意Polygon頂点の投入や頂点編集機能は追加していない。

三角形は標準の半径100pxの頂点を使い、基準1920×1080に合わせた固定倍率Groupでサイズを整える。Triangle BurstはGroup倍率で円周のRadiusも変わるため、その倍率を差し引いてRadiusを設定している。Circleの楕円化も既存の非等方Scaleで表現する。

[追加計画](../../../Docs/ShapeSynth/PresetCollectionPlan.md)の初期数値を基に作成。疎配置のX Randは画面幅比0.008、Y Randは高さ比0.015とし、おおむね短辺の0.015以内へ収めた。

## ID一覧

| ID | 名前 | Values | Bind数 |
|---|---|---|---|
| `vj30-01-01` | 01_Hit/01 Solid Pulse | 1 | 3 |
| `vj30-01-02` | 01_Hit/02 Ring Bloom | 1 | 3 |
| `vj30-01-03` | 01_Hit/03 Diamond Snap | 1 | 3 |
| `vj30-01-04` | 01_Hit/04 Cross Punch | 1 | 4 |
| `vj30-01-05` | 01_Hit/05 Star Spring | 2 | 5 |
| `vj30-01-06` | 01_Hit/06 Facet Flutter | 2 | 4 |
| `vj30-02-01` | 02_Line/01 Horizontal Sweep | 2 | 2 |
| `vj30-02-02` | 02_Line/02 Rising Bar | 2 | 2 |
| `vj30-02-03` | 02_Line/03 Opposed Slashes | 1 | 6 |
| `vj30-02-04` | 02_Line/04 Corner Brackets | 1 | 16 |
| `vj30-02-05` | 02_Line/05 Ribbon Stretch | 2 | 3 |
| `vj30-02-06` | 02_Line/06 Rotating Needle | 2 | 2 |
| `vj30-03-01` | 03_Array/01 Dot Row | 1 | 3 |
| `vj30-03-02` | 03_Array/02 Tile Wave | 1 | 3 |
| `vj30-03-03` | 03_Array/03 Checker Split | 1 | 3 |
| `vj30-03-04` | 03_Array/04 Sparse Confetti | 1 | 12 |
| `vj30-03-05` | 03_Array/05 Center Fan | 1 | 2 |
| `vj30-03-06` | 03_Array/06 Alternating Bounce | 2 | 4 |
| `vj30-04-01` | 04_Radial/01 Triangle Burst | 2 | 2 |
| `vj30-04-02` | 04_Radial/02 Orbit Dots | 2 | 2 |
| `vj30-04-03` | 04_Radial/03 Ring Chase | 1 | 3 |
| `vj30-04-04` | 04_Radial/04 Petal Breath | 1 | 2 |
| `vj30-04-05` | 04_Radial/05 Iris Close | 2 | 12 |
| `vj30-04-06` | 04_Radial/06 Radial Ticks | 2 | 3 |
| `vj30-05-01` | 05_Organic/01 Floating Ellipse | 2 | 4 |
| `vj30-05-02` | 05_Organic/02 Breathing Frame | 1 | 3 |
| `vj30-05-03` | 05_Organic/03 Facet Pair | 2 | 8 |
| `vj30-05-04` | 05_Organic/04 Lissajous Trio | 3 | 6 |
| `vj30-05-05` | 05_Organic/05 Elastic Stack | 2 | 12 |
| `vj30-05-06` | 05_Organic/06 Soft Orbit | 3 | 6 |
