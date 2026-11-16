using System;
using System.Linq;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using UnityEngine;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private const double PreviewDelaySeconds = 0.3;

        private PreviewSignal[] _previewSignals = Array.Empty<PreviewSignal>();
        private bool _refreshPreview;
        private double _previewDue;
        private bool _playing;
        private bool _loop = true;
        private float _beat;
        private float _bpm = 120;
        private int _seed = 1234;

        public bool IsPlaying => _playing;

        public float Beat => _beat;

        /// <summary>
        /// Longest ADSR lifetime plus the longest copy Delay, capped to the graph range, so one loop matches
        /// one trigger including its last delayed copy.
        /// </summary>
        public float LoopBeats
        {
            get
            {
                var lifetime = PresetCompiler.Components(Document)
                    .Where(model => model.Kind == ModulatorKind.Adsr)
                    .Select(model => model.Attack + model.Decay + model.SustainTime + model.Release)
                    .Where(value => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0)
                    .DefaultIfEmpty(PresetSignalSampler.GraphBeats)
                    .Max();
                var delay = Document.Nodes()
                    .Where(node => node.Layout != null && node.Layout.Delay > 0)
                    .Select(node => PresetCopyPattern.MaxDelayBeats(node.Layout))
                    .DefaultIfEmpty(0)
                    .Max();
                return Mathf.Clamp((float)(lifetime + delay), 0.25f, PresetSignalSampler.GraphBeats);
            }
        }

        /// <summary>Compiles the document and shows it with every Value sampled at the current Beat.</summary>
        public void Preview()
        {
            _refreshPreview = false;
            if (!_controller) return;

            Guard(() =>
            {
                var target = PresetCompiler.Compile(Document);
                _previewSignals = PresetCompiler.Components(Document)
                    .Select(model => new PreviewSignal(model, () => _beat))
                    .ToArray();
                UpdatePreviewSignals();
                _controller.StaticPreview = new HierarchyEvaluator(target.Definition, _previewSignals, _seed);
            });
        }

        public void StopPreview()
        {
            if (_controller) _controller.StaticPreview = null;

            _playing = false;
            _refreshPreview = false;
        }

        public void Play()
        {
            _beat = 0;
            _playing = true;
            Preview();
        }

        public void Stop()
        {
            _playing = false;
        }

        /// <summary>Scrubs, or seeks while playing, without recompiling.</summary>
        public void SetBeat(float beat)
        {
            _beat = Mathf.Clamp(beat, 0, PresetSignalSampler.GraphBeats);
            UpdatePreviewSignals();
        }

        private Element BuildPreviewPane()
        {
            return UI.Column(
                PaneTitle("Preview"),
                UI.Image(() => _controller && _controller.OutputCamera ? _controller.OutputCamera.targetTexture : null)
                    .SetHeight(300),
                UI.Row(
                    UI.Button(UI.Label(() => _playing ? "Stop" : "Play"), () =>
                    {
                        if (_playing) Stop();
                        else Play();
                    }),
                    LoopButton()),
                // Own row: sharing one with labeled fields pushed the slider past the pane edge.
                UI.Slider("Beat", () => _beat, SetBeat, 0f, PresetSignalSampler.GraphBeats),
                UI.Field("BPM", () => _bpm, value => _bpm = Mathf.Max(0, value)),
                UI.Field("Seed", () => _seed, value =>
                {
                    _seed = value;
                    QueuePreview();
                }));
        }

        /// <summary>A compact toggle button; highlighted while Loop is on.</summary>
        private Element LoopButton()
        {
            var button = UI.Button("Loop", () => _loop = !_loop);
            button.RegisterUpdateCallback(element =>
                element.Style.BackgroundColor = _loop ? PresetEditorPalette.Selected : null);
            return button;
        }

        private void QueuePreview()
        {
            _refreshPreview = true;
            _previewDue = Time.realtimeSinceStartupAsDouble + PreviewDelaySeconds;
        }

        private void AdvancePlayback(float deltaSeconds)
        {
            if (!_playing || _bpm <= 0) return;

            _beat += deltaSeconds * _bpm / 60f;
            var end = LoopBeats;
            if (_beat >= end)
            {
                // Without Loop, playback holds the last frame of one trigger and stops.
                if (_loop) _beat %= end;
                else
                {
                    _beat = end;
                    _playing = false;
                }
            }

            UpdatePreviewSignals();
        }

        private void UpdatePreviewSignals()
        {
            foreach (var signal in _previewSignals)
            {
                signal.Value = PresetSignalSampler.SampleOrZero(signal.Model, _beat);
            }
        }

        /// <summary>The current value of a Value at the preview Beat.</summary>
        private double SignalAtBeat(string valueId)
        {
            return PresetSignalSampler.SampleOrZero(FindValue(valueId), _beat);
        }

        /// <summary>
        /// A still component signal whose value can change without recompiling the hierarchy.
        /// Delayed Bindings sample the component at (Beat − delay) instead.
        /// </summary>
        private sealed class PreviewSignal : IDelayableModulator
        {
            private readonly Func<float> _beat;
            private readonly IModulator _source;

            public PreviewSignal(PresetComponent model, Func<float> beat)
            {
                Model = model;
                _beat = beat;
                PresetSignalSampler.TryCreate(model, out _source);
            }

            public PresetComponent Model { get; }

            public double Value { get; set; }

            public double Evaluate(double nowSeconds)
            {
                return Value;
            }

            // The sampler's modulators run one beat per second, so beats are passed as seconds.
            public double EvaluateDelayed(double nowSeconds, double delayBeats)
            {
                return _source?.Evaluate(_beat() - delayBeats) ?? 0;
            }
        }
    }
}
