using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;

namespace Krab.ShapeSynth.Core.Presets
{
    /// <summary>What a Parameter measures; decides which units can be chosen.</summary>
    public enum PresetQuantity
    {
        Length,
        Angle,
        Scale,
        Ratio
    }

    public enum PresetUnit
    {
        Pixel,
        PercentWidth,
        PercentHeight,
        PercentMin,
        PercentMax,
        Degree,
        Turn,
        Scale,
        Ratio
    }

    /// <summary>
    /// Authoring units on top of the stored Reference fields, so the JSON schema is unchanged.
    /// Percent units are canvas references shown ×100; Turn is Absolute with Multiplier 360.
    /// Every unit keeps AbsoluteValue at 1.
    /// </summary>
    public static class PresetUnits
    {
        public const double DegreesPerTurn = 360;

        private static readonly PresetUnit[] LengthUnits =
        {
            PresetUnit.Pixel, PresetUnit.PercentWidth, PresetUnit.PercentHeight, PresetUnit.PercentMin,
            PresetUnit.PercentMax
        };

        private static readonly PresetUnit[] AngleUnits = { PresetUnit.Degree, PresetUnit.Turn };
        private static readonly PresetUnit[] ScaleUnits = { PresetUnit.Scale };
        private static readonly PresetUnit[] RatioUnits = { PresetUnit.Ratio };

        public static IReadOnlyList<PresetUnit> Choices(PresetQuantity quantity)
        {
            switch (quantity)
            {
                case PresetQuantity.Length:
                    return LengthUnits;
                case PresetQuantity.Angle:
                    return AngleUnits;
                case PresetQuantity.Scale:
                    return ScaleUnits;
                default:
                    return RatioUnits;
            }
        }

        public static PresetUnit UnitOf(PresetParameter parameter, PresetQuantity quantity)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));

            switch (quantity)
            {
                case PresetQuantity.Length:
                    switch (parameter.Reference)
                    {
                        case ReferenceSource.CanvasWidth:
                            return PresetUnit.PercentWidth;
                        case ReferenceSource.CanvasHeight:
                            return PresetUnit.PercentHeight;
                        case ReferenceSource.MinDimension:
                            return PresetUnit.PercentMin;
                        case ReferenceSource.MaxDimension:
                            return PresetUnit.PercentMax;
                        default:
                            return PresetUnit.Pixel;
                    }
                case PresetQuantity.Angle:
                    return parameter.Reference == ReferenceSource.Absolute
                           && parameter.Multiplier == DegreesPerTurn
                        ? PresetUnit.Turn
                        : PresetUnit.Degree;
                case PresetQuantity.Scale:
                    return PresetUnit.Scale;
                default:
                    return PresetUnit.Ratio;
            }
        }

        /// <summary>Factor between the stored Base/Depth and the number shown to the user.</summary>
        public static double DisplayScale(PresetUnit unit) => IsPercent(unit) ? 100 : 1;

        public static bool IsPercent(PresetUnit unit) =>
            unit == PresetUnit.PercentWidth || unit == PresetUnit.PercentHeight
                                            || unit == PresetUnit.PercentMin || unit == PresetUnit.PercentMax;

        /// <summary>ASCII labels; RosettaUI's default font lacks some symbols.</summary>
        public static string Symbol(PresetUnit unit)
        {
            switch (unit)
            {
                case PresetUnit.Pixel:
                    return "px";
                case PresetUnit.PercentWidth:
                    return "%W";
                case PresetUnit.PercentHeight:
                    return "%H";
                case PresetUnit.PercentMin:
                    return "%Min";
                case PresetUnit.PercentMax:
                    return "%Max";
                case PresetUnit.Degree:
                    return "deg";
                case PresetUnit.Turn:
                    return "turn";
                case PresetUnit.Scale:
                    return "x";
                default:
                    return "0-1";
            }
        }

        /// <summary>Suffix of the evaluated output (pixels or degrees); empty for unitless quantities.</summary>
        public static string OutputSymbol(PresetQuantity quantity)
        {
            if (quantity == PresetQuantity.Length) return "px";

            return quantity == PresetQuantity.Angle ? "deg" : "";
        }

        /// <summary>
        /// Switches the unit and rescales Base and every Depth so the output in context stays the same.
        /// </summary>
        public static void SetUnit(PresetParameter parameter, PresetQuantity quantity, PresetUnit unit,
            ReferenceContext context)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));
            if (Array.IndexOf((PresetUnit[])Choices(quantity), unit) < 0)
                throw new ArgumentException($"{unit} is not a {quantity} unit.", nameof(unit));

            var before = Reference(parameter).Evaluate(context);
            parameter.Reference = Source(unit);
            parameter.Multiplier = unit == PresetUnit.Turn ? DegreesPerTurn : 1;
            parameter.AbsoluteValue = 1;
            var after = Reference(parameter).Evaluate(context);
            if (after == 0) return;

            var factor = before / after;
            parameter.Base *= factor;
            foreach (var binding in parameter.Bindings)
            {
                binding.Depth *= factor;
            }
        }

        private static ReferenceDefinition Reference(PresetParameter parameter) =>
            new ReferenceDefinition(parameter.Reference, parameter.Multiplier, parameter.AbsoluteValue);

        private static ReferenceSource Source(PresetUnit unit)
        {
            switch (unit)
            {
                case PresetUnit.PercentWidth:
                    return ReferenceSource.CanvasWidth;
                case PresetUnit.PercentHeight:
                    return ReferenceSource.CanvasHeight;
                case PresetUnit.PercentMin:
                    return ReferenceSource.MinDimension;
                case PresetUnit.PercentMax:
                    return ReferenceSource.MaxDimension;
                default:
                    return ReferenceSource.Absolute;
            }
        }
    }
}
