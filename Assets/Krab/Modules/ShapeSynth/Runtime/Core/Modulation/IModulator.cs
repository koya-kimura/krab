namespace Krab.ShapeSynth.Core.Modulation
{
    /// <summary>A scalar signal only. Trigger, release and lifetime remain type-specific.</summary>
    public interface IModulator
    {
        double Evaluate(double nowSeconds);
    }
}
