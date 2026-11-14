using Krab.Common.Timing;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Runtime;
using Krab.ShapeSynth.Unity.Rendering;
using Unite.Canvas;
using Unite.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimpleContainer;

namespace Krab.ShapeSynth.Samples
{
    /// <summary>Disposable integration sketch, not the production synth renderer.</summary>
    [RequireComponent(typeof(TargetCameraCanvas))]
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

        [Header("Parameter / Binding comparison")]
        [SerializeField] private float _bindingBase = 0.2f;
        [SerializeField] private float _bindingAdsrDepth = 0.65f;
        [SerializeField] private float _bindingLfoDepth = 0.15f;
        [SerializeField, Range(0.05f, 0.4f)] private float _bindingSizeFraction = 0.2f;

        private ParameterEvaluator _boundDiameter;
        private ParameterEvaluator _boundOpacity;
        private ParameterEvaluator _fixedDiameter;
        public double BoundDiameterPixels { get; private set; }
        public double BoundOpacity { get; private set; }

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

            ((TargetCameraCanvas)_canvas).Initialize(_outputCamera);
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
            RefreshBindings();
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
            RefreshBindings();
        }

        private void RefreshBindings()
        {
            if (_envelope == null || _lfo == null) return;
            // Both evaluators resolve slot 0 to this exact envelope, not another ADSR instance.
            var signals = new IModulator[] { _envelope, _lfo };
            _boundDiameter = new ParameterEvaluator(new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.MinDimension, _bindingSizeFraction), _bindingBase,
                new BindingDefinition(0, _bindingAdsrDepth), new BindingDefinition(1, _bindingLfoDepth)), signals);
            _boundOpacity = new ParameterEvaluator(new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute), 0, new BindingDefinition(0, 1)), signals);
            _fixedDiameter = new ParameterEvaluator(new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.MinDimension, 0.18), 1), signals);
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
            // Keep the original four modulation demos together in the left panel.
            var legacyWidth = width * 0.55f;
            var legacyOffset = width * 0.08f;
            var paneSize = Mathf.Min(legacyWidth * 0.5f, height * 0.42f);
            SampleDiameterPixels = (float)SampleValue * _maxDiameterFraction * paneSize;
            LfoSampleValue = _lfo.Evaluate(now);
            LfoPositionX = legacyOffset + legacyWidth * 0.75f + (float)LfoSampleValue * legacyWidth * _lfoTravelFraction;
            var context = new ReferenceContext(width, height);
            BoundDiameterPixels = _boundDiameter.Evaluate(now, context);
            BoundOpacity = _boundOpacity.Evaluate(now, context);
            var fixedDiameter = _fixedDiameter.Evaluate(now, context);
            // Through this ScreenOverlay -> RT -> Preview path, smaller Y appears higher.
            // Keep this display placement separate from Reference evaluation and future local transforms.
            var upperY = height * 0.38f;
            var lowerY = height * 0.63f;
            _canvas.ResetMatrix();
            _canvas.NoStroke();
            _canvas.Fill(_circleColor);
            if (SampleDiameterPixels > 0)
                _canvas.Circle(legacyOffset + legacyWidth * 0.25f, upperY, SampleDiameterPixels);
            _canvas.Fill(_lfoColor);
            _canvas.Circle(LfoPositionX, upperY, paneSize * _lfoDiameterFraction);
            // Geometry/opacity constraints belong to the drawing boundary, not ParameterEvaluator.
            _canvas.Fill(new Color(0.7f, 0.3f, 1, 1));
            if (BoundDiameterPixels > 0)
                _canvas.Circle(legacyOffset + legacyWidth * 0.25f, lowerY, (float)BoundDiameterPixels);
            _canvas.Fill(new Color(0.2f, 1, 0.4f, Mathf.Clamp01((float)BoundOpacity)));
            _canvas.Circle(legacyOffset + legacyWidth * 0.75f, lowerY, (float)fixedDiameter);
            SubmittedFrameCount++;
        }
    }
}
