using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using Krab.ShapeSynth.Unity.Persistence;
using RosettaUI;
using UnityEngine;

namespace Krab.ShapeSynth.Performance
{
    public sealed partial class ShapeSynthPerformance
    {
        private const double RefreshIntervalSeconds = 1;
        private const float NameColumnWidth = 180;

        private static readonly Color ErrorColor = new(1f, 0.45f, 0.4f);

        private PresetRepository _presetRepository;
        private IReadOnlyList<PresetDocument> _userPresets = Array.Empty<PresetDocument>();
        private string _filesSignature;
        private string _rowsSignature;
        private string _error;
        private int _userPresetRevision;
        private int _seed = 1234;
        private double _nextRefreshCheck;

        public PresetRepository UserPresetRepository => _presetRepository ??= new PresetRepository();

        public IReadOnlyList<PresetDocument> UserPresets => _userPresets;

        public string Error => _error;

        /// <summary>Lists the saved presets by name; unreadable files are reported on the error line.</summary>
        public void RefreshUserPresets()
        {
            _filesSignature = FilesSignature();
            try
            {
                _userPresets = UserPresetRepository.List(out var errors)
                    .OrderBy(document => document.Name, StringComparer.Ordinal)
                    .ThenBy(document => document.Id, StringComparer.Ordinal)
                    .ToList();
                _error = errors.Count == 0 ? null : string.Join("; ", errors);
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }

            // Rows hold MIDI binding windows, so rebuild them only when presets or their names change, not on
            // every save of a preset's contents.
            var rows = string.Join("\n", _userPresets.Select(document => document.Id + "\t" + document.Name));
            if (rows == _rowsSignature) return;

            _rowsSignature = rows;
            _userPresetRevision++;
            SyncMidiTriggers();
        }

        public VoiceInstance TriggerUserPreset(string id)
        {
            try
            {
                // Reload and compile at Trigger, so the Preset Editor's saved edits apply only to a new Voice.
                var document = UserPresetRepository.Load(id);
                var target = PresetCompiler.Compile(document);
                LastVoice = _controller.Spawn(target, _bpm.CurrentBpm, _seed);
                // Each Trigger gets a new seed, so Rand differs from the previous Voice.
                _seed = unchecked(_seed + 1);
                return LastVoice;
            }
            catch (Exception exception)
            {
                _error = "Trigger failed: " + exception.Message;
                return null;
            }
        }

        /// <summary>
        /// Presets are saved from the Preset Editor, possibly in another Scene, so the list follows the folder:
        /// about once a second it compares file names and write times and reloads only when they changed.
        /// </summary>
        private void RefreshWhenFilesChange()
        {
            var now = Time.unscaledTimeAsDouble;
            if (now < _nextRefreshCheck) return;

            _nextRefreshCheck = now + RefreshIntervalSeconds;
            if (FilesSignature() != _filesSignature) RefreshUserPresets();
        }

        private string FilesSignature()
        {
            var directory = UserPresetRepository.DirectoryPath;
            if (!Directory.Exists(directory)) return string.Empty;

            var files = Directory.GetFiles(directory, "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            var signature = new StringBuilder();
            foreach (var file in files)
            {
                signature.Append(file).Append('|').Append(File.GetLastWriteTimeUtc(file).Ticks).Append('\n');
            }

            return signature.ToString();
        }

        private Element BuildUserPresets()
        {
            return UI.Column(
                UI.DynamicElementOnStatusChanged(() => _userPresetRevision, _ => BuildUserPresetRows()),
                UI.DynamicElementIf(() => _error != null, () => UI.Label(() => _error).SetColor(ErrorColor)));
        }

        private Element BuildUserPresetRows()
        {
            if (_userPresets.Count == 0) return UI.Label("No presets yet. Save one in the Preset Editor.");

            return UI.Column(_userPresets.Select(document => UI.Row(
                UI.Label(document.Name).SetWidth(NameColumnWidth),
                UI.Button("Trigger", () => TriggerUserPreset(document.Id)),
                MidiTriggerButton(document.Id, document.Name))));
        }
    }
}
