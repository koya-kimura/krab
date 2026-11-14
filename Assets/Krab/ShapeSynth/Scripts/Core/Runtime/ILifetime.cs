namespace Krab.ShapeSynth.Core.Runtime
{
    /// <summary>Reports whether a time-based voice has finished at an absolute time.</summary>
    public interface ILifetime
    {
        bool IsFinished(double nowSeconds);
    }
}
