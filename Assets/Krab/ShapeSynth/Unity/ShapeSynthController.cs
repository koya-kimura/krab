using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core;
using UnityEngine;

namespace Krab.ShapeSynth.Unity
{
    /// <summary>Unity clock/render boundary. Caller supplies current BPM; Core owns all voice state.</summary>
    [RequireComponent(typeof(TargetCameraCanvas))]
    public sealed class ShapeSynthController : MonoBehaviour
    {
        [SerializeField] private Camera _outputCamera;
        [SerializeField] private bool _invertTargetY = true;
        private TargetCameraCanvas _canvas;
        public VoiceCollection Collection { get; } = new VoiceCollection();
        public IReadOnlyList<DrawItem> LastItems { get; private set; } = Array.Empty<DrawItem>();
        public int SubmittedFrameCount { get; private set; }
        public Camera OutputCamera => _outputCamera;

        private void Awake()
        {
            _canvas = GetComponent<TargetCameraCanvas>();
            if (!_outputCamera)
            {
                Debug.LogError("ShapeSynthController requires an output Camera.", this);
                enabled = false;
                _canvas.enabled = false;
                return;
            }
            _canvas.Initialize(_outputCamera);
        }

        public VoiceInstance Spawn(TriggerTarget target, double currentBpm, int voiceSeed = 0) =>
            Collection.Spawn(target, Time.timeAsDouble, currentBpm, voiceSeed);

        public VoiceInstance Restart(TriggerTarget target, double currentBpm, int voiceSeed = 0) =>
            Collection.Restart(target, Time.timeAsDouble, currentBpm, voiceSeed);

        private void Update()
        {
            var width = _outputCamera.pixelWidth;
            var height = _outputCamera.pixelHeight;
            LastItems = Collection.Evaluate(Time.timeAsDouble, new ReferenceContext(width, height));
            UniteShapeAdapter.Draw(_canvas, LastItems, width, height, _invertTargetY);
            SubmittedFrameCount++;
        }
    }
}
