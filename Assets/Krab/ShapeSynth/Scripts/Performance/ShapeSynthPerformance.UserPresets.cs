using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using Krab.ShapeSynth.Unity.Persistence;
using RosettaUI;

namespace Krab.ShapeSynth.Performance
{
    public sealed partial class ShapeSynthPerformance
    {
        private PresetRepository _presetRepository;
        private IReadOnlyList<PresetDocument> _userPresets = Array.Empty<PresetDocument>();
        private string _userPresetStatus = "Refresh to load saved presets.";
        private string _selectedUserPresetId;
        private int _userPresetRevision;
        private int _userSeed = 1234;

        public PresetRepository UserPresetRepository => _presetRepository ??= new PresetRepository();

        public void RefreshUserPresets()
        {
            try
            {
                _userPresets = UserPresetRepository.List(out var errors);
                _userPresetStatus = $"{_userPresets.Count} user presets. " + string.Join("; ", errors);
                _userPresetRevision++;
            }
            catch (Exception exception)
            {
                _userPresetStatus = exception.Message;
            }
        }

        public VoiceInstance TriggerUserPreset(string id)
        {
            try
            {
                // Reload and compile at Trigger, so another Scene's saved edits apply only to a new Voice.
                var document = UserPresetRepository.Load(id);
                var target = PresetCompiler.Compile(document);
                LastVoice = _controller.Spawn(target, _bpm.CurrentBpm, _userSeed);
                _userSeed = unchecked(_userSeed + 1);
                _userPresetStatus = "Triggered " + document.Name;
                return LastVoice;
            }
            catch (Exception exception)
            {
                _userPresetStatus = "User preset rejected: " + exception.Message;
                return null;
            }
        }

        private Element BuildUserPresets()
        {
            RefreshUserPresets();
            return UI.Fold("User Presets", UI.Column(
                UI.Button("Refresh User Presets", RefreshUserPresets),
                UI.Field("User seed", () => _userSeed, value => _userSeed = value),
                UI.DynamicElementOnStatusChanged(() => _userPresetRevision, _ => BuildUserPresetRows()),
                UI.Button("Trigger Selected User Preset", () => TriggerUserPreset(_selectedUserPresetId)),
                UI.Label(() => _userPresetStatus)));
        }

        private Element BuildUserPresetRows()
        {
            var rows = new List<Element>();
            foreach (var document in _userPresets)
            {
                var label = UI.Label(() => (_selectedUserPresetId == document.Id ? "> " : "") + document.Name);
                rows.Add(UI.Row(
                    UI.Button(label, () => _selectedUserPresetId = document.Id),
                    UI.Button("Trigger", () => TriggerUserPreset(document.Id))));
            }

            return UI.Column(rows);
        }
    }
}
