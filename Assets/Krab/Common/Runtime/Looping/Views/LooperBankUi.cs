using System;
using System.Collections.Generic;
using Krab.Common.Looping.Core;
using RosettaUI;

namespace Krab.Common.Looping.Views
{
    /// <summary>
    /// Settings UI rows for a looper bank. Input-specific controls (e.g. MIDI learn buttons) are passed in
    /// as accessories so this UI does not depend on any input system.
    /// </summary>
    public static class LooperBankUi
    {
        public static Element Create(ILooperBank bank, string durationUnit,
            Func<int, Element> cycleAccessory = null, Func<int, Element> clearAccessory = null)
        {
            return Create(bank, _ => durationUnit, cycleAccessory, clearAccessory);
        }

        /// <param name="durationUnit">The unit shown after a slot's length; slots may use different clocks.</param>
        /// <param name="settingsAccessory">Per-slot settings placed after the status, e.g. a clock switch.</param>
        public static Element Create(ILooperBank bank, Func<int, string> durationUnit,
            Func<int, Element> cycleAccessory = null, Func<int, Element> clearAccessory = null,
            Func<int, Element> settingsAccessory = null)
        {
            if (bank == null) throw new ArgumentNullException(nameof(bank));
            if (durationUnit == null) throw new ArgumentNullException(nameof(durationUnit));

            var rows = new List<Element>();
            for (var slot = 0; slot < bank.Count; slot++)
            {
                var index = slot;
                rows.Add(UI.Row(
                    UI.Label($"Loop {index + 1}").SetWidth(50),
                    UI.Button(UI.Label(() => ButtonTitle(bank.GetState(index))), () => bank.Cycle(index))
                        .SetWidth(150),
                    cycleAccessory?.Invoke(index) ?? UI.Space(),
                    UI.Button("Clear", () => bank.Clear(index)),
                    clearAccessory?.Invoke(index) ?? UI.Space(),
                    UI.Label(() => Status(bank, index, durationUnit(index))).SetWidth(190),
                    settingsAccessory?.Invoke(index) ?? UI.Space()));
            }

            return UI.Fold("Loopers", rows);
        }

        private static string ButtonTitle(EventLooperState state)
        {
            return state switch
            {
                EventLooperState.Recording => "Recording → Play",
                EventLooperState.Playing => "Playing → Clear",
                _ => "Record"
            };
        }

        private static string Status(ILooperBank bank, int slot, string durationUnit)
        {
            return bank.GetState(slot) == EventLooperState.Recording
                ? $"{bank.GetEventCount(slot)} events · recording"
                : $"{bank.GetEventCount(slot)} events · {bank.GetDuration(slot):0.###} {durationUnit}";
        }
    }
}
