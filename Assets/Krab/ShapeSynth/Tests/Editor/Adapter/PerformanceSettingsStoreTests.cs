using System;
using System.IO;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Unity.Persistence;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor.Adapter
{
    public sealed class PerformanceSettingsStoreTests
    {
        private string _directory;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Krab-" + Guid.NewGuid().ToString("N"));
            _path = System.IO.Path.Combine(_directory, "settings.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void DefaultRoundTripPreservesIdsNamesReferencesAndAllModulatorFields()
        {
            var source = PerformanceSettings.CreateDefault();
            source.Modulators[0].Name = "Envelope renamed";
            source.Modulators[0].Attack = 0.23;
            source.Modulators[0].Decay = 0.41;
            source.Modulators[0].SustainLevel = 0.67;
            source.Modulators[0].SustainTime = 3.2;
            source.Modulators[0].Release = 4.3;
            source.Modulators[1].Name = "Oscillator renamed";
            source.Modulators[1].CyclesPerBeat = 1.25;
            source.Modulators[1].PhaseOffset = 0.375;
            source.Modulators[1].Waveform = LfoWaveform.Square;

            var decoded = PerformanceSettingsStore.Decode(PerformanceSettingsStore.Encode(source));

            Assert.That(decoded.Modulators[0].Id, Is.EqualTo("fx01-envelope"));
            Assert.That(decoded.Modulators[0].Name, Is.EqualTo("Envelope renamed"));
            Assert.That(decoded.Modulators[0].Kind, Is.EqualTo(ModulatorKind.Adsr));
            Assert.That(decoded.Modulators[0].Attack, Is.EqualTo(0.23));
            Assert.That(decoded.Modulators[0].Decay, Is.EqualTo(0.41));
            Assert.That(decoded.Modulators[0].SustainLevel, Is.EqualTo(0.67));
            Assert.That(decoded.Modulators[0].SustainTime, Is.EqualTo(3.2));
            Assert.That(decoded.Modulators[0].Release, Is.EqualTo(4.3));
            Assert.That(decoded.Modulators[1].Id, Is.EqualTo("fx01-oscillator"));
            Assert.That(decoded.Modulators[1].Name, Is.EqualTo("Oscillator renamed"));
            Assert.That(decoded.Modulators[1].Kind, Is.EqualTo(ModulatorKind.Lfo));
            Assert.That(decoded.Modulators[1].CyclesPerBeat, Is.EqualTo(1.25));
            Assert.That(decoded.Modulators[1].PhaseOffset, Is.EqualTo(0.375));
            Assert.That(decoded.Modulators[1].Waveform, Is.EqualTo(LfoWaveform.Square));
            Assert.That(decoded.Fx01.CircleCount, Is.EqualTo(150));
            Assert.That(decoded.Fx01.EnvelopeId, Is.EqualTo("fx01-envelope"));
            Assert.That(decoded.Fx01.OscillatorId, Is.EqualTo("fx01-oscillator"));
            Assert.That(decoded.Fx01.Seed, Is.EqualTo(1234));
            Assert.That(decoded.BuildLibrary().Get(decoded.Fx01.EnvelopeId).Name, Is.EqualTo("Envelope renamed"));
        }

        [Test]
        public void DecodeUsesNumericLfoEnumValues()
        {
            var json = PerformanceSettingsStore.Encode(PerformanceSettings.CreateDefault())
                .Replace("\"Waveform\": 0", "\"Waveform\": 3");
            Assert.That(PerformanceSettingsStore.Decode(json).Modulators[1].Waveform, Is.EqualTo((LfoWaveform)3));
        }

        [Test]
        public void BuildLibraryRejectsInvalidDocumentValues()
        {
            var defaults = PerformanceSettings.CreateDefault();
            Assert.Throws<ArgumentException>(() => PerformanceSettingsStore.Decode("{"));
            defaults.Version = 2;
            Assert.Throws<ArgumentException>(() => PerformanceSettingsStore.Encode(defaults));
            defaults = PerformanceSettings.CreateDefault();
            defaults.Modulators.Add(defaults.Modulators[0]);
            Assert.Throws<ArgumentException>(() => PerformanceSettingsStore.Encode(defaults));
            defaults = PerformanceSettings.CreateDefault();
            defaults.Fx01.EnvelopeId = "missing";
            Assert.Throws<ArgumentException>(() => PerformanceSettingsStore.Encode(defaults));
            defaults = PerformanceSettings.CreateDefault();
            defaults.Modulators[0].Kind = (ModulatorKind)99;
            Assert.Throws<ArgumentOutOfRangeException>(() => PerformanceSettingsStore.Encode(defaults));
            defaults = PerformanceSettings.CreateDefault();
            defaults.Modulators[0].Attack = double.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(() => PerformanceSettingsStore.Encode(defaults));
            defaults = PerformanceSettings.CreateDefault();
            defaults.Fx01.CircleCount = 2001;
            Assert.Throws<ArgumentOutOfRangeException>(() => PerformanceSettingsStore.Encode(defaults));
        }

        [Test]
        public void SaveLoadAndReplaceUseOnlyTheSuppliedTemporaryPath()
        {
            var store = new PerformanceSettingsStore(_path);
            var original = PerformanceSettings.CreateDefault();
            store.Save(original);
            Assert.That(store.Exists, Is.True);
            Assert.That(store.Load().Fx01.CircleCount, Is.EqualTo(150));

            original.Fx01.CircleCount = 240;
            store.Save(original);
            Assert.That(store.Load().Fx01.CircleCount, Is.EqualTo(240));
            Assert.That(Directory.GetFiles(_directory), Is.EqualTo(new[] { _path }));
        }

        [Test]
        public void InvalidSaveLeavesPreviousFileUntouched()
        {
            var store = new PerformanceSettingsStore(_path);
            store.Save(PerformanceSettings.CreateDefault());
            var previous = File.ReadAllText(_path);
            var invalid = PerformanceSettings.CreateDefault();
            invalid.Fx01.OscillatorId = "missing";

            Assert.Throws<ArgumentException>(() => store.Save(invalid));
            Assert.That(File.ReadAllText(_path), Is.EqualTo(previous));
            Assert.That(Directory.GetFiles(_directory), Is.EqualTo(new[] { _path }));
        }

        [Test]
        public void MalformedExistingFileThrowsAndRemainsUntouched()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_path, "not valid settings JSON");
            var before = File.ReadAllText(_path);

            Assert.Throws<ArgumentException>(() => new PerformanceSettingsStore(_path).Load());
            Assert.That(File.ReadAllText(_path), Is.EqualTo(before));
        }
    }
}
