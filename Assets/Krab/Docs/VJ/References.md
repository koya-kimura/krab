# リファレンス台帳

発表「みんなで楽しいVJ」の構成（2Dアニメーション VJ／ライブパフォーマンス／その他）に合わせて、2026-11-25に再編した。発表との関係が薄い候補は外した。外した候補はGit履歴に残っている。IDは変えず、再利用もしない。

公開できる情報だけを載せる。作品・論文・公式ページへのリンクと、その要点、このプロジェクトとの接点を書く。

## 確認状態の表記

| 表記 | 意味 |
| --- | --- |
| 本文 | 本文を読んで確認した |
| 要旨 | 要旨・公式の説明・目次まで確認した |
| 紹介 | 第三者の紹介記事や二次資料で確認した |
| リンク | 発表スライドのリンクから転記した。内容の要約はしていない |

動画は、特記がなければ掲載ページの説明まで確認した段階で、全編は鑑賞していない。

## 一覧

| 章 | ID |
| --- | --- |
| 1. 2Dアニメーション VJ | R43、R38、R47、R49、R39、R70、R02、R27、R71、R24、R31、R63、R67 |
| 2. ライブパフォーマンス | R01、R72、R74、R19、R36 |
| 3. その他 | R35 |
| ジェネVJの実践 | R73、R05、R03 |

---

## 1. 2Dアニメーション VJ

### アニメーションの原則

#### R43 — Creating Usability with Motion: The UX in Motion Manifesto

- Issara Willenskomer、2017。[UX Magazine](https://uxmag.com/articles/creating-usability-with-motion-the-ux-in-motion-manifesto)、[著者のMedium](https://medium.com/@ux_in_motion)。確認：要旨。
- 要点：UIアニメーションの12原則（Easing、Offset & Delay、Parenting、Transformation、Value Change、Masking、Overlay、Cloning、Obscuration、Parallax、Dimensionality、Dolly & Zoom）。発表では、原則 → 技法 → プロパティ → 値の4階層の考え方を引用している。
- 接点：Offset & Delay（遅れ）、Parenting（親子）、Cloning（複製）が、図形シンセの構成に近い。
- 違い：UIの使いやすさのための原則で、演奏の気持ちよさは目的にしていない。

#### R38 — アニメーションの12原則

- 原典：Frank Thomas、Ollie Johnston『The Illusion of Life: Disney Animation』（1981）。確認：書誌。
- 解説：[Alan Becker「12 Principles of Animation (Official Full Series)」](https://www.youtube.com/watch?v=uDqjIdI4bF4)（確認：紹介）、[コマツアヤヒトによる日本語解説（animlog）](https://animlog.net/12principles-of-animation/)（確認：本文）。
- CGへの適用：John Lasseter「Principles of Traditional Animation Applied to 3D Computer Animation」、SIGGRAPH 1987。[本文PDF](https://www.evl.uic.edu/aej/527/papers/lasseter.pdf)。確認：書誌。
- 接点：ADSR・イージング・遅れで作った動きを、つぶしと伸ばし、予備動作、フォロースルー、加減速などの語彙で見比べる。

#### R47 — Easing Equations

- Robert Penner。初出は『Programming Macromedia Flash MX』（2002）第7章。[作者サイト](http://robertpenner.com/easing/)、[easings.net](https://easings.net/)。確認：要旨。
- 要点：現在広く使われるSine / Quad / Cubic / Expo / Back / Elastic / Bounceのイージングの元。
- 接点：図形シンセのカーブはeasings.netの定義に合わせている。

#### R49 — The Book of Shaders「Shaping Functions」／ Inigo Quilez「useful little functions」

- Patricio Gonzalez Vivo、Jen Lowe。[第5章](https://thebookofshaders.com/05/)。Inigo Quilez。[useful little functions](https://iquilezles.org/articles/functions/)、[Graphtoy](https://graphtoy.com/)。確認：要旨。
- 要点：0〜1の値をさまざまな形へ写す関数の辞書。Graphtoyは関数のグラフを描いて試す道具。
- 接点：「BPMに沿った値をfractしてEasingする」方法を、イージング以外の形へ広げるときの参照。

#### R39 — An Experimental Study of Apparent Behavior

- Fritz Heider、Marianne Simmel、1944、American Journal of Psychology。[本文PDF](https://cs.uky.edu/~sgware/reading/papers/heider1944experimental.pdf)。確認：要旨。
- 要点：三角形と円が動くだけの映像から、見る人は意図や感情を読み取る。
- 関連：Tremoulet & Feldman「Perception of Animacy from the Motion of a Single Object」、2000、Perception。DOI: [10.1068/p3101](https://journals.sagepub.com/doi/10.1068/p3101)。物体が1つでも、速さと向きの変化の大きさ、形と進む向きの一致で、生き物らしさの評価が変わる。確認：要旨。
- 接点：プリミティブな図形でも、動きの質で有機的に見せられることの根拠。

### シンセサイザーの値の動かし方

#### R70 — Learning Synths（Ableton）

- Ableton。[Learning Synths](https://learningsynths.ableton.com/)。確認：紹介（[MakeUseOfの紹介記事](https://www.makeuseof.com/ableton-learning-synths-playground-synth-components-breakdown/)）。
- 要点：ブラウザで動かせるシンセの入門教材。エンベロープ（ADSR）とLFOを、音を聞きながら試せる。
- 接点：発表で扱うADSRとLFOを、音の側で体験してもらう入口。

#### R02 — Videosync / Squares

- Showsync。[概要・デモ](https://www.showsync.com/videosync)、[Squaresの仕様](https://support.showsync.com/videosync/device-reference/instruments#squares)。確認：本文。
- 要点：Ableton Live上で映像を構成・演奏する。SquaresはMIDIノートで四角形を出し、不透明度とサイズにエンベロープを持つ。
- 接点：図形をシンセのように鳴らす発想の、最も近い既存の道具。

#### R27 — Lumière

- Robert Henke、2013年〜。[作者ページ](https://www.roberthenke.com/concerts/lumiere.html)。確認：本文。
- 要点：MIDIノート1つごとに、対になったレーザーの図形とFMの音が発音される。
- 接点：図形をワンショットで鳴らす実例。
- 違い：音と図形を同じ譜面から作る。音楽に後から応答するVJとは条件が違う。

### Looper

#### R71 — So-So｜GRAND BEATBOX BATTLE 2021: WORLD LEAGUE｜Solo Loopstation Elimination

- [動画](https://www.youtube.com/watch?v=mZK63OelndQ)。確認：リンク。
- 接点：録音した音をループさせて重ねる演奏の例。発表では、ルーパーを映像へ持ち込む発想の元として紹介する。

#### R24 — Motion Sketch / Motion Phone

- Scott Snibbe、1989–1996。[作者ページ](https://www.snibbe.com/generative/motionphone)。確認：本文。
- 要点：手の動きが抽象図形の動きになり、短いループへ記録される。描くたびに同じループへ重なり、複数のリズムが積み上がる。
- 接点：映像のルーパーの原型の一つ。
- 違い：BPM同期、固定長のスロット、MIDIパッドによる発火ではない。

### 図形と音の対応

#### R31 — Haisuinonasa「Dynamics of the Subway」

- 大西景太監督、2012年頃のMV。[紹介記事](https://www.creativebloq.com/animation/haisuinonasa-music-video-animation-1012876)。確認：本文。
- 要点：楽器の各音が白黒の幾何図形になり、地下鉄を形づくりながら2Dから3Dへ移る。
- 接点：図形の明快さを保ったまま、音と一対一で対応させる。細かな震えも動きの質として効いている。
- 違い：特定の曲のために作り込まれたMVであり、未知の曲への演奏ではない。

#### R63 — Music and movement share a dynamic structure that supports universal expressions of emotion

- Beau Sievers、Larry Polansky、Michael Casey、Thalia Wheatley、2013、PNAS 110(1)、70–75。[PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC3538264)。確認：要旨。
- 要点：同じモデルから旋律と跳ねるボールのアニメーションを作る。参加者がスライダーで感情（怒り、喜び、穏やか、悲しみ、恐れ）を表すと、音楽でも動きでも似た設定になった。
- 接点：図形の動きを数本のパラメータで操作して、音楽と同じ感情を表せることの根拠。

#### R67 — シンクレシス（Michel Chion『Audio-Vision』）

- Michel Chion『L'Audio-Vision』（1990）、英訳1994、第2版2019。[用語解説（USC Scalar）](https://scalar.usc.edu/works/audiovisualities/synchresis)。確認：要旨。
- 要点：同時に起きた音と映像は、無関係なものでも一つに結び付いて知覚される。
- 接点：押した瞬間に図形が出るだけで、音と図形は強く結び付く。何を出すかより、いつ出すかが先に効く。

---

## 2. ライブパフォーマンス

### R01 — Pop Live Coding Encounters: Reflections on Practice

- Sarah Davis（DJ_Dave）、Jack Armitage（Lil Data）、Gus Lobban（Kero Kero Bonito）、2024、ICLC。[論文](https://iil.is/pdf/2024_iclc_davis_et_al_pop.pdf)。確認：本文。
- 要点：制作過程を重視するライブコーディングと、完成した作品を重視するポップミュージックを結ぶ実践の省察。既知の曲を入口にして、コードに詳しくない観客にも変化を感じてもらう。
- 接点：コアな表現を、事前知識のない人にも楽しんでもらうための中心的な参照。
- 限界：実践者の省察で、観客への効果を比べた実験ではない。

### R72 — Pop Live Codingに関連する演奏映像

- [DJ_Dave Hybrid Set for 808 on Twitch Week](https://www.youtube.com/watch?v=E1K6Sv-oIb0&t=389s)。確認：リンク。
- [Azikazin Magic World - Rhythm* Pocket!:「Dorephin Shopping Center」| Lionclad](https://www.youtube.com/watch?v=gvtrG92q32Q)。確認：リンク。

### R74 — 画像・テキストでポップにする実践の場

- [Maltine Records 20th Anniversary Event「CITY」](https://maltine20.net/)。確認：リンク。
- [Gigandect（TuneCore Japan）](https://www.tunecore.co.jp/artists?id=390571&lang=ja)。確認：リンク。
- [マルチネ20 VJアーカイブ（X）](https://x.com/kim___megane/status/1997949839682961431)。確認：リンク。
- 接点：見たことのある画像や文字を重ねて情報量を増やし、図形の映像をポップにする試み。

### R19 — Designing the Spectator Experience

- Stuart Reeves、Steve Benford、Claire O'Malley、Mike Fraser、2005、CHI。[大学リポジトリ](https://nottingham-repository.worktribe.com/output/1020600/designing-the-spectator-experience)。確認：要旨。
- 要点：観客から操作と結果がどう見えるかで、4つの方針に分ける。両方を隠すsecretive、両方を見せるexpressive、効果だけを見せるmagical、操作は見えるが効果が後から現れるsuspenseful。
- 接点：MIDI機器を叩く手元をカメラで映すのはexpressive。ルーパーの再生は操作が過去にあるため、magical寄りになる（このプロジェクトの解釈）。

### R36 — A VJ Centered Exploration of Expressive Interaction

- Jonathan Hook ほか、2011、CHI。DOI: 10.1145/1978942.1979130。[本文PDF](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/VJ20CHI20201120v2032028Camera20Ready29.pdf)。確認：要旨。
- 要点：VJへの質的調査から「表現的な操作」をテーマごとに整理する。演奏者と観客の関係を主な関心とする。
- 接点：叩く楽しさを観客にも伝える、というカメラ入力の狙いの参照。

---

## 3. その他

### R35 — A/V Interchange: Performing Music with Visuals

- Lisa Blanning、2017-09-05、Ableton Blog。[記事](https://www.ableton.com/en/blog/performing-music-with-visuals/)。確認：本文。
- 要点：音の直接的な視覚化、没入、スペクタクル、実験など、ライブの音と映像のやり方を、アーティストの発言とともに紹介する。
- 接点：発表の「映像が行うべきこと」の出典候補。どの発言を引くかは記事で確認してから決める。

---

## ジェネVJの実践

### R73 — Flowと関連する演奏記録

- [Flow（X）](https://x.com/flow_fellow)：オーディオビジュアルイベント。確認：リンク。
- [Flow vol.4 archive](https://www.youtube.com/watch?v=qcl5qgte6lA)、[Flow vol.9](https://www.youtube.com/watch?v=uoORWlE3Tts)、[Flow vol.10](https://www.youtube.com/watch?v=4YFuah6sxGM)。確認：リンク。
- [岡本斗志貴 - ゆnovation & 岡本斗志貴 @Flow 260314](https://www.youtube.com/watch?v=QIBcIDUwKEg)。確認：リンク。
- [VJ | Ken-ichi KAWAMURA - HyperGeek Archive](https://www.youtube.com/playlist?list=PLg2l7bvilqWctE3cLGB9n58YHKpINQC9a)。確認：リンク。

### R05 — draw(tokyo); #3 / VJ Syoronpo. b2b monoton

- 公演2025-10-13、clubasia。monotonによる後記、2025-10-14。[制作後記・公演映像への入口](https://note.com/monoton_music/n/n9275b3cb8870)。確認：本文。
- 要点：ジェネVJに焦点を当てた国内イベント。両者がTouchDesignerで共同の映像を作る仕組みを準備している。カメラ映像と画面収録の両方が公開されている。
- 接点：国内の現在のジェネVJ、共演、会場での映像の働きを具体的に見られる。

### R03 — VISOR / CJing

- Jack Purvis。研究はJack Purvis、Craig Anslow、James Noble、ICLC 2019–2020。[作者のVJ実践](https://www.jackvpurvis.com/performance-art/vjing)、[研究紹介](https://ecs.wgtn.ac.nz/Groups/HCI/CodeJockeying)。確認：本文。
- 要点：コードによる制作と、GUI・MIDIによる本番の操作を組み合わせる。作者は、VJでは主に準備した素材を使い、インターフェースとMIDIで演奏すると説明する。
- 接点：本番中にコードを書かず、作り込んだ表現を叩いて演奏する立場に近い。
