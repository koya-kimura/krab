using System;
using System.Collections.Generic;
using Krab.Common.Midi.Actions;
using Krab.Common.Midi.Core;
using UnityEngine;

namespace Krab.Common.Midi.Bindings
{
    public sealed class MidiBindingManager : IDisposable
    {
        private readonly Dictionary<string, MidiBinding> _bindings = new();
        private readonly Dictionary<string, ActionRegistration> _actions = new();
        private readonly HashSet<IMidiActionRuntime> _runtimes = new(ReferenceEqualityComparer<IMidiActionRuntime>.Instance);
        private readonly HashSet<ITransientMidiAction> _transientActions = new(ReferenceEqualityComparer<ITransientMidiAction>.Instance);
        private readonly MidiBindingRepository _repository;
        private readonly MidiActionStateRepository _stateRepository;
        private string _learningParamId;

        public event Action<string, MidiControl?> BindingChanged;
        public IEnumerable<MidiBinding> Bindings => _bindings.Values;
        public string LearningParamId => _learningParamId;

        public MidiBindingManager() : this(new MidiBindingRepository(), new MidiActionStateRepository()) { }

        public MidiBindingManager(MidiBindingRepository repository, MidiActionStateRepository stateRepository = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _stateRepository = stateRepository ?? new MidiActionStateRepository();
        }

        public void Register(string paramId, IMidiAction action)
        {
            ValidateActionId(paramId);
            if (action == null) throw new ArgumentNullException(nameof(action));
            EnsureActionIdAvailable(paramId);
            try
            {
                RegisterBinding(paramId, action);
                _actions.Add(paramId, new ActionRegistration(action, new[] { paramId }));
                RebuildRuntimeSets();
            }
            catch
            {
                RemoveBinding(paramId);
                action.Dispose();
                throw;
            }
        }

        public ToggleMidiAction RegisterToggle(string actionId, Param<bool> value)
        {
            var action = new ToggleMidiAction(actionId, value, _stateRepository);
            Register(actionId, action);
            return action;
        }

        public FloatMidiAction RegisterFloat(string actionId, Param<float> value)
        {
            var action = new FloatMidiAction(value);
            Register(actionId, action);
            return action;
        }

        public OneShotMidiAction RegisterOneShot(string actionId, Param<bool> value)
        {
            var action = new OneShotMidiAction(value);
            Register(actionId, action);
            return action;
        }

        public MomentaryMidiAction RegisterMomentary(string actionId, Param<bool> value)
        {
            var action = new MomentaryMidiAction(value);
            Register(actionId, action);
            return action;
        }

        public StateMidiAction RegisterState(string actionId, Param<int> value, int stateCount,
            IReadOnlyList<Color> stateColors = null)
        {
            var action = new StateMidiAction(actionId, value, stateCount, stateColors, _stateRepository);
            Register(actionId, action);
            return action;
        }

        public RadioMidiAction RegisterRadio(string actionId, Param<int> selectedIndex, int optionCount)
        {
            var optionKeys = new string[optionCount];
            for (var i = 0; i < optionKeys.Length; i++) optionKeys[i] = i.ToString();
            return RegisterRadio(actionId, selectedIndex, optionKeys);
        }

        public RadioMidiAction RegisterRadio(string actionId, Param<int> selectedIndex,
            IReadOnlyList<string> optionKeys)
        {
            ValidateActionId(actionId);
            EnsureActionIdAvailable(actionId);
            var action = new RadioMidiAction(actionId, selectedIndex, optionKeys, _stateRepository);
            var bindingIds = new string[optionKeys.Count];
            var registeredCount = 0;
            try
            {
                for (var i = 0; i < optionKeys.Count; i++)
                {
                    var bindingId = action.GetBindingIdForKey(optionKeys[i]);
                    bindingIds[i] = bindingId;
                    RegisterBinding(bindingId, action.CreateOption(optionKeys[i]));
                    registeredCount++;
                }
                _actions.Add(actionId, new ActionRegistration(action, bindingIds));
                RebuildRuntimeSets();
                return action;
            }
            catch
            {
                for (var i = 0; i < registeredCount; i++) RemoveBinding(bindingIds[i]);
                action.Dispose();
                throw;
            }
        }

        public void SetRadioOptions(string actionId, IReadOnlyList<string> optionKeys)
        {
            if (!_actions.TryGetValue(actionId, out var registration) ||
                registration.Owner is not RadioMidiAction radio)
                throw new InvalidOperationException($"Radio action is not registered: {actionId}");
            if (optionKeys == null || optionKeys.Count < 1)
                throw new ArgumentException("A radio group requires at least one option.", nameof(optionKeys));

            var newBindingIds = new string[optionKeys.Count];
            var unique = new HashSet<string>();
            for (var i = 0; i < optionKeys.Count; i++)
            {
                var bindingId = radio.GetBindingIdForKey(optionKeys[i]);
                if (!unique.Add(bindingId))
                    throw new ArgumentException("Radio option keys must be unique.", nameof(optionKeys));
                if (_bindings.ContainsKey(bindingId) && Array.IndexOf(registration.BindingIds, bindingId) < 0)
                    throw new InvalidOperationException($"Already registered: {bindingId}");
                newBindingIds[i] = bindingId;
            }

            foreach (var oldBindingId in registration.BindingIds)
                if (Array.IndexOf(newBindingIds, oldBindingId) < 0) RemoveBinding(oldBindingId);

            radio.SetOptionKeys(optionKeys);
            for (var i = 0; i < optionKeys.Count; i++)
                if (!_bindings.ContainsKey(newBindingIds[i]))
                    RegisterBinding(newBindingIds[i], radio.CreateOption(optionKeys[i]));

            registration.BindingIds = newBindingIds;
            RebuildRuntimeSets();
        }

        public SequenceMidiAction RegisterSequence(string actionId, int stepCount,
            SequenceLedPalette? palette = null)
        {
            ValidateActionId(actionId);
            EnsureActionIdAvailable(actionId);
            var action = new SequenceMidiAction(actionId, stepCount, palette ?? SequenceLedPalette.Default,
                _stateRepository);
            var bindingIds = new string[stepCount];
            var registeredCount = 0;
            try
            {
                for (var i = 0; i < stepCount; i++)
                {
                    var bindingId = action.GetBindingId(i);
                    bindingIds[i] = bindingId;
                    RegisterBinding(bindingId, action.CreateStep(i));
                    registeredCount++;
                }
                _actions.Add(actionId, new ActionRegistration(action, bindingIds));
                RebuildRuntimeSets();
                return action;
            }
            catch
            {
                for (var i = 0; i < registeredCount; i++) RemoveBinding(bindingIds[i]);
                action.Dispose();
                throw;
            }
        }

        public void SetSequenceStepCount(string actionId, int stepCount)
        {
            if (stepCount < 1) throw new ArgumentOutOfRangeException(nameof(stepCount));
            if (!_actions.TryGetValue(actionId, out var registration) ||
                registration.Owner is not SequenceMidiAction sequence)
                throw new InvalidOperationException($"Sequence action is not registered: {actionId}");
            if (sequence.StepCount == stepCount) return;

            var newBindingIds = new string[stepCount];
            for (var i = 0; i < stepCount; i++)
            {
                var bindingId = sequence.GetBindingIdForIndex(i);
                if (_bindings.ContainsKey(bindingId) && Array.IndexOf(registration.BindingIds, bindingId) < 0)
                    throw new InvalidOperationException($"Already registered: {bindingId}");
                newBindingIds[i] = bindingId;
            }

            foreach (var oldBindingId in registration.BindingIds)
                if (Array.IndexOf(newBindingIds, oldBindingId) < 0) RemoveBinding(oldBindingId);

            sequence.SetStepCount(stepCount);
            for (var i = 0; i < stepCount; i++)
                if (!_bindings.ContainsKey(newBindingIds[i]))
                    RegisterBinding(newBindingIds[i], sequence.CreateStep(i));

            registration.BindingIds = newBindingIds;
            RebuildRuntimeSets();
        }

        public void Learn(string paramId)
        {
            if (_bindings.ContainsKey(paramId)) _learningParamId = paramId;
        }

        public void CancelLearn() => _learningParamId = null;

        public void Bind(string paramId, MidiControl control)
        {
            if (!_bindings.TryGetValue(paramId, out var binding)) return;
            foreach (var other in _bindings.Values)
            {
                if (other.ParamId == paramId || other.Control != control) continue;
                other.Control = null;
                _repository.Save(other.ParamId, null);
                BindingChanged?.Invoke(other.ParamId, null);
            }
            binding.Control = control;
            _repository.Save(paramId, control);
            BindingChanged?.Invoke(paramId, control);
        }

        public void Unbind(string paramId)
        {
            if (!_bindings.TryGetValue(paramId, out var binding)) return;
            binding.Control = null;
            _repository.Save(paramId, null);
            BindingChanged?.Invoke(paramId, null);
        }

        public void Unregister(string paramId)
        {
            string actionId = null;
            foreach (var pair in _actions)
            {
                if (Array.IndexOf(pair.Value.BindingIds, paramId) < 0) continue;
                actionId = pair.Key;
                break;
            }
            if (actionId != null) UnregisterAction(actionId);
        }

        public void UnregisterAction(string actionId)
        {
            if (!_actions.Remove(actionId, out var registration)) return;
            foreach (var bindingId in registration.BindingIds) RemoveBinding(bindingId);
            registration.Owner.Dispose();
            RebuildRuntimeSets();
        }

        public MidiControl? GetControl(string paramId) =>
            _bindings.TryGetValue(paramId, out var binding) ? binding.Control : null;

        public MidiBinding GetBinding(string paramId) =>
            !string.IsNullOrEmpty(paramId) && _bindings.TryGetValue(paramId, out var binding) ? binding : null;

        public void Receive(MidiMessage message)
        {
            if (_learningParamId != null)
            {
                if (!message.IsPressed && message.Control.Type != MidiMessageType.ControlChange) return;
                var paramId = _learningParamId;
                CancelLearn();
                Bind(paramId, message.Control);
                return;
            }
            foreach (var binding in _bindings.Values)
            {
                if (binding.Control != message.Control) continue;
                binding.Action.Execute(message);
                break;
            }
        }

        public void UpdateActions(int frameCount, long beatIndex)
        {
            var context = new MidiActionUpdateContext(frameCount, beatIndex);
            foreach (var runtime in _runtimes) runtime.Update(context);
        }

        public void ReleaseTransientActions()
        {
            foreach (var action in _transientActions) action.ResetTransientState();
        }

        public void Dispose()
        {
            var actionIds = new List<string>(_actions.Keys);
            foreach (var actionId in actionIds) UnregisterAction(actionId);
            BindingChanged = null;
        }

        private void RegisterBinding(string bindingId, IMidiAction action)
        {
            if (string.IsNullOrWhiteSpace(bindingId)) throw new ArgumentException("A binding id is required.", nameof(bindingId));
            if (_bindings.ContainsKey(bindingId)) throw new InvalidOperationException($"Already registered: {bindingId}");
            var loadedControl = _repository.Load(bindingId);
            if (loadedControl.HasValue)
            {
                foreach (var registered in _bindings.Values)
                {
                    if (registered.Control != loadedControl) continue;
                    loadedControl = null;
                    _repository.Save(bindingId, null);
                    break;
                }
            }
            _bindings.Add(bindingId, new MidiBinding(bindingId, action,
                _repository.GetLedColor(bindingId, Color.cyan)) { Control = loadedControl });
        }

        private void RemoveBinding(string bindingId)
        {
            if (!_bindings.Remove(bindingId)) return;
            if (_learningParamId == bindingId) CancelLearn();
            BindingChanged?.Invoke(bindingId, null);
        }

        private void RebuildRuntimeSets()
        {
            _runtimes.Clear();
            _transientActions.Clear();
            foreach (var binding in _bindings.Values)
            {
                if (binding.Action.Runtime != null) _runtimes.Add(binding.Action.Runtime);
                if (binding.Action is ITransientMidiAction transient) _transientActions.Add(transient);
            }
        }

        private void EnsureActionIdAvailable(string actionId)
        {
            if (_actions.ContainsKey(actionId) || _bindings.ContainsKey(actionId))
                throw new InvalidOperationException($"Already registered: {actionId}");
        }

        private static void ValidateActionId(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("An action id is required.", nameof(actionId));
        }

        private sealed class ActionRegistration
        {
            public ActionRegistration(IDisposable owner, string[] bindingIds)
            {
                Owner = owner;
                BindingIds = bindingIds;
            }
            public IDisposable Owner { get; }
            public string[] BindingIds { get; set; }
        }

        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceEqualityComparer<T> Instance = new();
            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
