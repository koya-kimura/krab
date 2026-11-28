using Krab.Common.Midi.Bindings;
using Krab.Common.Midi.Views;
using Krab.Common.Timing;
using Krab.Common.Views;
using Krab.ShapeSynth.Core.Voices;
using Krab.ShapeSynth.Unity;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.ShapeSynth.Performance
{
    /// <summary>
    /// Plays the presets saved from the Preset Editor: one row per preset with a Trigger button and a MIDI
    /// binding. Each Trigger reloads and compiles the file, so saved edits apply to the next Voice.
    /// </summary>
    [RequireComponent(typeof(ShapeSynthController))]
    [UiTarget("ShapeSynth/Performance", "Performance", 200)]
    public sealed partial class ShapeSynthPerformance : MonoBehaviour, IUiTarget
    {
        private IBPMManager _bpm;
        private ShapeSynthController _controller;

        private void Start()
        {
            _controller = GetComponent<ShapeSynthController>();
            if (_bpm == null)
            {
                Debug.LogError($"{nameof(ShapeSynthPerformance)} requires BPM injection.", this);
                enabled = false;
                return;
            }

            // Load the saved presets now so their MIDI triggers work before the UI is opened.
            InitializeLoopers();
            RefreshUserPresets();
        }

        private void Update()
        {
            RefreshWhenFilesChange();
            UpdateLoopers();
        }

        public VoiceInstance LastVoice { get; private set; }

        public int VoiceCount => _controller ? _controller.Collection.Voices.Count : 0;

        [Inject]
        public void Construct(IBPMManager bpm, IMidiBindingProvider midi, IMidiBindingUi midiUi)
        {
            _bpm = bpm;
            _midiBindings = midi.Bindings;
            _midiUi = midiUi;
        }

        public Element CreateElement()
        {
            return UI.ScrollViewVerticalAndHorizontal(null, null,
                UI.Column(BuildLoopers(), BuildUserPresets()).SetWidth(700).SetFlexShrink(0));
        }
    }
}
