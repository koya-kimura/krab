using System;
using System.Collections.Generic;
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
        private Dictionary<PresetComponent, PreviewSignal> _signalByComponent = new();
        private float _loopBeats = PresetSignalSampler.GraphBeats;
        private bool _refreshPreview;
        private double _previewDue;
        private bool _playing;
        private bool _loop = true;
        private float _beat;
        private float _bpm = 120;
        private int _seed = 1234;

        public bool IsPlaying => _playing;

        public float Beat => _beat;

        /// <summary>One loop of playback; recomputed on each compile (every edit queues one), not per frame.</summary>
        public float LoopBeats => _loopBeats;

        /// <summary>
        /// Longest ADSR lifetime plus the longest copy Delay, capped to the graph range, so one loop matches
        /// one trigger including its last delayed copy.
        /// </summary>
        private float ComputeLoopBeats()
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

        /// <summary>Compiles the document and shows it with every Value sampled at the current Beat.</summary>
        public void Preview()
        {
            _refreshPreview = false;
            _loopBeats = ComputeLoopBeats();
            if (!_controller) return;

            Guard(() =>
            {
                var target = PresetCompiler.Compile(Document);
                _previewSignals = PresetCompiler.Components(Document)
                    .Select(model => new PreviewSignal(model, () => _beat))
                    .ToArray();
                _signalByComponent = _previewSignals.ToDictionary(signal => signal.Model);
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

        /// <summary>Playback of the background output, shared by the Visual and Values tabs.</summary>
        private Element BuildTransportBar()
        {
            return UI.Column(
                UI.Row(
                    UI.Button(UI.Label(() => _playing ? "Stop" : "Play"), () =>
                    {
                        if (_playing) Stop();
                        else Play();
                    }),
                    LoopButton(),
                    UI.Field("BPM", () => _bpm, value => _bpm = Mathf.Max(0, value)),
                    UI.Field("Seed", () => _seed, value =>
                    {
                        _seed = value;
                        QueuePreview();
                    })),
                // Own row: sharing one with labeled fields pushed the slider past the edge.
                UI.Slider("Beat", () => _beat, SetBeat, 0f, PresetSignalSampler.GraphBeats));
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
                signal.Value = signal.Sample(_beat);
            }
        }

        /// <summary>The current value of a Value at the preview Beat.</summary>
        /// <summary>Σ Weight × the compiled preview signals; samples afresh only for components added since.</summary>
        private double SignalAtBeat(string valueId)
        {
            var value = FindValue(valueId);
            if (value == null) return 0;

            var sum = 0.0;
            foreach (var component in value.Components)
            {
                sum += component.Weight * (_signalByComponent.TryGetValue(component, out var signal)
                    ? signal.Value
                    : PresetSignalSampler.SampleOrZero(component, _beat));
            }

            return sum;
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

            /// <summary>The component at a beat, from the modulator built once at compile (0 if it was invalid).</summary>
            // The sampler's modulators run one beat per second, so beats are passed as seconds.
            public double Sample(double beat) => _source?.Evaluate(beat) ?? 0;

            public double EvaluateDelayed(double nowSeconds, double delayBeats) => Sample(_beat() - delayBeats);
        }
    }
}
