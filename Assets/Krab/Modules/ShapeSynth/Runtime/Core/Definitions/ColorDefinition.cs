using System;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Voices;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>
    /// A color made only of HSB Parameters evaluated per frame: Hue in degrees (wraps), Saturation and Brightness
    /// clamped to 0–1 when resolved, and a constant Alpha. Fill and stroke use the same definition.
    /// </summary>
    public sealed class ColorDefinition
    {
        public ParameterDefinition Hue { get; }
        public ParameterDefinition Saturation { get; }
        public ParameterDefinition Brightness { get; }
        public double Alpha { get; }

        /// <summary>The exact source RGBA of a Constant color; null when the color is built from Parameters.</summary>
        public RgbaColor? ConstantColor { get; }

        public ColorDefinition(ParameterDefinition hue = null, ParameterDefinition saturation = null,
            ParameterDefinition brightness = null, double alpha = 1)
            : this(hue, saturation, brightness, alpha, null)
        {
        }

        private ColorDefinition(ParameterDefinition hue, ParameterDefinition saturation,
            ParameterDefinition brightness, double alpha, RgbaColor? constantColor)
        {
            VoiceTime.RequireFinite(alpha, nameof(alpha));
            Hue = hue ?? ParameterDefinition.Constant(0);
            Saturation = saturation ?? ParameterDefinition.Constant(0);
            Brightness = brightness ?? ParameterDefinition.Constant(1);
            Alpha = alpha;
            ConstantColor = constantColor;
        }

        public static ColorDefinition White { get; } = Constant(RgbaColor.White);

        /// <summary>A fixed color. Resolving it returns the given RGBA exactly, without an HSB round trip.</summary>
        public static ColorDefinition Constant(RgbaColor color)
        {
            var (hue, saturation, brightness) = ToHsb(color);
            return new ColorDefinition(ParameterDefinition.Constant(hue), ParameterDefinition.Constant(saturation),
                ParameterDefinition.Constant(brightness), color.A, color);
        }

        public RgbaColor Resolve(double hueDegrees, double saturation, double brightness)
        {
            return FromHsb(hueDegrees, saturation, brightness, Alpha);
        }

        /// <summary>HSB (= HSV) of an RGB color. Hue is 0 for grays; Saturation is 0 for black.</summary>
        public static (double Hue, double Saturation, double Brightness) ToHsb(RgbaColor color)
        {
            var max = Math.Max(color.R, Math.Max(color.G, color.B));
            var min = Math.Min(color.R, Math.Min(color.G, color.B));
            var chroma = max - min;
            if (chroma <= 0) return (0, 0, max);

            double sector;
            if (max == color.R) sector = (color.G - color.B) / chroma;
            else if (max == color.G) sector = (color.B - color.R) / chroma + 2;
            else sector = (color.R - color.G) / chroma + 4;

            return (WrapHue(sector * 60), max > 0 ? chroma / max : 0, max);
        }

        /// <summary>Hue wraps to [0, 360); Saturation and Brightness are clamped to 0–1.</summary>
        public static RgbaColor FromHsb(double hueDegrees, double saturation, double brightness, double alpha = 1)
        {
            VoiceTime.RequireFinite(hueDegrees, nameof(hueDegrees));
            VoiceTime.RequireFinite(saturation, nameof(saturation));
            VoiceTime.RequireFinite(brightness, nameof(brightness));

            var s = Clamp01(saturation);
            var v = Clamp01(brightness);
            var chroma = v * s;
            var sector = WrapHue(hueDegrees) / 60;
            var x = chroma * (1 - Math.Abs(sector % 2 - 1));
            double r, g, b;
            if (sector < 1) (r, g, b) = (chroma, x, 0);
            else if (sector < 2) (r, g, b) = (x, chroma, 0);
            else if (sector < 3) (r, g, b) = (0, chroma, x);
            else if (sector < 4) (r, g, b) = (0, x, chroma);
            else if (sector < 5) (r, g, b) = (x, 0, chroma);
            else (r, g, b) = (chroma, 0, x);

            var min = v - chroma;
            return new RgbaColor(r + min, g + min, b + min, alpha);
        }

        private static double WrapHue(double degrees)
        {
            var hue = degrees % 360;
            if (hue < 0) hue += 360;
            // Adding 360 to a tiny negative remainder can round to exactly 360.
            return hue >= 360 ? 0 : hue;
        }

        private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
    }
}
