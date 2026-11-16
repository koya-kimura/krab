namespace Krab.TypeLayout.Core.Placement
{
    /// <summary>Which vertical point of the glyph sits on the slot.</summary>
    public enum VerticalAnchor
    {
        Baseline,

        /// <summary>Center of the em box (ascent to descent). Shared by every glyph of the font.</summary>
        EmCenter,

        /// <summary>Half the cap height above the baseline. Shared by every glyph; capitals look centered.</summary>
        CapCenter,

        /// <summary>
        /// Center of each glyph's own shape. Lowercase and capitals are each centered, losing a shared baseline.
        /// </summary>
        InkCenter
    }
}
