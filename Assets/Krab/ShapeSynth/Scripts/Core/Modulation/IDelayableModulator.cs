namespace Krab.ShapeSynth.Core.Modulation
{
    /// <summary>A signal that can be read a number of beats in the past (a Binding delay).</summary>
    public interface IDelayableModulator : IModulator
    {
        double EvaluateDelayed(double nowSeconds, double delayBeats);
    }
}
