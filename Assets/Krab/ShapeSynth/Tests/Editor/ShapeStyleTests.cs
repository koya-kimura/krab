using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class ShapeStyleTests
    {
        [Test]
        public void DefaultStylePreservesFillOnlyBehavior()
        {
            var shape = ShapeDefinition.Circle(ParameterDefinition.Constant(10));

            Assert.That(shape.Style.FillEnabled, Is.True);
            Assert.That(shape.Style.StrokeEnabled, Is.False);
            Assert.That(shape.Style.StrokeWidth, Is.EqualTo(1));
            Assert.That(shape.StrokeColor, Is.SameAs(shape.FillColor));
        }

        [Test]
        public void StyleIsRetainedOnDefinitionAndEvaluatedDrawItem()
        {
            var strokeColor = new RgbaColor(0.2, 0.3, 0.4, 0.5);
            var style = new ShapeStyle(fill: false, stroke: true, strokeWidth: 2.5);
            var shape = ShapeDefinition.Rectangle(ParameterDefinition.Constant(4), ParameterDefinition.Constant(3),
                style: style, strokeColor: ColorDefinition.Constant(strokeColor));
            var item = new HierarchyEvaluator(new GroupDefinition(null, shape), Array.Empty<IModulator>())
                .Evaluate(0, new ReferenceContext(100, 100))[0];

            Assert.That(shape.Style, Is.SameAs(style));
            Assert.That(item.Style, Is.SameAs(style));
            Assert.That(item.Style.FillEnabled, Is.False);
            Assert.That(item.Style.StrokeEnabled, Is.True);
            Assert.That(item.Style.StrokeWidth, Is.EqualTo(2.5));
            Assert.That(item.StrokeColor, Is.EqualTo(strokeColor));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(-0.01)]
        public void StrokeWidthMustBeFiniteAndNonnegative(double width)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShapeStyle(strokeWidth: width));
        }

        [Test]
        public void ZeroStrokeWidthIsValidAndCanDisableBothPaints()
        {
            var style = new ShapeStyle(fill: false, stroke: true, strokeWidth: 0);

            Assert.That(style.FillEnabled, Is.False);
            Assert.That(style.StrokeEnabled, Is.True);
            Assert.That(style.StrokeWidth, Is.Zero);
        }
    }
}
