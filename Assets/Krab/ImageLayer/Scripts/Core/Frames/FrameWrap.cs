namespace Krab.ImageLayer.Core.Frames
{
    /// <summary>What happens when a playback position runs past the ends of a clip.</summary>
    public enum FrameWrap
    {
        /// <summary>Stays on the first or last frame.</summary>
        Clamp,

        /// <summary>Starts over from the first frame.</summary>
        Loop,

        /// <summary>Plays back and forth without repeating the end frames.</summary>
        PingPong,
    }
}
