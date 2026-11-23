using System;

namespace Krab.ImageLayer.Core.Presets
{
    /// <summary>
    /// A choice on the image radio: which set (and optionally which one clip) to draw, with its color look and
    /// playback speed. Layout presets decide where the images go.
    /// </summary>
    public sealed class ImageEntry
    {
        public const int AllClips = -1;
        public const double DefaultFramesPerBeat = 8;
        public const double MaxFramesPerBeat = 60;

        private string _name;
        private string _setName;
        private int _clip;
        private string _look;
        private double _framesPerBeat;

        public ImageEntry(string id, string name, string setName, int clip = AllClips, string look = null,
            double framesPerBeat = DefaultFramesPerBeat)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An entry id is required.", nameof(id));

            Id = id;
            Name = name;
            SetName = setName;
            Clip = clip;
            Look = look;
            FramesPerBeat = framesPerBeat;
        }

        /// <summary>Stable key for files and MIDI bindings; never changes after creation.</summary>
        public string Id { get; }

        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? Id : value.Trim();
        }

        public string SetName
        {
            get => _setName;
            set => _setName = value?.Trim() ?? string.Empty;
        }

        /// <summary>Index of the only clip to use, or <see cref="AllClips"/>.</summary>
        public int Clip
        {
            get => _clip;
            set => _clip = Math.Max(AllClips, value);
        }

        /// <summary>Name of the color look; interpreted by the renderer, empty for its default.</summary>
        public string Look
        {
            get => _look;
            set => _look = value?.Trim() ?? string.Empty;
        }

        public double FramesPerBeat
        {
            get => _framesPerBeat;
            set => _framesPerBeat = double.IsNaN(value) || double.IsInfinity(value)
                ? DefaultFramesPerBeat
                : Math.Clamp(value, 0, MaxFramesPerBeat);
        }

        public ImageEntry Duplicate(string id, string name) => new(id, name, SetName, Clip, Look, FramesPerBeat);
    }
}
