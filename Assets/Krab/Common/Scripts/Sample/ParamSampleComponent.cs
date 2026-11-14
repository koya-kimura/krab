using System;
using System.Collections.Generic;
using Krab.Common.Midi.Actions;
using Krab.Common.Midi.Bindings;
using Krab.Common.Midi.Core;
using Krab.Common.Midi.Views;
using Krab.Common.Views;
using PrefsGUI;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.Common.Sample
{
    /// <summary>
    /// Executable examples for each MIDI Action and common UI-to-Param adapters.
    /// The examples intentionally keep conversion logic on the consumer side so the
    /// reusable MIDI API remains based on bool, int, and normalized float Params.
    /// </summary>
    [UiTarget("Samples/MIDI", "Parameters", 300)]
    public class ParamSampleComponent : MonoBehaviour, IUiTarget
    {
        private const string ToggleId = "Sample.Page0.Enabled";
        private const string FloatId = "Sample.Fader0";
        private const string OneShotId = "Sample.Action.OneShot";
        private const string MomentaryId = "Sample.Action.Momentary";
        private const string StateId = "Sample.Action.State";
        private const string RadioId = "Sample.Action.Radio";
        private const string SequenceId = "Sample.Action.Sequence";
        private const string AngleId = "Sample.Action.Angle";

        private static readonly Color SequenceInactiveColor = new(0.16f, 0.16f, 0.16f);
        private static readonly Color SequenceActiveColor = new(0.1f, 0.72f, 0.72f);
        private static readonly Color SequenceCurrentColor = new(1f, 0.78f, 0.08f);
        private static readonly Param<bool> ToggleValue = new(false);
        private static readonly Param<float> FloatValue = new(0.5f);
        private static readonly Param<bool> OneShotValue = new(false);
        private static readonly Param<bool> MomentaryValue = new(false);
        private static readonly Param<int> StateValue = new(0);
        private static readonly Param<int> RadioValue = new(0);
        private static readonly Param<float> AngleValue = new(0.5f);
        private static readonly string[] StateLabels = { "Off", "Low", "Medium", "High" };
        private static readonly PrefsList<RadioListItem> RadioItems = new("Sample.Radio.Items", new List<RadioListItem>
        {
            new("camera", "Camera"),
            new("movie", "Movie"),
            new("noise", "Noise"),
            new("feedback", "Feedback")
        });
        private static readonly PrefsInt SequenceStepCount = new("Sample.Sequence.StepCount", 8);
        private MidiBindingManager _bindings;
        private IMidiBindingUi _midiUi;
        private RadioMidiAction _radio;
        private SequenceMidiAction _sequence;
        private bool _registered;
        private int _radioListRevision;
        private int _sequenceRevision;

        private void OnDestroy()
        {
            if (!_registered) return;

            _bindings.UnregisterAction(ToggleId);
            _bindings.UnregisterAction(FloatId);
            _bindings.UnregisterAction(OneShotId);
            _bindings.UnregisterAction(MomentaryId);
            _bindings.UnregisterAction(StateId);
            _bindings.UnregisterAction(RadioId);
            _bindings.UnregisterAction(SequenceId);
            _bindings.UnregisterAction(AngleId);
            _registered = false;
        }

        [Inject]
        public void Construct(IMidiBindingProvider midi, IMidiBindingUi midiUi)
        {
            _midiUi = midiUi;
            if (_registered) return;

            _bindings = midi.Bindings;
            _bindings.RegisterToggle(ToggleId, ToggleValue);
            _bindings.RegisterFloat(FloatId, FloatValue);
            _bindings.RegisterOneShot(OneShotId, OneShotValue);
            _bindings.RegisterMomentary(MomentaryId, MomentaryValue);
            _bindings.RegisterState(StateId, StateValue, 4, new Color[]
            {
                new Color32(32, 32, 32, 255),
                Color.cyan,
                Color.yellow,
                new Color32(243, 141, 178, 255)
            });
            NormalizeRadioItems();
            _radio = _bindings.RegisterRadio(RadioId, RadioValue, GetRadioItemIds());
            var sequenceStepCount = Mathf.Clamp(SequenceStepCount.Get(), 1, 16);
            SequenceStepCount.Set(sequenceStepCount);
            _sequence = _bindings.RegisterSequence(SequenceId, sequenceStepCount);
            _bindings.RegisterFloat(AngleId, AngleValue);
            _registered = true;
        }

        public Element CreateElement()
        {
            var elements = new List<Element>
            {
                UI.Label("Basic actions"),
                UI.Row(
                    UI.Toggle("Toggle", () => ToggleValue.Value, ToggleValue.SetValue),
                    _midiUi.CreateButton(ToggleId, "Toggle")),
                UI.Row(
                    UI.Slider(UI.Label("Normalized float"), () => FloatValue.Value,
                        FloatValue.SetValue, 0f, 1f),
                    _midiUi.CreateButton(FloatId, "Normalized float")),
            };
            elements.Add(UI.Row(
                UI.Label(() => $"OneShot (held): {OneShotValue.Value}"),
                _midiUi.CreateButton(OneShotId, "OneShot")));
            elements.Add(UI.Row(
                UI.Label(() => $"Momentary (one frame): {MomentaryValue.Value}"),
                _midiUi.CreateButton(MomentaryId, "Momentary")));
            elements.Add(UI.Label("State → named value"));
            elements.Add(UI.Row(
                UI.Label(() => $"Intensity: {StateLabels[StateValue.Value]}"),
                _midiUi.CreateButton(StateId, "Intensity state")));

            elements.Add(CreateRadioFold());

            elements.Add(UI.Label("Float 0–1 → application range adapter"));
            elements.Add(UI.Row(
                UI.Slider(UI.Label("Rotation (-180°..180°)"),
                    () => Mathf.Lerp(-180f, 180f, AngleValue.Value),
                    value => AngleValue.SetValue(Mathf.InverseLerp(-180f, 180f, value)),
                    -180f, 180f),
                _midiUi.CreateButton(AngleId, "Rotation normalized float")));

            elements.Add(CreateSequenceFold());

            return UI.ScrollViewVertical(600f, elements);
        }

        private Element CreateRadioFold()
        {
            return UI.Fold(
                UI.Label(() => $"Radio: Sources — selected {RadioValue.Value + 1}/{RadioItems.Count}"),
                new[]
                {
                    UI.DynamicElementOnStatusChanged(
                        () => _radioListRevision,
                        _ => CreateRadioListContents())
                }).Open();
        }

        private Element CreateRadioListContents()
        {
            var rows = new List<Element>();
            for (var i = 0; i < RadioItems.Count; i++)
            {
                var itemId = RadioItems[i].Id;
                rows.Add(UI.Row(
                    UI.Button(UI.Label(() => RadioValue.Value == FindRadioItemIndex(itemId) ? "●" : "○"),
                        () => SelectRadioItem(itemId)).SetWidth(36f),
                    UI.Field(UI.Label("Name"),
                        () => GetRadioItemName(itemId),
                        value => SetRadioItemName(itemId, value)),
                    _midiUi.CreateButton(_radio.GetBindingIdForKey(itemId), "Radio source"),
                    UI.Button("−", () => RemoveRadioItem(itemId))
                        .RegisterUpdateCallback(element => element.Interactable = RadioItems.Count > 1)));
            }

            rows.Add(UI.Button("+ Add source", AddRadioItem));
            return UI.Column(rows);
        }

        private Element CreateSequenceFold()
        {
            return UI.Fold(
                UI.Row(
                    UI.Label(() => $"Sequence: step {_sequence.CurrentStepIndex + 1}/{_sequence.StepCount} — " +
                                   (_sequence.IsCurrentStepEnabled ? "enabled" : "disabled")),
                    UI.Button("−", () => SetSequenceStepCount(_sequence.StepCount - 1))
                        .RegisterUpdateCallback(element => element.Interactable = _sequence.StepCount > 1),
                    UI.Label(() => _sequence.StepCount.ToString()),
                    UI.Button("+", () => SetSequenceStepCount(_sequence.StepCount + 1))
                        .RegisterUpdateCallback(element => element.Interactable = _sequence.StepCount < 16)),
                new[]
                {
                    UI.DynamicElementOnStatusChanged(
                        () => _sequenceRevision,
                        _ => CreateSequenceSteps())
                }).Open();
        }

        private Element CreateSequenceSteps()
        {
            var steps = new Element[_sequence.StepCount];
            for (var index = 0; index < steps.Length; index++)
            {
                var stepIndex = index;
                steps[index] = UI.Column(
                    CreateSequenceStepButton(stepIndex),
                    _midiUi.CreateButton(_sequence.GetBindingId(stepIndex), $"Sequence step {stepIndex + 1}"))
                    .SetMinWidth(78f);
            }

            return UI.ScrollViewHorizontal(760f, UI.Row(steps));
        }

        private Element CreateSequenceStepButton(int stepIndex)
        {
            var button = UI.Button((stepIndex + 1).ToString(), () => _sequence.ToggleStep(stepIndex))
                .SetWidth(70f).SetHeight(38f);
            button.RegisterUpdateCallback(element =>
            {
                var isCurrent = _sequence.CurrentStepIndex == stepIndex;
                var isEnabled = _sequence.IsStepEnabled(stepIndex);
                var color = isCurrent
                    ? SequenceCurrentColor
                    : isEnabled ? SequenceActiveColor : SequenceInactiveColor;
                element.Style.BackgroundColor = color;
                element.Style.Color = isCurrent ? Color.black : Color.white;
            });

            return button;
        }

        private void SetSequenceStepCount(int stepCount)
        {
            var clamped = Mathf.Clamp(stepCount, 1, 16);
            if (clamped == _sequence.StepCount) return;

            _bindings.SetSequenceStepCount(SequenceId, clamped);
            SequenceStepCount.Set(clamped);
            _sequenceRevision++;
        }

        private void AddRadioItem()
        {
            RadioItems.Add(new RadioListItem(Guid.NewGuid().ToString("N"), $"Source {RadioItems.Count + 1}"));
            ReconcileRadioItems();
        }

        private void RemoveRadioItem(string itemId)
        {
            if (RadioItems.Count <= 1) return;

            var index = FindRadioItemIndex(itemId);
            if (index < 0) return;

            RadioItems.RemoveAt(index);
            ReconcileRadioItems();
        }

        private void SelectRadioItem(string itemId)
        {
            var index = FindRadioItemIndex(itemId);
            if (index >= 0) RadioValue.SetValue(index);
        }

        private static int FindRadioItemIndex(string itemId)
        {
            for (var i = 0; i < RadioItems.Count; i++)
            {
                if (RadioItems[i].Id == itemId) return i;
            }

            return -1;
        }

        private static string GetRadioItemName(string itemId)
        {
            var index = FindRadioItemIndex(itemId);
            return index >= 0 ? RadioItems[index].Name : "Removed source";
        }

        private static void SetRadioItemName(string itemId, string value)
        {
            var index = FindRadioItemIndex(itemId);
            if (index < 0) return;

            var item = RadioItems[index];
            item.Name = string.IsNullOrWhiteSpace(value) ? $"Source {index + 1}" : value;
            RadioItems[index] = item;
        }

        private void ReconcileRadioItems()
        {
            _bindings.SetRadioOptions(RadioId, GetRadioItemIds());
            _radioListRevision++;
        }

        private static string[] GetRadioItemIds()
        {
            var ids = new string[RadioItems.Count];
            for (var i = 0; i < ids.Length; i++) ids[i] = RadioItems[i].Id;

            return ids;
        }

        private static void NormalizeRadioItems()
        {
            if (RadioItems.Count == 0) RadioItems.Add(new RadioListItem(Guid.NewGuid().ToString("N"), "Source 1"));

            var ids = new HashSet<string>();
            for (var i = 0; i < RadioItems.Count; i++)
            {
                var item = RadioItems[i];
                if (string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id))
                    item.Id = Guid.NewGuid().ToString("N");

                if (string.IsNullOrWhiteSpace(item.Name)) item.Name = $"Source {i + 1}";
                RadioItems[i] = item;
            }
        }

        [Serializable]
        private sealed class RadioListItem
        {
            public string Id;
            public string Name;

            public RadioListItem()
                : this(Guid.NewGuid().ToString("N"), "Source")
            {
            }

            public RadioListItem(string id, string name)
            {
                Id = id;
                Name = name;
            }
        }
    }
}
