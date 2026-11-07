namespace Krab.Common.Midi.Bindings
{
    /// <summary>Scene components inject this interface to register their MIDI actions.</summary>
    public interface IMidiBindingProvider
    {
        MidiBindingManager Bindings { get; }
    }
}
