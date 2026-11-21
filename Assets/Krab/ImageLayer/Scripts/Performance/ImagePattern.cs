using System;
using System.Collections.Generic;
using Krab.ImageLayer.Unity.Library;
using Krab.TypeLayout.Core.Text;
using Krab.TypeLayout.Performance;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance
{
    /// <summary>
    /// An image arrangement written in code. The settings UI picks the set, the look and the pattern's own
    /// parameters, which are saved in Prefs. Lengths are reference pixels (1080 tall).
    /// </summary>
    public abstract class ImagePattern
    {
        private const float WindowWidth = 320f;
        private const float LauncherWidth = 64f;
        private const int DescriptionLineLength = 52;
        private const float ThresholdSoftness = 0.03f;

        private static readonly string[] LookNames = Enum.GetNames(typeof(ImageLook));

        private readonly PrefsBool _enabled;
        private readonly PrefsString _set;
        private readonly PrefsInt _look;
        private readonly PrefsFloat _framesPerBeat;

        private IImageSetCatalog _catalog;

        protected ImagePattern(string name, string description, string defaultSet, ImageLook defaultLook,
            bool enabled = false)
        {
            Name = name;
            Description = description;
            _enabled = new PrefsBool(Key("Enabled"), enabled);
            _set = new PrefsString(Key("Set"), defaultSet);
            _look = new PrefsInt(Key("Look"), (int)defaultLook);
            _framesPerBeat = new PrefsFloat(Key("Frames Per Beat"), 8);
        }

        public string Name { get; }

        public string Description { get; }

        public bool Enabled
        {
            get => _enabled.Get();
            set => _enabled.Set(value);
        }

        public string SetName
        {
            get => _set.Get();
            set => _set.Set(value);
        }

        public ImageLook Look
        {
            get => (ImageLook)Math.Clamp(_look.Get(), 0, LookNames.Length - 1);
            set => _look.Set((int)value);
        }

        /// <summary>Why the last draw was skipped (missing set, out-of-range parameter), or null.</summary>
        public string Error { get; private set; }

        public void Bind(IImageSetCatalog catalog)
        {
            _catalog = catalog;
        }

        /// <summary>One row: the on/off toggle and a button opening this pattern's parameter window.</summary>
        public Element CreateElement()
        {
            var window = UI.Window(Name, UI.Page(
                    UI.Label(UiText.Wrap(Description, DescriptionLineLength)),
                    _enabled.CreateElement("Enabled"),
                    CreateSetDropdown(),
                    UI.Dropdown("Look", () => (int)Look, value => Look = (ImageLook)value, LookNames),
                    _framesPerBeat.CreateSlider("Frames Per Beat", 0f, 30f),
                    CreateParameters(),
                    UI.DynamicElementIf(() => Error != null,
                        () => UI.HelpBox(UI.Label(() => $"Skipped: {Error}"), HelpBoxType.Warning))))
                .SetWidth(WindowWidth);
            return UI.Row(
                _enabled.CreateElement(Name),
                UI.WindowLauncher(UI.Label(() => Error == null ? "Edit" : "Edit !"), window).SetWidth(LauncherWidth));
        }

        public void Draw(in ImagePatternContext context)
        {
            if (!Enabled) return;

            if (!context.Library.TryGet(SetName, out var set))
            {
                Error = $"Set '{SetName}' is not in library '{context.Library.Name}'.";
                return;
            }

            var canvas = context.Canvas;
            canvas.Push();
            try
            {
                ApplyLook(context);
                DrawPattern(context, set);
                Error = null;
            }
            catch (ArgumentException exception)
            {
                // Parameters typed into the UI can be out of range; skip and show why instead of logging every frame.
                Error = exception.Message;
            }
            finally
            {
                canvas.Pop();
            }
        }

        protected string Key(string setting) => $"ImageLayer {Name} {setting}";

        /// <summary>Frame of a clip on the beat clock; offset (in frames) lets copies run out of step.</summary>
        protected ImageFrame Frame(ImageClip clip, in ImagePatternContext context, double offset = 0)
        {
            return clip.AtPosition(context.Beats * _framesPerBeat.Get() + offset);
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

        protected abstract Element CreateParameters();

        protected abstract void DrawPattern(in ImagePatternContext context, ImageSet set);

        private void ApplyLook(in ImagePatternContext context)
        {
            var canvas = context.Canvas;
            switch (Look)
            {
                case ImageLook.Duotone:
                    canvas.Duotone(context.Dark, context.Light);
                    break;
                case ImageLook.Threshold:
                    canvas.Duotone(context.Dark, context.Light);
                    canvas.Threshold(context.ThresholdLevel, ThresholdSoftness);
                    break;
            }
        }

        private Element CreateSetDropdown()
        {
            int builtVersion = -1;
            return UI.DynamicElementOnTrigger(_ => _catalog != null && _catalog.Version != builtVersion, () =>
            {
                builtVersion = _catalog?.Version ?? -1;
                var names = new List<string>(_catalog?.SetNames ?? Array.Empty<string>());
                if (!names.Contains(SetName)) names.Insert(0, SetName);

                return UI.Dropdown("Set", () => SetName, value => SetName = value, names);
            });
        }
    }
}
