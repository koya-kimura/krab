using System;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance
{
    /// <summary>
    /// A text layout whose arrangement and motion are written in code. Only its parameters (text, size, speed...)
    /// are exposed to the settings UI and saved in Prefs. Lengths are reference pixels (1080 tall).
    /// </summary>
    public abstract class TypePattern
    {
        private const float WindowWidth = 320f;
        private const float LauncherWidth = 64f;

        // Characters per description line that fit the fixed window width.
        private const int DescriptionLineLength = 52;

        private readonly PrefsBool _enabled;

        private string _lettersSource;
        private string _letters;

        protected TypePattern(string name, string description, bool enabled = false)
        {
            Name = name;
            Description = description;
            _enabled = new PrefsBool(Key("Enabled"), enabled);
        }

        public string Name { get; }

        public string Description { get; }

        public bool Enabled
        {
            get => _enabled.Get();
            set => _enabled.Set(value);
        }

        /// <summary>Why the last draw was skipped (an out-of-range parameter), or null.</summary>
        public string Error { get; private set; }

        /// <summary>One row: the on/off toggle and a button opening this pattern's parameter window.</summary>
        public Element CreateElement()
        {
            var window = UI.Window(Name, UI.Page(
                    UI.Label(UiText.Wrap(Description, DescriptionLineLength)),
                    _enabled.CreateElement("Enabled"),
                    CreateParameters(),
                    UI.DynamicElementIf(() => Error != null,
                        () => UI.HelpBox(UI.Label(() => $"Skipped: {Error}"), HelpBoxType.Warning))))
                .SetWidth(WindowWidth);
            return UI.Row(
                _enabled.CreateElement(Name),
                UI.WindowLauncher(UI.Label(() => Error == null ? "Edit" : "Edit !"), window).SetWidth(LauncherWidth));
        }

        public void Draw(in PatternContext context)
        {
            if (!Enabled) return;

            // Parameters typed into the UI can be out of range; skip the pattern and show why instead of
            // logging every frame.
            try
            {
                DrawPattern(context);
                Error = null;
            }
            catch (ArgumentException exception)
            {
                Error = exception.Message;
            }
        }

        protected string Key(string setting) => $"TypeLayout {Name} {setting}";

        /// <summary>
        /// The text without spaces, for patterns that place single letters. Cached until the text changes.
        /// </summary>
        protected string Letters(string text)
        {
            if (text == _lettersSource) return _letters;

            _lettersSource = text;
            _letters = text?.Replace(" ", string.Empty) ?? string.Empty;
            return _letters;
        }

        protected abstract Element CreateParameters();

        protected abstract void DrawPattern(in PatternContext context);
    }
}
