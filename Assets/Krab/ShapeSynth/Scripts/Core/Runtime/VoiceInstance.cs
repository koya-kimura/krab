using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;

namespace Krab.ShapeSynth.Core.Runtime
{
    /// <summary>One trigger's owned signal/lifetime states and compiled hierarchy.</summary>
    public sealed class VoiceInstance
    {
        private readonly HierarchyEvaluator _evaluator;
        private readonly List<IReleasable> _releasables = new();
        private bool _stopped;

        public TriggerTarget Target { get; }
        public VoiceTime Time { get; }
        public int VoiceSeed { get; }
        public IReadOnlyList<IModulator> Modulators { get; }
        public IReadOnlyList<ILifetime> Lifetimes { get; }

        public VoiceInstance(TriggerTarget target, VoiceTime time, int voiceSeed = 0)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            time.ElapsedBeats(time.StartSeconds);
            Time = time;
            VoiceSeed = voiceSeed;
            var modulators = new List<IModulator>();
            var lifetimes = new List<ILifetime>();
            _evaluator = new HierarchyEvaluator(target.Definition, Array.Empty<IModulator>(), (group, inherited) =>
            {
                var runtime = group.Runtime;
                if (runtime == null) return inherited;

                foreach (var timer in runtime.Timers)
                {
                    lifetimes.Add(new TimerInstance(timer, time));
                }

                if (runtime.InheritModulators) return inherited;

                // Every occurrence gets its own dictionary: shared subtree Definitions never share state accidentally.
                var states = new Dictionary<object, IModulator>();
                var releasePolicies = new Dictionary<object, bool>();
                var scope = new IModulator[runtime.Modulators.Count];
                for (var i = 0; i < scope.Length; i++)
                {
                    var slot = runtime.Modulators[i];
                    if (states.TryGetValue(slot.Source, out var existing))
                    {
                        if (slot.Adsr != null && releasePolicies[slot.Source] != slot.AutoRelease)
                        {
                            throw new ArgumentException(
                                "One ADSR source cannot have conflicting release policies in the same scope.",
                                nameof(target));
                        }

                        scope[i] = existing;
                        continue;
                    }

                    var signal = slot.Create(time);
                    scope[i] = signal;
                    states.Add(slot.Source, signal);
                    releasePolicies.Add(slot.Source, slot.AutoRelease);
                    modulators.Add(signal);
                    if (signal is ILifetime lifetime) lifetimes.Add(lifetime);
                    if (signal is IReleasable releasable) _releasables.Add(releasable);
                }

                return Array.AsReadOnly(scope);
            }, voiceSeed);

            if (lifetimes.Count == 0)
                throw new ArgumentException("A triggered Group hierarchy needs at least one ADSR or Timer.",
                    nameof(target));
            Modulators = modulators.AsReadOnly();
            Lifetimes = lifetimes.AsReadOnly();
        }

        public bool IsFinished(double nowSeconds)
        {
            VoiceTime.RequireFinite(nowSeconds, nameof(nowSeconds));
            if (_stopped) return true;

            // Delayed Bindings read signals in the past, so lifetimes are judged that much later.
            var delayedNow = nowSeconds - _evaluator.MaxDelayBeats * 60.0 / Time.StartBpm;
            foreach (var lifetime in Lifetimes)
            {
                if (!lifetime.IsFinished(delayedNow)) return false;
            }

            return true;
        }

        public void Release(double nowSeconds)
        {
            Time.ElapsedBeats(nowSeconds);
            if (nowSeconds < Time.StartSeconds) throw new ArgumentOutOfRangeException(nameof(nowSeconds));

            foreach (var releasable in _releasables)
            {
                releasable.Release(nowSeconds);
            }
        }

        public IReadOnlyList<DrawItem> Evaluate(double nowSeconds, ReferenceContext context)
        {
            if (IsFinished(nowSeconds) || nowSeconds < Time.StartSeconds) return Array.Empty<DrawItem>();

            return _evaluator.Evaluate(nowSeconds, context);
        }

        internal void Stop()
        {
            _stopped = true;
        }
    }
}
