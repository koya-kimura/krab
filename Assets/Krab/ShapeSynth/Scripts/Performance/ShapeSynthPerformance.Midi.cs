using System;
using System.Collections.Generic;
using System.Linq;
using Krab.Common.Midi.Bindings;
using Krab.Common.Midi.Core;
using Krab.Common.Midi.Views;
using RosettaUI;

namespace Krab.ShapeSynth.Performance
{
    /// <summary>
    /// One press-to-trigger MIDI action per saved user preset, the same pattern as the MIDI sample. Bindings are
    /// stored by action ID, which uses the preset ID, so they survive renames and re-registration.
    /// </summary>
    public sealed partial class ShapeSynthPerformance
    {
        private const string MidiActionPrefix = "ShapeSynth.UserPreset.";

        private readonly Dictionary<string, Param<bool>> _midiTriggers = new(StringComparer.Ordinal);

        private MidiBindingManager _midiBindings;
        private IMidiBindingUi _midiUi;

        private void OnDestroy()
        {
            UnregisterLooperMidi();
            if (_midiBindings == null) return;

            foreach (var presetId in _midiTriggers.Keys)
            {
                _midiBindings.UnregisterAction(MidiActionId(presetId));
            }

            _midiTriggers.Clear();
        }

        private static string MidiActionId(string presetId) => MidiActionPrefix + presetId;

        /// <summary>Registers actions for newly listed presets and removes those whose file is gone.</summary>
        private void SyncMidiTriggers()
        {
            if (_midiBindings == null) return;

            var listed = new HashSet<string>(_userPresets.Select(document => document.Id), StringComparer.Ordinal);
            foreach (var presetId in _midiTriggers.Keys.Where(id => !listed.Contains(id)).ToList())
            {
                _midiBindings.UnregisterAction(MidiActionId(presetId));
                _midiTriggers.Remove(presetId);
            }

            foreach (var presetId in listed.Where(id => !_midiTriggers.ContainsKey(id)))
            {
                var pressed = new Param<bool>(false);
                // Momentary is true for the frame of the press only, so each press triggers exactly once.
                pressed.Changed += value =>
                {
                    if (value) TriggerUserPreset(presetId);
                };
                _midiBindings.RegisterMomentary(MidiActionId(presetId), pressed);
                _midiTriggers.Add(presetId, pressed);
            }
        }

        private Element MidiTriggerButton(string presetId, string presetName)
        {
            return _midiUi == null ? UI.Space() : _midiUi.CreateButton(MidiActionId(presetId), presetName);
        }
    }
}
