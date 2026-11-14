using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Runtime;
using Krab.ShapeSynth.Unity.Rendering;
using UnityEngine;

namespace Krab.ShapeSynth.Unity.Runtime
{
    /// <summary>Unity clock/render boundary. Caller supplies current BPM; Core owns all voice state.</summary>
    [RequireComponent(typeof(TargetCameraCanvas))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom("Krab.ShapeSynth.Unity")]
    public sealed class ShapeSynthController : MonoBehaviour
    {
        [SerializeField] private Camera _outputCamera;
        [SerializeField] private bool _invertTargetY = true;

        private TargetCameraCanvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<TargetCameraCanvas>();
            if (!_outputCamera)
            {
                Debug.LogError($"{nameof(ShapeSynthController)} requires an output {nameof(Camera)}.", this);
                enabled = false;
                _canvas.enabled = false;
                return;
            }

            _canvas.Initialize(_outputCamera);
        }

        private void Update()
        {
            var width = _outputCamera.pixelWidth;
            var height = _outputCamera.pixelHeight;
            var context = new ReferenceContext(width, height);
            var voices = Collection.Evaluate(Time.timeAsDouble, context);
            LastItems = StaticPreview != null ? StaticPreview.Evaluate(0, context) : voices;
            UniteShapeAdapter.Draw(_canvas, LastItems, width, height, _invertTargetY);
            SubmittedFrameCount++;
        }

        public VoiceCollection Collection { get; } = new();

        public IReadOnlyList<DrawItem> LastItems { get; private set; } = Array.Empty<DrawItem>();

        public HierarchyEvaluator StaticPreview { get; set; }

        public int SubmittedFrameCount { get; private set; }

        public Camera OutputCamera => _outputCamera;

        public VoiceInstance Spawn(TriggerTarget target, double currentBpm, int voiceSeed = 0)
        {
            return Collection.Spawn(target, Time.timeAsDouble, currentBpm, voiceSeed);
        }

        public VoiceInstance Restart(TriggerTarget target, double currentBpm, int voiceSeed = 0)
        {
            return Collection.Restart(target, Time.timeAsDouble, currentBpm, voiceSeed);
        }
    }
}
