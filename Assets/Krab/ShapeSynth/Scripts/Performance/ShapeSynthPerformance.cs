using System;
using System.Collections.Generic;
using System.Linq;
using Krab.Common.Timing;
using Krab.Common.Views;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using Krab.ShapeSynth.Unity.Persistence;
using Krab.ShapeSynth.Unity.Runtime;
using RosettaUI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimpleContainer;

namespace Krab.ShapeSynth.Performance
{
    /// <summary>One performance input/UI boundary. Immutable Core snapshots are rebuilt only on Trigger.</summary>
    [RequireComponent(typeof(ShapeSynthController))]
    [UiTarget("ShapeSynth/Performance", "Performance", 200)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom("Krab.ShapeSynth.Samples.Performance")]
    public sealed partial class ShapeSynthPerformance : MonoBehaviour, IUiTarget
    {
        private static readonly (Key Key, string Id)[] KeyBindings = PresetCatalog.Entries.Select(preset =>
            ((Key)Enum.Parse(typeof(Key), preset.Key.ToString().ToUpperInvariant()), preset.Id)).ToArray();

        private readonly Dictionary<string, int> _nextSeeds = new(StringComparer.Ordinal);

        private IBPMManager _bpm;
        private ShapeSynthController _controller;
        private PerformanceSettingsStore _store;
        private string _status = "Edit Modulators, then Trigger a preset. Changes apply to new Voices.";

        private void Awake()
        {
            EnsureSettings();
        }

        private void Start()
        {
            _controller = GetComponent<ShapeSynthController>();
            if (_bpm == null)
            {
                Debug.LogError($"{nameof(ShapeSynthPerformance)} requires BPM injection.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (RosettaUIRoot.IsFocused || Keyboard.current == null) return;

            foreach (var binding in KeyBindings)
            {
                if (Keyboard.current[binding.Key].wasPressedThisFrame)
                {
                    TriggerPreset(binding.Id);
                }
            }
        }

        public PerformanceSettings Settings { get; private set; }

        public VoiceInstance LastVoice { get; private set; }

        public string Status => _status;

        public string StoragePath => _store.Path;

        public int VoiceCount => _controller ? _controller.Collection.Voices.Count : 0;

        [Inject]
        public void Construct(IBPMManager bpm)
        {
            _bpm = bpm;
        }

        public VoiceInstance TriggerFx01()
        {
            return TriggerPreset("fx01");
        }

        public VoiceInstance TriggerPreset(string presetId)
        {
            try
            {
                EnsureSettings();
                if (!_nextSeeds.TryGetValue(presetId, out var seed))
                {
                    seed = PresetCatalog.SeedFor(Settings, presetId);
                }

                var target = PresetCatalog.Build(Settings, presetId, seed);
                LastVoice = _controller.Spawn(target, _bpm.CurrentBpm, seed);
                _nextSeeds[presetId] = unchecked(seed + 1);
                _status = $"{presetId}: seed {seed}. Active Voices: {VoiceCount}";
                return LastVoice;
            }
            catch (Exception e)
            {
                _status = "Trigger rejected: " + e.Message;
                return null;
            }
        }

        public bool SaveSettings()
        {
            try
            {
                EnsureSettings();
                _store.Save(Settings);
                _status = "Saved ShapeSynth settings: " + _store.Path;
                return true;
            }
            catch (Exception e)
            {
                _status = "Save rejected: " + e.Message;
                return false;
            }
        }

        public bool ReloadSettings()
        {
            try
            {
                // Validate completely before replacing the active edit model.
                var loaded = _store.Load();
                Settings = loaded;
                _nextSeeds.Clear();
                _status = "Reloaded ShapeSynth settings. Existing Voices are unchanged.";
                return true;
            }
            catch (Exception e)
            {
                _status = "Reload rejected: " + e.Message;
                return false;
            }
        }

        private void EnsureSettings()
        {
            if (Settings != null) return;

            _store = new PerformanceSettingsStore();
            Settings = PerformanceSettings.CreateForPerformance();
            if (!_store.Exists) return;

            try
            {
                Settings = _store.Load();
                _status = "Loaded ShapeSynth settings.";
            }
            catch (Exception e)
            {
                _status = "Could not load saved settings: " + e.Message;
                Debug.LogWarning(_status, this);
            }
        }

        private void ResetSeed(string id)
        {
            _nextSeeds.Remove(id);
        }
    }
}
