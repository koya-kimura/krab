using Krab.Common.Timing;
using Krab.ShapeSynth.Core;
using Unite.Canvas;
using Unite.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimpleContainer;

namespace Krab.ShapeSynth.Samples
{
    /// <summary>Disposable integration sketch, not the production synth renderer.</summary>
    [RequireComponent(typeof(UCanvas))]
    public sealed class ShapeSynthSmokeTest : MonoBehaviour
    {
        [SerializeField] private Camera _outputCamera;
        [SerializeField, Min(0)] private float _attackBeats = 1;
        [SerializeField, Min(0)] private float _decayBeats = 0.5f;
        [SerializeField, Range(0, 1)] private float _sustainLevel = 0.35f;
        [SerializeField, Min(0)] private float _sustainBeats = 1;
        [SerializeField, Min(0)] private float _releaseBeats = 1;
        [SerializeField, Min(0.1f)] private float _pauseBeats = 1;
        [SerializeField, Range(0.1f, 0.95f)] private float _maxDiameterFraction = 0.7f;
        [SerializeField] private Color _circleColor = new(0.1f, 0.9f, 0.85f, 1);
        [SerializeField] private bool _autoRepeat = true;

        [Header("LFO comparison (sample mapping only)")]
        [SerializeField, Min(0)] private float _lfoCyclesPerBeat = 0.25f;
        [SerializeField, Range(0, 1)] private float _lfoPhaseOffset;
        [SerializeField] private LfoWaveform _lfoWaveform = LfoWaveform.Sine;
        [SerializeField, Range(0, 0.2f)] private float _lfoTravelFraction = 0.14f;
        [SerializeField, Range(0.05f, 0.3f)] private float _lfoDiameterFraction = 0.18f;
        [SerializeField] private Color _lfoColor = new(1, 0.5f, 0.15f, 1);

        private IBPMManager _bpm;
        private UCanvas _canvas;
        private AdsrInstance _envelope;
        private double _nextTriggerSeconds;
        private LfoInstance _lfo;
        private bool _lfoSettingsChanged;

        public double LfoSampleValue { get; private set; }
        public float LfoPositionX { get; private set; }
        public double LfoStartBpm => _lfo?.Time.StartBpm ?? 0;

        public double SampleValue { get; private set; }
        public float SampleDiameterPixels { get; private set; }
        public int TriggerCount { get; private set; }
        public int SubmittedFrameCount { get; private set; }
        public double StartBpm => _envelope?.Time.StartBpm ?? 0;

        [Inject]
        public void Construct(IBPMManager bpm)
        {
            _bpm = bpm;
        }

        private void Start()
        {
            _canvas = GetComponent<UCanvas>();
            if (!_outputCamera || _bpm == null)
            {
                Debug.LogError("ShapeSynth smoke scene requires an output Camera and SceneContainer BPM injection.", this);
                _canvas.enabled = false;
                enabled = false;
                return;
            }

            _canvas.CreateCanvas(DrawingMode.ScreenOverlay);
            Trigger();
            CreateLfo(_envelope.Time);
        }

        [ContextMenu("Trigger")]
        public void Trigger()
        {
            if (!Application.isPlaying || _bpm == null) return;
            var definition = new AdsrDefinition(_attackBeats, _decayBeats, _sustainLevel,
                _sustainBeats, _releaseBeats);
            var time = new VoiceTime(Time.timeAsDouble, _bpm.CurrentBpm);
            _envelope = new AdsrInstance(definition, time);
            _nextTriggerSeconds = time.StartSeconds +
                (definition.Attack + definition.Decay + definition.SustainTime + definition.Release +
                 _pauseBeats) * 60 / time.StartBpm;
            TriggerCount++;
        }

        private void OnValidate() => _lfoSettingsChanged = true;

        [ContextMenu("Restart LFO")]
        public void RestartLfo()
        {
            if (Application.isPlaying && _bpm != null)
                CreateLfo(new VoiceTime(Time.timeAsDouble, _bpm.CurrentBpm));
        }

        private void CreateLfo(VoiceTime time)
        {
            _lfo = new LfoInstance(new LfoDefinition(_lfoCyclesPerBeat, _lfoPhaseOffset, _lfoWaveform), time);
            _lfoSettingsChanged = false;
        }

        private void Update()
        {
            if (_envelope == null) return;
            var now = Time.timeAsDouble;
            var keyboard = Keyboard.current;
            if ((_autoRepeat && now >= _nextTriggerSeconds) ||
                (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)) Trigger();
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) _envelope.Release(now);
            if (keyboard != null && keyboard.lKey.wasPressedThisFrame) RestartLfo();
            // Inspector edits keep the LFO's captured start time/BPM; they may jump phase.
            if (_lfoSettingsChanged) CreateLfo(_lfo.Time);

            SampleValue = _envelope.Evaluate(now);
            // Use the output camera's dimensions, not UCanvas's last-rendered camera (Scene View etc.).
            var width = _outputCamera.pixelWidth;
            var height = _outputCamera.pixelHeight;
            var paneSize = Mathf.Min(width * 0.5f, height);
            SampleDiameterPixels = (float)SampleValue * _maxDiameterFraction * paneSize;
            LfoSampleValue = _lfo.Evaluate(now);
            LfoPositionX = width * 0.75f + (float)LfoSampleValue * width * _lfoTravelFraction;
            _canvas.ResetMatrix();
            _canvas.NoStroke();
            _canvas.Fill(_circleColor);
            if (SampleDiameterPixels > 0)
                _canvas.Circle(width * 0.25f, height * 0.5f, SampleDiameterPixels);
            _canvas.Fill(_lfoColor);
            _canvas.Circle(LfoPositionX, height * 0.5f, paneSize * _lfoDiameterFraction);
            SubmittedFrameCount++;
        }
    }
}
