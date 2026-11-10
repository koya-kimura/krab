using System;
using System.Collections.Generic;

namespace Krab.ShapeSynth.Core
{
    /// <summary>Insertion-ordered voices; no clock, global BPM, pooling or renderer ownership.</summary>
    public sealed class VoiceCollection
    {
        private readonly List<VoiceInstance> _voices = new List<VoiceInstance>();
        public IReadOnlyList<VoiceInstance> Voices { get; }
        public VoiceCollection() => Voices = _voices.AsReadOnly();

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
                if (ReferenceEquals(voice.Target, target)) voice.Stop();
            _voices.RemoveAll(voice => ReferenceEquals(voice.Target, target));
            _voices.Add(replacement);
            return replacement;
        }

        public IReadOnlyList<DrawItem> Evaluate(double nowSeconds, ReferenceContext context)
        {
            VoiceTime.RequireFinite(nowSeconds, nameof(nowSeconds));
            _voices.RemoveAll(voice => voice.IsFinished(nowSeconds));
            var items = new List<DrawItem>();
            foreach (var voice in _voices) items.AddRange(voice.Evaluate(nowSeconds, context));
            return items.AsReadOnly();
        }
    }
}
