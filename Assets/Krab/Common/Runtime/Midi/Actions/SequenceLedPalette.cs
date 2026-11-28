using UnityEngine;

namespace Krab.Common.Midi.Actions
{
    public readonly struct SequenceLedPalette
    {
        public SequenceLedPalette(Color currentColor, float disabledBrightness = MidiLedColor.InactiveBrightness)
        {
            CurrentColor = currentColor;
            DisabledBrightness = Mathf.Clamp01(disabledBrightness);
        }

        public Color CurrentColor { get; }
        public float DisabledBrightness { get; }
        public static SequenceLedPalette Default => new(Color.yellow);
    }
}
