using System;
using System.Collections.Generic;
using Krab.Common.Looping.Core;
using Krab.Common.Midi.Bindings;
using Krab.Common.Midi.Core;
using UnityEngine;

namespace Krab.Common.Looping.Midi
{
    /// <summary>
    /// Optional MIDI adapter for a looper bank: per slot, a 3-state Cycle button whose LED follows the slot
    /// (Idle/Recording/Playing) and a Clear button. Bindings are learned and saved by action ID.
    /// </summary>
    public sealed class LooperBankMidi : IDisposable
    {
        private static readonly Color[] StateColors = { Color.gray, Color.red, Color.green };

        private readonly MidiBindingManager _bindings;
        private readonly ILooperBank _bank;
        private readonly string _actionPrefix;
        private readonly Param<int>[] _states;
        private readonly List<string> _actionIds = new();
        private bool _syncing;
        private bool _disposed;

        public LooperBankMidi(MidiBindingManager bindings, ILooperBank bank, string actionPrefix)
        {
            _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            _bank = bank ?? throw new ArgumentNullException(nameof(bank));
            if (string.IsNullOrWhiteSpace(actionPrefix))
                throw new ArgumentException("An action prefix is required.", nameof(actionPrefix));

            _actionPrefix = actionPrefix;
            _states = new Param<int>[bank.Count];
            for (var slot = 0; slot < bank.Count; slot++) Register(slot);

            _bank.StateChanged += SyncState;
        }

        public static string CycleActionId(string actionPrefix, int slot) => $"{actionPrefix}.{slot + 1}.Cycle";

        public static string ClearActionId(string actionPrefix, int slot) => $"{actionPrefix}.{slot + 1}.Clear";

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _bank.StateChanged -= SyncState;
            foreach (var id in _actionIds) _bindings.UnregisterAction(id);
            _actionIds.Clear();
        }

        private void Register(int slot)
        {
            var state = new Param<int>(0);
            var cycleId = CycleActionId(_actionPrefix, slot);
            _bindings.RegisterState(cycleId, state, StateColors.Length, StateColors);
            _actionIds.Add(cycleId);
            _states[slot] = state;

            // The state action restores its saved index, but loop content is session-only.
            SyncState(slot);
            state.Changed += _ =>
            {
                if (_syncing) return;

                _bank.Cycle(slot);
                SyncState(slot);
            };

            var clear = new Param<bool>(false);
            clear.Changed += pressed =>
            {
                if (pressed) _bank.Clear(slot);
            };
            var clearId = ClearActionId(_actionPrefix, slot);
            _bindings.RegisterMomentary(clearId, clear);
            _actionIds.Add(clearId);
        }

        private void SyncState(int slot)
        {
            _syncing = true;
            try
            {
                _states[slot].SetValue((int)_bank.GetState(slot));
            }
            finally
            {
                _syncing = false;
            }
        }
    }
}
