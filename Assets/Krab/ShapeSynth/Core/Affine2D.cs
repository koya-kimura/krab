using System;

namespace Krab.ShapeSynth.Core
{
    /// <summary>Column-vector 2D affine matrix. Multiply parent * local, including shear/reflection.</summary>
    public readonly struct Affine2D
    {
        public double M00 { get; }
        public double M01 { get; }
        public double M02 { get; }
        public double M10 { get; }
        public double M11 { get; }
        public double M12 { get; }
        public static Affine2D Identity => new Affine2D(1, 0, 0, 0, 1, 0);

        public Affine2D(double m00, double m01, double m02, double m10, double m11, double m12)
        {
            VoiceTime.RequireFinite(m00, nameof(m00));
            VoiceTime.RequireFinite(m01, nameof(m01));
            VoiceTime.RequireFinite(m02, nameof(m02));
            VoiceTime.RequireFinite(m10, nameof(m10));
            VoiceTime.RequireFinite(m11, nameof(m11));
            VoiceTime.RequireFinite(m12, nameof(m12));
            M00 = m00; M01 = m01; M02 = m02;
            M10 = m10; M11 = m11; M12 = m12;
        }

        public static Affine2D Trs(double x, double y, double rotationDegrees, double scaleX, double scaleY)
        {
            VoiceTime.RequireFinite(rotationDegrees, nameof(rotationDegrees));
            VoiceTime.RequireFinite(scaleX, nameof(scaleX));
            VoiceTime.RequireFinite(scaleY, nameof(scaleY));
            var radians = (rotationDegrees % 360) * (Math.PI / 180);
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);
            return new Affine2D(cos * scaleX, -sin * scaleY, x, sin * scaleX, cos * scaleY, y);
        }

        public static Affine2D operator *(Affine2D a, Affine2D b) => new Affine2D(
            a.M00 * b.M00 + a.M01 * b.M10,
            a.M00 * b.M01 + a.M01 * b.M11,
            a.M00 * b.M02 + a.M01 * b.M12 + a.M02,
            a.M10 * b.M00 + a.M11 * b.M10,
            a.M10 * b.M01 + a.M11 * b.M11,
            a.M10 * b.M02 + a.M11 * b.M12 + a.M12);

        public Point2 TransformPoint(Point2 point) => new Point2(
            M00 * point.X + M01 * point.Y + M02,
            M10 * point.X + M11 * point.Y + M12);
    }
}
