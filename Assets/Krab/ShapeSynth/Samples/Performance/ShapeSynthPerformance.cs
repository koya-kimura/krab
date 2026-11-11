using System;
using Krab.Common.Timing;
using Krab.Common.Views;
using Krab.ShapeSynth.Core;
using Krab.ShapeSynth.Unity;
using RosettaUI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimpleContainer;

namespace Krab.ShapeSynth.Samples.Performance
{
    /// <summary>One performance input/UI boundary. Immutable Core snapshots are rebuilt only on Trigger.</summary>
    [RequireComponent(typeof(ShapeSynthController))]
    [UiTarget("ShapeSynth/Performance", "Performance", 200)]
    public sealed partial class ShapeSynthPerformance : MonoBehaviour, IUiTarget
    {
        private IBPMManager _bpm;
        private ShapeSynthController _controller;
        private PerformanceSettingsStore _store;
        private int _nextSeed;
        private bool _seedInitialized;
        private string _status = "Edit Modulators, then Trigger FX01. Changes apply to new Voices.";
        public PerformanceSettings Settings { get; private set; }
        public VoiceInstance LastVoice { get; private set; }
        public string Status => _status;
        public string StoragePath => _store.Path;
        public int VoiceCount => _controller ? _controller.Collection.Voices.Count : 0;

        [Inject]
        public void Construct(IBPMManager bpm) => _bpm = bpm;

        private void Awake() => EnsureSettings();
        private void Start()
        {
            _controller = GetComponent<ShapeSynthController>();
            if (_bpm == null) { Debug.LogError("ShapeSynth Performance requires BPM injection.", this); enabled = false; }
        }

        private void EnsureSettings()
        {
            if (Settings != null) return;
            _store = new PerformanceSettingsStore();
            Settings = PerformanceSettings.CreateDefault();
            if (_store.Exists)
            {
                try { Settings = _store.Load(); _status = "Loaded ShapeSynth settings."; }
                catch (Exception e) { _status = "Could not load saved settings: " + e.Message; Debug.LogWarning(_status, this); }
            }
        }

        private void Update()
        {
            // Do not trigger while typing 'q' in a Modulator name or numeric field.
            if (!RosettaUIRoot.IsFocused && Keyboard.current?.qKey.wasPressedThisFrame == true) TriggerFx01();
        }

        public VoiceInstance TriggerFx01()
        {
            try
            {
                EnsureSettings();
                var library = Settings.BuildLibrary();
                var target = Fx01Preset.Build(library, Settings.Fx01);
                if (!_seedInitialized) { _nextSeed = Settings.Fx01.Seed; _seedInitialized = true; }
                var seed = _nextSeed;
                LastVoice = _controller.Spawn(target, _bpm.CurrentBpm, seed);
                _nextSeed = unchecked(_nextSeed + 1);
                _status = $"FX01: seed {seed}, {Settings.Fx01.CircleCount} circles. Active Voices: {VoiceCount}";
                return LastVoice;
            }
            catch (Exception e) { _status = "Trigger rejected: " + e.Message; return null; }
        }

        public bool SaveSettings()
        {
            try { EnsureSettings(); _store.Save(Settings); _status = "Saved ShapeSynth settings: " + _store.Path; return true; }
            catch (Exception e) { _status = "Save rejected: " + e.Message; return false; }
        }

        public bool ReloadSettings()
        {
            try
            {
                var loaded = _store.Load(); // Validate completely before replacing the active edit model.
                Settings = loaded;
                _seedInitialized = false;
                _status = "Reloaded ShapeSynth settings. Existing Voices are unchanged.";
                return true;
            }
            catch (Exception e) { _status = "Reload rejected: " + e.Message; return false; }
        }
    }
}
