using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Voices;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class PresetDocumentTests
    {
        [Test]
        public void NewDocumentHasValidRootAndLifetimeBoundCircle()
        {
            var document = PresetDocument.CreateNew();

            Assert.DoesNotThrow(document.Validate);
            Assert.That(document.RootGroup.Kind, Is.EqualTo(PresetNodeKind.Group));
            Assert.That(document.Nodes().Count(), Is.EqualTo(2));
            Assert.That(PresetCompiler.Compile(document), Is.Not.Null);
        }

        [Test]
        public void AddDeletePreservesChildOrderAndProtectsRoot()
        {
            var document = PresetDocument.CreateNew();
            var original = document.RootGroup.Children.Single();
            var first = document.AddNode(document.RootGroup.Id, PresetNodeKind.Group);
            var second = document.AddNode(document.RootGroup.Id, PresetNodeKind.Polygon);
            var nested = document.AddNode(first.Id, PresetNodeKind.Circle);

            Assert.That(
                document.RootGroup.Children.Select(node => node.Id),
                Is.EqualTo(new[] { original.Id, first.Id, second.Id }));
            Assert.That(first.Children.Single().Id, Is.EqualTo(nested.Id));
            Assert.Throws<ArgumentException>(() => document.AddNode(original.Id, PresetNodeKind.Circle));
            Assert.Throws<ArgumentException>(() => document.DeleteNode(document.RootGroup.Id));
            Assert.That(document.DeleteNode(first.Id), Is.True);
            Assert.That(document.Nodes().Any(node => node.Id == nested.Id), Is.False);
            Assert.That(document.DeleteNode("absent"), Is.False);
            Assert.That(
                document.RootGroup.Children.Select(node => node.Id),
                Is.EqualTo(new[] { original.Id, second.Id }));
        }

        [Test]
        public void ValidationRejectsDuplicateIdsUnknownBindingsAndNonFiniteNumbers()
        {
            var duplicateNode = PresetDocument.CreateNew();
            duplicateNode.RootGroup.Children.Add(PresetNode.Create(PresetNodeKind.Circle));
            duplicateNode.RootGroup.Children[1].Id = duplicateNode.RootGroup.Children[0].Id;

            Assert.Throws<ArgumentException>(duplicateNode.Validate);

            var duplicateValue = PresetDocument.CreateNew();
            duplicateValue.Values.Add(new PresetValue { Id = duplicateValue.Values[0].Id, Name = "Duplicate" });

            Assert.Throws<ArgumentException>(duplicateValue.Validate);

            var nonFiniteWeight = PresetDocument.CreateNew();
            nonFiniteWeight.Values[0].Components[0].Weight = double.NaN;

            Assert.Throws<ArgumentOutOfRangeException>(nonFiniteWeight.Validate);

            var unknownBinding = PresetDocument.CreateNew();
            unknownBinding.RootGroup.Children[0].Opacity.Bindings[0].ValueId = "missing";

            Assert.Throws<ArgumentException>(unknownBinding.Validate);

            var nonFinite = PresetDocument.CreateNew();
            nonFinite.RootGroup.Children[0].Width.Base = double.NaN;

            Assert.Throws<ArgumentOutOfRangeException>(nonFinite.Validate);
        }

        [Test]
        public void ParameterBindsAtMostOneValue()
        {
            var document = PresetDocument.CreateNew();
            var opacity = document.RootGroup.Children[0].Opacity;
            var adsrId = document.Values.Single().Id;
            var lfo = LfoValue("lfo", 1);
            document.Values.Add(lfo);

            var binding = opacity.Bind(lfo.Id, 0.5);

            Assert.That(opacity.Bindings, Has.Count.EqualTo(1));
            Assert.That(opacity.GetBinding(), Is.SameAs(binding));
            Assert.That(binding.ValueId, Is.EqualTo(lfo.Id));
            Assert.That(binding.Depth, Is.EqualTo(0.5));
            Assert.DoesNotThrow(document.Validate);

            opacity.Bindings.Add(new PresetBinding { ValueId = adsrId, Depth = 1 });

            var exception = Assert.Throws<ArgumentException>(document.Validate);
            Assert.That(exception.Message, Does.Contain("at most one Value"));

            opacity.Unbind();

            Assert.That(opacity.GetBinding(), Is.Null);
            Assert.DoesNotThrow(document.Validate);
            Assert.Throws<ArgumentNullException>(() => opacity.Bind(null, 1));
        }

        [Test]
        public void ValueCannotBeDeletedWhileReferenced()
        {
            var document = PresetDocument.CreateNew();
            var id = document.Values.Single().Id;
            Assert.That(document.BindingCount(id), Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => document.DeleteValue(id));

            document.RootGroup.Children[0].Opacity.Unbind();
            document.DeleteValue(id);

            Assert.That(document.Values, Is.Empty);
            Assert.DoesNotThrow(document.Validate);
        }

        [Test]
        public void GroupCompilerPreservesSlotsNestedTransformsAndShapeStyle()
        {
            var document = PresetDocument.CreateNew();
            var root = document.RootGroup;
            Assert.That(document.Values, Has.Count.EqualTo(1));
            var lfo = LfoValue("osc", 0.25, LfoWaveform.Square);
            document.Values.Add(lfo);
            var subgroup = document.AddNode(root.Id, PresetNodeKind.Group);
            subgroup.PositionX.Base = 12;
            subgroup.Rotation.Base = 30;
            var rectangle = document.AddNode(subgroup.Id, PresetNodeKind.Rectangle);
            rectangle.Width.Base = 10;
            rectangle.Height.Base = 20;
            rectangle.FillHue.Base = 210;
            rectangle.FillSaturation.Base = 0.5;
            rectangle.FillBrightness.Base = 0.6;
            rectangle.StrokeHue.Base = 30;
            rectangle.StrokeAlpha = 0.25;
            rectangle.Fill = false;
            rectangle.Stroke = true;
            rectangle.StrokeWidth.Base = 3;
            rectangle.PositionY.Bind(lfo.Id, 2);

            var compiled = PresetCompiler.CompileGroup(document);
            var childGroup = (GroupDefinition)compiled.Children[1];
            var shape = (ShapeDefinition)childGroup.Children.Single();

            Assert.That(compiled.Runtime.Modulators, Has.Count.EqualTo(2));
            Assert.That(childGroup.Transform.PositionX.BaseValue, Is.EqualTo(12));
            Assert.That(childGroup.Transform.RotationDegrees.BaseValue, Is.EqualTo(30));
            Assert.That(shape.Kind, Is.EqualTo(ShapeKind.Rectangle));
            Assert.That(shape.Width.BaseValue, Is.EqualTo(10));
            Assert.That(shape.Height.BaseValue, Is.EqualTo(20));
            Assert.That(shape.Transform.PositionY.Bindings.Single().ModulatorIndex, Is.EqualTo(1));
            Assert.That(shape.Transform.PositionY.Bindings.Single().Depth, Is.EqualTo(2));
            Assert.That(shape.FillColor.Hue.BaseValue, Is.EqualTo(210));
            Assert.That(shape.FillColor.Saturation.BaseValue, Is.EqualTo(0.5));
            Assert.That(shape.FillColor.Brightness.BaseValue, Is.EqualTo(0.6));
            Assert.That(shape.StrokeColor, Is.Not.SameAs(shape.FillColor));
            Assert.That(shape.StrokeColor.Hue.BaseValue, Is.EqualTo(30));
            Assert.That(shape.StrokeColor.Alpha, Is.EqualTo(0.25));
            Assert.That(shape.Style.FillEnabled, Is.False);
            Assert.That(shape.Style.StrokeEnabled, Is.True);
            Assert.That(shape.StrokeWidth.BaseValue, Is.EqualTo(3));
        }

        [Test]
        public void CompositeValueBindsEveryComponentWithDepthTimesWeight()
        {
            var document = PresetDocument.CreateNew();
            var opacity = document.RootGroup.Children[0].Opacity;
            var mix = LfoValue("mix", 1);
            mix.Components[0].Weight = 0.5;
            mix.Components.Insert(0, PresetComponent.Create(ModulatorKind.Adsr));
            document.Values.Add(mix);
            opacity.Bind(mix.Id, 2);

            var compiled = PresetCompiler.CompileGroup(document);
            var bindings = ((ShapeDefinition)compiled.Children.Single()).Opacity.Bindings;

            // The Default ADSR is no longer bound, so only the two components of mix become modulators.
            Assert.That(compiled.Runtime.Modulators, Has.Count.EqualTo(2));
            Assert.That(bindings.Select(binding => binding.ModulatorIndex), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(bindings.Select(binding => binding.Depth), Is.EqualTo(new[] { 2.0, 1.0 }));

            mix.Components.Clear();
            var empty = (ShapeDefinition)PresetCompiler.CompileGroup(document).Children.Single();

            Assert.That(empty.Opacity.Bindings, Is.Empty);
        }

        [Test]
        public void UnboundSharedValuesAddNoModulators()
        {
            var document = PresetDocument.CreateNew();
            var unused = LfoValue("unused", 1);
            unused.Components.Add(PresetComponent.Create(ModulatorKind.Adsr));
            unused.Components[1].SustainTime = 50;
            document.Values.Insert(0, unused);

            var compiled = PresetCompiler.CompileGroup(document);

            Assert.That(document.BoundValues(), Is.EqualTo(new[] { document.Values[1] }));
            Assert.That(PresetCompiler.Components(document), Is.EqualTo(document.Values[1].Components));
            Assert.That(compiled.Runtime.Modulators, Has.Count.EqualTo(1));
            Assert.That(((ShapeDefinition)compiled.Children.Single()).Opacity.Bindings.Single().ModulatorIndex,
                Is.EqualTo(0));
        }

        [Test]
        public void NewPresetsShareTheGivenValuesAndAddADefaultAdsrOnlyWhenNoneExists()
        {
            var values = new List<PresetValue> { LfoValue("osc", 1) };

            var first = PresetDocument.CreateNew(values);
            var second = PresetDocument.CreateNew(values);

            Assert.That(first.Values, Is.SameAs(values));
            Assert.That(second.Values, Is.SameAs(values));
            Assert.That(values, Has.Count.EqualTo(2));
            Assert.That(values[1].Name, Is.EqualTo("Default ADSR"));
            Assert.That(first.RootGroup.Children[0].Opacity.GetBinding().ValueId, Is.EqualTo(values[1].Id));
            Assert.That(second.RootGroup.Children[0].Opacity.GetBinding().ValueId, Is.EqualTo(values[1].Id));
        }

        [Test]
        public void DraftWithoutLifetimeValidatesAndCompilesGroupButCannotCompileTrigger()
        {
            var document = PresetDocument.CreateNew();
            document.Values.Clear();
            document.RootGroup.Children[0].Opacity.Bindings.Clear();

            Assert.DoesNotThrow(document.Validate);
            Assert.DoesNotThrow(() => PresetCompiler.CompileGroup(document));
            Assert.Throws<ArgumentException>(() => PresetCompiler.Compile(document));
        }

        [Test]
        public void CompiledTargetsAreSnapshotsAndEachVoiceHasIndependentModulatorState()
        {
            var document = PresetDocument.CreateNew();
            var envelope = document.Values.Single().Components.Single();
            envelope.SustainTime = 0.1;
            envelope.Release = 0.1;
            var target = PresetCompiler.Compile(document);
            var first = new VoiceInstance(target, new VoiceTime(0, 60));
            var second = new VoiceInstance(target, new VoiceTime(0, 60));

            Assert.That(first.Modulators[0], Is.Not.SameAs(second.Modulators[0]));
            first.Release(0.1);

            Assert.That(((ILifetime)first.Modulators[0]).IsFinished(0.21), Is.True);
            Assert.That(((ILifetime)second.Modulators[0]).IsFinished(0.21), Is.False);


            envelope.Attack = 5;
            var laterTarget = PresetCompiler.Compile(document);
            var laterVoice = new VoiceInstance(laterTarget, new VoiceTime(1, 60));

            Assert.That(((AdsrInstance)first.Modulators[0]).Definition.Attack, Is.EqualTo(0.1));
            Assert.That(((AdsrInstance)laterVoice.Modulators[0]).Definition.Attack, Is.EqualTo(5));
            Assert.That(first.IsFinished(0.21), Is.True);
            Assert.That(second.IsFinished(0.21), Is.False);
        }

        [Test]
        public void CompileWithoutAutoReleaseHoldsSustainUntilReleased()
        {
            var document = PresetDocument.CreateNew();
            var envelope = document.Values.Single().Components.Single();
            envelope.Attack = 0.1;
            envelope.Decay = 0.1;
            envelope.SustainTime = 0.1;
            envelope.Release = 0.1;
            var released = new VoiceInstance(PresetCompiler.Compile(document), new VoiceTime(0, 60));
            var held = new VoiceInstance(PresetCompiler.Compile(document, false), new VoiceTime(0, 60));
            var heldEnvelope = (AdsrInstance)held.Modulators[0];

            Assert.That(((ILifetime)released.Modulators[0]).IsFinished(1), Is.True);
            Assert.That(((ILifetime)heldEnvelope).IsFinished(100), Is.False);
            Assert.That(heldEnvelope.Evaluate(100), Is.EqualTo(envelope.SustainLevel).Within(1e-9));

            held.Release(100);
            Assert.That(((ILifetime)heldEnvelope).IsFinished(100.05), Is.False);
            Assert.That(((ILifetime)heldEnvelope).IsFinished(100.2), Is.True);
        }

        [Test]
        public void CollectionCanStopOnlyOneOwnedVoice()
        {
            var target = PresetCompiler.Compile(PresetDocument.CreateNew());
            var otherTarget = PresetCompiler.Compile(PresetDocument.CreateNew());
            var voices = new VoiceCollection();
            var stopped = voices.Spawn(target, 0, 60);
            var retained = voices.Spawn(otherTarget, 0, 60);

            Assert.That(voices.Stop(stopped), Is.True);
            Assert.That(voices.Stop(stopped), Is.False);
            Assert.That(voices.Stop(null), Is.False);
            Assert.That(voices.Voices, Is.EqualTo(new[] { retained }));
            Assert.That(stopped.IsFinished(0), Is.True);
            Assert.That(voices.Evaluate(0, new ReferenceContext(100, 80)), Has.Count.EqualTo(1));
        }

        [Test]
        public void RestartReplacesPreviewWithoutOverlapAndLeavesOtherTargetRunning()
        {
            var previewTarget = PresetCompiler.Compile(PresetDocument.CreateNew());
            var performanceTarget = PresetCompiler.Compile(PresetDocument.CreateNew());
            var voices = new VoiceCollection();
            var oldPreview = voices.Spawn(previewTarget, 0, 60);
            var performance = voices.Spawn(performanceTarget, 0, 60);

            var replacement = voices.Restart(previewTarget, 0.01, 60);

            Assert.That(oldPreview.IsFinished(0.01), Is.True);
            Assert.That(voices.Voices, Is.EqualTo(new[] { performance, replacement }));
            Assert.That(voices.Evaluate(0.01, new ReferenceContext(100, 80)), Has.Count.EqualTo(2));
        }

        [Test]
        public void NestedTransformEvaluatesAndCompiledVoiceEndsNaturally()
        {
            var document = PresetDocument.CreateNew();
            var envelope = document.Values.Single().Components.Single();
            envelope.Attack = 0;
            envelope.Decay = 0;
            envelope.SustainTime = 0.1;
            envelope.Release = 0.1;
            var root = document.RootGroup;
            root.PositionX.Base = 5;
            var nested = document.AddNode(root.Id, PresetNodeKind.Group);
            nested.PositionX.Base = 10;
            var rectangle = document.AddNode(nested.Id, PresetNodeKind.Rectangle);
            rectangle.PositionY.Base = 3;
            rectangle.Width.Base = 8;
            rectangle.Height.Base = 6;
            rectangle.Width.Reference = ReferenceSource.Absolute;
            rectangle.Height.Reference = ReferenceSource.Absolute;

            var voice = new VoiceInstance(PresetCompiler.Compile(document), new VoiceTime(0, 60));
            var items = voice.Evaluate(0.05, new ReferenceContext(100, 80));
            var nestedItem = items.Single(item => item.Kind == ShapeKind.Rectangle);

            Assert.That(nestedItem.WorldTransform.M02, Is.EqualTo(15).Within(1e-9));
            Assert.That(nestedItem.WorldTransform.M12, Is.EqualTo(3).Within(1e-9));
            Assert.That(nestedItem.Width, Is.EqualTo(8));
            Assert.That(nestedItem.Height, Is.EqualTo(6));
            Assert.That(voice.IsFinished(0.19), Is.False);
            Assert.That(voice.IsFinished(0.2), Is.True);
            Assert.That(voice.Evaluate(0.2, new ReferenceContext(100, 80)), Is.Empty);
        }

        [Test]
        public void MoveNodeReordersSiblingsUsingPreRemovalInsertionIndices()
        {
            var document = PresetDocument.CreateNew();
            var root = document.RootGroup;
            var original = root.Children[0];
            var first = document.AddNode(root.Id, PresetNodeKind.Circle);
            var second = document.AddNode(root.Id, PresetNodeKind.Rectangle);
            var third = document.AddNode(root.Id, PresetNodeKind.Polygon);

            document.MoveNode(first.Id, root.Id, 3);
            Assert.That(root.Children.Select(node => node.Id),
                Is.EqualTo(new[] { original.Id, second.Id, first.Id, third.Id }));

            document.MoveNode(third.Id, root.Id, 1);
            Assert.That(root.Children.Select(node => node.Id),
                Is.EqualTo(new[] { original.Id, third.Id, second.Id, first.Id }));

            document.MoveNode(original.Id, root.Id, root.Children.Count);
            Assert.That(root.Children.Select(node => node.Id),
                Is.EqualTo(new[] { third.Id, second.Id, first.Id, original.Id }));
            Assert.DoesNotThrow(document.Validate);
        }

        [Test]
        public void MoveNodeReparentsSubtreeAndKeepsIdsAndOrderCompilable()
        {
            var document = PresetDocument.CreateNew();
            var root = document.RootGroup;
            var source = document.AddNode(root.Id, PresetNodeKind.Group);
            var destination = document.AddNode(root.Id, PresetNodeKind.Group);
            var destinationChild = document.AddNode(destination.Id, PresetNodeKind.Rectangle);
            var movingGroup = document.AddNode(source.Id, PresetNodeKind.Group);
            var movingChild = document.AddNode(movingGroup.Id, PresetNodeKind.Circle);
            var retainedId = movingGroup.Id;
            var retainedChildId = movingChild.Id;

            document.MoveNode(movingGroup.Id, destination.Id, 0);

            Assert.That(source.Children, Is.Empty);
            Assert.That(destination.Children.Select(node => node.Id),
                Is.EqualTo(new[] { retainedId, destinationChild.Id }));
            Assert.That(movingGroup.Children.Single().Id, Is.EqualTo(retainedChildId));
            Assert.That(document.Nodes().Select(node => node.Id), Does.Contain(retainedId));
            Assert.That(document.Nodes().Select(node => node.Id), Does.Contain(retainedChildId));
            Assert.DoesNotThrow(() => PresetCompiler.CompileGroup(document));
        }

        [Test]
        public void MoveNodeRejectsInvalidDestinationsAndIndicesWithoutChangingTree()
        {
            var document = PresetDocument.CreateNew();
            var root = document.RootGroup;
            var moving = document.AddNode(root.Id, PresetNodeKind.Group);
            var descendant = document.AddNode(moving.Id, PresetNodeKind.Group);
            document.AddNode(descendant.Id, PresetNodeKind.Circle);
            var leaf = root.Children[0];

            AssertMoveRejectedWithoutMutation(document, () => document.MoveNode(root.Id, root.Id, 0));
            AssertMoveRejectedWithoutMutation(document, () => document.MoveNode(moving.Id, moving.Id, 0));
            AssertMoveRejectedWithoutMutation(document, () => document.MoveNode(moving.Id, descendant.Id, 0));
            AssertMoveRejectedWithoutMutation(document, () => document.MoveNode(moving.Id, leaf.Id, 0));
            AssertMoveRejectedWithoutMutation(document, () => document.MoveNode(moving.Id, root.Id, -1));
            AssertMoveRejectedWithoutMutation(
                document, () => document.MoveNode(moving.Id, root.Id, root.Children.Count + 1));
        }

        [Test]
        public void MoveNodeRejectsSubtreeThatWouldExceedMaximumDepthWithoutChangingDocument()
        {
            var document = PresetDocument.CreateNew();
            var deepestGroup = document.RootGroup;
            for (var depth = 1; depth <= 64; depth++)
            {
                deepestGroup = document.AddNode(deepestGroup.Id, PresetNodeKind.Group);
            }

            var movingSubtree = document.AddNode(document.RootGroup.Id, PresetNodeKind.Group);
            document.AddNode(movingSubtree.Id, PresetNodeKind.Group);
            Assert.DoesNotThrow(document.Validate);

            AssertMoveRejectedWithoutMutation(
                document, () => document.MoveNode(movingSubtree.Id, deepestGroup.Id, 0));
        }

        [Test]
        public void CanMoveNodeMatchesMoveNodeWithoutChangingTree()
        {
            var document = PresetDocument.CreateNew();
            var root = document.RootGroup;
            var moving = document.AddNode(root.Id, PresetNodeKind.Group);
            var descendant = document.AddNode(moving.Id, PresetNodeKind.Group);
            var leaf = root.Children[0];
            var deepestGroup = root;
            var belowDeepest = root;
            for (var depth = 1; depth <= PresetDocument.MaxDepth; depth++)
            {
                belowDeepest = deepestGroup;
                deepestGroup = document.AddNode(deepestGroup.Id, PresetNodeKind.Group);
            }

            var before = DescribeTree(document);
            var cases = new (string Node, string Parent, int Index)[]
            {
                (root.Id, root.Id, 0),
                (moving.Id, moving.Id, 0),
                (moving.Id, descendant.Id, 0),
                (moving.Id, leaf.Id, 0),
                (moving.Id, root.Id, -1),
                (moving.Id, root.Id, root.Children.Count + 1),
                (moving.Id, belowDeepest.Id, 0),
                (descendant.Id, deepestGroup.Id, 0),
                ("missing", root.Id, 0),
                (moving.Id, "missing", 0)
            };
            foreach (var (node, parent, index) in cases)
            {
                Assert.That(document.CanMoveNode(node, parent, index), Is.False, $"{node} -> {parent}[{index}]");
            }

            Assert.That(DescribeTree(document), Is.EqualTo(before));
            Assert.That(document.CanMoveNode(leaf.Id, moving.Id, 0), Is.True);
            Assert.That(document.CanMoveNode(leaf.Id, root.Id, root.Children.Count), Is.True);
            Assert.That(document.CanMoveNode(descendant.Id, belowDeepest.Id, 0), Is.True);
            Assert.That(DescribeTree(document), Is.EqualTo(before));

            document.MoveNode(descendant.Id, belowDeepest.Id, 0);
            Assert.That(belowDeepest.Children[0], Is.SameAs(descendant));
        }

        private static void AssertMoveRejectedWithoutMutation(PresetDocument document, TestDelegate move)
        {
            var before = DescribeTree(document);

            Assert.Catch<ArgumentException>(move);

            Assert.That(DescribeTree(document), Is.EqualTo(before));
            Assert.DoesNotThrow(document.Validate);
        }

        private static string[] DescribeTree(PresetDocument document)
        {
            return document.Nodes()
                .Select(node => node.Id + ":" + string.Join(",", node.Children.Select(child => child.Id)))
                .ToArray();
        }

        private static PresetValue LfoValue(string id, double cyclesPerBeat, LfoWaveform waveform = LfoWaveform.Sine)
        {
            var component = PresetComponent.Create(ModulatorKind.Lfo);
            component.CyclesPerBeat = cyclesPerBeat;
            component.Waveform = waveform;
            return new PresetValue { Id = id, Name = id, Components = new List<PresetComponent> { component } };
        }
    
        [Test]
        public void DuplicatedValueHasItsOwnIdNameAndComponents()
        {
            var source = new PresetValue
            {
                Id = "source",
                Name = "fx/Swell",
                Components = new List<PresetComponent> { PresetComponent.Create(ModulatorKind.Adsr) }
            };

            var copy = source.Duplicate("copy", "fx/Swell Copy");
            copy.Components[0].Attack = 3;
            copy.Components.Add(PresetComponent.Create(ModulatorKind.Lfo));

            Assert.That((copy.Id, copy.Name), Is.EqualTo(("copy", "fx/Swell Copy")));
            Assert.That(source.Components, Has.Count.EqualTo(1));
            Assert.That(source.Components[0].Attack, Is.EqualTo(PresetComponent.Create(ModulatorKind.Adsr).Attack));
            Assert.Throws<ArgumentException>(() => source.Duplicate("bad id", "x"));
        }
    
        [Test]
        public void LocalCopyChangesOnlyThisPresetAndCompilesLikeASharedValue()
        {
            var shared = new List<PresetValue>();
            var document = PresetDocument.CreateNew(shared);
            var circle = document.RootGroup.Children[0];
            var source = shared[0];

            var local = document.AddLocalCopy(source, "Fade");
            circle.Opacity.Bind(local.Id, 1);
            local.Components[0].Attack = 2;

            Assert.That(document.IsLocal(local.Id), Is.True);
            Assert.That(document.IsLocal(source.Id), Is.False);
            Assert.That(source.Components[0].Attack, Is.Not.EqualTo(2));
            Assert.That(document.BoundValues(), Is.EqualTo(new[] { local }));
            Assert.That(PresetCompiler.Components(document).Single().Attack, Is.EqualTo(2));
            Assert.DoesNotThrow(() => PresetCompiler.Compile(document));

            Assert.Throws<ArgumentException>(() => document.DeleteValue(local.Id));
            circle.Opacity.Unbind();
            document.DeleteValue(local.Id);
            Assert.That(document.LocalValues, Is.Empty);
            Assert.That(shared, Has.Count.EqualTo(1));
        }

        [Test]
        public void LocalValueIdsMustNotRepeatASharedId()
        {
            var document = PresetDocument.CreateNew();
            var local = document.AddLocalCopy(document.Values[0], "Copy");
            local.Id = document.Values[0].Id;

            Assert.Throws<ArgumentException>(() => document.Validate());

            document.LocalValues = null;
            Assert.Throws<ArgumentException>(() => document.Validate());
        }
    }
}
