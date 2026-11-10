using Krab.Common.Timing;
using Krab.ShapeSynth.Core;
using Krab.ShapeSynth.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimpleContainer;

namespace Krab.ShapeSynth.Samples
{
    [RequireComponent(typeof(ShapeSynthController))]
    public sealed class VariationSmokeTest : MonoBehaviour
    {
        [SerializeField] private int _seed = 1234;
        [SerializeField, Min(0)] private float _lifetimeBeats = 128;
        [SerializeField] private bool _showInstructions = true;
        private IBPMManager _bpm;
        private ShapeSynthController _controller;
        public TriggerTarget Target { get; private set; }
        public int Seed => _seed;

        [Inject]
        public void Construct(IBPMManager bpm) => _bpm = bpm;

        private void Start()
        {
            _controller = GetComponent<ShapeSynthController>();
            if (_bpm == null || !_controller.enabled)
            {
                Debug.LogError("Variation sample requires BPM injection and an output Camera.", this);
                enabled = false;
                return;
            }
            Target = new TriggerTarget(BuildDefinition());
            SpawnCurrent();
        }

        public VoiceInstance SpawnCurrent() => _controller.Spawn(Target, _bpm.CurrentBpm, _seed);
        public VoiceInstance SpawnNext() { _seed = unchecked(_seed + 1); return SpawnCurrent(); }
        public VoiceInstance RestartCurrent() => _controller.Restart(Target, _bpm.CurrentBpm, _seed);
        public VoiceInstance RestartNext() { _seed = unchecked(_seed + 1); return RestartCurrent(); }

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.nKey.wasPressedThisFrame) SpawnCurrent();
            if (keys.mKey.wasPressedThisFrame) SpawnNext();
            if (keys.jKey.wasPressedThisFrame) RestartCurrent();
            if (keys.kKey.wasPressedThisFrame) RestartNext();
        }

        private void OnGUI()
        {
            if (!_showInstructions || _controller == null) return;
            GUI.Box(new Rect(12, 80, 570, 42),
                $"Variation seed: {_seed}   voices: {_controller.Collection.Voices.Count}\n" +
                "N: same seed Spawn   M: next seed Spawn   J: Restart   K: next seed Restart");
        }

        private GroupDefinition BuildDefinition()
        {
            var cyan = new RgbaColor(0.25, 0.8, 1, 0.85);
            var pink = new RgbaColor(1, 0.25, 0.65, 0.8);
            var orange = new RgbaColor(1, 0.65, 0.15, 0.85);
            var green = new RgbaColor(0.3, 1, 0.5, 0.8);
            var circle = ShapeDefinition.Circle(Size(0.014),
                new TransformDefinition(positionX: AltSize(-0.025, 0.025)), color: cyan);
            var pair = new GroupDefinition(null, circle, circle);
            // Two manually placed rows, each sharing the same pair/shape definitions. No Layout system.
            var alternating = new GroupDefinition(Panel(0.28),
                new GroupDefinition(new TransformDefinition(positionY: Size(0.024)), pair),
                new GroupDefinition(new TransformDefinition(positionY: Size(-0.024)), pair));

            var rectangle = ShapeDefinition.Rectangle(Size(0.025), Size(0.012),
                new TransformDefinition(positionX: AltSize(-0.025, 0.025), rotationDegrees:
                    new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 0,
                        new BindingDefinition(0, 0, VariationDefinition.Alternating(60, -60)))), color: pink);
            var opposite = new GroupDefinition(Panel(0.08), rectangle, rectangle);

            var randomCircle = ShapeDefinition.Circle(Size(0).WithBaseVariation(VariationDefinition.Random(0.008, 0.018)),
                new TransformDefinition(
                    Size(0).WithBaseVariation(VariationDefinition.Random(-0.04, 0.04)),
                    Size(0).WithBaseVariation(VariationDefinition.Random(-0.04, 0.04))), color: orange);
            var random = new GroupDefinition(Panel(-0.14), randomCircle, randomCircle, randomCircle,
                randomCircle, randomCircle, randomCircle);

            var inner = ShapeDefinition.Circle(AltSize(0.009, 0.015),
                new TransformDefinition(positionX: AltSize(-0.012, 0.012)), color: green);
            var sharedGroup = new GroupDefinition(new TransformDefinition(positionX: AltSize(-0.033, 0.033),
                rotationDegrees: ParameterDefinition.Constant(0).WithBaseVariation(VariationDefinition.Alternating(30, -30))), inner, inner);
            var nested = new GroupDefinition(Panel(-0.34), sharedGroup, sharedGroup);
            return new GroupDefinition(null, new GroupRuntimeDefinition(
                    new[] { new ModulatorSlotDefinition(new LfoDefinition(0.125)) },
                    new[] { new TimerDefinition(_lifetimeBeats) }), alternating, opposite, random, nested);
        }

        private static ParameterDefinition Size(double value) =>
            new ParameterDefinition(new ReferenceDefinition(ReferenceSource.MinDimension), value);
        private static ParameterDefinition AltSize(double even, double odd) =>
            Size(0).WithBaseVariation(VariationDefinition.Alternating(even, odd));
        private static TransformDefinition Panel(double y) => new TransformDefinition(
            new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasWidth), -0.45),
            new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasHeight), y));
    }
}
