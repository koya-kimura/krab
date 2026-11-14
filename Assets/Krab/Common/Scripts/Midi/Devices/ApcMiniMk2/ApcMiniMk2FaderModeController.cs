using System;
using Krab.Common.Utils;

namespace Krab.Common.Midi.Devices.ApcMiniMk2
{
    public sealed class ApcMiniMk2FaderModeController
    {
        private readonly float[] _rawValues = new float[ApcMiniMk2.FaderCount];
        private readonly FaderModeAssignment[] _assignments = new FaderModeAssignment[ApcMiniMk2.ChannelFaderCount];
        private readonly bool[] _active = new bool[ApcMiniMk2.ChannelFaderCount];
        private readonly float[] _previousValues = new float[ApcMiniMk2.ChannelFaderCount];
        private readonly ApcMiniMk2FaderModeRepository _repository;
        private long _beatIndex;

        public ApcMiniMk2FaderModeController()
            : this(new ApcMiniMk2FaderModeRepository())
        {
        }

        public ApcMiniMk2FaderModeController(ApcMiniMk2FaderModeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));

            for (var i = 0; i < ApcMiniMk2.ChannelFaderCount; i++)
            {
                _assignments[i] = _repository.LoadAssignment(i);
                _active[i] = _repository.LoadActive(i);
            }
        }

        public event Action<int, float> EffectiveValueChanged;
        public event Action StateChanged;

        public float GetRawValue(int index)
        {
            ApcMiniMk2.ValidateFaderIndex(index);
            return _rawValues[index];
        }

        public float GetValue(int index)
        {
            ApcMiniMk2.ValidateFaderIndex(index);
            if (index == ApcMiniMk2.MasterFaderIndex || !_active[index]) return _rawValues[index];

            return _assignments[index] == FaderModeAssignment.Mute ? 0f : GetRandomValue(index);
        }

        public FaderModeAssignment GetAssignment(int index)
        {
            ApcMiniMk2.ValidateChannelFaderIndex(index);
            return _assignments[index];
        }

        public bool IsAssignedModeActive(int index)
        {
            ApcMiniMk2.ValidateChannelFaderIndex(index);
            return _active[index];
        }

        public void SetRawValue(int index, float value)
        {
            ApcMiniMk2.ValidateFaderIndex(index);
            _rawValues[index] = Math.Clamp(value, 0f, 1f);
        }

        public void SetAssignment(int index, FaderModeAssignment assignment)
        {
            ApcMiniMk2.ValidateChannelFaderIndex(index);
            if (assignment != FaderModeAssignment.Mute && assignment != FaderModeAssignment.Random)
                throw new ArgumentOutOfRangeException(nameof(assignment));

            if (_assignments[index] == assignment) return;

            var previous = GetValue(index);
            _assignments[index] = assignment;
            _repository.SaveAssignment(index, assignment);
            StateChanged?.Invoke();
            PublishIfChanged(index, previous);
        }

        public void ToggleAssignedMode(int index)
        {
            ApcMiniMk2.ValidateChannelFaderIndex(index);
            var previous = GetValue(index);
            _active[index] = !_active[index];
            _repository.SaveActive(index, _active[index]);
            StateChanged?.Invoke();
            PublishIfChanged(index, previous);
        }

        public void SetBeat(long beatIndex)
        {
            if (_beatIndex == beatIndex) return;

            for (var i = 0; i < _previousValues.Length; i++) _previousValues[i] = GetValue(i);

            _beatIndex = beatIndex;
            for (var i = 0; i < _previousValues.Length; i++) PublishIfChanged(i, _previousValues[i]);
        }

        private float GetRandomValue(int index)
        {
            var hash = Pcg.Hash2D(new UInt2(unchecked((uint)_beatIndex), (uint)index));
            return (hash.X & 1u) == 0u ? 0f : 1f;
        }

        private void PublishIfChanged(int index, float previous)
        {
            var current = GetValue(index);
            if (!current.Equals(previous)) EffectiveValueChanged?.Invoke(index, current);
        }
    }
}
