namespace Krab.ShapeSynth.Core
{
    /// <summary>Fixed display color; numeric modulation remains scalar. No implicit clamping.</summary>
    public readonly struct RgbaColor
    {
        public double R { get; }
        public double G { get; }
        public double B { get; }
        public double A { get; }
        public static RgbaColor White => new RgbaColor(1, 1, 1);

        public RgbaColor(double r, double g, double b, double a = 1)
        {
            VoiceTime.RequireFinite(r, nameof(r));
            VoiceTime.RequireFinite(g, nameof(g));
            VoiceTime.RequireFinite(b, nameof(b));
            VoiceTime.RequireFinite(a, nameof(a));
            R = r; G = g; B = b; A = a;
        }
    }
}
