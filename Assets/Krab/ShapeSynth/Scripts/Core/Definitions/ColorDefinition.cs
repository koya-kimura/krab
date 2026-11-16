using System;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>
    /// Fill color evaluated per frame: RGBA linear blend From→To by Mix (clamped to 0–1),
    /// then the hue rotated by HueShiftDegrees in HSV. Saturation, value and alpha are kept.
    /// </summary>
    public sealed class ColorDefinition
    {
        public RgbaColor From { get; }
        public RgbaColor To { get; }
        public ParameterDefinition Mix { get; }
        public ParameterDefinition HueShiftDegrees { get; }

        public ColorDefinition(RgbaColor from, RgbaColor to, ParameterDefinition mix = null,
            ParameterDefinition hueShiftDegrees = null)
        {
            From = from;
            To = to;
            Mix = mix ?? ParameterDefinition.Constant(0);
            HueShiftDegrees = hueShiftDegrees ?? ParameterDefinition.Constant(0);
        }

        public static ColorDefinition Constant(RgbaColor color) => new ColorDefinition(color, color);

        public RgbaColor Resolve(double mix, double hueShiftDegrees)
        {
            VoiceTime.RequireFinite(mix, nameof(mix));
            VoiceTime.RequireFinite(hueShiftDegrees, nameof(hueShiftDegrees));

            var t = Math.Max(0, Math.Min(1, mix));
            var blended = new RgbaColor(
                Lerp(From.R, To.R, t), Lerp(From.G, To.G, t), Lerp(From.B, To.B, t), Lerp(From.A, To.A, t));
            return hueShiftDegrees == 0 ? blended : ShiftHue(blended, hueShiftDegrees);
        }

        /// <summary>Rotates hue in HSV. Gray (no saturation) is unchanged.</summary>
        public static RgbaColor ShiftHue(RgbaColor color, double degrees)
        {
            VoiceTime.RequireFinite(degrees, nameof(degrees));

            var max = Math.Max(color.R, Math.Max(color.G, color.B));
            var min = Math.Min(color.R, Math.Min(color.G, color.B));
            var chroma = max - min;
            if (chroma <= 0) return color;

            double hue;
            if (max == color.R) hue = (color.G - color.B) / chroma;
            else if (max == color.G) hue = (color.B - color.R) / chroma + 2;
            else hue = (color.R - color.G) / chroma + 4;

            hue = (hue * 60 + degrees % 360) % 360;
            if (hue < 0) hue += 360;

            var sector = hue / 60;
            var x = chroma * (1 - Math.Abs(sector % 2 - 1));
            double r, g, b;
            if (sector < 1) (r, g, b) = (chroma, x, 0);
            else if (sector < 2) (r, g, b) = (x, chroma, 0);
            else if (sector < 3) (r, g, b) = (0, chroma, x);
            else if (sector < 4) (r, g, b) = (0, x, chroma);
            else if (sector < 5) (r, g, b) = (x, 0, chroma);
            else (r, g, b) = (chroma, 0, x);

            return new RgbaColor(r + min, g + min, b + min, color.A);
        }

        private static double Lerp(double from, double to, double t) => from * (1 - t) + to * t;
    }
}
