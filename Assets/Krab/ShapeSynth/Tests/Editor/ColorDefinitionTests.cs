using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class ColorDefinitionTests
    {
        private static readonly ReferenceContext Context = new(100, 100);

        [TestCase(1, 0, 0, 0, 1, 1)]
        [TestCase(0, 1, 0, 120, 1, 1)]
        [TestCase(0, 0, 1, 240, 1, 1)]
        [TestCase(1, 0, 1, 300, 1, 1)]
        [TestCase(1, 0.5, 0.5, 0, 0.5, 1)]
        [TestCase(0.4, 0.4, 0.4, 0, 0, 0.4)]
        [TestCase(0, 0, 0, 0, 0, 0)]
        public void ConvertsBetweenRgbAndHsb(double r, double g, double b, double hue, double saturation,
            double brightness)
        {
            var (h, s, v) = ColorDefinition.ToHsb(new RgbaColor(r, g, b));

            Assert.That(new[] { h, s, v }, Is.EqualTo(new[] { hue, saturation, brightness }).Within(1e-12));
            AssertColor(ColorDefinition.FromHsb(hue, saturation, brightness), r, g, b, 1);
        }

        [Test]
        public void RoundTripsArbitraryColors()
        {
            foreach (var color in new[]
                     {
                         new RgbaColor(0.13, 0.57, 0.91), new RgbaColor(0.99, 0.2, 0.6), new RgbaColor(0.3, 0.3, 0.1)
                     })
            {
                var (h, s, v) = ColorDefinition.ToHsb(color);
                AssertColor(ColorDefinition.FromHsb(h, s, v), color.R, color.G, color.B, 1);
            }
        }

        [TestCase(-120, 0, 0, 1)]
        [TestCase(360, 1, 0, 0)]
        [TestCase(780, 1, 1, 0)]
        [TestCase(-1e-15, 1, 0, 0)]
        public void HueWrapsAroundTheCircle(double hue, double r, double g, double b)
        {
            AssertColor(ColorDefinition.FromHsb(hue, 1, 1), r, g, b, 1);
        }

        [Test]
        public void SaturationAndBrightnessAreClampedAndAlphaIsKept()
        {
            AssertColor(ColorDefinition.FromHsb(0, 1.5, 2, 0.3), 1, 0, 0, 0.3);
            AssertColor(ColorDefinition.FromHsb(0, -0.2, 0.5), 0.5, 0.5, 0.5, 1);
            AssertColor(ColorDefinition.FromHsb(0, 1, -1), 0, 0, 0, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => ColorDefinition.FromHsb(double.NaN, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ColorDefinition(alpha: double.NaN));
        }

        [Test]
        public void ConstantColorsResolveToTheirExactRgba()
        {
            var color = new RgbaColor(252 / 255.0, 70 / 255.0, 34 / 255.0, 0.7);
            var item = Evaluate(ShapeDefinition.Circle(ParameterDefinition.Constant(10), color: color));

            Assert.That(item.FillColor, Is.EqualTo(color));
            Assert.That(item.StrokeColor, Is.EqualTo(color));
        }

        [Test]
        public void DefaultColorIsWhite()
        {
            var item = Evaluate(ShapeDefinition.Circle(ParameterDefinition.Constant(10)));

            AssertColor(item.FillColor, 1, 1, 1, 1);
            AssertColor(new ColorDefinition().Resolve(0, 0, 1), 1, 1, 1, 1);
        }

        [Test]
        public void BoundHueSaturationAndBrightnessAreEvaluatedEachFrame()
        {
            var fill = new ColorDefinition(Bound(0, 120), Bound(0, 2), ParameterDefinition.Constant(0.5), 0.8);
            var shape = ShapeDefinition.Circle(ParameterDefinition.Constant(10), fillColor: fill);

            // Signal 1 → hue 120°, saturation 2 clamped to 1, brightness 0.5.
            AssertColor(Evaluate(shape, 1).FillColor, 0, 0.5, 0, 0.8);
            // Signal 0 → hue 0°, saturation 0: a gray at the same brightness.
            AssertColor(Evaluate(shape, 0).FillColor, 0.5, 0.5, 0.5, 0.8);
        }

        [Test]
        public void FillAndStrokeAreIndependentColorsOfTheSameForm()
        {
            var fill = ColorDefinition.Constant(new RgbaColor(1, 0, 0));
            var stroke = new ColorDefinition(Bound(240, 120), ParameterDefinition.Constant(1),
                ParameterDefinition.Constant(1), 0.25);
            var shape = ShapeDefinition.Circle(ParameterDefinition.Constant(10), fillColor: fill,
                strokeColor: stroke);

            var item = Evaluate(shape, 1);

            AssertColor(item.FillColor, 1, 0, 0, 1);
            AssertColor(item.StrokeColor, 1, 0, 0, 0.25);
            AssertColor(Evaluate(shape, 0).StrokeColor, 0, 0, 1, 0.25);
        }

        [Test]
        public void PresetColorsCompileToHsbParameters()
        {
            var document = PresetDocument.CreateNew();
            var node = document.RootGroup.Children[0];
            node.FillHue.Base = 120;
            node.FillSaturation.Base = 1;
            node.FillBrightness.Base = 0.5;
            node.FillAlpha = 0.5;
            node.StrokeHue.Bind(document.Values[0].Id, 90);

            var shape = (ShapeDefinition)PresetCompiler.CompileGroup(document).Children[0];

            AssertColor(shape.FillColor.Resolve(120, 1, 0.5), 0, 0.5, 0, 0.5);
            Assert.That(shape.FillColor.ConstantColor, Is.Null);
            Assert.That(shape.StrokeColor.Hue.Bindings, Has.Count.EqualTo(1));
            Assert.That(shape.StrokeColor.Saturation.BaseValue, Is.EqualTo(0));
            Assert.That(shape.StrokeColor.Brightness.BaseValue, Is.EqualTo(1));
        }

        [Test]
        public void ValidationRejectsNonFiniteAlpha()
        {
            var document = PresetDocument.CreateNew();
            document.RootGroup.Children[0].FillAlpha = double.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(document.Validate);

            document.RootGroup.Children[0].FillAlpha = 1;
            document.RootGroup.Children[0].StrokeAlpha = double.PositiveInfinity;
            Assert.Throws<ArgumentOutOfRangeException>(document.Validate);
        }

        private static ParameterDefinition Bound(double baseValue, double depth)
        {
            return new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), baseValue,
                new BindingDefinition(0, depth));
        }

        private static DrawItem Evaluate(ShapeDefinition shape, double signal = 0)
        {
            var evaluator = new HierarchyEvaluator(new GroupDefinition(null, shape),
                new IModulator[] { new Fixed(signal) });
            return evaluator.Evaluate(0, Context)[0];
        }

        private static void AssertColor(RgbaColor color, double r, double g, double b, double a)
        {
            Assert.That(new[] { color.R, color.G, color.B, color.A },
                Is.EqualTo(new[] { r, g, b, a }).Within(1e-9));
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
