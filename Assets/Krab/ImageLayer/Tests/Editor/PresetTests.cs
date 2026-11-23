using System;
using System.Collections.Generic;
using Krab.ImageLayer.Core.Presets;
using NUnit.Framework;

namespace Krab.ImageLayer.Tests.Editor
{
    public sealed class PresetTests
    {
        private static readonly PatternParameter Size = PatternParameter.Float("Size", 220, 40, 600);
        private static readonly PatternParameter Rows = PatternParameter.Int("Rows", 1, 1, 12);
        private static readonly PatternParameter Alternate = PatternParameter.Bool("Alternate", true);

        [Test]
        public void MissingValuesReadAsDefaults()
        {
            var values = new ParameterValues();

            Assert.That(values.GetFloat(Size), Is.EqualTo(220));
            Assert.That(values.GetInt(Rows), Is.EqualTo(1));
            Assert.That(values.GetBool(Alternate), Is.True);
        }

        [Test]
        public void ValuesAreClampedRoundedAndNonFiniteFallsBackToDefault()
        {
            Assert.That(Size.Normalize(1000), Is.EqualTo(600));
            Assert.That(Size.Normalize(-5), Is.EqualTo(40));
            Assert.That(Size.Normalize(double.NaN), Is.EqualTo(220));
            Assert.That(Rows.Normalize(2.5), Is.EqualTo(3));
            Assert.That(Rows.Normalize(99), Is.EqualTo(12));
            Assert.That(Alternate.Normalize(0.49), Is.EqualTo(0));
            Assert.That(Alternate.Normalize(0.5), Is.EqualTo(1));
        }

        [Test]
        public void StoredValuesOutOfRangeAreNormalizedWhenRead()
        {
            var values = new ParameterValues(new Dictionary<string, double> { ["Size"] = 9999, ["Rows"] = 0.4 });

            Assert.That(values.GetFloat(Size), Is.EqualTo(600));
            Assert.That(values.GetInt(Rows), Is.EqualTo(1));
        }

        [Test]
        public void SetStoresTheNormalizedValueAndKeepsUnknownNames()
        {
            var values = new ParameterValues(new Dictionary<string, double> { ["Removed"] = 7 });
            values.Set(Size, 1000);

            Assert.That(values.Stored["Size"], Is.EqualTo(600));
            Assert.That(values.Stored["Removed"], Is.EqualTo(7));
        }

        [Test]
        public void InvalidDefinitionsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => PatternParameter.Float("", 1, 0, 2));
            Assert.Throws<ArgumentException>(() => PatternParameter.Float("A", 1, 2, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => PatternParameter.Int("A", 5, 0, 2));
        }

        [Test]
        public void DuplicatedPresetsDoNotShareValues()
        {
            var original = new LayoutPreset("march", "March", "March", ParameterValues.Defaults(new[] { Size }));
            var copy = original.Duplicate("march-2", "March 2");
            copy.Values.Set(Size, 300);

            Assert.That(original.Values.GetFloat(Size), Is.EqualTo(220));
            Assert.That(copy.Values.GetFloat(Size), Is.EqualTo(300));
            Assert.That(copy.Pattern, Is.EqualTo("March"));
        }

        [Test]
        public void BlankNamesFallBackToTheId()
        {
            var preset = new LayoutPreset("march", " ", "March", new ParameterValues());
            var entry = new ImageEntry("shirt", null, "shirt");

            Assert.That(preset.Name, Is.EqualTo("march"));
            Assert.That(entry.Name, Is.EqualTo("shirt"));
        }

        [Test]
        public void ImageEntryKeepsClipAndSpeedInRange()
        {
            var entry = new ImageEntry("a", "A", "shirt", clip: -7, framesPerBeat: double.NaN);

            Assert.That(entry.Clip, Is.EqualTo(ImageEntry.AllClips));
            Assert.That(entry.FramesPerBeat, Is.EqualTo(ImageEntry.DefaultFramesPerBeat));

            entry.FramesPerBeat = 1000;
            entry.Clip = 3;
            var copy = entry.Duplicate("b", "B");
            entry.Clip = 0;

            Assert.That(copy.FramesPerBeat, Is.EqualTo(ImageEntry.MaxFramesPerBeat));
            Assert.That(copy.Clip, Is.EqualTo(3));
        }

        [Test]
        public void IdsAreSlugsNumberedWhenTaken()
        {
            Assert.That(PresetNaming.CreateId("March 4 Rows!", new List<string>()), Is.EqualTo("march-4-rows"));
            Assert.That(PresetNaming.CreateId("シャツ", new List<string>()), Is.EqualTo("item"));
            Assert.That(PresetNaming.CreateId("March", new List<string> { "march", "march-2" }), Is.EqualTo("march-3"));
        }

        [Test]
        public void NamesAreNumberedWhenTaken()
        {
            Assert.That(PresetNaming.CreateName("March", new List<string>()), Is.EqualTo("March"));
            Assert.That(PresetNaming.CreateName("March", new List<string> { "March", "March 2" }),
                Is.EqualTo("March 3"));
        }
    }
}
