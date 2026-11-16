namespace Krab.TypeLayout.Core.Distribution
{
    public enum FlowFill
    {
        /// <summary>The text appears once.</summary>
        None,

        /// <summary>The text repeats as many glyphs as fit, possibly stopping mid-word.</summary>
        Glyphs,

        /// <summary>The text repeats only as a whole, so a closed loop reads seamlessly across the seam.</summary>
        WholeText
    }
}
