using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class MultiPresetTests
    {
        private static readonly ReferenceContext Canvas = new(1920, 1080);

        [Test]
        public void CatalogHasFiveUniquePresetIdsAndKeysAndLeavesOtherKeysUnmapped()
        {
            var entries = PresetCatalog.Entries;

            Assert.That(entries, Has.Count.EqualTo(5));
            Assert.That(entries.Select(entry => entry.Id).Distinct().Count(), Is.EqualTo(5));
            Assert.That(entries.Select(entry => entry.Key).Distinct().Count(), Is.EqualTo(5));
            Assert.That(PresetCatalog.IdForKey('Q'), Is.EqualTo("fx01"));
            Assert.That(PresetCatalog.IdForKey('w'), Is.EqualTo("pad01"));
            Assert.That(PresetCatalog.IdForKey('E'), Is.EqualTo("snare01"));
            Assert.That(PresetCatalog.IdForKey('a'), Is.EqualTo("lead01"));
            Assert.That(PresetCatalog.IdForKey('S'), Is.EqualTo("lead03"));
            Assert.That(PresetCatalog.IdForKey('x'), Is.Null);
            Assert.That(PresetCatalog.IdForKey('d'), Is.Null);
        }

        [Test]
        public void PerformanceDefaultsBuildFxPadAndSnareExpectedDrawDataAtAttack()
        {
            var settings = PerformanceSettings.CreateForPerformance();
            var fxItems = Evaluate(settings, "fx01", 1234, 0.05);

            Assert.That(fxItems, Has.Count.EqualTo(150));
            Assert.That(fxItems.All(item => item.Kind == ShapeKind.Circle), Is.True);

            var padItems = Evaluate(settings, "pad01", 1234, 0.05);

            Assert.That(padItems, Has.Count.EqualTo(5));
            Assert.That(padItems.All(item => item.Kind == ShapeKind.Circle), Is.True);
            foreach (var item in padItems)
            {
                Assert.That(item.Style.FillEnabled, Is.False);
                Assert.That(item.Style.StrokeEnabled, Is.True);
                Assert.That(item.Style.StrokeWidth, Is.EqualTo(4));
                AssertColor(item.FillColor, 252 / 255.0, 70 / 255.0, 34 / 255.0);
                Assert.That(item.Opacity, Is.InRange(0, 1));
                Assert.That(item.WorldTransform.M02, Is.EqualTo(0).Within(1e-9));
                Assert.That(item.WorldTransform.M12, Is.EqualTo(0).Within(1e-9));
            }

            var snareItems = Evaluate(settings, "snare01", 1234, 0.05);

            Assert.That(snareItems, Has.Count.EqualTo(5));
            Assert.That(snareItems.All(item => item.Kind == ShapeKind.Polygon && item.Vertices.Count == 3), Is.True);

            var radii = new List<double>();
            foreach (var item in snareItems)
            {
                var x = item.WorldTransform.M02;
                var y = item.WorldTransform.M12;
                var radius = Math.Sqrt(x * x + y * y);
                radii.Add(radius);
                Assert.That(radius, Is.GreaterThan(0));
                Assert.That(x + Canvas.CanvasWidth / 2, Is.InRange(0, Canvas.CanvasWidth));
                Assert.That(y + Canvas.CanvasHeight / 2, Is.InRange(0, Canvas.CanvasHeight));
                Assert.That(item.Opacity, Is.InRange(0, 1));
            }

            Assert.That(radii.Max() - radii.Min(), Is.LessThan(1e-8));

            var before = Evaluate(settings, "snare01", 1234, 0)[0].WorldTransform;
            var peak = Evaluate(settings, "snare01", 1234, 0.025)[0].WorldTransform;
            Assert.That(peak.M02 * peak.M02 + peak.M12 * peak.M12,
                Is.GreaterThan(before.M02 * before.M02 + before.M12 * before.M12));
        }

        [Test]
        public void Lead01HasTenDistinctFiniteSeededVerticesAndReproducesItsSeed()
        {
            var settings = PerformanceSettings.CreateForPerformance();
            var first = PresetCatalog.Build(settings, "lead01", 812);
            var repeated = PresetCatalog.Build(settings, "lead01", 812);
            var different = PresetCatalog.Build(settings, "lead01", 813);
            var vertices = ((ShapeDefinition)first.Definition.Children[0]).Vertices;
            var sameSeedVertices = ((ShapeDefinition)repeated.Definition.Children[0]).Vertices;
            var otherSeedVertices = ((ShapeDefinition)different.Definition.Children[0]).Vertices;

            Assert.That(vertices, Has.Count.EqualTo(10));
            for (var i = 0; i < vertices.Count; i++)
            {
                Assert.That(double.IsNaN(vertices[i].X) || double.IsInfinity(vertices[i].X), Is.False);
                Assert.That(double.IsNaN(vertices[i].Y) || double.IsInfinity(vertices[i].Y), Is.False);
                AssertPoint(vertices[i], sameSeedVertices[i]);
            }
            Assert.That(vertices.Select(vertex => $"{vertex.X:R},{vertex.Y:R}").Distinct().Count(), Is.EqualTo(10));
            Assert.That(
                vertices.Where((vertex, index) =>
                    vertex.X != otherSeedVertices[index].X || vertex.Y != otherSeedVertices[index].Y).Any(),
                Is.True);
        }

        [Test]
        public void Lead03HasTwentyFiveGridRectanglesAndSeededRedCircleSubset()
        {
            var settings = PerformanceSettings.CreateForPerformance();
            var target = PresetCatalog.Build(settings, "lead03", 1234);
            var shapes = target.Definition.Children.Cast<ShapeDefinition>().ToArray();
            var rectangles = shapes.Where(shape => shape.Kind == ShapeKind.Rectangle).ToArray();
            var circles = shapes.Where(shape => shape.Kind == ShapeKind.Circle).ToArray();

            Assert.That(rectangles, Has.Length.EqualTo(25));
            Assert.That(circles.Length, Is.InRange(1, 25));
            Assert.That(
                rectangles.All(shape =>
                    !shape.Style.FillEnabled && shape.Style.StrokeEnabled && shape.Style.StrokeWidth == 3),
                Is.True);
            Assert.That(circles.Select(shape => shape.FillColor.ConstantColor.Value)
                .All(color => color.R > color.G && color.R > color.B), Is.True);

            var items = Evaluate(settings, "lead03", 1234, 0.05);
            var rectangleItems = items.Where(item => item.Kind == ShapeKind.Rectangle).ToArray();
            var xCoordinates = rectangleItems
                .Select(item => Math.Round(item.WorldTransform.M02, 6)).Distinct().OrderBy(x => x).ToArray();
            var yCoordinates = rectangleItems
                .Select(item => Math.Round(item.WorldTransform.M12, 6)).Distinct().OrderBy(y => y).ToArray();

            Assert.That(xCoordinates, Has.Length.EqualTo(5));
            Assert.That(yCoordinates, Has.Length.EqualTo(5));
            Assert.That(xCoordinates[0], Is.EqualTo(-302.4).Within(1e-5));
            Assert.That(xCoordinates[4], Is.EqualTo(302.4).Within(1e-5));
            Assert.That(yCoordinates[0], Is.EqualTo(-302.4).Within(1e-5));
            Assert.That(yCoordinates[4], Is.EqualTo(302.4).Within(1e-5));
            Assert.That(rectangleItems.All(item => item.Opacity >= 0 && item.Opacity <= 1), Is.True);
        }

        [Test]
        public void AllPresetsAndRepeatedSpawnsHaveIndependentStateAndExpireNaturally()
        {
            var settings = PerformanceSettings.CreateForPerformance();
            var collection = new VoiceCollection();
            var voices = new List<VoiceInstance>();
            foreach (var descriptor in PresetCatalog.Entries)
            {
                voices.Add(collection.Spawn(PresetCatalog.Build(settings, descriptor.Id, 100), 0, 120, 100));
            }

            var repeated = collection.Spawn(PresetCatalog.Build(settings, "pad01", 101), 0, 120, 101);
            voices.Add(repeated);

            Assert.That(collection.Voices, Has.Count.EqualTo(6));
            Assert.That(voices[1].Modulators[0], Is.Not.SameAs(repeated.Modulators[0]));
            for (var i = 0; i < voices.Count; i++)
            {
                for (var j = i + 1; j < voices.Count; j++)
                {
                    Assert.That(voices[i].Modulators[0], Is.Not.SameAs(voices[j].Modulators[0]));
                }
            }

            foreach (var voice in voices)
            {
                Assert.That(voice.Evaluate(0.05, Canvas), Is.Not.Empty);
            }

            Assert.That(collection.Evaluate(20, Canvas), Is.Empty);
            Assert.That(collection.Voices, Is.Empty);
        }

        [Test]
        public void EditedPresetSettingsAffectNewVoicesWithoutChangingExistingVoice()
        {
            var settings = PerformanceSettings.CreateForPerformance();
            var oldTarget = PresetCatalog.Build(settings, "lead03", 1234);
            var oldVoice = new VoiceInstance(oldTarget, new VoiceTime(0, 120), 1234);
            var oldItemsBeforeEdit = oldVoice.Evaluate(0.05, Canvas);
            var oldOpacity = oldItemsBeforeEdit[0].Opacity;
            var oldEnvelope = (AdsrInstance)oldVoice.Modulators[0];

            settings.Modulators.Single(modulator => modulator.Id == "lead03-envelope").Attack = 0.01;
            var newVoice = new VoiceInstance(
                PresetCatalog.Build(settings, "lead03", 1234), new VoiceTime(0, 120), 1234);
            var newOpacity = newVoice.Evaluate(0.05, Canvas)[0].Opacity;

            Assert.That(oldEnvelope.Definition.Attack, Is.EqualTo(0.2));
            Assert.That(oldVoice.Evaluate(0.05, Canvas)[0].Opacity, Is.EqualTo(oldOpacity));
            Assert.That(newVoice.Modulators[0], Is.Not.SameAs(oldEnvelope));
            Assert.That(newOpacity, Is.Not.EqualTo(oldOpacity));
        }

        private static IReadOnlyList<DrawItem> Evaluate(
            PerformanceSettings settings, string id, int seed, double seconds)
        {
            var voice = new VoiceInstance(PresetCatalog.Build(settings, id, seed), new VoiceTime(0, 120), seed);
            return voice.Evaluate(seconds, Canvas);
        }

        private static void AssertColor(RgbaColor actual, double r, double g, double b)
        {
            Assert.That(actual.R, Is.EqualTo(r));
            Assert.That(actual.G, Is.EqualTo(g));
            Assert.That(actual.B, Is.EqualTo(b));
        }

        private static void AssertPoint(Point2 actual, Point2 expected)
        {
            Assert.That(actual.X, Is.EqualTo(expected.X));
            Assert.That(actual.Y, Is.EqualTo(expected.Y));
        }
    }
}
