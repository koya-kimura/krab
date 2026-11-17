namespace Krab.ShapeSynth.Core.Modulation
{
    /// <summary>A signal that reacts to a Voice release event, given in absolute seconds.</summary>
    public interface IReleasable
    {
        public void Release(double nowSeconds);
    }
}
