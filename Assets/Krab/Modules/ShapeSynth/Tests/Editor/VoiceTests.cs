using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Voices;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class VoiceTests
    {
        private static readonly ReferenceContext Context = new(100, 80);

        [Test]
        public void NestedInheritedGroupsShareParentScopeAndSampleAtSameTime()
        {
            var adsr = Envelope();
            var root = Group(Scope(new ModulatorSlotDefinition(adsr, false)),
                Group(null, Group(null, Shape(new BindingDefinition(0, 3)))));
            var voice = new VoiceInstance(new TriggerTarget(root), new VoiceTime(2, 60));

            Assert.That(voice.Modulators, Has.Count.EqualTo(1));
            Assert.That(voice.Evaluate(2, Context)[0].Width, Is.EqualTo(4));
            Assert.That(voice.Evaluate(2, Context)[0].Width, Is.EqualTo(4));
        }

        [Test]
        public void SharedDefinitionAcrossVoicesAndPlacementsHasIndependentState()
        {
            var adsr = Envelope();
            var subtree = Group(Scope(new ModulatorSlotDefinition(adsr, false)), Shape());
            var target = new TriggerTarget(Group(null, subtree, subtree));
            var first = new VoiceInstance(target, new VoiceTime(0, 60));
            var second = new VoiceInstance(target, new VoiceTime(2, 120));

            Assert.That(first.Modulators, Has.Count.EqualTo(2));
            Assert.That(first.Lifetimes, Has.Count.EqualTo(2));
            Assert.That(second.Modulators, Has.Count.EqualTo(2));
            Assert.That(first.Modulators[0], Is.Not.SameAs(first.Modulators[1]));
            Assert.That(first.Modulators[0], Is.Not.SameAs(second.Modulators[0]));

            first.Release(1);

            Assert.That(((ILifetime)first.Modulators[0]).IsFinished(2), Is.True);
            Assert.That(((ILifetime)second.Modulators[0]).IsFinished(2), Is.False);
        }

        [Test]
        public void RepeatedSourceSlotsAliasAndBindingsResolveSameInstance()
        {
            var adsr = Envelope();
            var root = Group(
                Scope(new ModulatorSlotDefinition(adsr, false), new ModulatorSlotDefinition(adsr, false)),
                Shape(new BindingDefinition(0, 1), new BindingDefinition(1, 2)));
            var voice = new VoiceInstance(new TriggerTarget(root), new VoiceTime(0, 60));

            Assert.That(voice.Modulators, Has.Count.EqualTo(1));
            Assert.That(voice.Evaluate(0, Context)[0].Width, Is.EqualTo(4));
        }

        [Test]
        public void TriggerTimeAndCapturedBpmDriveAllSignalAndTimerInstances()
        {
            var lfo = new LfoDefinition(0.25, waveform: LfoWaveform.Square);
            var adsr = Envelope(sustainTime: 2, release: 0);
            var runtime = new GroupRuntimeDefinition(new ModulatorSlotDefinition[]
            {
                new ModulatorSlotDefinition(lfo), new ModulatorSlotDefinition(adsr)
            }, new[] { new TimerDefinition(2) });
            var target = new TriggerTarget(Group(runtime, Group(null, Shape())));
            var slow = new VoiceInstance(target, new VoiceTime(0, 60));
            var fast = new VoiceInstance(target, new VoiceTime(0, 120));

            Assert.That(slow.Time.StartBpm, Is.EqualTo(60));
            Assert.That(fast.Time.StartBpm, Is.EqualTo(120));
            Assert.That(slow.Modulators[0].Evaluate(1), Is.EqualTo(1));
            Assert.That(fast.Modulators[0].Evaluate(1), Is.EqualTo(-1));
            Assert.That(
                ((LfoInstance)slow.Modulators[0]).Time.StartSeconds, Is.EqualTo(slow.Time.StartSeconds));
            Assert.That(
                ((AdsrInstance)slow.Modulators[1]).Time.StartSeconds, Is.EqualTo(slow.Time.StartSeconds));
            Assert.That(slow.Lifetimes, Has.Count.EqualTo(2));
            Assert.That(slow.IsFinished(1), Is.False);
            Assert.That(fast.IsFinished(1), Is.True);
            Assert.That(((ILifetime)slow.Modulators[1]).IsFinished(1), Is.False);
        }

        [Test]
        public void RuntimeNullInheritsModulatorsThroughDescendantsAtOneTriggerTime()
        {
            var timer = new TimerDefinition(2);
            var root = Group(Scope(new ModulatorSlotDefinition(Envelope(), false)),
                Group(new GroupRuntimeDefinition(timers: new[] { timer }), Shape(new BindingDefinition(0, 1))));
            var voice = new VoiceInstance(new TriggerTarget(root), new VoiceTime(0, 60));

            Assert.That(voice.Modulators, Has.Count.EqualTo(1));
            Assert.That(voice.Lifetimes, Has.Count.EqualTo(2));
            Assert.That(voice.IsFinished(1), Is.False);
            // Held ADSR outlives timer.
            Assert.That(voice.IsFinished(2), Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TimerAndHeldEnvelopeBothMustFinishRegardlessOfOrder(bool timerFirst)
        {
            var timerBeats = timerFirst ? 1 : 5;
            var envBeats = timerFirst ? 5 : 1;
            var runtime = new GroupRuntimeDefinition(
                new[] { new ModulatorSlotDefinition(Envelope(sustainTime: envBeats, release: 0), true) },
                new[] { new TimerDefinition(timerBeats) });
            var voice = new VoiceInstance(new TriggerTarget(Group(runtime, Shape())), new VoiceTime(0, 60));

            Assert.That(voice.IsFinished(Math.Min(timerBeats, envBeats) - 0.001), Is.False);
            Assert.That(voice.IsFinished(Math.Min(timerBeats, envBeats)), Is.False);
            Assert.That(voice.IsFinished(Math.Max(timerBeats, envBeats) - 0.001), Is.False);
            Assert.That(voice.IsFinished(Math.Max(timerBeats, envBeats)), Is.True);
        }

        [Test]
        public void LifetimeMayBeNestedButHierarchyNeedsOneLifetimeSomewhere()
        {
            var valid = Group(
                null, Group(new GroupRuntimeDefinition(timers: new[] { new TimerDefinition(1) }), Shape()));

            Assert.DoesNotThrow(() => new VoiceInstance(new TriggerTarget(valid), new VoiceTime(0, 60)));
            Assert.Throws<ArgumentException>(
                () => new VoiceInstance(new TriggerTarget(Group(null, Shape())), new VoiceTime(0, 60)));
        }

        [Test]
        public void ReleaseForwardsToEveryAdsrAndTimerStillControlsFinalLifetime()
        {
            var first = Envelope(sustainTime: 100, release: 1);
            var second = Envelope(sustainTime: 100, release: 1);
            var root = Group(new GroupRuntimeDefinition(new[] { new ModulatorSlotDefinition(first, false) },
                    new[] { new TimerDefinition(100) }),
                Group(Scope(new ModulatorSlotDefinition(second, false)), Shape()));
            var voice = new VoiceInstance(new TriggerTarget(root), new VoiceTime(0, 60));
            voice.Release(2);

            Assert.That(voice.Lifetimes, Has.Count.EqualTo(3));
            Assert.That(((ILifetime)voice.Modulators[0]).IsFinished(3), Is.True);
            Assert.That(((ILifetime)voice.Modulators[1]).IsFinished(3), Is.True);
            Assert.That(voice.IsFinished(3), Is.False);
            Assert.That(voice.IsFinished(100), Is.True);
        }

        [Test]
        public void RestartReplacesOnlyExactTargetAndSameDefinitionCanHaveMultipleTargets()
        {
            var definition = Group(new GroupRuntimeDefinition(timers: new[] { new TimerDefinition(10) }), Shape());
            var a = new TriggerTarget(definition);
            var b = new TriggerTarget(definition);
            var collection = new VoiceCollection();
            var oldA = collection.Spawn(a, 0, 60);
            var oldB = collection.Spawn(b, 0, 60);
            var restarted = collection.Restart(a, 1, 60);

            Assert.That(collection.Voices, Has.Count.EqualTo(2));
            Assert.That(collection.Voices[0], Is.SameAs(oldB));
            Assert.That(collection.Voices[1], Is.SameAs(restarted));
            Assert.That(restarted, Is.Not.SameAs(oldA));
            Assert.That(oldA.IsFinished(1), Is.True);
            Assert.That(oldA.Evaluate(1, Context), Is.Empty);
        }

        [Test]
        public void FailedRestartPreservesExistingVoices()
        {
            var good = new TriggerTarget(
                Group(new GroupRuntimeDefinition(timers: new[] { new TimerDefinition(10) }), Shape()));
            var bad = new TriggerTarget(Group(null, Shape()));
            var collection = new VoiceCollection();
            var live = collection.Spawn(good, 0, 60);

            Assert.Throws<ArgumentException>(() => collection.Restart(bad, 1, 60));
            Assert.That(collection.Voices, Has.Count.EqualTo(1));
            Assert.That(collection.Voices[0], Is.SameAs(live));
        }

        [Test]
        public void FinishedBoundaryIsRemovedBeforeDrawAndRemainingDrawOrderIsStable()
        {
            var definition = Group(new GroupRuntimeDefinition(timers: new[] { new TimerDefinition(1) }), Shape());
            var collection = new VoiceCollection();
            collection.Spawn(new TriggerTarget(definition), 0, 60);
            collection.Spawn(new TriggerTarget(definition), 1, 60);

            Assert.That(collection.Evaluate(1, Context), Has.Count.EqualTo(1));
            Assert.That(collection.Voices, Has.Count.EqualTo(1));
            Assert.That(collection.Voices[0].Time.StartSeconds, Is.EqualTo(1));
        }

        [Test]
        public void ActiveVoicesDrawInSpawnOrderWithTheirOwnGeometry()
        {
            var timer = new GroupRuntimeDefinition(timers: new[] { new TimerDefinition(10) });
            var a = new TriggerTarget(Group(timer, Shape()));
            var b = new TriggerTarget(Group(timer, ShapeDefinition.Circle(ParameterDefinition.Constant(2))));
            var collection = new VoiceCollection();
            collection.Spawn(a, 0, 60);
            collection.Spawn(b, 0, 60);
            var items = collection.Evaluate(0, Context);

            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(collection.Voices[0].Target, Is.SameAs(a));
            Assert.That(collection.Voices[1].Target, Is.SameAs(b));
            Assert.That(items[0].Width, Is.EqualTo(1));
            Assert.That(items[1].Width, Is.EqualTo(2));
        }

        [Test]
        public void NullsConflictingPoliciesAndInvalidBindingsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new TriggerTarget(null));
            Assert.Throws<ArgumentNullException>(() => new VoiceInstance(null, new VoiceTime(0, 60)));
            Assert.Throws<ArgumentException>(
                () => new GroupRuntimeDefinition(new ModulatorSlotDefinition[] { null }));

            var shared = Envelope();
            Assert.Throws<ArgumentException>(() => new VoiceInstance(
                new TriggerTarget(Group(
                    Scope(new ModulatorSlotDefinition(shared, true), new ModulatorSlotDefinition(shared, false)),
                    Shape())),
                new VoiceTime(0, 60)));

            var invalid = Group(
                Scope(new ModulatorSlotDefinition(new LfoDefinition(1))), Shape(new BindingDefinition(1, 1)));
            Assert.Throws<ArgumentException>(
                () => new VoiceInstance(new TriggerTarget(invalid), new VoiceTime(0, 60)));
        }

        [Test]
        public void RuntimeAndVoiceCollectionsAreDefensiveReadOnlyCopies()
        {
            var slots = new List<ModulatorSlotDefinition> { new ModulatorSlotDefinition(Envelope(), false) };
            var runtime = new GroupRuntimeDefinition(slots);
            slots.Clear();

            Assert.That(runtime.Modulators, Has.Count.EqualTo(1));
            Assert.Throws<NotSupportedException>(
                () => ((IList<ModulatorSlotDefinition>)runtime.Modulators).Clear());

            var voice = new VoiceInstance(new TriggerTarget(Group(runtime, Shape())), new VoiceTime(0, 60));
            Assert.Throws<NotSupportedException>(() => ((IList<IModulator>)voice.Modulators).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<ILifetime>)voice.Lifetimes).Clear());
        }

        private static ShapeDefinition Shape(params BindingDefinition[] bindings)
        {
            return ShapeDefinition.Circle(
                new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 1, bindings));
        }

        private static GroupDefinition Group(GroupRuntimeDefinition runtime, params INodeDefinition[] children)
        {
            return new GroupDefinition(null, runtime, children);
        }

        private static GroupRuntimeDefinition Scope(params ModulatorSlotDefinition[] slots)
        {
            return new GroupRuntimeDefinition(slots);
        }

        private static AdsrDefinition Envelope(double sustain = 1, double sustainTime = 10, double release = 1)
        {
            return new AdsrDefinition(0, 0, sustain, sustainTime, release);
        }
    }
}
