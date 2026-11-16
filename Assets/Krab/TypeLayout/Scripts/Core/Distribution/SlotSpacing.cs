namespace Krab.TypeLayout.Core.Distribution
{
    public enum SlotSpacing
    {
        /// <summary>Open paths: first and last slots sit exactly on the ends. Closed paths: the first slot sits on the seam.</summary>
        Ends,

        /// <summary>The path is cut into equal cells and each slot sits at a cell center.</summary>
        Cells
    }
}
