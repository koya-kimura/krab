using System;
using System.IO;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Unity.Persistence;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor.Adapter
{
    public sealed class MultiPresetSettingsTests
    {
        private string _directory;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "Krab-" + Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, "settings.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void LegacySettingsMigrateWithoutChangingFx01OrExistingModulators()
        {
            var legacy = PerformanceSettings.CreateDefault();
            legacy.Modulators[0].Name = "User envelope";
            legacy.Modulators[1].Name = "User oscillator";
            legacy.Fx01.CircleCount = 321;
            legacy.Fx01.Seed = 9876;

            var decoded = PerformanceSettingsStore.Decode(PerformanceSettingsStore.Encode(legacy));

            Assert.That(decoded.Version, Is.EqualTo(1));
            Assert.That(decoded.Fx01.CircleCount, Is.EqualTo(321));
            Assert.That(decoded.Fx01.EnvelopeId, Is.EqualTo("fx01-envelope"));
            Assert.That(decoded.Fx01.OscillatorId, Is.EqualTo("fx01-oscillator"));
            Assert.That(decoded.Fx01.Seed, Is.EqualTo(9876));
            Assert.That(decoded.Modulators[0].Id, Is.EqualTo("fx01-envelope"));
            Assert.That(decoded.Modulators[0].Name, Is.EqualTo("User envelope"));
            Assert.That(decoded.Modulators[1].Id, Is.EqualTo("fx01-oscillator"));
            Assert.That(decoded.Modulators[1].Name, Is.EqualTo("User oscillator"));
            Assert.That(decoded.Modulators, Has.Count.EqualTo(8));
            Assert.That(decoded.Presets, Has.Count.EqualTo(4));
        }

        [Test]
        public void CreateDefaultStaysLegacyWhilePerformanceDefaultsAndRoundTripHaveFourPresets()
        {
            Assert.That(PerformanceSettings.CreateDefault().Modulators, Has.Count.EqualTo(2));

            var source = PerformanceSettings.CreateForPerformance();
            Assert.That(source.Modulators, Has.Count.EqualTo(8));

            source.Presets[0].Seed = 7654;
            var sourceEnvelope = source.Modulators.Find(entry => entry.Id == "pad01-envelope");
            sourceEnvelope.Attack = 1.15;
            sourceEnvelope.Name = "Edited pad envelope";

            var decoded = PerformanceSettingsStore.Decode(PerformanceSettingsStore.Encode(source));
            var envelope = decoded.Modulators.Find(entry => entry.Id == "pad01-envelope");

            Assert.That(decoded.Presets, Has.Count.EqualTo(4));
            Assert.That(decoded.Presets[0].Id, Is.EqualTo("pad01"));
            Assert.That(decoded.Presets[0].Seed, Is.EqualTo(7654));
            Assert.That(envelope.Name, Is.EqualTo("Edited pad envelope"));
            Assert.That(envelope.Attack, Is.EqualTo(1.15));
        }

        [Test]
        public void EncodeRejectsUnknownOrDuplicatePresetIds()
        {
            var unknown = PerformanceSettings.CreateForPerformance();
            unknown.Presets[0].Id = "other";

            Assert.Throws<ArgumentException>(() => PerformanceSettingsStore.Encode(unknown));

            var duplicate = PerformanceSettings.CreateForPerformance();
            duplicate.Presets[3].Id = "lead01";
            Assert.Throws<ArgumentException>(() => PerformanceSettingsStore.Encode(duplicate));
        }

        [Test]
        public void EncodeRejectsMissingOrWrongKindPresetModulatorReferences()
        {
            var missing = PerformanceSettings.CreateForPerformance();
            missing.Presets[0].EnvelopeId = "not-present";
            Assert.Throws<ArgumentException>(() => PerformanceSettingsStore.Encode(missing));

            var wrongKind = PerformanceSettings.CreateForPerformance();
            wrongKind.Presets[0].EnvelopeId = "fx01-oscillator";
            Assert.Throws<ArgumentException>(() => PerformanceSettingsStore.Encode(wrongKind));
        }

        [Test]
        public void EncodeRejectsDuplicateModulatorIds()
        {
            var settings = PerformanceSettings.CreateForPerformance();
            settings.Modulators.Add(new ModulatorSettings
            {
                Id = "pad01-envelope", Name = "Duplicate", Kind = ModulatorKind.Adsr
            });
            Assert.Throws<ArgumentException>(() => PerformanceSettingsStore.Encode(settings));
        }

        [Test]
        public void InvalidPresetSaveLeavesPreviousFileUntouched()
        {
            var store = new PerformanceSettingsStore(_path);
            store.Save(PerformanceSettings.CreateForPerformance());
            var previous = File.ReadAllText(_path);
            var invalid = PerformanceSettings.CreateForPerformance();
            invalid.Presets[1].EnvelopeId = "missing-envelope";

            Assert.Throws<ArgumentException>(() => store.Save(invalid));
            Assert.That(File.ReadAllText(_path), Is.EqualTo(previous));
            Assert.That(Directory.GetFiles(_directory), Is.EqualTo(new[] { _path }));
        }
    }
}
