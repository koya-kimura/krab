using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Presets;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class PresetUnitsTests
    {
        private static readonly ReferenceContext Canvas = new(1920, 1080);

        [Test]
        public void DefaultParametersMapToExpectedUnits()
        {
            var node = PresetNode.Create(PresetNodeKind.Circle);

            Assert.That(PresetUnits.UnitOf(node.PositionX, PresetQuantity.Length), Is.EqualTo(PresetUnit.Pixel));
            Assert.That(PresetUnits.UnitOf(node.Width, PresetQuantity.Length), Is.EqualTo(PresetUnit.ScreenMin));
            Assert.That(PresetUnits.UnitOf(node.Rotation, PresetQuantity.Angle), Is.EqualTo(PresetUnit.Degree));
            Assert.That(PresetUnits.UnitOf(node.ScaleX, PresetQuantity.Scale), Is.EqualTo(PresetUnit.Scale));
            Assert.That(PresetUnits.UnitOf(node.Opacity, PresetQuantity.Ratio), Is.EqualTo(PresetUnit.Ratio));
            Assert.That(PresetUnits.Symbol(PresetUnit.ScreenMin), Is.EqualTo("xMin"));
            Assert.That(PresetUnits.IsScreenRelative(PresetUnit.ScreenMin), Is.True);
            Assert.That(PresetUnits.IsScreenRelative(PresetUnit.Pixel), Is.False);
        }

        [Test]
        public void SwitchingLengthUnitKeepsOutputAndConvertsDepth()
        {
            var parameter = new PresetParameter { Reference = ReferenceSource.MinDimension, Base = 0.2 };
            parameter.Bind("value", 0.1);

            PresetUnits.SetUnit(parameter, PresetQuantity.Length, PresetUnit.Pixel, Canvas);

            Assert.That(parameter.Reference, Is.EqualTo(ReferenceSource.Absolute));
            Assert.That(parameter.Base, Is.EqualTo(216).Within(1e-9));
            Assert.That(parameter.GetBinding().Depth, Is.EqualTo(108).Within(1e-9));

            PresetUnits.SetUnit(parameter, PresetQuantity.Length, PresetUnit.ScreenWidth, Canvas);

            Assert.That(PresetUnits.UnitOf(parameter, PresetQuantity.Length), Is.EqualTo(PresetUnit.ScreenWidth));
            Assert.That(parameter.Base, Is.EqualTo(216.0 / 1920).Within(1e-12));
        }

        [Test]
        public void TurnIsAbsoluteTimes360AndRoundTripsWithDegrees()
        {
            var parameter = new PresetParameter { Base = 90 };
            parameter.Bind("value", 360);

            PresetUnits.SetUnit(parameter, PresetQuantity.Angle, PresetUnit.Turn, Canvas);

            Assert.That(PresetUnits.UnitOf(parameter, PresetQuantity.Angle), Is.EqualTo(PresetUnit.Turn));
            Assert.That(parameter.Multiplier, Is.EqualTo(360));
            Assert.That(parameter.Base, Is.EqualTo(0.25).Within(1e-12));
            Assert.That(parameter.GetBinding().Depth, Is.EqualTo(1).Within(1e-12));

            PresetUnits.SetUnit(parameter, PresetQuantity.Angle, PresetUnit.Degree, Canvas);

            Assert.That(parameter.Multiplier, Is.EqualTo(1));
            Assert.That(parameter.Base, Is.EqualTo(90).Within(1e-9));
        }

        [Test]
        public void SetUnitNormalizesLegacyAbsoluteValueAndKeepsOutput()
        {
            var parameter = new PresetParameter { AbsoluteValue = 4, Multiplier = 2, Base = 3 };

            PresetUnits.SetUnit(parameter, PresetQuantity.Length, PresetUnit.Pixel, Canvas);

            Assert.That(parameter.AbsoluteValue, Is.EqualTo(1));
            Assert.That(parameter.Multiplier, Is.EqualTo(1));
            Assert.That(parameter.Base, Is.EqualTo(24).Within(1e-9));
        }

        [Test]
        public void SetUnitRejectsUnitOfAnotherQuantity()
        {
            var parameter = new PresetParameter();

            Assert.Throws<ArgumentException>(() =>
                PresetUnits.SetUnit(parameter, PresetQuantity.Angle, PresetUnit.ScreenMin, Canvas));
        }
    }
}
