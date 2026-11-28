using System;
using System.Collections.Generic;
using System.Linq;
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
        ScreenWidth,
        ScreenHeight,
        ScreenMin,
        ScreenMax,
        Degree,
        Turn,
        Scale,
        Ratio
    }

    /// <summary>
    /// Authoring units on top of the stored Reference fields, so the JSON schema is unchanged.
    /// Screen units are canvas references: Base 0.1 in xW means 0.1 × canvas width. Turn is Absolute × 360.
    /// Every unit keeps AbsoluteValue at 1.
    /// </summary>
    public static class PresetUnits
    {
        public const double DegreesPerTurn = 360;

        private static readonly PresetUnit[] LengthUnits =
        {
            PresetUnit.Pixel, PresetUnit.ScreenWidth, PresetUnit.ScreenHeight, PresetUnit.ScreenMin,
            PresetUnit.ScreenMax
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
                            return PresetUnit.ScreenWidth;
                        case ReferenceSource.CanvasHeight:
                            return PresetUnit.ScreenHeight;
                        case ReferenceSource.MinDimension:
                            return PresetUnit.ScreenMin;
                        case ReferenceSource.MaxDimension:
                            return PresetUnit.ScreenMax;
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

        public static bool IsScreenRelative(PresetUnit unit) =>
            unit == PresetUnit.ScreenWidth || unit == PresetUnit.ScreenHeight
                                            || unit == PresetUnit.ScreenMin || unit == PresetUnit.ScreenMax;

        /// <summary>ASCII labels; RosettaUI's default font lacks some symbols.</summary>
        public static string Symbol(PresetUnit unit)
        {
            switch (unit)
            {
                case PresetUnit.Pixel:
                    return "px";
                case PresetUnit.ScreenWidth:
                    return "xW";
                case PresetUnit.ScreenHeight:
                    return "xH";
                case PresetUnit.ScreenMin:
                    return "xMin";
                case PresetUnit.ScreenMax:
                    return "xMax";
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

            Scale(parameter, before / after);
        }

        /// <summary>
        /// Folds legacy factors into Base, Rand and Depth so the shown numbers are the real ones:
        /// Absolute Value always, and Multiplier unless it is the Turn marker (360 on Absolute).
        /// The evaluated output is unchanged.
        /// </summary>
        public static void Normalize(PresetParameter parameter)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));

            var turn = parameter.Reference == ReferenceSource.Absolute && parameter.Multiplier == DegreesPerTurn;
            var factor = parameter.Reference == ReferenceSource.Absolute ? parameter.AbsoluteValue : 1;
            if (!turn) factor *= parameter.Multiplier;

            parameter.AbsoluteValue = 1;
            if (!turn) parameter.Multiplier = 1;
            if (factor != 1) Scale(parameter, factor);
        }

        public static void Normalize(PresetDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            foreach (var parameter in document.Nodes().SelectMany(node => node.Parameters()))
            {
                Normalize(parameter);
            }
        }

        private static void Scale(PresetParameter parameter, double factor)
        {
            parameter.Base *= factor;
            parameter.Random = Math.Abs(parameter.Random * factor);
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
                case PresetUnit.ScreenWidth:
                    return ReferenceSource.CanvasWidth;
                case PresetUnit.ScreenHeight:
                    return ReferenceSource.CanvasHeight;
                case PresetUnit.ScreenMin:
                    return ReferenceSource.MinDimension;
                case PresetUnit.ScreenMax:
                    return ReferenceSource.MaxDimension;
                default:
                    return ReferenceSource.Absolute;
            }
        }
    }
}
