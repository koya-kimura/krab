namespace Krab.TypeLayout.Core.Placement
{
    /// <summary>Which horizontal point of the glyph sits on the slot.</summary>
    public enum HorizontalAnchor
    {
        /// <summary>Center of the advance box. Keeps the rhythm of the font's spacing.</summary>
        AdvanceCenter,

        /// <summary>Center of the drawn shape. Narrow letters such as "i" and "l" sit visually centered.</summary>
        InkCenter
    }
}
