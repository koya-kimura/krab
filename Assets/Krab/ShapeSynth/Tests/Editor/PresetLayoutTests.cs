using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class PresetLayoutTests
    {
        [Test]
        public void NoneDrawsOnceAtOwnPositionIgnoringCount()
        {
            var document = DocumentWithShape(PresetLayoutKind.None, 5, out var shape);
            shape.PositionX.Base = 7;

            Assert.That(Positions(document), Is.EqualTo(new[] { (7.0, 0.0) }));
        }

        [Test]
        public void RowAndColumnDrawCountCopiesCenteredWithColumnTopFirst()
        {
            var row = DocumentWithShape(PresetLayoutKind.Row, 3, out _);

            Assert.That(Positions(row), Is.EqualTo(new[] { (-10.0, 0.0), (0.0, 0.0), (10.0, 0.0) }));

            var column = DocumentWithShape(PresetLayoutKind.Column, 2, out _);

            Assert.That(Positions(column), Is.EqualTo(new[] { (0.0, 5.0), (0.0, -5.0) }));
        }

        [Test]
        public void GridFillsRowsLeftToRightAndCentersPartialBlock()
        {
            var document = DocumentWithShape(PresetLayoutKind.Grid, 5, out var shape);
            shape.Layout.Columns = 3;
            shape.Layout.SpacingY.AbsoluteValue = 20;

            Assert.That(Positions(document), Is.EqualTo(new[]
            {
                (-10.0, 10.0), (0.0, 10.0), (10.0, 10.0),
                (-10.0, -10.0), (0.0, -10.0)
            }));

            var single = DocumentWithShape(PresetLayoutKind.Grid, 2, out var singleShape);
            singleShape.Layout.Columns = 4;

            Assert.That(Positions(single), Is.EqualTo(new[] { (-5.0, 0.0), (5.0, 0.0) }));
        }

        [Test]
        public void PositionMovesArrangementAndRotationAppliesToEachCopy()
        {
            var document = DocumentWithShape(PresetLayoutKind.Row, 2, out var shape);
            shape.PositionX.Base = 100;
            shape.PositionY.Base = 3;
            shape.Rotation.Base = 90;
            var items = Evaluate(document);

            Assert.That(Positions(document), Is.EqualTo(new[] { (95.0, 3.0), (105.0, 3.0) }));
            Assert.That(items.Select(item => Round(item.WorldTransform.M10)), Is.EqualTo(new[] { 1.0, 1.0 }));
        }

        [Test]
        public void ShapesInOneGroupKeepTheirOwnLayouts()
        {
            var document = DocumentWithShape(PresetLayoutKind.Row, 2, out _);
            var ring = document.AddNode(document.RootGroup.Id, PresetNodeKind.Rectangle);
            ring.Layout.Kind = PresetLayoutKind.Circle;
            ring.Layout.Count = 4;
            ring.Layout.Radius = Absolute(10);

            Assert.That(Positions(document), Is.EqualTo(new[]
            {
                (-5.0, 0.0), (5.0, 0.0),
                (0.0, 10.0), (10.0, 0.0), (0.0, -10.0), (-10.0, 0.0)
            }));
        }

        [Test]
        public void BoundSpacingScalesWithSlotAndSignal()
        {
            var document = DocumentWithShape(PresetLayoutKind.Row, 3, out var shape);
            shape.Layout.SpacingX.Base = 1;
            shape.Layout.SpacingX.Bind(document.Values[0].Id, 2);

            // AbsoluteValue 10 × (1 + 2 × 0.5) = 20 per slot.
            Assert.That(Positions(document, 0.5), Is.EqualTo(new[] { (-20.0, 0.0), (0.0, 0.0), (20.0, 0.0) }));
        }

        [Test]
        public void CircleStartsAtTopAndRunsClockwiseUsingRadius()
        {
            var document = DocumentWithShape(PresetLayoutKind.Circle, 4, out var shape);
            shape.Layout.Radius = Absolute(10);

            Assert.That(Positions(document), Is.EqualTo(new[]
            {
                (0.0, 10.0), (10.0, 0.0), (0.0, -10.0), (-10.0, 0.0)
            }));

            shape.Layout.StartAngle = 0;

            Assert.That(Positions(document)[0], Is.EqualTo((10.0, 0.0)));
        }

        [Test]
        public void CircleFaceOutwardPointsLocalUpOutward()
        {
            var document = DocumentWithShape(PresetLayoutKind.Circle, 4, out var shape);
            shape.Layout.Radius = Absolute(10);
            shape.Layout.FaceOutward = true;

            // Second copy is at +X (angle 0°), so its local +Y maps to world +X.
            var transform = Evaluate(document)[1].WorldTransform;
            Assert.That(transform.M01, Is.EqualTo(1).Within(1e-9));
            Assert.That(transform.M11, Is.EqualTo(0).Within(1e-9));

            shape.Layout.FaceOutward = false;

            Assert.That(Evaluate(document)[1].WorldTransform.M11, Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void ValidationRejectsInvalidLayout()
        {
            Assert.Throws<ArgumentOutOfRangeException>(Invalid(shape => shape.Layout.Count = 0).Validate);
            Assert.Throws<ArgumentOutOfRangeException>(
                Invalid(shape => shape.Layout.Count = PresetLayout.MaxCount + 1).Validate);
            Assert.Throws<ArgumentOutOfRangeException>(Invalid(shape => shape.Layout.Columns = 0).Validate);
            Assert.Throws<ArgumentOutOfRangeException>(
                Invalid(shape => shape.Layout.StartAngle = double.NaN).Validate);
            Assert.Throws<ArgumentException>(Invalid(shape => shape.Layout = null).Validate);
            Assert.Throws<ArgumentException>(Invalid(shape => shape.Layout.Kind = (PresetLayoutKind)99).Validate);

            var group = DocumentWithShape(PresetLayoutKind.None, 1, out _);
            group.RootGroup.Layout.Kind = PresetLayoutKind.Row;

            Assert.Throws<ArgumentException>(group.Validate);

            var tooMany = DocumentWithShape(PresetLayoutKind.Row, PresetLayout.MaxCount, out _);
            var copiesPerShape = PresetLayout.MaxTotalCopies / PresetLayout.MaxCount;
            for (var i = 0; i < copiesPerShape; i++)
            {
                var extra = tooMany.AddNode(tooMany.RootGroup.Id, PresetNodeKind.Circle);
                extra.Layout.Kind = PresetLayoutKind.Row;
                extra.Layout.Count = PresetLayout.MaxCount;
            }

            Assert.Throws<ArgumentException>(tooMany.Validate);
        }

        [Test]
        public void SlotRejectsIndexOutsideCount()
        {
            var layout = new PresetLayout { Kind = PresetLayoutKind.Row };

            Assert.Throws<ArgumentOutOfRangeException>(() => layout.Slot(2, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.Slot(0, 0));
        }

        private static PresetDocument Invalid(Action<PresetNode> change)
        {
            var document = DocumentWithShape(PresetLayoutKind.Row, 2, out var shape);
            change(shape);
            return document;
        }

        private static PresetDocument DocumentWithShape(PresetLayoutKind kind, int count, out PresetNode shape)
        {
            var document = PresetDocument.CreateNew();
            var root = document.RootGroup;
            root.Children.Clear();
            shape = document.AddNode(root.Id, PresetNodeKind.Circle);
            shape.Layout.Kind = kind;
            shape.Layout.Count = count;
            shape.Layout.SpacingX = Absolute(10);
            shape.Layout.SpacingY = Absolute(10);
            return document;
        }

        private static PresetParameter Absolute(double value)
        {
            return new PresetParameter { Reference = ReferenceSource.Absolute, AbsoluteValue = value, Base = 1 };
        }

        private static (double X, double Y)[] Positions(PresetDocument document, double signal = 0)
        {
            return Evaluate(document, signal)
                .Select(item => (Round(item.WorldTransform.M02), Round(item.WorldTransform.M12)))
                .ToArray();
        }

        private static IReadOnlyList<DrawItem> Evaluate(PresetDocument document, double signal = 0)
        {
            var modulators = PresetCompiler.Components(document)
                .Select(_ => (IModulator)new ConstantSignal(signal))
                .ToArray();
            var evaluator = new HierarchyEvaluator(PresetCompiler.CompileGroup(document), modulators);
            return evaluator.Evaluate(0, new ReferenceContext(100, 100));
        }

        private static double Round(double value) => Math.Round(value, 9) + 0.0;

        private sealed class ConstantSignal : IModulator
        {
            private readonly double _value;

            public ConstantSignal(double value)
            {
                _value = value;
            }

            public double Evaluate(double nowSeconds) => _value;
        }
    }
}
