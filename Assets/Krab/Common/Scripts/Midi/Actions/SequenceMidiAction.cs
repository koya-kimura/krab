using System;
using System.Text;
using Krab.Common.Midi.Core;
using UnityEngine;

namespace Krab.Common.Midi.Actions
{
    public sealed class SequenceMidiAction : IDisposable, IMidiActionRuntime
    {
        private readonly string _actionId;
        private readonly SequenceLedPalette _palette;
        private readonly MidiActionStateRepository _repository;
        private bool[] _steps;
        private long _beatIndex;

        public SequenceMidiAction(string actionId, int stepCount, SequenceLedPalette palette,
            MidiActionStateRepository repository)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("An action id is required.", nameof(actionId));

            if (stepCount < 1) throw new ArgumentOutOfRangeException(nameof(stepCount));

            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _actionId = actionId;
            _steps = new bool[stepCount];
            _palette = palette;

            var normalized = NormalizePattern(_repository.LoadSequencePattern(_actionId, new string('0', stepCount)));
            for (var i = 0; i < stepCount; i++) _steps[i] = normalized[i] == '1';
            _repository.SaveSequencePattern(_actionId, normalized);
        }

        public int StepCount => _steps.Length;
        public int CurrentStepIndex => PositiveModulo(_beatIndex, StepCount);
        public bool IsCurrentStepEnabled => _steps[CurrentStepIndex];

        public bool IsStepEnabled(int index)
        {
            return _steps[Validate(index)];
        }

        public void ToggleStep(int index)
        {
            index = Validate(index);
            _steps[index] = !_steps[index];
            SavePattern();
        }

        public string GetBindingId(int index)
        {
            return $"{_actionId}.Step.{Validate(index)}";
        }

        public void Update(in MidiActionUpdateContext context)
        {
            _beatIndex = context.BeatIndex;
        }

        public void Dispose()
        {
        }

        internal string GetBindingIdForIndex(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            return $"{_actionId}.Step.{index}";
        }

        internal IMidiAction CreateStep(int index)
        {
            return new SequenceStepMidiAction(this, Validate(index));
        }

        internal void SetStepCount(int stepCount)
        {
            if (stepCount < 1) throw new ArgumentOutOfRangeException(nameof(stepCount));

            if (stepCount == StepCount) return;

            var resized = new bool[stepCount];
            Array.Copy(_steps, resized, Math.Min(_steps.Length, resized.Length));
            _steps = resized;
            SavePattern();
        }

        private static int PositiveModulo(long value, int divisor)
        {
            var result = value % divisor;
            return (int)(result < 0 ? result + divisor : result);
        }

        private int Validate(int index)
        {
            if (index < 0 || index >= StepCount) throw new ArgumentOutOfRangeException(nameof(index));

            return index;
        }

        private string NormalizePattern(string pattern)
        {
            var chars = new char[StepCount];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = pattern != null && i < pattern.Length && pattern[i] == '1' ? '1' : '0';
            return new string(chars);
        }

        private void SavePattern()
        {
            var builder = new StringBuilder(StepCount);
            for (var i = 0; i < StepCount; i++) builder.Append(_steps[i] ? '1' : '0');
            _repository.SaveSequencePattern(_actionId, builder.ToString());
        }

        private Color ResolveStepColor(int index, Color configuredColor)
        {
            if (index == CurrentStepIndex) return _palette.CurrentColor;

            return _steps[index] ? configuredColor : MidiLedColor.Dim(configuredColor, _palette.DisabledBrightness);
        }

        private sealed class SequenceStepMidiAction : IMidiAction
        {
            private readonly SequenceMidiAction _owner;
            private readonly int _index;

            public SequenceStepMidiAction(SequenceMidiAction owner, int index)
            {
                _owner = owner;
                _index = index;
            }

            public MidiActionKind Kind => MidiActionKind.Sequence;
            public string DisplayName => $"Sequence (step {_index + 1}/{_owner.StepCount})";
            public IMidiActionRuntime Runtime => _owner;

            public void Execute(MidiMessage message)
            {
                if (message.IsPressed) _owner.ToggleStep(_index);
            }

            public Color ResolveLedColor(Color configuredColor)
            {
                return _owner.ResolveStepColor(_index, configuredColor);
            }

            public void Dispose()
            {
            }
        }
    }
}
