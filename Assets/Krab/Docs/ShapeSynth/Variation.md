# Random / Delay / Flip（旧Phase 6 Variationを置き換え、2026-11-17）

Phase 6のVariation（`VariationDefinition` のAlternating/Randomによる **置き換え**、`WithBaseVariation`、Binding の `DepthVariation`）は廃止した。複製ごと・Triggerごとの差は、次の3つで表す。旧Variationの検証用 `ShapeSynth/Variation`（VariationSmokeTest）と `VariationTests` も削除した。

| 機能 | どこで決まるか | 単位・範囲 |
| --- | --- | --- |
| Random（Base / Depth / Delay への加算） | 評価器の構築時（Voice生成時）に、Voice Seedと出現パスから1回だけ | 各値と同じ単位。毎フレームは変わらない |
| Delay（Bindingの遅れ） | 固定値はCompile時、Random分は構築時 | 拍。0以上 |
| Flip（Depthの符号反転） | Preset Compile時（複製の番号から） | +1 / −1 |

## Core API

```csharp
var rotation = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 90)
    .WithBaseRandom(RandomRange.Spread(45));             // 45〜135度（出現ごと・Seedごと）
var binding = new BindingDefinition(slot, depth: 0,
    depthRandom: new RandomRange(-0.04, 0.04, salt: 1), // Depthへ加算
    delayBeats: 0.25,                                     // 0.25拍前の信号を読む
    delayRandom: new RandomRange(0, 1));                  // さらに0〜1拍
var value = SeededRandom.Range(0, 360, new RandomContext(seed, i), salt: 81); // 単発の決定的乱数
```

- `RandomRange(min, max, salt)` / `RandomRange.Spread(±)`：有限・min<=max。**加算**。旧Variationの置き換えとは異なる（Fx01はBase/Depth=0に変更して同じ範囲を維持）。
- `RandomContext(voiceSeed, path...)`：旧VariationContext（ファイル・GUIDを引き継いで改名）。
- `SeededRandom.Range`：旧VariationDefinition.Randomと同じMix64ハッシュ。AdditionalPresetsは同じ値を再現する。
- `RandomResolver.Resolve(definition, context, parameterKey)`：Random分を足した通常のDefinitionを返す。channel = `(parameterKey << 32) | component`（0=Base、i+1=Binding iのDepth）。**DelayのRandomは全Bindingで共通のchannel** を使い、1つの出現（1つの複製）は1つの遅れで動く。parameterKeyは 1=X 2=Y 3=Rotation 4=ScaleX 5=ScaleY 6=Width/Diameter 7=Height 8=Opacity 9=Color Mix 10=Hue Shift。
- `ParameterEvaluator`：Random未解決のDefinitionは拒否。Delay>0のBindingは `IDelayableModulator.EvaluateDelayed(now, delayBeats)` を呼ぶ（ADSR/LFOが実装。Voice開始時のBPMで拍→秒）。Delayに対応しない信号は構築時に拒否。`MaxDelayBeats` を公開。
- `HierarchyEvaluator.MaxDelayBeats`：全出現の最大Delay。`VoiceInstance.IsFinished` はLifetimeを `now − MaxDelay` で判定し、遅れた最後の複製が終わるまでVoiceを残す。
- 遅れたADSRは開始前0、遅れたLFOは開始前は位相0の値を保つ。外部Releaseも同じだけ遅れて効く（残響のように後ろの複製が遅れて消える）。

## Preset Editor

- **Layout欄**：`Delay (beats)` と `Delay Order`。複製の遅れ = Delay × 順番の値。
  - Index（0,1,2…）/ Reverse / CenterOut（中央からの距離）/ Column / Row / Random（各複製が0〜Delayのランダム。Trigger＝Seedごとに変わる）。
  - Layout=Noneでも、Randomを選べばTriggerごとにランダムに遅れる。
- **表のRand列**：Baseに加える±の幅（Baseと同じ単位）。複製ごと・Triggerごとに別の値。例：Rotation Base 0 / Rand 180 で、全方向を向く。
- **表のFlip列**：Bind中かつLayoutに複製があるときだけ選べる。None / Alternate（番号の偶奇）/ Column / Row / Checker（市松）で、該当する複製のDepthを反転する。
- 複製は1つずつ別のShapeDefinitionにCompileする（Flip/Delayが複製ごとに違うため）。ShapeのPositionも各複製のTransformに入れる（結果は従来の「並び全体を動かす」と同じで、Delay/Flip/Randも複製ごとに効く）。
- Previewは、遅れたBindingだけ `Beat − Delay` の時点で信号を計算する。Loopの長さはADSRの長さ＋最大Delay（8拍まで）。
- データ: `PresetParameter.Random` / `Flip`、`PresetLayout.Delay` / `DelayOrder`。既定値は0 / None / 0 / Indexで、既存Presetの見た目は変わらない。SchemaVersionは2のまま。

## 検証（2026-11-17）

EditMode 220件成功（Variation関連19件を削除、`RandomAndDelayTests` 9件を追加）。Smoke Sceneから `ShapeSynth/Variation` をEditor経由で削除して保存。Preset EditorでのDelay/Rand/Flipの見た目と操作はユーザー確認。

## Randomの段数（Steps、2026-11-26）

- `RandomRange` に `Steps` を追加した。0か1は従来どおり範囲内の任意の値。2以上なら、MinからMaxまで（両端を含む）を等間隔に分けたSteps個の値のどれかになる。選び方は従来と同じSeedとチャンネルで決まり、Triggerごと・複製ごとに1回だけ決まる。上限は1000。
- プリセットでは `PresetParameter.RandomSteps`（既定0）。Preset EditorではRandが0以外のときだけ、その行の下（Rand列の下）にStepsの欄が出る。
- 例：PositionXをCanvasWidth基準でBase 0、Rand 0.4、Steps 5にすると、毎回−0.4／−0.2／0／+0.2／+0.4（画面幅比）のどれか、つまり幅1/5の帯の中心のどれかから出る。
- 叩くたびに順番に巡る方式（Cycle）は未実装。

