using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using UnityEngine;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        // Kept between edits so Rand does not jump while tuning; each loop of playback moves to the next seed, as if
        // the pad were pressed again.
        private int _previewSeed = 1;
        private GroupDefinition _previewDefinition;

        private const double PreviewDelaySeconds = 0.3;

        private PreviewSignal[] _previewSignals = Array.Empty<PreviewSignal>();
        private Dictionary<PresetComponent, PreviewSignal> _signalByComponent = new();
        private float _loopBeats = PresetSignalSampler.MaxPreviewBeats;
        private bool _refreshPreview;
        private double _previewDue;
        private bool _playing;
        private bool _loop = true;
        private float _beat;
        private float _bpm = 120;

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
                .DefaultIfEmpty(PresetSignalSampler.MaxPreviewBeats)
                .Max();
            var delay = Document.Nodes()
                .Where(node => node.Layout != null && node.Layout.Delay > 0)
                .Select(node => PresetCopyPattern.MaxDelayBeats(node.Layout))
                .DefaultIfEmpty(0)
                .Max();
            return Mathf.Clamp((float)(lifetime + delay), 0.25f, PresetSignalSampler.MaxPreviewBeats);
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
                _previewDefinition = target.Definition;
                _controller.StaticPreview = new HierarchyEvaluator(_previewDefinition, _previewSignals, _previewSeed);
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
            _beat = Mathf.Clamp(beat, 0, PresetSignalSampler.MaxPreviewBeats);
            UpdatePreviewSignals();
        }

        /// <summary>Playback of the background output. Shown in the Visual window; the Values graph follows its Beat.</summary>
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
                    UI.Field("BPM", () => _bpm, value => _bpm = Mathf.Max(0, value))),
                // Own row: sharing one with labeled fields pushed the slider past the edge.
                UI.Slider("Beat", () => _beat, SetBeat, 0f, PresetSignalSampler.MaxPreviewBeats));
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
                if (_loop)
                {
                    _beat %= end;
                    NextPreviewSeed();
                }
                else
                {
                    _beat = end;
                    _playing = false;
                }
            }

            UpdatePreviewSignals();
        }

        private void NextPreviewSeed()
        {
            _previewSeed++;
            if (_controller && _previewDefinition != null)
                _controller.StaticPreview = new HierarchyEvaluator(_previewDefinition, _previewSignals, _previewSeed);
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
