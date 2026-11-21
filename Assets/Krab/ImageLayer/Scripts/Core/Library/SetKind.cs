namespace Krab.ImageLayer.Core.Library
{
    public enum SetKind
    {
        /// <summary>PNGs placed directly in the set folder. Each file is a one-frame clip.</summary>
        Stills,

        /// <summary>Subfolders in the set folder. Each subfolder is a clip of numbered frames.</summary>
        Sequences,
    }
}
