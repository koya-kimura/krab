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
        private float _beat;
        private float _bpm = 120;
        private int _seed = 1234;

        public bool IsPlaying => _playing;

        public float Beat => _beat;

        /// <summary>Longest ADSR lifetime, capped to the graph range, so one loop matches one trigger.</summary>
        public float LoopBeats
        {
            get
            {
                var lifetime = Document.Modulators
                    .Where(model => model.Kind == ModulatorKind.Adsr)
                    .Select(model => model.Attack + model.Decay + model.SustainTime + model.Release)
                    .Where(value => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0)
                    .DefaultIfEmpty(PresetSignalSampler.GraphBeats)
                    .Max();
                return Mathf.Clamp((float)lifetime, 0.25f, PresetSignalSampler.GraphBeats);
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
                _previewSignals = Document.Modulators.Select(model => new PreviewSignal(model)).ToArray();
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
                    UI.Slider("Beat", () => _beat, SetBeat, 0f, PresetSignalSampler.GraphBeats)
                ),
                UI.Field("BPM", () => _bpm, value => _bpm = Mathf.Max(0, value)),
                UI.Field("Seed", () => _seed, value =>
                {
                    _seed = value;
                    QueuePreview();
                }));
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
            var loop = LoopBeats;
            if (_beat >= loop) _beat %= loop;

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
        private double SignalAtBeat(string modulatorId)
        {
            var model = Document.Modulators.FirstOrDefault(entry => entry.Id == modulatorId);
            return model == null ? 0 : PresetSignalSampler.SampleOrZero(model, _beat);
        }

        /// <summary>A still signal whose value can change without recompiling the hierarchy.</summary>
        private sealed class PreviewSignal : IModulator
        {
            public PreviewSignal(ModulatorSettings model)
            {
                Model = model;
            }

            public ModulatorSettings Model { get; }

            public double Value { get; set; }

            public double Evaluate(double nowSeconds)
            {
                return Value;
            }
        }
    }
}
