namespace Krab.TypeLayout.Core.Placement
{
    /// <summary>Which vertical point of the glyph sits on the slot.</summary>
    public enum VerticalAnchor
    {
        /// <summary>The baseline sits on the slot: text rests on a bottom edge.</summary>
        Baseline,

        /// <summary>The top of the capitals sits on the slot: text hangs from a top edge.</summary>
        CapTop,

        /// <summary>The bottom of the em box sits on the slot: descenders rest on a bottom edge.</summary>
        EmBottom,

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
