using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Presets;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class StepsAndAnchorTests
    {
        private static readonly ReferenceContext Canvas = new(1000, 500);

        [Test]
        public void StepsPickOnlyEvenlySpacedValuesIncludingBothEnds()
        {
            var range = RandomRange.Spread(0.4, steps: 5);
            var picked = Enumerable.Range(0, 400)
                .Select(seed => range.Resolve(new RandomContext(seed)))
                .Select(value => Math.Round(value, 9))
                .Distinct()
                .OrderBy(value => value)
                .ToArray();

            Assert.That(picked, Is.EqualTo(new[] { -0.4, -0.2, 0, 0.2, 0.4 }));
        }

        [Test]
        public void FewerThanTwoStepsKeepsAnyValueAndInvalidStepsAreRejected()
        {
            var continuous = Enumerable.Range(0, 50)
                .Select(seed => RandomRange.Spread(1, steps: 1).Resolve(new RandomContext(seed)))
                .Distinct()
                .Count();

            Assert.That(continuous, Is.EqualTo(50));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RandomRange(0, 1, steps: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RandomRange(0, 1, steps: RandomRange.MaxSteps + 1));
        }

        [Test]
        public void PresetStepsPlaceEachTriggerInOneOfFiveColumns()
        {
            var document = PresetDocument.CreateNew();
            var bar = document.RootGroup.Children[0];
            bar.PositionX.Reference = ReferenceSource.CanvasWidth;
            bar.PositionX.Random = 0.4;
            bar.PositionX.RandomSteps = 5;
            var group = PresetCompiler.CompileGroup(document);

            var columns = Enumerable.Range(0, 300)
                .Select(seed => new HierarchyEvaluator(group, Modulators(document), seed).Evaluate(0, Canvas)[0])
                .Select(item => Math.Round(item.WorldTransform.TransformPoint(new Point2(0, 0)).X, 6))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            Assert.That(columns, Is.EqualTo(new[] { -400.0, -200, 0, 200, 400 }));
        }

        [Test]
        public void BottomAnchorKeepsTheBottomEdgeWhileScaleYGrows()
        {
            var shape = ShapeDefinition.Rectangle(ParameterDefinition.Constant(20), ParameterDefinition.Constant(100),
                new TransformDefinition(scaleY: ParameterDefinition.Constant(2))).WithAnchor(0, -0.5);

            var item = new HierarchyEvaluator(new GroupDefinition(null, shape), Array.Empty<IModulator>())
                .Evaluate(0, Canvas)[0];

            // The rectangle is drawn around its local center; with the anchor the bottom edge stays at the origin.
            Assert.That(item.WorldTransform.TransformPoint(new Point2(0, -50)).Y, Is.EqualTo(0).Within(1e-9));
            Assert.That(item.WorldTransform.TransformPoint(new Point2(0, 50)).Y, Is.EqualTo(200).Within(1e-9));
            Assert.That(shape.Anchor, Is.EqualTo(new Point2(0, -0.5)));
            Assert.Throws<ArgumentOutOfRangeException>(() => shape.WithAnchor(0, 0.6));
        }

        [Test]
        public void PolygonAnchorUsesTheVertexBounds()
        {
            var triangle = ShapeDefinition.Polygon(new[] { new Point2(0, 10), new Point2(-10, -5), new Point2(10, -5) })
                .WithAnchor(0.5, 0.5);

            var offset = triangle.AnchorOffset(0, 0);

            Assert.That((offset.X, offset.Y), Is.EqualTo((-10.0, -10.0)));
        }

        [Test]
        public void PresetAnchorIsCompiledForShapesAndRejectedOnGroups()
        {
            var document = PresetDocument.CreateNew();
            document.RootGroup.Children[0].Anchor = PresetAnchor.TopRight;

            Assert.DoesNotThrow(() => PresetCompiler.CompileGroup(document));
            Assert.That(PresetCompiler.AnchorPoint(PresetAnchor.TopRight), Is.EqualTo((0.5, 0.5)));

            document.RootGroup.Anchor = PresetAnchor.Bottom;
            Assert.Throws<ArgumentException>(() => document.Validate());
            document.RootGroup.Anchor = PresetAnchor.Center;
            document.RootGroup.Children[0].Opacity.RandomSteps = -1;
            Assert.Throws<ArgumentException>(() => document.Validate());
        }

        private static IReadOnlyList<IModulator> Modulators(PresetDocument document) =>
            PresetCompiler.Components(document)
                .Select(component => (IModulator)PresetCompiler.ModulatorDefinition(component)
                    .Create(new Core.Runtime.VoiceTime(0, 60)))
                .ToArray();
    }
}
