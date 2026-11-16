using System;
using System.Collections.Generic;
using System.Linq;
using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Motion;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Text;
using NUnit.Framework;

namespace Krab.TypeLayout.Tests.Editor
{
    public sealed class MotionTests
    {
        private static readonly string[] Letters = { "M", "o", "d", "e", "l" };
        private static readonly LinePath Line = new(new Point2(0, 0), new Point2(1000, 0));
        private readonly List<string> _picked = new();
        private readonly List<FlowItem> _items = new();

        [Test]
        public void RandomPicksAreReproducibleAndIndependentPerSlot()
        {
            TextPicker.Random(Letters, 200, 7, 0, _picked);
            var first = _picked.ToArray();
            TextPicker.Random(Letters, 200, 7, 0, _picked);
            Assert.That(_picked, Is.EqualTo(first));

            // Fewer slots keep the same leading picks: a slot does not depend on how many follow it.
            TextPicker.Random(Letters, 10, 7, 0, _picked);
            Assert.That(_picked, Is.EqualTo(first.Take(10)));

            Assert.That(first.Distinct().Count(), Is.EqualTo(Letters.Length));
            foreach (var letter in Letters)
                Assert.That(first.Count(pick => pick == letter), Is.InRange(20, 60));
        }

        [Test]
        public void RandomRerollsWithStepAndSeed()
        {
            TextPicker.Random(Letters, 60, 7, 0, _picked);
            var baseline = _picked.ToArray();
            TextPicker.Random(Letters, 60, 7, 1, _picked);
            Assert.That(_picked, Is.Not.EqualTo(baseline));
            TextPicker.Random(Letters, 60, 8, 0, _picked);
            Assert.That(_picked, Is.Not.EqualTo(baseline));
            Assert.That(TextPicker.Hash(7, 3, 2), Is.EqualTo(TextPicker.Hash(7, 3, 2)));
            Assert.Throws<ArgumentException>(() => TextPicker.Random(Array.Empty<string>(), 1, 0, 0, _picked));
        }

        [Test]
        public void StepsChangeExactlyOnBeatBoundaries()
        {
            Assert.That(BeatSteps.Step(0, 1), Is.EqualTo(0));
            Assert.That(BeatSteps.Step(3.999, 1), Is.EqualTo(3));
            Assert.That(BeatSteps.Step(4, 1), Is.EqualTo(4));
            Assert.That(BeatSteps.Step(4.2, 0.1), Is.EqualTo(42));
            Assert.That(BeatSteps.Step(7.9, 4), Is.EqualTo(1));
            Assert.That(BeatSteps.Step(-0.5, 1), Is.EqualTo(-1));
            Assert.That(BeatSteps.Scroll(2.5, 40), Is.EqualTo(100));
            Assert.Throws<ArgumentOutOfRangeException>(() => BeatSteps.Step(1, 0));
        }

        [Test]
        public void MarqueeWrapsByOneRepeatAndCoversBothEnds()
        {
            // Five glyphs of 50 with 10 between: one repeat is 300 long.
            var advances = Enumerable.Repeat(50.0, 5).ToArray();
            var options = new FlowOptions(10, fill: FlowFill.Glyphs);
            FlowDistributor.Flow(Line, advances, options, _items);
            var still = _items.Select(item => item.Distance).ToArray();

            FlowDistributor.Flow(Line, advances, new FlowOptions(10, fill: FlowFill.Glyphs, offset: 300), _items);
            Assert.That(_items.Select(item => item.Distance), Is.EqualTo(still).Within(1e-9));

            FlowDistributor.Flow(Line, advances, new FlowOptions(10, fill: FlowFill.Glyphs, offset: 1230), _items);
            var distances = _items.Select(item => item.Distance).ToArray();
            Assert.That(distances.Min() - 25, Is.LessThanOrEqualTo(60));
            Assert.That(distances.Max() + 25, Is.GreaterThanOrEqualTo(940));
            Assert.That(distances.Zip(distances.Skip(1), (a, b) => b - a), Is.All.EqualTo(60).Within(1e-9));

            // The scroll moves the text: offset 30 shifts every glyph by 30 compared with offset 0.
            FlowDistributor.Flow(Line, advances, new FlowOptions(10, fill: FlowFill.Glyphs, offset: 30), _items);
            // A glyph only partly on the path is dropped, so the first visible one is the shifted first glyph.
            Assert.That(_items[0].ClusterIndex, Is.EqualTo(0));
            Assert.That(_items[0].Distance, Is.EqualTo(still[0] + 30).Within(1e-9));
        }

        [Test]
        public void TextWithoutFillScrollsOffAnOpenPath()
        {
            var advances = Enumerable.Repeat(50.0, 5).ToArray();
            FlowDistributor.Flow(Line, advances, new FlowOptions(offset: 2000), _items);
            Assert.That(_items, Is.Empty);
        }
    }
}
