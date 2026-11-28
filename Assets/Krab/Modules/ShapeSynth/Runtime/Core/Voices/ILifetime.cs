namespace Krab.ShapeSynth.Core.Voices
{
    /// <summary>Reports whether a time-based voice has finished at an absolute time.</summary>
    public interface ILifetime
    {
        bool IsFinished(double nowSeconds);
    }
}
