using UnityEngine;

namespace Krab.ImageLayer.Performance.Palette
{
    /// <summary>A set of colors that the background and image looks are drawn from, so they always match.</summary>
    public readonly struct ColorPalette
    {
        public static readonly ColorPalette[] Presets =
        {
            new("Lemon", new Color(0.98f, 0.84f, 0.2f), new Color(0.12f, 0.12f, 0.14f, 0.35f),
                new Color(0.12f, 0.12f, 0.14f), Color.white, new Color(0.95f, 0.3f, 0.75f)),
            new("Sticker", new Color(0.93f, 0.42f, 0.62f), new Color(0.16f, 0.2f, 0.55f, 0.5f),
                new Color(0.16f, 0.2f, 0.55f), Color.white, new Color(0.98f, 0.84f, 0.2f)),
            new("Tomato", new Color(0.85f, 0.22f, 0.2f), new Color(0.35f, 0.05f, 0.05f, 0.45f),
                new Color(0.08f, 0.06f, 0.06f), Color.white, new Color(0.15f, 0.2f, 0.85f)),
            new("Pool", new Color(0.2f, 0.35f, 0.9f), new Color(1f, 1f, 1f, 0.4f),
                new Color(0.06f, 0.08f, 0.3f), new Color(1f, 0.95f, 0.85f), new Color(0.9f, 0.2f, 0.2f)),
            new("Paper", Color.white, new Color(0f, 0f, 0f, 0.25f), Color.black, Color.white,
                new Color(0.9f, 0.2f, 0.2f)),
            new("Night", new Color(0.08f, 0.08f, 0.1f), new Color(1f, 1f, 1f, 0.2f), Color.black,
                new Color(0.95f, 0.9f, 0.7f), new Color(0.95f, 0.3f, 0.75f))
        };

        public ColorPalette(string name, Color background, Color line, Color dark, Color light, Color accent)
        {
            Name = name;
            Background = background;
            Line = line;
            Dark = dark;
            Light = light;
            Accent = accent;
        }

        public string Name { get; }

        /// <summary>Ground color behind everything.</summary>
        public Color Background { get; }

        /// <summary>Grid lines and other thin marks on the background.</summary>
        public Color Line { get; }

        /// <summary>Dark and light ends of the image Duotone / Threshold looks.</summary>
        public Color Dark { get; }

        public Color Light { get; }

        /// <summary>Second color of stripes and checkers.</summary>
        public Color Accent { get; }
    }
}
