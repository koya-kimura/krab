using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public class ModulatorLibraryTests
    {
        private static AdsrDefinition Envelope(double attack = 0.1) => new AdsrDefinition(attack, 0, 1, 10, 1);
        private static PerformanceSettings Defaults() => PerformanceSettings.CreateDefault();

        [Test]
        public void EntryRequiresNonblankIdAndNonNullNameButAllowsEmptyName()
        {
            Assert.Throws<ArgumentException>(() => new ModulatorEntry(string.Empty, "Name", Envelope()));
            Assert.Throws<ArgumentException>(() => new ModulatorEntry(null, "Name", Envelope()));
            Assert.Throws<ArgumentException>(() => new ModulatorEntry(" ", "", Envelope()));
            Assert.Throws<ArgumentNullException>(() => new ModulatorEntry("x", null, Envelope()));
            Assert.That(new ModulatorEntry("x", "", Envelope()).Name, Is.Empty);
        }

        [Test]
        public void LibraryRejectsNullCollectionAndNullEntry()
        {
            Assert.Throws<ArgumentNullException>(() => new ModulatorLibrary(null));
            Assert.Throws<ArgumentException>(() => new ModulatorLibrary(new ModulatorEntry[] { null }));
        }

        [Test]
        public void DuplicateIdsUseOrdinalComparison()
        {
            Assert.Throws<ArgumentException>(() => new ModulatorLibrary(new[]
            {
                new ModulatorEntry("same", "a", Envelope()), new ModulatorEntry("same", "b", Envelope())
            }));
            Assert.That(new ModulatorLibrary(new[]
            {
                new ModulatorEntry("A", "a", Envelope()), new ModulatorEntry("a", "b", Envelope())
            }).Entries, Has.Count.EqualTo(2));
        }

        [Test]
        public void MissingIdsAreDescriptiveAndNullIdsRejected()
        {
            var library = new ModulatorLibrary(Array.Empty<ModulatorEntry>());
            var exception = Assert.Throws<ArgumentException>(() => library.Get("missing"));
            Assert.That(exception.Message, Does.Contain("missing"));
            Assert.Throws<ArgumentNullException>(() => library.Get(null));
        }

        [Test]
        public void EntryInputIsSnapshottedAndExposedEntriesCannotBeMutated()
        {
            var entry = new ModulatorEntry("one", "One", Envelope());
            var source = new List<ModulatorEntry> { entry };
            var library = new ModulatorLibrary(source);
            source.Clear();
            Assert.That(library.Entries, Has.Count.EqualTo(1));
            Assert.Throws<NotSupportedException>(() => ((IList<ModulatorEntry>)library.Entries).Clear());
        }

        [Test]
        public void ResolvedAliasIdsReuseTheSameImmutableSlot()
        {
            var entry = new ModulatorEntry("env", "Env", Envelope());
            var slots = new ModulatorLibrary(new[] { entry }).ResolveSlots("env", "env");
            Assert.That(slots[0], Is.SameAs(slots[1]));
            Assert.That(slots[0], Is.SameAs(entry.Slot));
        }

        [Test]
        public void RenamingEntryPreservesFx01IdentityReferences()
        {
            var settings = Defaults();
            settings.Modulators[0].Name = "Renamed envelope";
            var library = settings.BuildLibrary();
            Assert.That(library.Get(settings.Fx01.EnvelopeId).Name, Is.EqualTo("Renamed envelope"));
            Assert.That(settings.Fx01.EnvelopeId, Is.EqualTo("fx01-envelope"));
        }

        [Test]
        public void DefaultSettingsBuildExpectedImmutableModulators()
        {
            var settings = Defaults();
            var library = settings.BuildLibrary();
            Assert.That(library.Entries, Has.Count.EqualTo(2));
            Assert.That(library.Get("fx01-envelope").Adsr.Attack, Is.EqualTo(0.1));
            Assert.That(library.Get("fx01-envelope").Adsr.Decay, Is.EqualTo(0.5));
            Assert.That(library.Get("fx01-envelope").Adsr.SustainLevel, Is.EqualTo(0.5));
            Assert.That(library.Get("fx01-envelope").Adsr.SustainTime, Is.EqualTo(2));
            Assert.That(library.Get("fx01-envelope").Adsr.Release, Is.EqualTo(2));
            Assert.That(library.Get("fx01-oscillator").Lfo.CyclesPerBeat, Is.EqualTo(0.5));
            Assert.That(library.Get("fx01-oscillator").Lfo.Waveform, Is.EqualTo(LfoWaveform.Sine));
        }

        [Test]
        public void BuildLibraryRejectsVersionNullsAndInvalidCount()
        {
            var settings = Defaults(); settings.Version = 2;
            Assert.Throws<ArgumentException>(() => settings.BuildLibrary());
            settings = Defaults(); settings.Modulators = null;
            Assert.Throws<ArgumentException>(() => settings.BuildLibrary());
            settings = Defaults(); settings.Fx01 = null;
            Assert.Throws<ArgumentException>(() => settings.BuildLibrary());
            settings = Defaults(); settings.Modulators.Clear();
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.BuildLibrary());
            settings = Defaults(); settings.Fx01.CircleCount = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.BuildLibrary());
            settings.Fx01.CircleCount = 2001;
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.BuildLibrary());
        }

        [Test]
        public void BuildLibraryRejectsInvalidKindAndModulatorDefinitions()
        {
            var settings = Defaults(); settings.Modulators[0].Kind = (ModulatorKind)90;
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.BuildLibrary());
            settings = Defaults(); settings.Modulators[0].Attack = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.BuildLibrary());
            settings = Defaults(); settings.Modulators[1].Waveform = (LfoWaveform)90;
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.BuildLibrary());
            settings = Defaults(); settings.Modulators[1].CyclesPerBeat = double.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.BuildLibrary());
        }

        [Test]
        public void BuildLibraryRequiresExistingFx01ReferencesWithExpectedTypes()
        {
            var settings = Defaults(); settings.Fx01.EnvelopeId = "absent";
            Assert.Throws<ArgumentException>(() => settings.BuildLibrary());
            settings = Defaults(); settings.Fx01.EnvelopeId = "fx01-oscillator";
            Assert.Throws<ArgumentException>(() => settings.BuildLibrary());
            settings = Defaults(); settings.Fx01.OscillatorId = "fx01-envelope";
            Assert.Throws<ArgumentException>(() => settings.BuildLibrary());
        }

        [Test]
        public void EditingDefinitionsAffectsNewVoicesButNotExistingVoicesOrCapturedBpm()
        {
            var settings = Defaults();
            settings.Modulators[0].Attack = 0;
            settings.Modulators[0].Decay = 0;
            settings.Modulators[0].SustainLevel = 1;
            settings.Modulators[0].SustainTime = 20;
            settings.Modulators[0].Release = 1;
            settings.Modulators[1].CyclesPerBeat = 0.25;
            var firstLibrary = settings.BuildLibrary();
            var target = Fx01Preset.Build(firstLibrary, settings.Fx01);
            var first = new VoiceInstance(target, new VoiceTime(0, 120), 7);

            settings.Modulators[0].SustainLevel = 0.25;
            settings.Modulators[1].CyclesPerBeat = 0.5;
            var secondLibrary = settings.BuildLibrary();
            var second = new VoiceInstance(Fx01Preset.Build(secondLibrary, settings.Fx01), new VoiceTime(0, 180), 7);

            Assert.That(firstLibrary.Get("fx01-envelope").Adsr.SustainLevel, Is.EqualTo(1));
            Assert.That(secondLibrary.Get("fx01-envelope").Adsr.SustainLevel, Is.EqualTo(0.25));
            Assert.That(((AdsrInstance)first.Modulators[0]).Definition.SustainLevel, Is.EqualTo(1));
            Assert.That(((AdsrInstance)second.Modulators[0]).Definition.SustainLevel, Is.EqualTo(0.25));
            Assert.That(first.Modulators[0].Evaluate(0.25), Is.EqualTo(1));
            Assert.That(second.Modulators[0].Evaluate(0.25), Is.EqualTo(0.25));
            Assert.That(((LfoInstance)first.Modulators[1]).Definition.CyclesPerBeat, Is.EqualTo(0.25));
            Assert.That(((LfoInstance)second.Modulators[1]).Definition.CyclesPerBeat, Is.EqualTo(0.5));
            Assert.That(first.Modulators[1].Evaluate(0.5), Is.EqualTo(1).Within(1e-12));
            Assert.That(second.Modulators[1].Evaluate(0.5), Is.EqualTo(-1).Within(1e-12));
            Assert.That(((LfoInstance)first.Modulators[1]).Time.StartBpm, Is.EqualTo(120));
            Assert.That(((LfoInstance)second.Modulators[1]).Time.StartBpm, Is.EqualTo(180));
        }

        [Test]
        public void SharedRootSlotsCreateOneSignalOfEachTypePerVoice()
        {
            var settings = Defaults();
            var target = Fx01Preset.Build(settings.BuildLibrary(), settings.Fx01);
            var first = new VoiceInstance(target, new VoiceTime(0, 120));
            var second = new VoiceInstance(target, new VoiceTime(0, 120));
            Assert.That(target.Definition.Runtime.Modulators, Has.Count.EqualTo(2));
            Assert.That(first.Modulators, Has.Count.EqualTo(2));
            Assert.That(second.Modulators, Has.Count.EqualTo(2));
            Assert.That(first.Modulators[0], Is.Not.SameAs(second.Modulators[0]));
            Assert.That(first.Modulators[1], Is.Not.SameAs(second.Modulators[1]));
        }
    }
}
