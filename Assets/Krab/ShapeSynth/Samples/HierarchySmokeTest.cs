using System.Collections.Generic;
using Krab.Common.Timing;
using Krab.ShapeSynth.Core;
using Krab.ShapeSynth.Unity;
using Unite.Canvas;
using Unite.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimpleContainer;

namespace Krab.ShapeSynth.Samples
{
    [RequireComponent(typeof(UCanvas))]
    public sealed class HierarchySmokeTest : MonoBehaviour
    {
        [SerializeField] private Camera _outputCamera;
        [SerializeField, Min(0)] private float _rotationCyclesPerBeat = 0.0625f;
        [SerializeField] private bool _invertTargetY = true;
        private IBPMManager _bpm;
        private UCanvas _canvas;
        private GroupDefinition _definition;
        private HierarchyEvaluator _evaluator;
        private AdsrInstance _adsr;
        private LfoInstance _lfo;
        private double _nextTriggerSeconds;
        private bool _settingsChanged;

        public IReadOnlyList<DrawItem> LastItems { get; private set; }
        public int SubmittedFrameCount { get; private set; }
        public double AdsrValue { get; private set; }
        public double LfoValue { get; private set; }

        [Inject]
        public void Construct(IBPMManager bpm) => _bpm = bpm;
        private void OnValidate() => _settingsChanged = true;

        private void Start()
        {
            _canvas = GetComponent<UCanvas>();
            if (!_outputCamera || _bpm == null)
            {
                Debug.LogError("Hierarchy smoke test requires an output Camera and SceneContainer BPM injection.", this);
                _canvas.enabled = false;
                enabled = false;
                return;
            }
            _canvas.CreateCanvas(DrawingMode.ScreenOverlay);
            _definition = BuildDefinition();
            Trigger();
            RestartLfo();
            _settingsChanged = false;
        }

        [ContextMenu("Trigger ADSR")]
        public void Trigger()
        {
            if (!Application.isPlaying || _bpm == null) return;
            var time = new VoiceTime(Time.timeAsDouble, _bpm.CurrentBpm);
            _adsr = new AdsrInstance(new AdsrDefinition(2, 1, 0.35, 2, 2), time);
            _nextTriggerSeconds = time.StartSeconds + 8 * 60 / time.StartBpm;
            Rebind();
        }

        [ContextMenu("Restart LFO")]
        public void RestartLfo()
        {
            if (!Application.isPlaying || _bpm == null) return;
            _lfo = new LfoInstance(new LfoDefinition(_rotationCyclesPerBeat),
                new VoiceTime(Time.timeAsDouble, _bpm.CurrentBpm));
            Rebind();
        }

        private void Rebind()
        {
            if (_adsr != null && _lfo != null && _definition != null)
                _evaluator = new HierarchyEvaluator(_definition, new IModulator[] { _adsr, _lfo });
        }

        private void Update()
        {
            if (_evaluator == null) return;
            var keyboard = Keyboard.current;
            if (Time.timeAsDouble >= _nextTriggerSeconds || (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)) Trigger();
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) _adsr.Release(Time.timeAsDouble);
            if (_settingsChanged || (keyboard != null && keyboard.lKey.wasPressedThisFrame))
            {
                RestartLfo();
                _settingsChanged = false;
            }
            var now = Time.timeAsDouble;
            AdsrValue = _adsr.Evaluate(now);
            LfoValue = _lfo.Evaluate(now);
            LastItems = _evaluator.Evaluate(now, new ReferenceContext(_outputCamera.pixelWidth, _outputCamera.pixelHeight));
            UniteShapeAdapter.Draw(_canvas, LastItems, _outputCamera.pixelWidth, _outputCamera.pixelHeight, _invertTargetY);
            SubmittedFrameCount++;
        }

        private static GroupDefinition BuildDefinition()
        {
            var cyan = new RgbaColor(0.1, 0.9, 0.85);
            var orange = new RgbaColor(1, 0.5, 0.15);
            var pink = new RgbaColor(0.9, 0.3, 0.7);
            var pivot = ShapeDefinition.Circle(Dimension(0.006), color: new RgbaColor(0.65, 0.65, 0.65));

            // A: centered circle and rectangle orbit the same fixed group origin.
            var groupA = new GroupDefinition(Panel(0.2, 0.12, Angle(90)),
                ShapeDefinition.Circle(Dimension(0.04), Local(-0.065, 0), color: cyan),
                ShapeDefinition.Rectangle(Dimension(0.05), Dimension(0.035), Local(0.065, 0), color: orange), pivot);

            // B: nested local positions/rotations/scales, including reflection and non-uniform scale.
            var deepest = new GroupDefinition(Local(0.035, 0.02, rotation: ParameterDefinition.Constant(-25)),
                ShapeDefinition.Polygon(new[] { new Point2(-15, -10), new Point2(15, -10), new Point2(0, 20) }, color: pink));
            var nested = new GroupDefinition(Local(0.055, 0, ParameterDefinition.Constant(45), -0.8, 0.7),
                ShapeDefinition.Rectangle(Dimension(0.04), Dimension(0.025), color: orange), deepest, pivot);
            var groupB = new GroupDefinition(Panel(0.36, -0.08, Angle(-55)),
                ShapeDefinition.Circle(Dimension(0.025), Local(-0.04, 0), color: cyan), nested, pivot);

            // C: shared external ADSR/LFO slots modulate both the group and child geometry/position.
            var diameter = Dimension(0.05, 0.2, new BindingDefinition(0, 0.8));
            var position = Dimension(0.07, 0.4, new BindingDefinition(0, 0.8));
            var groupC = new GroupDefinition(Panel(0.2, -0.27, Angle(90)),
                ShapeDefinition.Circle(diameter, Local(-0.06, 0), color: cyan),
                ShapeDefinition.Rectangle(Dimension(0.04), Dimension(0.025),
                    new TransformDefinition(positionX: position), color: orange), pivot);
            return new GroupDefinition(null, groupA, groupB, groupC);
        }

        private static ParameterDefinition Dimension(double multiplier, double baseValue = 1, params BindingDefinition[] bindings) =>
            new ParameterDefinition(new ReferenceDefinition(ReferenceSource.MinDimension, multiplier), baseValue, bindings);

        private static ParameterDefinition Angle(double amplitude) => new ParameterDefinition(
            new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: amplitude), 0, new BindingDefinition(1, 1));

        private static TransformDefinition Panel(double x, double y, ParameterDefinition rotation) => new TransformDefinition(
            new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasWidth), x),
            new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasHeight), y), rotation);

        private static TransformDefinition Local(double x, double y, ParameterDefinition rotation = null, double sx = 1, double sy = 1) =>
            new TransformDefinition(Dimension(x), Dimension(y), rotation, ParameterDefinition.Constant(sx), ParameterDefinition.Constant(sy));
    }
}
