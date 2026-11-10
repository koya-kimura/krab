# Phase 6：Index / Random Variation

Variationは個体の係数を決める設定であり、時間信号ではない。共有Definitionを変更せず、HierarchyEvaluator構築時（VoiceではTrigger時）にBase/Depthを解決し、通常のParameterEvaluatorに渡す。評価式 `Reference × (Base + Σ Depth × Signal)` は変更しない。ADSR/LFO/Timerの設定・寿命・scopeには作用しない。

## API

```csharp
var baseValue = ParameterDefinition.Constant(0)
    .WithBaseVariation(VariationDefinition.Alternating(-1, 1));
var binding = new BindingDefinition(0, 0,
    VariationDefinition.Random(-1, 1, salt: 2));
var context = new VariationContext(voiceSeed: 1234).Child(2).Child(1);
var resolved = VariationResolver.Resolve(parameter, context, parameterKey: 1);
var voice = collection.Spawn(target, nowSeconds, bpm, voiceSeed: 1234);
collection.Restart(target, nowSeconds, bpm, voiceSeed: 1234);
```

- `VariationContext(int voiceSeed = 0, params int[] path)`：不変の配置パスとSeed。RootのPathは空、LocalIndexは0。それ以外はパスの最後のIndex。Childで新しいContextを作る。負Index/nullのpathを拒否し、呼出側の配列を保持しない。
- `VariationDefinition.Alternating(even, odd)`：所有NodeのLocalIndexで選択。
- `VariationDefinition.Random(min, max, salt = 0)`：有限範囲、min<=max。min=maxは定数。`Resolve(context, channel)` は同じ入力なら同じ値。
- `ParameterDefinition.WithBaseVariation`：元のDefinitionを変更せず設定を付けた新しいDefinitionを返す。
- `BindingDefinition(index, depth, depthVariation = null)`：参照slotは変更せずDepthだけ解決。
- `VariationResolver.Resolve`：Variationは元の値への加算・乗算ではなく**置き換え**。未設定なら元の値。解決済みDefinitionにVariation設定は残さない。共有元の設定は変更しない。
- `ParameterEvaluator(definition, modulators, context, parameterKey)`：単独Parameterの構築時解決。従来の2引数版へ未解決Variationを渡すと例外となり、黙って無視しない。
- `HierarchyEvaluator(definition, modulators, voiceSeed)`：従来の外部signal配列でも利用可能。省略Seedは0。
- VoiceInstance / VoiceCollection.Spawn / Restart / ShapeSynthController.Spawn / Restartに省略可能な `int voiceSeed = 0` を追加。VoiceInstance.VoiceSeedで取得できる。暗黙の乱数やSeedの自動採番はしない。

## 配置とscope

Indexは親Groupの子配列内の順番で、Shape/Groupを区別しない。ネストで数え直し、同じDefinitionを再利用しても各出現のパスを使う。GroupのTransformはそのGroup自身のLocalIndex、ShapeのGeometry/Transform/OpacityとBinding DepthはそのShape自身のLocalIndexを使う。

信号は既存の最寄りscopeのModulatorIndexで解決する。係数が違ってもModulatorは複製しない。Voice間・明示scopeの別出現間の状態独立性はPhase 5のまま。Runtimeに解決したParameterだけが出現ごとの個体差を保持する。

## Randomの決定性と範囲

UnityEngine.Randomや順番に取り出すRandomは使用しない。固定64bit整数のMix64（明示的unchecked）へ、VoiceSeed、パス長と各Index、channel、salt、範囲両端のdoubleビット列を混合する。上位53bitを[0,1)の値にし、両端の重み付き和で範囲へ写す。max-minを作らないため、±double.MaxValueの範囲にも対応する。丸めで上端と等しくなる場合は許容する。結果は範囲内に制限し、min=maxはその値そのもの。

HierarchyのparameterKeyは固定：1=X、2=Y、3=RotationDegrees、4=ScaleX、5=ScaleY、6=Width/Diameter、7=Rectangle Height、8=Opacity。CircleのHeightはDiameterと同じ6。内部channelは `(parameterKey << 32) | component`、component0=Base、1+i=Binding iのDepth。単独ResolverのparameterKeyはuint範囲とする。

既存位置より後へ子を追加しても既存パスの値は変化しない。他のParameterや別の配置を評価しても影響しない。子の挿入/削除/並べ替えやBinding順の変更でパス/channelが変わればRandom結果も変わる。異なるSeedは一般に異なる値だが、定数範囲・有限精度やhash衝突で必ず異なる保証はしない。これは暗号乱数ではない。

## Scene

`Assets/Krab/ShapeSynth/Samples/ShapeSynthSmokeTest.unity` の `ShapeSynth/Variation` にTargetCameraCanvas、ShapeSynthController、VariationSmokeTestを配置する。Main Camera/既存RT/Preview/Syphonを共用する。Play開始時にSeed1234、128拍TimerのVoiceを1つ生成。

左端の列（上から）：

1. 青Circleの左右交互配置。手動配置の2行は同じpair/Shape Definitionを再利用し、各行のLocalIndexは0/1。
2. ピンクRectangle：同じLFOのBinding Depthを+60/-60にして逆回転。
3. オレンジCircle：同じDefinitionを6回配置し、Position X/YとDiameterをRandomで決定。
4. 緑のネストGroup：親GroupのIndex0/1で位置・回転を変え、子ShapeのIndexを各Group内0/1へリセットして位置・サイズを変える。

N=現在SeedでSpawn、M=Seedを1増やしてSpawn、J=現在SeedでRestart、K=Seedを1増やしてRestart。同SeedのRandom配置は一致し、違うSeedでは配置が変わる。Indexの値はSeedに依存しない。動くデモのLFO位相は各Voiceの開始時刻が基準なので、異なるTrigger時刻では同じSeedでも動きの位相は異なる。

InspectorのSeedは次のTriggerから適用する。生成済みVoiceの係数は変わらない。左上のOnGUIは操作補助で、RT/Syphonには描かない。図形のためのGameObjectや新しいRenderer/OutputManagerは追加しない。

## 後続へ残すこと

自動Layout、位置由来のVariation、ADSR/LFO DefinitionへのVariation、保存UI、任意合成グラフは未実装。Runtimeパスは永続IDではない。編集時に並べ替え前のRandom結果をどこまで維持するか、配置ID/Bindingの識別・参照リマップをどう保存するかは後続で決める。今回の固定channel/hashを変更すると再現される値も変わるため、保存形式導入時にバージョニングを検討する。

## 検証結果（2026-11-11）

Unityコンパイル成功。既存115件を含むEditMode134件すべて成功。実EditorのPlayで同Seed一致、異Seedで6図形のRandom結果変化、生存中の固定、3→1 VoiceのRestartを確認。16 DrawItemを既存RTへ送信し、Preview/Syphon参照一致を確認した。SceneはEditor経由で保存済み。最終描画の目視・実キー入力・外部Syphon受信はユーザー確認対象。実行記録は `Reports/Phase6Validation-2026-11-11.md`。
