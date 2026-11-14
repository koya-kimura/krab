using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Evaluation;

namespace Krab.ShapeSynth.Core.Runtime
{
    /// <summary>Insertion-ordered voices; no clock, global BPM, pooling or renderer ownership.</summary>
    public sealed class VoiceCollection
    {
        private readonly List<VoiceInstance> _voices = new();

        public IReadOnlyList<VoiceInstance> Voices { get; }

        public VoiceCollection()
        {
            Voices = _voices.AsReadOnly();
        }

        public VoiceInstance Spawn(TriggerTarget target, double nowSeconds, double bpm, int voiceSeed = 0)
        {
            var voice = new VoiceInstance(target, new VoiceTime(nowSeconds, bpm), voiceSeed);
            _voices.Add(voice);
            return voice;
        }

        public VoiceInstance Restart(TriggerTarget target, double nowSeconds, double bpm, int voiceSeed = 0)
        {
            // Validate/compile the replacement before removing any live voices.
            var replacement = new VoiceInstance(target, new VoiceTime(nowSeconds, bpm), voiceSeed);
            foreach (var voice in _voices)
            {
                if (ReferenceEquals(voice.Target, target)) voice.Stop();
            }

            _voices.RemoveAll(voice => ReferenceEquals(voice.Target, target));
            _voices.Add(replacement);
            return replacement;
        }

        /// <summary>Stops only the requested voice owned by this collection.</summary>
        public bool Stop(VoiceInstance voice)
        {
            if (voice == null || !_voices.Remove(voice)) return false;
            voice.Stop();
            return true;
        }

        public IReadOnlyList<DrawItem> Evaluate(double nowSeconds, ReferenceContext context)
        {
            VoiceTime.RequireFinite(nowSeconds, nameof(nowSeconds));
            _voices.RemoveAll(voice => voice.IsFinished(nowSeconds));

            var items = new List<DrawItem>();
            foreach (var voice in _voices)
            {
                items.AddRange(voice.Evaluate(nowSeconds, context));
            }

            return items.AsReadOnly();
        }
    }
}
