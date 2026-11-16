namespace Krab.TypeLayout.Core.Paths
{
    /// <summary>Arc-length parameterized path that text runs along.</summary>
    public interface ILayoutPath
    {
        public double Length { get; }

        /// <summary>Closed paths wrap any distance; the end meets the start.</summary>
        public bool IsClosed { get; }

        /// <summary>
        /// Point and direction at a distance from the start. Open paths extend straight past their ends
        /// along the end direction, so scrolled text can leave the path smoothly.
        /// </summary>
        public LayoutSlot Sample(double distance);
    }
}
