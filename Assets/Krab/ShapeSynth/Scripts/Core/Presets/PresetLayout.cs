using System;
using Krab.ShapeSynth.Core.Definitions;

namespace Krab.ShapeSynth.Core.Presets
{
    public enum PresetLayoutKind
    {
        None,
        Row,
        Column,
        Grid,
        Circle
    }

    /// <summary>
    /// Draws a Shape Count times, arranged around the Shape's Position. None draws it once.
    /// </summary>
    [Serializable]
    public sealed class PresetLayout
    {
        public const int MaxColumns = 2000;
        public const int MaxCount = 2000;
        public const int MaxTotalCopies = 20000;

        public PresetLayoutKind Kind;
        public int Count = 4;
        public int Columns = 3;
        public PresetParameter SpacingX = new() { Base = 0.25, Reference = ReferenceSource.MinDimension };
        public PresetParameter SpacingY = new() { Base = 0.25, Reference = ReferenceSource.MinDimension };

        // Circle: the first copy sits at StartAngle (degrees, counter-clockwise from +X); the rest follow clockwise.
        public PresetParameter Radius = new() { Base = 0.3, Reference = ReferenceSource.MinDimension };
        public double StartAngle = 90;
        public bool FaceOutward;

        /// <summary>How many Shapes this node draws.</summary>
        public int Copies => Kind == PresetLayoutKind.None ? 1 : Count;

        /// <summary>Parameters the Slot units multiply on each axis.</summary>
        public PresetParameter AxisX => Kind == PresetLayoutKind.Circle ? Radius : SpacingX;

        public PresetParameter AxisY => Kind == PresetLayoutKind.Circle ? Radius : SpacingY;

        /// <summary>Circle angle of a copy in degrees; copies are spread evenly over 360°.</summary>
        public double Angle(int index, int count) => StartAngle - index * 360.0 / count;

        /// <summary>
        /// Slot of copy index among count copies, in units of AxisX/AxisY.
        /// Rows run left to right, then top to bottom (Core Y is up); the whole block is centered.
        /// </summary>
        public (double X, double Y) Slot(int index, int count)
        {
            if (count <= 0 || index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));

            switch (Kind)
            {
                case PresetLayoutKind.None:
                    return (0, 0);
                case PresetLayoutKind.Row:
                    return (index - (count - 1) / 2.0, 0);
                case PresetLayoutKind.Column:
                    return (0, (count - 1) / 2.0 - index);
                case PresetLayoutKind.Grid:
                    RequireColumns(Columns);
                    var columnsUsed = Math.Min(count, Columns);
                    var rows = (count + Columns - 1) / Columns;
                    return (index % Columns - (columnsUsed - 1) / 2.0, (rows - 1) / 2.0 - index / Columns);
                case PresetLayoutKind.Circle:
                    var radians = Angle(index, count) * Math.PI / 180;
                    return (Math.Cos(radians), Math.Sin(radians));
                default:
                    throw new ArgumentException("Unknown Layout kind.");
            }
        }

        public static void RequireCount(int count)
        {
            if (count < 1 || count > MaxCount)
                throw new ArgumentOutOfRangeException(nameof(count), $"Layout count must be 1–{MaxCount}.");
        }

        public static void RequireColumns(int columns)
        {
            if (columns < 1 || columns > MaxColumns)
                throw new ArgumentOutOfRangeException(nameof(columns), $"Grid columns must be 1–{MaxColumns}.");
        }
    }
}
