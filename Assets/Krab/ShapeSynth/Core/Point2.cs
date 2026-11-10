namespace Krab.ShapeSynth.Core
{
    public readonly struct Point2
    {
        public double X { get; }
        public double Y { get; }
        public Point2(double x, double y)
        {
            VoiceTime.RequireFinite(x, nameof(x));
            VoiceTime.RequireFinite(y, nameof(y));
            X = x;
            Y = y;
        }
    }
}
