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

        private IBPMManager _bpm;
        private UCanvas _canvas;
        private AdsrInstance _envelope;
        private double _nextTriggerSeconds;

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

        private void Update()
        {
            if (_envelope == null) return;
            var now = Time.timeAsDouble;
            var keyboard = Keyboard.current;
            if ((_autoRepeat && now >= _nextTriggerSeconds) ||
                (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)) Trigger();
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) _envelope.Release(now);

            SampleValue = _envelope.Evaluate(now);
            // Use the output camera's dimensions, not UCanvas's last-rendered camera (Scene View etc.).
            SampleDiameterPixels = (float)SampleValue * _maxDiameterFraction *
                                   Mathf.Min(_outputCamera.pixelWidth, _outputCamera.pixelHeight);
            _canvas.ResetMatrix();
            _canvas.NoStroke();
            _canvas.Fill(_circleColor);
            if (SampleDiameterPixels > 0)
                _canvas.Circle(_outputCamera.pixelWidth * 0.5f, _outputCamera.pixelHeight * 0.5f,
                    SampleDiameterPixels);
            SubmittedFrameCount++;
        }
    }
}
