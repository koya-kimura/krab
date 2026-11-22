using System;
using System.Collections.Generic;
using System.Linq;
using Krab.Common.Looping.Core;
using NUnit.Framework;

namespace Krab.Common.Tests.Editor.Looping
{
    public sealed class LooperBankTests
    {
        private double _now;

        [Test]
        public void CycleRecordsPlaysAndClearsOnOneButton()
        {
            var bank = CreateBank(2);
            var changed = new List<int>();
            bank.StateChanged += changed.Add;

            _now = 1;
            bank.Cycle(0);
            Assert.That(bank.GetState(0), Is.EqualTo(EventLooperState.Recording));

            bank.Record("a");
            _now = 3;
            bank.Cycle(0);
            Assert.That(bank.GetState(0), Is.EqualTo(EventLooperState.Playing));
            Assert.That(bank.GetDuration(0), Is.EqualTo(2));

            bank.Cycle(0);
            Assert.That(bank.GetState(0), Is.EqualTo(EventLooperState.Idle));
            Assert.That(bank.GetEventCount(0), Is.Zero);
            Assert.That(changed, Is.EqualTo(new[] { 0, 0, 0 }));
        }

        [Test]
        public void EmptyRecordingReturnsToIdleAndNotifies()
        {
            var bank = CreateBank(1);
            var changed = 0;
            bank.StateChanged += _ => changed++;

            bank.Cycle(0);
            _now = 2;
            bank.Cycle(0);

            Assert.That(bank.GetState(0), Is.EqualTo(EventLooperState.Idle));
            Assert.That(changed, Is.EqualTo(2));
        }

        [Test]
        public void RecordGoesOnlyToRecordingSlots()
        {
            var bank = CreateBank(3);
            bank.Cycle(0);
            bank.Cycle(2);

            Assert.That(bank.Record("x"), Is.EqualTo(2));
            Assert.That(bank.GetEventCount(0), Is.EqualTo(1));
            Assert.That(bank.GetEventCount(1), Is.Zero);
            Assert.That(bank.GetEventCount(2), Is.EqualTo(1));
        }

        [Test]
        public void AdvanceUsesTheSharedClockPerSlot()
        {
            var bank = CreateBank(2);
            var output = new List<LoopPlaybackEvent<string>>();
            bank.Cycle(0);
            bank.Record("a");
            _now = 0.5;
            bank.Record("b");
            _now = 1;
            bank.Cycle(0);

            Assert.That(bank.Advance(0, output), Is.EqualTo(1));
            Assert.That(bank.Advance(1, output), Is.Zero);
            _now = 1.5;
            Assert.That(bank.Advance(0, output), Is.EqualTo(1));
            Assert.That(output.Select(item => item.Payload), Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void ClearSkipsNotificationWhenAlreadyEmpty()
        {
            var bank = CreateBank(2);
            var changed = new List<int>();
            bank.StateChanged += changed.Add;

            bank.Clear(0);
            bank.Cycle(1);
            bank.ClearAll();

            Assert.That(changed, Is.EqualTo(new[] { 1, 1 }));
            Assert.That(bank.GetState(1), Is.EqualTo(EventLooperState.Idle));
        }

        [Test]
        public void RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LooperBank<string>(0, () => 0));
            Assert.Throws<ArgumentNullException>(() => new LooperBank<string>(1, null));
            var bank = CreateBank(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => bank.Cycle(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => bank.GetState(-1));
        }

        private LooperBank<string> CreateBank(int count)
        {
            _now = 0;
            return new LooperBank<string>(count, () => _now);
        }
    }
}
