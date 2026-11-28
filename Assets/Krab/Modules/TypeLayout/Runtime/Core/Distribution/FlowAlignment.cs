namespace Krab.TypeLayout.Core.Distribution
{
    public enum FlowAlignment
    {
        /// <summary>Open paths: text starts at the path start. Closed paths: text starts at the seam.</summary>
        Start,

        /// <summary>Open paths: centered on the path. Closed paths: centered on the seam.</summary>
        Center,

        /// <summary>Open paths: text ends at the path end. Closed paths: text ends at the seam.</summary>
        End,

        /// <summary>
        /// Spreads the spacing so the text spans the path exactly: first and last glyph boxes touch the ends of an
        /// open path, and a closed path gets the same gap at the seam as between every other pair.
        /// </summary>
        Justify
    }
}
