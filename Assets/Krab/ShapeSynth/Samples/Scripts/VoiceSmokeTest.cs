using System;
using Krab.Common.Timing;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Runtime;
using Krab.ShapeSynth.Unity.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimpleContainer;

namespace Krab.ShapeSynth.Samples
{
    /// <summary>Keyboard-only sample. Runtime voices/rendering live in Core and the Unity controller.</summary>
    [RequireComponent(typeof(ShapeSynthController))]
    public sealed class VoiceSmokeTest : MonoBehaviour
    {
        [SerializeField, Min(0)] private float _timerBeats = 8;
        [SerializeField] private bool _showInstructions = true;
        private IBPMManager _bpm;
        private ShapeSynthController _controller;
        public TriggerTarget TimerTarget { get; private set; }
        public TriggerTarget MixedTarget { get; private set; }
        public TriggerTarget HeldTarget { get; private set; }
        public int VoiceCount => _controller?.Collection.Voices.Count ?? 0;
        public double CurrentBpm => _bpm?.CurrentBpm ?? 0;
        public double LastTriggeredBpm { get; private set; }

        [Inject]
        public void Construct(IBPMManager bpm) => _bpm = bpm;

        private void Start()
        {
            _controller = GetComponent<ShapeSynthController>();
            if (_bpm == null || !_controller.enabled)
            {
                Debug.LogError("Voice smoke test requires BPM injection and configured ShapeSynthController.", this);
                enabled = false;
                return;
            }
            TimerTarget = new TriggerTarget(BuildTimer());
            MixedTarget = new TriggerTarget(BuildMixed(false));
            HeldTarget = new TriggerTarget(BuildMixed(true));
            SpawnTimer();
            SpawnMixed();
        }

        public VoiceInstance SpawnTimer() => Spawn(TimerTarget);
        public VoiceInstance SpawnMixed() => Spawn(MixedTarget);
        public VoiceInstance SpawnHeld() => Spawn(HeldTarget);
        private VoiceInstance Spawn(TriggerTarget target)
        {
            LastTriggeredBpm = _bpm.CurrentBpm;
            return _controller.Spawn(target, LastTriggeredBpm);
        }
        public VoiceInstance RestartMixed()
        {
            LastTriggeredBpm = _bpm.CurrentBpm;
            return _controller.Restart(MixedTarget, LastTriggeredBpm);
        }
        public void ReleaseHeld()
        {
            foreach (var voice in _controller.Collection.Voices)
                if (ReferenceEquals(voice.Target, HeldTarget)) voice.Release(Time.timeAsDouble);
        }

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.zKey.wasPressedThisFrame) SpawnTimer();
            if (keys.xKey.wasPressedThisFrame) SpawnMixed();
            if (keys.cKey.wasPressedThisFrame) SpawnHeld();
            if (keys.vKey.wasPressedThisFrame) RestartMixed();
            if (keys.hKey.wasPressedThisFrame) ReleaseHeld();
        }

        private void OnGUI()
        {
            if (!_showInstructions || _controller == null) return;
            GUI.Box(new Rect(12, 12, 570, 64),
                $"Voice: {VoiceCount}   current BPM: {CurrentBpm:F1}   last Trigger: {LastTriggeredBpm:F1}\n" +
                "Z: Timer Spawn   X: ADSR+Timer Spawn   V: Restart X\nC: held ADSR+Timer Spawn   H: Release C   D: BPM UI");
        }

        private GroupDefinition BuildTimer()
        {
            var signals = new[] { new ModulatorSlotDefinition(new LfoDefinition(0.25)) };
            return new GroupDefinition(Panel(-0.36, 0.28), new GroupRuntimeDefinition(signals,
                    new[] { new TimerDefinition(_timerBeats) }),
                ShapeDefinition.Circle(Size(0.045), new TransformDefinition(positionX: Bound(0, 0.065)),
                    color: new RgbaColor(0.2, 0.75, 1, 0.6)));
        }

        private GroupDefinition BuildMixed(bool held)
        {
            var adsr = new AdsrDefinition(1, 0.5, 0.6, 4.5, 2); // 8 beats total; timer is only 2 beats.
            var signals = new[] { new ModulatorSlotDefinition(adsr, !held), new ModulatorSlotDefinition(new LfoDefinition(0.125)) };
            var opacity = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 0,
                new BindingDefinition(0, 0.8));
            var child = new GroupDefinition(new TransformDefinition(rotationDegrees:
                    new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 0, new BindingDefinition(1, 35))),
                ShapeDefinition.Rectangle(Size(0.045), Size(0.03),
                    new TransformDefinition(positionX: Bound(1, 0.06)), opacity,
                    held ? new RgbaColor(1, 0.7, 0.2) : new RgbaColor(0.95, 0.25, 0.65)),
                ShapeDefinition.Circle(Size(0.025), new TransformDefinition(positionX: Size(0.045)), opacity,
                    color: held ? new RgbaColor(1, 0.7, 0.2) : new RgbaColor(0.95, 0.25, 0.65)));
            return new GroupDefinition(Panel(held ? 0.02 : -0.16, 0.28),
                new GroupRuntimeDefinition(signals, new[] { new TimerDefinition(2) }), child);
        }

        private static ParameterDefinition Size(double fraction) =>
            new ParameterDefinition(new ReferenceDefinition(ReferenceSource.MinDimension), fraction);
        private static ParameterDefinition Bound(int index, double depth) =>
            new ParameterDefinition(new ReferenceDefinition(ReferenceSource.MinDimension), 0, new BindingDefinition(index, depth));
        private static TransformDefinition Panel(double x, double y) => new TransformDefinition(
            new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasWidth), x),
            new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasHeight), y));
    }
}
