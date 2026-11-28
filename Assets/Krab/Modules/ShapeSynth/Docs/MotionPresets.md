# Motionプリセット（12原則、2026-11-26）

ディズニーアニメーションの12原則（[リファレンス R38](../../../Docs/VJ/References.md#r38--アニメーションの12原則)）を、ShapeSynthの機能だけで表した演奏用プリセット。見た目と動きの評価は本人がGallery・Value Galleryで行い、対話で調整する。

## 方針

- 動きの「語彙」は共有Valueの **Motion** グループに置き、プリセットはそれを組み合わせる。プリセット固有の調整は、そのプリセットのローカルValueで行う（[共通Valueライブラリ](SharedValueLibrary.md)）。
- 一打は0.5〜3拍程度。主役は1つ、図形はプリミティブ、色はコントラスト高め。
- Staging、Solid Drawing、Appealは単独のプリセットにせず、全体で守る指針とする。

## 共有Value（Motion）

単位は拍。いずれもADSR 1つ。

| Value | Attack | Decay | Sustain | Hold | Release | 形 |
|---|---|---|---|---|---|---|
| Motion/Pop | 0.25 OutBack | 0 | 1 | 0.75 | 0 | 0→1で少し行き過ぎて止まる（決めポーズ） |
| Motion/Windup | 0.5 InBack | 0 | 1 | 0.5 | 0 | 一度0より下（約−0.1）へ振れてから1へ（予備動作） |
| Motion/Drop | 0 | 0 | 1 | 0 | 1 OutBounce | 1から0へ落ち、弾みながら止まる |
| Motion/Wobble | 0.5 OutElastic | 0 | 1 | 0.5 | 0 | 1の周りで揺れて収まる |
| Motion/Impact | 0.36 InExpo | 0.15 OutQuad | 0 | 0 | 0 | ほぼ0のまま待ち、0.36拍で一瞬だけ1。DropのOutBounceが最初に着地する時刻（1/2.75拍）に合わせた |
| Motion/Fade | 0 | 0 | 1 | 0.5 | 0.5 InQuad | 1のまま、最後の0.5拍で消える |

## プリセット

名前は `Motion/…` で、PerformanceのMotionタブに並ぶ。座標はCanvas中央が原点、Y上向き。大きさと位置は画面の短辺に対する比（xMin）。

2026-11-26に、もう一段抽象的に作り直した。最初の版（落ちて弾むボール、棒の付いた振り子、惑星のような周回など）は具体的な物を説明しすぎていて、A/Vの素材には向かなかった。地面・支点・衛星のような「物」を置かず、白い線と正方形だけで、原則を動きの質として表す。IDは変えていない（MIDI割り当てはそのまま）。

| プリセット（旧名） | 主な原則 | 構成 |
|---|---|---|
| Motion/Squash（Bounce Drop） | Squash & Stretch、Timing | 横長の棒1本。横に1.6倍・縦0.3倍に潰れた状態で現れ、Wobbleで元の形へ揺れ戻る（ScaleX = 1.6 − 0.6 × Wobble、ScaleY = 0.3 + 0.7 × Wobble）。OpacityはFade |
| Motion/Anticipate（Wind-up Burst） | Anticipation、Exaggeration | 放射状の短い線16本（Layout Circle、FaceOutward）。Radius = 0.12 + 0.33 × Windupで、一度内へ寄ってから大きく広がる |
| Motion/Follow（Trailing Ribbon） | Follow-through、Overlapping Action | 縦線9本（Row、Delay 0.05拍・CenterOut）。ScaleY = Wobbleで、中央から順に伸び、少し行き過ぎて収まる |
| Motion/Arc（Pendulum） | Arcs | 円周上の正方形6個（FaceOutward）を入れたGroupを、Rotation = −90 + 90 × Sweep で4分の1周だけ弧に沿って回し、揺れて止める。Sweepはこのプリセットのローカル値（Attack 1拍 OutElastic、Hold 0.5拍） |
| Motion/Pose（Pop Grid） | Pose to Pose、Slow in/out | 正方形25個（Grid 5列、Delay 0.03拍・CenterOut）。Scale = Pop、Rotation = 45 − 45 × Popで、中央から順に回り込んで決めのポーズで止まる |

Motion/DropとMotion/Impactは、作り直した版では使っていない（共有Valueとしては残している）。

2026-11-26：当面は1種類のShapeで表せるものに絞るため、2種類のShape（枠線の正方形と小さな正方形）を使っていたMotion/Secondary（Secondary Action、Staging）を削除した。色もいったん白で考える（既存のRow Circlesも白にした）。

## 作成と確認

- Unity Editor（Play停止中）で、一度きりのスクリプトから `PresetRepository` を通して作成した。共有Valueは名前が同じものがあれば作らない。プリセットは同名があれば書き込まない。全プリセットを書き込む前にValidateとCompileを通した。スクリプトはリポジトリに入れていない。
- 保存後、9個（既存3＋新規6）のプリセットが読込エラーなしで読めることを確認した。Motionの各Valueを0〜1拍で標本化し、Windupの最小値が約−0.10、Impactの最大がDropの最初の着地（0.36拍、Drop ≈ 0.02）と一致することを確認した。
- 作り直しは同じ方法で行い、9個とも読込エラーがないことを確認した。
- 見た目・動きの印象は未確認。本人がGalleryで確認する。

## Monoプリセット（2026-11-26）

方針：1種類のShape（全て長方形、Layoutによる複製は可）、白、共有Valueのみ（All Shapes／ADSR／Motion）、ワンショット。RandomのStepsとAnchorを使い、画面の1/5・1/4・1/2のような決まった位置から出す。Unity CLIの一度きりのスクリプトで、1つずつ検証・Compileして保存した（同名は飛ばすので、途中で止まっても再実行できる）。25個（既存7＋Mono 18）が読込エラーなし。見た目は未確認。

| プリセット | 動き |
|---|---|
| Mono/Column Rise | 幅1/5の柱が、5か所のどれかで下から伸びて戻る（Anchor Bottom、X Steps 5、ScaleY = Rise Fall） |
| Mono/Column Flash | 幅1/5の柱が、5か所のどれかで光って消える（Opacity = ADSR/Fast） |
| Mono/Band Wipe | 高さ1/5の帯が、5段のどれかで左から伸びて戻る（Anchor Left） |
| Mono/Column Stagger | 5本の柱が左から順に下から伸びる（Delay 0.125拍） |
| Mono/Column Scatter | 5本の柱がばらばらの時刻に伸びる（Delay Random 0.5拍） |
| Mono/Full Wipe | 画面全体が下から埋まって戻る |
| Mono/Horizon | 細い横線が中央から開いて消える（Pop、Fade） |
| Mono/Tilt | 棒が±45°のどちらかの傾きで中央から開いて消える（Rotation Rand 45・Steps 2） |
| Mono/Quarter | 画面の1/4の四角が、4象限のどれかに出る（X・Y Steps 2、Pop） |
| Mono/Grid Pop | 5×5の小さな正方形が中央から順に出る |
| Mono/Grid Twinkle | 4×4のタイルがばらばらに光る |
| Mono/Tick Chase | 円周上の24本の目盛りが順に光る |
| Mono/Tick Burst | 放射状の32本が一度寄ってから広がる（Windup） |
| Mono/Line Curtain | 画面幅に17本の縦線が中央から順に伸びて揺れる（Wobble） |
| Mono/Corner Grow | 正方形が左下の角から、3×3のどこかで広がる（Anchor BottomLeft） |
| Mono/Flash | 画面全体が一瞬光る（Fall） |
| Mono/Quarter Turn | 正方形が4分の1回転して止まる（Wobble） |
| Mono/Band Stack | 高さ1/5の帯が下から順に5段積まれる |

## 共有Valueの追加（2026-11-26）

- **LFO/…**（16個）：Sine／Triangle／Sawtooth／Square × 0.25／0.5／1／2周期/拍のLFO単体。揺れ用で、一打の長さは別のADSR（Opacityなど）で決める。
- **Eased/…**（16個）：All Shapesの16個に、Attack OutSine・Decay InOutSine・Release InSineを掛けた版（12原則のSlow in／Slow out）。All ShapesはLinearの基準として残す。

## Genプリセット（自動生成、2026-11-26）

出力の絵を想定せずにランダムにバインドし、数値で「マシな動き」だけを残した100個（`Gen/001…100`）。名前は「番号 形 Layout 動かしたParameter」。

- 生成：Rectangle／Circleの1ノード（Layout複製は可）、白。大きさ、Anchor、枠線、Layout（None／Row／Column／Grid／Circle、数、間隔、FaceOutward、Delayと順番）、単独のときは位置のSteps 3か5、をランダムに選ぶ。OpacityにはADSRのValue（Eased／Motion／ADSR/Fast）を必ずバインドして一打の長さにする。動かすParameterを1〜2個（X、Y、回転、拡大、横・縦の拡大、大きさ、並びの広がり）選び、Eased／Motion／LFOのValueをバインドする。乱数の種は固定（20261126）。
- ふるい分け（一打を24点で評価）：長さ0.5〜4拍、図形の中心の85%以上が画面内、24点の30%以上で不透明度0.2超、動きの量（中心位置と大きさの変化の合計）が0.08〜6、既に残したものと動きの曲線の平均差が0.02以上。
- 結果：263個を試し、100個を保存（落としたもの：動きが少ない83、似すぎ35、見えない16、長さ13、画面外12、動きすぎ4）。全150個が読込エラーなし。見た目の良し悪しは未確認で、Galleryで選ぶ。
- 生成スクリプトは `Assets/Krab/Modules/ShapeSynth/Samples/Generators/GenPresets.csx.txt`（Unity CLIの `script eval_file` 用。型名を完全修飾名に置き換えてから実行する）。

### 追加の200個と番号の付け直し（2026-11-26）

- `GenPresetsMore.csx.txt` で200個を追加した（乱数の種20261127）。ふるい分けは最初の100個と同じで、さらに保存済みの全プリセット（測れる131個）とも動きが似すぎていないかを比べた。510個を試し200個を保存（落としたもの：動きが少ない113、似すぎ95、見えない44、長さ22、画面外20、動きすぎ16）。
- その後、全プリセットの名前を、グループ順（Gen、Mono、Motion、グループなしは最後）・元の番号順に001から付け直した（例：`Gen/001 Circle Column PositionX+Size`）。Genの新しい200個は最初の100個の後ろに続く。IDは変えていない。Visualウィンドウで開いていた「New Preset」は書き換えていない。
- 合計351個、読込エラーなし。見た目は未確認。

### 合計500個へ（2026-11-26）

- 同じスクリプト（乱数の種を20261128にし、合計が500になる数だけ作る設定）で149個を追加した。保存済みの測れる331個とも比べた。449個を試し149個を保存（動きが少ない109、似すぎ99、見えない41、長さ25、画面外16、動きすぎ10）。
- 全500個を同じ規則で001から番号を付け直した（前回まで番号のなかった「New Preset」も番号付き）。読込エラーなし。見た目は未確認。

### 最初の100個の作り直し（2026-11-26）

最初のGen 100個（Gen/001〜100）は、保存済みのものと比べずに作っていたため、後の回より質が低かった。同じ番号とIDのまま中身だけを、後の回と同じ基準（残りの380個とも似すぎていないか比べる）で作り直した（`GenPresetsRedo.csx.txt`、乱数の種20261129）。309個を試し100個を保存（動きが少ない76、似すぎ69、見えない26、長さ18、画面外11、動きすぎ9）。名前の番号は同じで、後ろの説明は新しい中身に合わせた。合計500個、読込エラーなし。Play中に実行したが、開いていたのは未保存の新規プリセットで、作り直しの対象ではない。見た目は未確認。
