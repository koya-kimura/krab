using System;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Presets;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class ColorDefinitionTests
    {
        private static readonly RgbaColor Red = new(1, 0, 0);
        private static readonly RgbaColor Blue = new(0, 0, 1, 0.5);

        [Test]
        public void MixBlendsRgbaAndClampsToEnds()
        {
            var color = new ColorDefinition(Red, Blue);

            AssertColor(color.Resolve(0, 0), 1, 0, 0, 1);
            AssertColor(color.Resolve(0.25, 0), 0.75, 0, 0.25, 0.875);
            AssertColor(color.Resolve(-3, 0), 1, 0, 0, 1);
            AssertColor(color.Resolve(7, 0), 0, 0, 1, 0.5);
        }

        [Test]
        public void HueShiftRotatesHueAndWrapsBothWays()
        {
            AssertColor(ColorDefinition.ShiftHue(Red, 120), 0, 1, 0, 1);
            AssertColor(ColorDefinition.ShiftHue(Red, 240), 0, 0, 1, 1);
            AssertColor(ColorDefinition.ShiftHue(Red, -120), 0, 0, 1, 1);
            AssertColor(ColorDefinition.ShiftHue(Red, 720 + 60), 1, 1, 0, 1);
            AssertColor(ColorDefinition.ShiftHue(Red, 360), 1, 0, 0, 1);
        }

        [Test]
        public void HueShiftKeepsSaturationValueAlphaAndLeavesGray()
        {
            var pastel = new RgbaColor(1, 0.5, 0.5, 0.3);

            AssertColor(ColorDefinition.ShiftHue(pastel, 120), 0.5, 1, 0.5, 0.3);
            AssertColor(ColorDefinition.ShiftHue(new RgbaColor(0.4, 0.4, 0.4), 90), 0.4, 0.4, 0.4, 1);
        }

        [Test]
        public void ResolveRejectsNonFiniteInputs()
        {
            var color = ColorDefinition.Constant(Red);

            Assert.Throws<ArgumentOutOfRangeException>(() => color.Resolve(double.NaN, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => color.Resolve(0, double.PositiveInfinity));
        }

        [Test]
        public void WithFillColorLeavesSourceShapeUnchanged()
        {
            var shape = ShapeDefinition.Circle(ParameterDefinition.Constant(10), color: Red);
            var blended = shape.WithFillColor(new ColorDefinition(Red, Blue, ParameterDefinition.Constant(1)));

            Assert.That(shape.FillColor.Mix.BaseValue, Is.EqualTo(0));
            Assert.That(blended.FillColor.To.B, Is.EqualTo(1));
            Assert.That(blended.Width, Is.SameAs(shape.Width));
            Assert.That(blended.Color.R, Is.EqualTo(1));
        }

        [Test]
        public void PresetBindingsDriveMixAndHuePerFrame()
        {
            var document = PresetDocument.CreateNew();
            var shape = document.RootGroup.Children.Single();
            shape.Red = 1;
            shape.Green = 0;
            shape.Blue = 0;
            shape.ToRed = 0;
            shape.ToGreen = 0;
            shape.ToBlue = 1;
            shape.Opacity.Unbind();
            var signal = new SettableSignal();
            var context = new ReferenceContext(100, 100);

            shape.ColorMix.Bind(document.Values[0].Id, 1);
            var evaluator = new HierarchyEvaluator(PresetCompiler.CompileGroup(document), new IModulator[] { signal });
            signal.Value = 0.5;

            AssertColor(evaluator.Evaluate(0, context)[0].Color, 0.5, 0, 0.5, 1);

            signal.Value = 1;

            AssertColor(evaluator.Evaluate(0, context)[0].Color, 0, 0, 1, 1);

            shape.ColorMix.Unbind();
            shape.HueShift.Bind(document.Values[0].Id, 360);
            evaluator = new HierarchyEvaluator(PresetCompiler.CompileGroup(document), new IModulator[] { signal });
            signal.Value = 1.0 / 3;

            AssertColor(evaluator.Evaluate(0, context)[0].Color, 0, 1, 0, 1);
        }

        [Test]
        public void ValidationRejectsNonFiniteToColor()
        {
            var document = PresetDocument.CreateNew();
            document.RootGroup.Children[0].ToGreen = double.NaN;

            Assert.Throws<ArgumentOutOfRangeException>(document.Validate);
        }

        private static void AssertColor(RgbaColor color, double r, double g, double b, double a)
        {
            Assert.That(new[] { color.R, color.G, color.B, color.A },
                Is.EqualTo(new[] { r, g, b, a }).Within(1e-9));
        }

        private sealed class SettableSignal : IModulator
        {
            public double Value { get; set; }

            public double Evaluate(double nowSeconds) => Value;
        }
    }
}
