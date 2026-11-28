using System;
using System.Collections.Generic;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;
using Krab.TypeLayout.Core.Text;

namespace Krab.ImageLayer.Performance
{
    /// <summary>
    /// An image arrangement written in code. It holds no settings: layout presets supply the parameter values and
    /// the image radio supplies the set, so one instance serves every preset of its kind. Lengths are reference
    /// pixels (1080 tall).
    /// </summary>
    public abstract class ImagePattern
    {
        private const int MaxSeed = 9999;

        private readonly List<PatternParameter> _parameters = new();

        protected ImagePattern(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>Kind name stored in layout presets.</summary>
        public string Name { get; }

        public string Description { get; }

        public IReadOnlyList<PatternParameter> Parameters => _parameters;

        /// <summary>Draws the pattern. Returns why it was skipped (an out-of-range value), or null.</summary>
        public string Draw(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            try
            {
                DrawPattern(context, set, values);
                return null;
            }
            catch (ArgumentException exception)
            {
                // Skip and report instead of logging every frame.
                return exception.Message;
            }
        }

        protected PatternParameter Float(string name, double defaultValue, double min, double max,
            string label = null)
        {
            return Add(PatternParameter.Float(name, defaultValue, min, max, label));
        }

        protected PatternParameter Int(string name, int defaultValue, int min, int max, string label = null)
        {
            return Add(PatternParameter.Int(name, defaultValue, min, max, label));
        }

        protected PatternParameter Bool(string name, bool defaultValue, string label = null)
        {
            return Add(PatternParameter.Bool(name, defaultValue, label));
        }

        protected PatternParameter Seed(int defaultValue)
        {
            return Add(PatternParameter.Int("Seed", defaultValue, 0, MaxSeed));
        }

        /// <summary>Frame of a clip on the beat clock; offset (in frames) lets copies run out of step.</summary>
        protected static ImageFrame Frame(ImageClip clip, in ImagePatternContext context, double offset = 0)
        {
            return clip.AtPosition(context.Beats * context.FramesPerBeat + offset);
        }

        /// <summary>Seeded random value in [0, 1) that stays the same for the same seed, slot and step.</summary>
        protected static double Random01(int seed, int slot, long step)
        {
            return (TextPicker.Hash(seed, slot, step) >> 11) * (1.0 / (1UL << 53));
        }

        protected static int RandomIndex(int seed, int slot, long step, int count)
        {
            return (int)(TextPicker.Hash(seed, slot, step) % (ulong)Math.Max(1, count));
        }

        /// <summary>0 → 1 → 0 once per period, for swings and breathing.</summary>
        protected static float Zigzag(double beats, double period)
        {
            double phase = beats / period % 1.0;
            if (phase < 0) phase += 1.0;

            return (float)(phase < 0.5 ? phase * 2 : 2 - phase * 2);
        }

        protected static void RequirePositive(float value, string name)
        {
            if (!(value > 0)) throw new ArgumentException($"{name} must be greater than 0.");
        }

        protected abstract void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values);

        private PatternParameter Add(PatternParameter parameter)
        {
            _parameters.Add(parameter);
            return parameter;
        }
    }
}
