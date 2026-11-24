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
    /// Two MIDI actions per saved user preset: Trigger (Momentary, one Voice per press that releases itself)
    /// and Hold (OneShot, the Voice stays at Sustain while the pad is down and is released when it goes up).
    /// Bindings are stored by action ID, which uses the preset ID, so they survive renames and re-registration.
    /// </summary>
    public sealed partial class ShapeSynthPerformance
    {
        private const string MidiActionPrefix = "ShapeSynth.UserPreset.";
        private const string MidiHoldActionPrefix = "ShapeSynth.UserPresetHold.";

        private readonly Dictionary<string, Param<bool>> _midiTriggers = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Param<bool>> _midiHolds = new(StringComparer.Ordinal);

        private MidiBindingManager _midiBindings;
        private IMidiBindingUi _midiUi;

        private void OnDestroy()
        {
            DetachLooperMidi();
            if (_midiBindings == null) return;

            foreach (var presetId in _midiTriggers.Keys)
            {
                _midiBindings.UnregisterAction(MidiActionId(presetId));
            }

            foreach (var presetId in _midiHolds.Keys)
            {
                _midiBindings.UnregisterAction(MidiHoldActionId(presetId));
            }

            _midiTriggers.Clear();
            _midiHolds.Clear();
        }

        private static string MidiActionId(string presetId) => MidiActionPrefix + presetId;

        private static string MidiHoldActionId(string presetId) => MidiHoldActionPrefix + presetId;

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

            foreach (var presetId in _midiHolds.Keys.Where(id => !listed.Contains(id)).ToList())
            {
                _midiBindings.UnregisterAction(MidiHoldActionId(presetId));
                _midiHolds.Remove(presetId);
                EndHold(presetId);
            }

            foreach (var presetId in listed.Where(id => !_midiHolds.ContainsKey(id)))
            {
                var held = new Param<bool>(false);
                // OneShot follows the pad: true from press to release, also reset to false when MIDI is reset.
                held.Changed += value =>
                {
                    if (value)
                        StartHold(presetId);
                    else
                        EndHold(presetId);
                };
                _midiBindings.RegisterOneShot(MidiHoldActionId(presetId), held);
                _midiHolds.Add(presetId, held);
            }
        }

        private Element MidiTriggerButton(string presetId, string presetName)
        {
            return _midiUi == null ? UI.Space() : _midiUi.CreateButton(MidiActionId(presetId), presetName);
        }

        private Element MidiHoldButton(string presetId, string presetName)
        {
            if (_midiUi == null) return UI.Space();

            // Both launchers read "MIDI", so the Hold one gets its own label.
            return UI.Row(UI.Label("Hold").SetWidth(40),
                _midiUi.CreateButton(MidiHoldActionId(presetId), presetName + " (Hold)"));
        }
    }
}
