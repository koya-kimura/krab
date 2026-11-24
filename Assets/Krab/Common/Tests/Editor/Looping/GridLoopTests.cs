using System;
using System.Collections.Generic;
using System.Linq;
using Krab.Common.Looping.Core;
using NUnit.Framework;

namespace Krab.Common.Tests.Editor.Looping
{
    public sealed class GridLoopTests
    {
        [TestCase(0.2, 15.7, 0, 16, 16)]
        [TestCase(0.2, 16.3, 0, 16, 20)]
        [TestCase(3.9, 4.5, 4, 4, 8)]
        [TestCase(4, 16, 4, 12, 16)]
        [TestCase(1, 1.5, 0, 4, 4)]
        [TestCase(3.9, 3.95, 4, 4, 4)]
        public void SnapToGridRoundsOriginAndLengthAndStartsOnTheNextGridLine(
            double recordStart, double stop, double origin, double duration, double start)
        {
            var window = LoopWindow.SnapToGrid(recordStart, stop, 4);

            Assert.That(window.Origin, Is.EqualTo(origin));
            Assert.That(window.Duration, Is.EqualTo(duration));
            Assert.That(window.Start, Is.EqualTo(start));
        }

        [Test]
        public void SnapToGridKeepsAStopJustPastAGridLineOnThatLine()
        {
            var window = LoopWindow.SnapToGrid(0, 16 + 1e-9, 4);

            Assert.That(window.Duration, Is.EqualTo(16));
            Assert.That(window.Start, Is.EqualTo(16 + 1e-9));
        }

        [Test]
        public void SnapToGridRejectsInvalidArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LoopWindow.SnapToGrid(0, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => LoopWindow.SnapToGrid(0, 1, double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => LoopWindow.SnapToGrid(2, 1, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => LoopWindow.SnapToGrid(double.NaN, 1, 4));
        }

        [Test]
        public void WindowedFinishWrapsLateEventsAndWaitsForPlaybackStart()
        {
            var looper = new EventLooper<string>();
            var output = new List<LoopPlaybackEvent<string>>();
            looper.StartRecording(0.2);
            looper.Record("a", 1);
            looper.Record("b", 3);
            looper.Record("late", 4.1);
            looper.FinishRecording(4.3, 0, 4, 8);

            Assert.That(looper.State, Is.EqualTo(EventLooperState.Playing));
            Assert.That(looper.Duration, Is.EqualTo(4));
            Assert.That(looper.PlaybackStartPosition, Is.EqualTo(8));
            Assert.That(looper.Advance(7.9, output), Is.Zero);

            looper.Advance(12, output);
            Assert.That(output.Select(item => item.Payload), Is.EqualTo(new[] { "late", "a", "b" }));
            Assert.That(output.Select(item => item.Position), Is.EqualTo(new[] { 8.1, 9, 11 }).Within(1e-9));
        }

        [Test]
        public void WindowedFinishMeasuresEarlyEventsFromTheSnappedOrigin()
        {
            var looper = new EventLooper<string>();
            var output = new List<LoopPlaybackEvent<string>>();
            looper.StartRecording(3.8);
            looper.Record("early", 3.9);
            looper.Record("on", 4);
            looper.FinishRecording(8, 4, 4, 8);

            looper.Advance(11.95, output);
            Assert.That(output.Select(item => item.Payload), Is.EqualTo(new[] { "on", "early" }));
            Assert.That(output.Select(item => item.Position), Is.EqualTo(new[] { 8, 11.9 }).Within(1e-9));
        }

        [Test]
        public void WindowedFinishKeepsRecordingOrderForSimultaneousWrappedEvents()
        {
            var looper = new EventLooper<string>();
            var output = new List<LoopPlaybackEvent<string>>();
            looper.StartRecording(0);
            looper.Record("zero", 0);
            looper.Record("first", 4);
            looper.Record("second", 4);
            looper.FinishRecording(4, 0, 4, 4);

            looper.Advance(4, output);
            Assert.That(output.Select(item => item.Payload), Is.EqualTo(new[] { "zero", "first", "second" }));
        }

        [Test]
        public void WindowedFinishValidatesArgumentsAndReturnsEmptyRecordingToIdle()
        {
            var looper = new EventLooper<string>();
            looper.StartRecording(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => looper.FinishRecording(4, 0, 0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => looper.FinishRecording(4, 0, 4, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => looper.FinishRecording(4, double.NaN, 4, 4));
            Assert.That(looper.State, Is.EqualTo(EventLooperState.Recording));

            looper.FinishRecording(4, 0, 4, 4);
            Assert.That(looper.State, Is.EqualTo(EventLooperState.Idle));
        }

        [Test]
        public void BankUsesEachSlotsClockAndSnapsOnlyGridSlots()
        {
            double seconds = 0;
            double beats = 0;
            var bank = new LooperBank<string>(2, slot => slot == 0 ? seconds : beats, slot => slot == 0 ? 0 : 4);
            var free = new List<LoopPlaybackEvent<string>>();
            var synced = new List<LoopPlaybackEvent<string>>();

            seconds = 10;
            beats = 0.3;
            bank.Cycle(0);
            bank.Cycle(1);
            seconds = 11;
            beats = 1;
            Assert.That(bank.Record("x"), Is.EqualTo(2));

            seconds = 12.5;
            beats = 3.8;
            bank.Cycle(0);
            bank.Cycle(1);

            Assert.That(bank.GetDuration(0), Is.EqualTo(2.5));
            Assert.That(bank.GetDuration(1), Is.EqualTo(4));
            Assert.That(bank.GetGridLength(0), Is.Zero);
            Assert.That(bank.GetGridLength(1), Is.EqualTo(4));
            Assert.That(bank[1].PlaybackStartPosition, Is.EqualTo(4));

            seconds = 13.5;
            beats = 5;
            bank.Advance(0, free);
            bank.Advance(1, synced);
            Assert.That(free.Select(item => item.Position), Is.EqualTo(new[] { 13.5 }));
            Assert.That(synced.Select(item => item.Position), Is.EqualTo(new[] { 5d }));
        }
    }
}
