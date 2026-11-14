using System.Linq;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using UnityEngine;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        public void Preview()
        {
            Guard(() =>
            {
                _refreshPreview = false;
                var target = PresetCompiler.Compile(Document);
                VoiceInstance replacement = null;
                HierarchyEvaluator still = null;
                if (_previewMode != PresetPreviewMode.Animated)
                {
                    _previewSignals = Document.Modulators.Select(model => new PreviewSignal(model)).ToArray();
                    UpdatePreviewSignals();
                    still = new HierarchyEvaluator(target.Definition, _previewSignals, _seed);
                }
                else
                {
                    replacement = _controller.Spawn(target, _bpm, _seed);
                    _beat = 0; // A new Voice starts at its own beat 0; keep the cursor in step with it.
                }

                _controller.Collection.Stop(_preview);
                _preview = replacement;
                _controller.StaticPreview = still;
                _hasPreview = true;
                PreviewCompileCount++;
                _status = PreviewStatus();
            });
        }

        public void StopPreview()
        {
            if (_controller)
            {
                _controller.Collection.Stop(_preview);
                _controller.StaticPreview = null;
            }

            _preview = null;
            _hasPreview = false;
            _refreshPreview = false;
            _playing = false;
            _status = "Preview stopped.";
        }

        /// <summary>
        /// Preview button: plays the preview from beat 0 with time flowing at the current BPM, looping over one
        /// Lifetime. Static switches to Beat, because a fixed signal of 1 cannot move. Pressing again restarts.
        /// </summary>
        public void Play()
        {
            if (_previewMode == PresetPreviewMode.Static) _previewMode = PresetPreviewMode.Beat;

            _beat = 0;
            _playing = true;
            Preview();
            if (!_hasPreview)
            {
                _playing = false;
                return;
            }

            _status = $"Playing at {_bpm:G} BPM — loop {LoopBeats:F2} beats.";
        }

        /// <summary>Stop button: time stops. Beat keeps the current frame for scrubbing; Animated removes its Voice.</summary>
        public void StopPlayback()
        {
            if (!_playing) return;

            _playing = false;
            if (_previewMode == PresetPreviewMode.Animated) StopPreview();

            _status = $"Stopped at beat {_beat:F2}.";
        }

        public void SetPreviewMode(PresetPreviewMode mode)
        {
            if (_previewMode == mode) return;

            _previewMode = mode;
            if (mode == PresetPreviewMode.Static) _playing = false;
            if (_playing && mode == PresetPreviewMode.Animated) _beat = 0;
            if (_hasPreview || _autoPreview) Preview();
        }

        /// <summary>Scrubs (or, while playing, seeks) without recompiling. Static switches to Beat, as the slider implies it.</summary>
        public void SetBeat(float beat)
        {
            _beat = Mathf.Clamp(beat, 0, PresetSignalSampler.GraphBeats);
            if (_previewMode == PresetPreviewMode.Static)
            {
                SetPreviewMode(PresetPreviewMode.Beat);
                return;
            }

            if (_previewMode != PresetPreviewMode.Beat) return;

            UpdatePreviewSignals();
            if (_hasPreview && !_playing) _status = PreviewStatus();
        }

        private void QueuePreview()
        {
            _refreshPreview = true;
            _previewDue = Time.realtimeSinceStartupAsDouble + 0.3;
        }

        private void AdvancePlayback(float deltaSeconds)
        {
            if (!_playing || !_hasPreview || _bpm <= 0) return;

            _beat += deltaSeconds * _bpm / 60f;
            if (_previewMode == PresetPreviewMode.Animated)
            {
                // The real Voice follows the same BPM; when it ends naturally, trigger it again.
                var alive = _controller && _preview != null && _controller.Collection.Voices.Contains(_preview);
                if (!alive || _beat > PresetSignalSampler.GraphBeats)
                {
                    _beat = 0;
                    Preview();
                }

                return;
            }

            var loop = LoopBeats;
            if (_beat >= loop) _beat %= loop;

            UpdatePreviewSignals();
        }

        private void UpdatePreviewSignals()
        {
            foreach (var signal in _previewSignals)
                signal.Value = _previewMode == PresetPreviewMode.Static ? 1 : PresetSignalSampler.SampleOrZero(signal.Model, _beat);
        }

        /// <summary>Signal value the Inspector shows for a binding: 1 in Static, otherwise the value at the scrub Beat.</summary>
        private double DisplaySignal(string modulatorId)
        {
            if (_previewMode == PresetPreviewMode.Static) return 1;

            var model = Document.Modulators.FirstOrDefault(entry => entry.Id == modulatorId);
            return model == null ? 0 : PresetSignalSampler.SampleOrZero(model, _beat);
        }

        private string PreviewStatus()
        {
            if (_previewMode == PresetPreviewMode.Static) return "Static Preview — every signal is fixed at 1.";
            if (_playing) return $"Playing at {_bpm:G} BPM — loop {LoopBeats:F2} beats.";
            if (_previewMode == PresetPreviewMode.Beat)
                return $"Beat Preview — signals sampled at beat {_beat:F2}. Press Preview to play.";

            return "Animated Preview restarted.";
        }

        /// <summary>A still signal whose value can change without recompiling the hierarchy.</summary>
        private sealed class PreviewSignal : IModulator
        {
            public PreviewSignal(ModulatorSettings model)
            {
                Model = model;
            }

            public ModulatorSettings Model { get; }

            public double Value { get; set; } = 1;

            public double Evaluate(double nowSeconds)
            {
                return Value;
            }
        }
    }
}
