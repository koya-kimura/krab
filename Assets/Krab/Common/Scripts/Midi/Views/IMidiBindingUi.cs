using RosettaUI;

namespace Krab.Common.Midi.Views
{
    /// <summary>Creates a MIDI settings launcher beside a parameter.</summary>
    public interface IMidiBindingUi
    {
        Element CreateButton(string paramId, string displayName);
    }
}
