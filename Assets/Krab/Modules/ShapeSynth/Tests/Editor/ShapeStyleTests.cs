using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Voices;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class ShapeStyleTests
    {
        private static readonly ReferenceContext Context = new(100, 100);

        [Test]
        public void DefaultStylePreservesFillOnlyBehavior()
        {
            var shape = ShapeDefinition.Circle(ParameterDefinition.Constant(10));

            Assert.That(shape.Style.FillEnabled, Is.True);
            Assert.That(shape.Style.StrokeEnabled, Is.False);
            Assert.That(shape.StrokeWidth.BaseValue, Is.EqualTo(1));
            Assert.That(shape.StrokeColor, Is.SameAs(shape.FillColor));
        }

        [Test]
        public void StyleAndStrokeAreRetainedOnDefinitionAndEvaluatedDrawItem()
        {
            var strokeColor = new RgbaColor(0.2, 0.3, 0.4, 0.5);
            var style = new ShapeStyle(fill: false, stroke: true);
            var shape = ShapeDefinition.Rectangle(ParameterDefinition.Constant(4), ParameterDefinition.Constant(3),
                style: style, strokeColor: ColorDefinition.Constant(strokeColor),
                strokeWidth: ParameterDefinition.Constant(2.5));
            var item = Evaluate(shape);

            Assert.That(shape.Style, Is.SameAs(style));
            Assert.That(item.Style, Is.SameAs(style));
            Assert.That(item.Style.FillEnabled, Is.False);
            Assert.That(item.Style.StrokeEnabled, Is.True);
            Assert.That(item.StrokeWidth, Is.EqualTo(2.5));
            Assert.That(item.StrokeColor, Is.EqualTo(strokeColor));
        }

        [Test]
        public void BoundStrokeWidthIsEvaluatedEachFrameAndMayGoBelowZero()
        {
            var width = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 2,
                new BindingDefinition(0, 4));
            var shape = ShapeDefinition.Circle(ParameterDefinition.Constant(10), strokeWidth: width);

            Assert.That(Evaluate(shape, 0.5).StrokeWidth, Is.EqualTo(4));
            // The Core value is not clamped; the adapter draws no stroke for 0 or below.
            Assert.That(Evaluate(shape, -1).StrokeWidth, Is.EqualTo(-2));
        }

        private static DrawItem Evaluate(ShapeDefinition shape, double signal = 0)
        {
            var evaluator = new HierarchyEvaluator(new GroupDefinition(null, shape),
                new IModulator[] { new Fixed(signal) });
            return evaluator.Evaluate(0, Context)[0];
        }

        private sealed class Fixed : IModulator
        {
            private readonly double _value;

            public Fixed(double value)
            {
                _value = value;
            }

            public double Evaluate(double nowSeconds) => _value;
        }
    }
}
