# Motionプリセット（12原則、2026-11-26）

ディズニーアニメーションの12原則（[リファレンス R38](../VJ/References.md#r38--アニメーションの12原則)）を、ShapeSynthの機能だけで表した演奏用プリセット。見た目と動きの評価は本人がGallery・Value Galleryで行い、対話で調整する。

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
| Motion/Secondary（Orbit Accent） | Secondary Action、Staging | 主役は枠線の正方形で、Rise Hold Fall（計3拍）に合わせて現れ、45°ゆっくり回る。中の小さな正方形はローカルのSpin（のこぎり波 1周期/拍）で速く回り、不透明度は主役の0.6倍 |

Motion/DropとMotion/Impactは、作り直した版では使っていない（共有Valueとしては残している）。

## 作成と確認

- Unity Editor（Play停止中）で、一度きりのスクリプトから `PresetRepository` を通して作成した。共有Valueは名前が同じものがあれば作らない。プリセットは同名があれば書き込まない。全プリセットを書き込む前にValidateとCompileを通した。スクリプトはリポジトリに入れていない。
- 保存後、9個（既存3＋新規6）のプリセットが読込エラーなしで読めることを確認した。Motionの各Valueを0〜1拍で標本化し、Windupの最小値が約−0.10、Impactの最大がDropの最初の着地（0.36拍、Drop ≈ 0.02）と一致することを確認した。
- 作り直しは同じ方法で行い、9個とも読込エラーがないことを確認した。
- 見た目・動きの印象は未確認。本人がGalleryで確認する。
