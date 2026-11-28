namespace Krab.Common.Midi.Core
{
    public readonly struct MidiMessage
    {
        public MidiControl Control { get; }
        public float Value { get; }
        public bool IsPressed { get; }
        public bool IsReleased { get; }

        public MidiMessage(MidiControl control, float value, bool isPressed, bool isReleased)
        {
            Control = control;
            Value = value;
            IsPressed = isPressed;
            IsReleased = isReleased;
        }
    }
}
