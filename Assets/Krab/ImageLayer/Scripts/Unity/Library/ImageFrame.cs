using Krab.ImageLayer.Core.Geometry;
using UnityEngine;

namespace Krab.ImageLayer.Unity.Library
{
    /// <summary>
    /// One loaded frame: a texture holding only the opaque content (bottom-left aligned, padded for block
    /// compression) and where that content sat in the original PNG.
    /// </summary>
    public sealed class ImageFrame
    {
        public ImageFrame(Texture2D texture, TrimmedFrame trim)
        {
            Texture = texture;
            Trim = trim;
            if (texture != null && !trim.IsEmpty)
            {
                UvScale = new Vector2((float)trim.Content.Width / texture.width,
                    (float)trim.Content.Height / texture.height);
            }
        }

        /// <summary>Null for a fully transparent frame, which draws nothing.</summary>
        public Texture2D Texture { get; private set; }

        public TrimmedFrame Trim { get; }

        /// <summary>Part of the texture covered by the content; the rest is padding.</summary>
        public Vector2 UvScale { get; }

        public int SourceWidth => Trim.SourceWidth;

        public int SourceHeight => Trim.SourceHeight;

        public bool IsEmpty => Texture == null;

        internal void Release()
        {
            UnityObjects.Destroy(Texture);
            Texture = null;
        }
    }
}
