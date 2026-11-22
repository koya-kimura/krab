using System;
using System.Collections.Generic;
using System.Linq;
using Krab.Common.Looping.Core;
using NUnit.Framework;

namespace Krab.Common.Tests.Editor.Looping
{
    public sealed class EventLooperTests
    {
        [Test]
        public void FinishStartsPlaybackWithRecordedOffsets()
        {
            var looper = new EventLooper<string>();
            var output = new List<LoopPlaybackEvent<string>>();
            looper.StartRecording(10);
            looper.Record("first", 10);
            looper.Record("second", 11.5);
            looper.FinishRecording(14);

            Assert.That(looper.State, Is.EqualTo(EventLooperState.Playing));
            Assert.That(looper.EventCount, Is.EqualTo(2));
            Assert.That(looper.Duration, Is.EqualTo(4));
            Assert.That(looper.RecordStartPosition, Is.EqualTo(10));
            Assert.That(looper.PlaybackStartPosition, Is.EqualTo(14));
            Assert.That(looper.Advance(14, output), Is.EqualTo(1));
            Assert.That(looper.Advance(15, output), Is.Zero);
            Assert.That(looper.Advance(15.5, output), Is.EqualTo(1));
            Assert.That(looper.Advance(15.5, output), Is.Zero);
            Assert.That(output.Select(item => item.Position), Is.EqualTo(new[] { 14d, 15.5 }));
            Assert.That(output.Select(item => item.Payload), Is.EqualTo(new[] { "first", "second" }));
        }

        [Test]
        public void AdvancePreservesSimultaneousAndWrapBoundaryOrder()
        {
            var looper = new EventLooper<string>();
            var output = new List<LoopPlaybackEvent<string>>();
            looper.StartRecording(0);
            looper.Record("start", 0);
            looper.Record("same", 0);
            looper.Record("end", 2);
            looper.FinishRecording(2);

            Assert.That(looper.Advance(6, output), Is.EqualTo(8));
            Assert.That(output.Select(item => item.Position), Is.EqualTo(new[] { 2d, 2, 4, 4, 4, 6, 6, 6 }));
            Assert.That(output.Select(item => item.Payload), Is.EqualTo(new[]
            {
                "start", "same", "end", "start", "same", "end", "start", "same"
            }));
        }

        [TestCase(false, 2d)]
        [TestCase(true, 0d)]
        public void FinishClearsEmptyOrZeroDurationRecording(bool recordEvent, double duration)
        {
            var looper = new EventLooper<int>();
            looper.StartRecording(0);
            if (recordEvent) looper.Record(1, 0);
            looper.FinishRecording(duration);

            Assert.That(looper.State, Is.EqualTo(EventLooperState.Idle));
            Assert.That(looper.EventCount, Is.Zero);
            Assert.That(looper.Duration, Is.Zero);
            Assert.That(looper.Advance(duration, new List<LoopPlaybackEvent<int>>()), Is.Zero);
        }

        [Test]
        public void InstancesCanRecordIndependentlyAndStartReplacesExistingLoop()
        {
            var first = new EventLooper<int>();
            var second = new EventLooper<int>();
            var output = new List<LoopPlaybackEvent<int>>();

            Assert.That(first.Record(0, 0), Is.False);
            first.StartRecording(0);
            first.Record(1, 0);
            second.StartRecording(5);
            second.Record(2, 5);
            first.FinishRecording(2);
            second.FinishRecording(6);

            Assert.That(first.Advance(2, output), Is.EqualTo(1));
            Assert.That(first.Record(9, 2), Is.False);
            first.StartRecording(2);
            first.Record(3, 2);
            Assert.That(first.Advance(2.5, output), Is.Zero);
            first.FinishRecording(3);
            Assert.That(first.Advance(3, output), Is.EqualTo(1));
            Assert.That(second.Advance(6, output), Is.EqualTo(1));

            first.Clear();
            first.Clear();
            Assert.That(first.State, Is.EqualTo(EventLooperState.Idle));
            Assert.That(first.EventCount, Is.Zero);
            Assert.That(first.Advance(4, output), Is.Zero);
            Assert.That(output.Select(item => item.Payload), Is.EqualTo(new[] { 1, 3, 2 }));
        }

        [Test]
        public void NewRecordingReplacesInProgressRecording()
        {
            var looper = new EventLooper<int>();
            var output = new List<LoopPlaybackEvent<int>>();
            looper.StartRecording(2);
            looper.Record(1, 3);
            looper.StartRecording(0);
            looper.Record(2, 0);
            looper.FinishRecording(1);

            Assert.That(looper.Advance(1, output), Is.EqualTo(1));
            Assert.That(output.Single().Payload, Is.EqualTo(2));
        }

        [Test]
        public void InvalidPositionsLeaveStateAndCursorUnchanged()
        {
            var looper = new EventLooper<int>();
            var output = new List<LoopPlaybackEvent<int>>();
            Assert.Throws<ArgumentOutOfRangeException>(() => looper.StartRecording(double.NaN));
            looper.StartRecording(3);
            looper.Record(1, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => looper.Record(2, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => looper.Record(2, double.NegativeInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => looper.FinishRecording(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => looper.FinishRecording(double.PositiveInfinity));
            looper.FinishRecording(4);
            Assert.Throws<ArgumentOutOfRangeException>(() => looper.Advance(3, output));
            Assert.Throws<ArgumentOutOfRangeException>(() => looper.Advance(double.NaN, output));
            Assert.That(looper.Advance(4, output), Is.EqualTo(1));
            Assert.That(output.Single().Payload, Is.EqualTo(1));
        }

        [Test]
        public void AdvanceBoundsCatchUpAndSkipsDueEventsAcrossManyCycles()
        {
            var looper = new EventLooper<int>();
            var output = new List<LoopPlaybackEvent<int>>();
            looper.StartRecording(0);
            looper.Record(0, 0);
            looper.Record(1, 0.5);
            looper.Record(2, 1);
            looper.FinishRecording(1);

            const double position = 1_000_000_000_000.5;
            var emitted = looper.Advance(position, output);

            Assert.That(emitted, Is.EqualTo(EventLooper<int>.MaxEventsPerAdvance));
            Assert.That(output.First().Position, Is.EqualTo(1));
            Assert.That(looper.DroppedEventCount, Is.EqualTo(2_999_999_999_999L - emitted));
            Assert.That(looper.Advance(position, output), Is.Zero);
            output.Clear();
            Assert.That(looper.Advance(position + 0.5, output), Is.EqualTo(2));
            Assert.That(output.Select(item => item.Payload), Is.EqualTo(new[] { 2, 0 }));
            looper.StartRecording(position + 0.5);
            Assert.That(looper.DroppedEventCount, Is.Zero);
        }

        [Test]
        public void ExtremeCatchUpSaturatesAndResumesFromCurrentPosition()
        {
            var looper = new EventLooper<int>();
            var output = new List<LoopPlaybackEvent<int>>();
            looper.StartRecording(0);
            looper.Record(0, 0);
            looper.FinishRecording(1);

            const double position = 1e20;
            Assert.That(looper.Advance(position, output), Is.EqualTo(EventLooper<int>.MaxEventsPerAdvance));
            Assert.That(looper.DroppedEventCount, Is.EqualTo(long.MaxValue));
            Assert.That(looper.Advance(position, output), Is.Zero);
            output.Clear();
            Assert.That(looper.Advance(position + 65536, output), Is.EqualTo(EventLooper<int>.MaxEventsPerAdvance));
            Assert.That(output.First().Position, Is.GreaterThan(position));
        }
    }
}
