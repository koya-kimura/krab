using System;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance
{
    /// <summary>
    /// One switchable text layout with its settings in Prefs. Lengths are reference pixels (1080 tall);
    /// speeds are reference pixels per beat.
    /// </summary>
    public abstract class TextLayer
    {
        protected const float MinSize = 4f;
        protected const float MaxSize = 400f;

        private readonly PrefsBool _enabled;
        private readonly PrefsString _text;
        private readonly PrefsFloat _size;

        protected TextLayer(string name, bool enabled, string text, float size)
        {
            Name = name;
            _enabled = new PrefsBool(Key("Enabled"), enabled);
            _text = new PrefsString(Key("Text"), text);
            _size = new PrefsFloat(Key("Size"), size);
        }

        public string Name { get; }

        public bool Enabled
        {
            get => _enabled.Get();
            set => _enabled.Set(value);
        }

        public string Text
        {
            get => _text.Get();
            set => _text.Set(value);
        }

        /// <summary>Why the last draw was skipped (an invalid setting), or null.</summary>
        public string Error { get; private set; }

        public Element CreateElement()
        {
            return UI.Fold(Name, UI.Column(
                _enabled.CreateElement("Enabled"),
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", MinSize, MaxSize),
                CreateSettings(),
                UI.Label(() => Error == null ? string.Empty : $"Skipped: {Error}")));
        }

        public void Draw(in LayerContext context)
        {
            if (!Enabled || string.IsNullOrEmpty(Text)) return;

            // Settings typed into the UI can be out of range; skip the layer and say why instead of logging every frame.
            try
            {
                DrawLayer(context);
                Error = null;
            }
            catch (ArgumentException exception)
            {
                Error = exception.Message;
            }
        }

        protected string Key(string setting) => $"TypeLayout {Name} {setting}";

        protected double Size(in LayerContext context) => context.Frame.ToPixels(_size.Get());

        protected abstract Element CreateSettings();

        protected abstract void DrawLayer(in LayerContext context);
    }
}
