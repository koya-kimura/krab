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
        private readonly PrefsBool _enabled;

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

        public Element CreateElement()
        {
            return UI.Fold(_enabled.CreateElement(Name), new[]
            {
                UI.Label(Description),
                CreateParameters(),
                UI.DynamicElementIf(() => Error != null,
                    () => UI.HelpBox(UI.Label(() => $"Skipped: {Error}"), HelpBoxType.Warning))
            });
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

        protected abstract Element CreateParameters();

        protected abstract void DrawPattern(in PatternContext context);
    }
}
