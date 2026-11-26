using System;
using System.Collections.Generic;
using Krab.Common.Midi.Bindings;
using Krab.Common.Midi.Core;
using Krab.Common.Midi.Devices.ApcMiniMk2;
using Krab.Common.Midi.Devices.Mpd218;
using PrefsGUI;
using RosettaUI;
using RosettaUI.Reactive;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.Common.Midi.Views
{
    /// <summary>Creates MIDI launchers beside parameters and keeps only one settings window open.</summary>
    public sealed class MidiBindingUi : MonoBehaviour, IMidiBindingUi
    {
        private const float WindowWidth = 430f;

        private readonly List<(string ParamId, WindowElement Window)> _windows = new();
        private readonly List<IDisposable> _openSubscriptions = new();
        private MidiBindingManager _bindings;
        private WindowElement _activeWindow;

        private void OnDestroy()
        {
            if (_bindings != null) _bindings.BindingChanged -= OnBindingChanged;

            foreach (var subscription in _openSubscriptions) subscription.Dispose();
        }

        [Inject]
        public void Construct(IMidiBindingProvider midi)
        {
            if (_bindings != null) _bindings.BindingChanged -= OnBindingChanged;

            _bindings = midi.Bindings;
            _bindings.BindingChanged += OnBindingChanged;
        }

        public Element CreateButton(string paramId, string displayName)
        {
            var title = string.IsNullOrEmpty(displayName) ? paramId : displayName;
            var window = UI.Window(
                    $"MIDI: {title}",
                    UI.DynamicElementOnStatusChanged(
                        readStatus: () => _bindings.GetBinding(paramId),
                        build: _ => CreateSelectedContents(paramId)))
                .SetWidth(WindowWidth);
            _windows.Add((paramId, window));
            _openSubscriptions.Add(window.IsOpenRx.Subscribe(isOpen =>
            {
                if (isOpen)
                {
                    if (_activeWindow != null && _activeWindow != window)
                        _activeWindow.IsOpen = false;
                    _activeWindow = window;
                }
                else if (_activeWindow == window)
                {
                    _activeWindow = null;
                }
            }));

            var launcher = UI.WindowLauncher(
                UI.Label(() => _bindings.GetBinding(paramId)?.HasControl == true ? "MIDI ●" : "MIDI"),
                window).SetWidth(70f);
            launcher.RegisterUpdateCallback(element => element.Interactable = _bindings.GetBinding(paramId) != null);
            return launcher;
        }

        private void OnBindingChanged(string paramId)
        {
            if (_bindings.GetBinding(paramId) != null) return;

            foreach (var entry in _windows)
            {
                if (entry.ParamId == paramId) entry.Window.IsOpen = false;
            }
        }

        private Element CreateSelectedContents(string paramId)
        {
            if (string.IsNullOrEmpty(paramId)) return UI.Label("Select a parameter's MIDI button.");

            var binding = _bindings.GetBinding(paramId);
            if (binding == null) return UI.Label("This parameter is no longer registered.");

            return UI.Column(
                UI.Label($"Action: {binding.Action.DisplayName}"),
                CreateControlRow(paramId, "APC mini mk2", MidiDeviceKind.ApcMiniMk2),
                CreateControlRow(paramId, "MPD218", MidiDeviceKind.Mpd218),
                UI.Label(() => _bindings.LearningParamId == paramId
                    ? "Learning: press a control on a device or simulator"
                    : ""),
                UI.Row(
                    UI.Button("Learn", () => _bindings.Learn(paramId)),
                    UI.Button("Cancel", _bindings.CancelLearn)
                        .RegisterUpdateCallback(element =>
                            element.Interactable = _bindings.LearningParamId == paramId)),
                UI.DynamicElementIf(
                    () => SupportsLedColor(binding),
                    () => UI.Field("Pad LED color", () => binding.LedColor.Get(),
                        color => binding.LedColor.Set(color))),
                UI.DynamicElementIf(
                    () => !SupportsLedColor(binding),
                    () => UI.Label("LED color is available for pads and the simulated Shift button.")),
                UI.Button("Save", Prefs.Save));
        }

        private Element CreateControlRow(string paramId, string deviceName, MidiDeviceKind device)
        {
            return UI.Row(
                UI.Label(() => $"{deviceName}: {DescribeControl(_bindings.GetControl(paramId, device))}"),
                UI.Button("Clear", () => _bindings.Unbind(paramId, device)).SetWidth(60f));
        }

        private static bool SupportsLedColor(MidiBinding binding)
        {
            if (!binding.HasControl) return true;

            if (binding.GetControl(MidiDeviceKind.Mpd218) is { Type: MidiMessageType.Note }) return true;

            return binding.GetControl(MidiDeviceKind.ApcMiniMk2) is { Type: MidiMessageType.Note } apc &&
                   (ApcMiniMk2.TryGetPadPosition(apc.Number, out _, out _) || apc.Number == ApcMiniMk2.ShiftButton);
        }

        private static string DescribeControl(MidiControl? control)
        {
            if (!control.HasValue) return "Unassigned";

            var value = control.Value;
            if (value.Device == MidiDeviceKind.Mpd218) return Mpd218.Describe(value);

            if (value.Type == MidiMessageType.Note)
            {
                if (ApcMiniMk2.TryGetPadPosition(value.Number, out var x, out var y))
                    return $"Pad ({value.Page}, {x}, {y})";
                if (value.Number >= ApcMiniMk2.TrackButtonFirst && value.Number <= ApcMiniMk2.TrackButtonLast)
                    return $"Track {value.Number - ApcMiniMk2.TrackButtonFirst} / page {value.Page}";
                if (value.Number == ApcMiniMk2.ShiftButton)
                    return value.Page == ApcMiniMk2.GlobalPage ? "Shift (global)" : $"Shift / page {value.Page}";
            }
            else if (value.Type == MidiMessageType.ControlChange &&
                     value.Number >= ApcMiniMk2.FaderCcFirst && value.Number <= ApcMiniMk2.FaderCcLast)
            {
                var index = value.Number - ApcMiniMk2.FaderCcFirst;
                return index == ApcMiniMk2.GridSize
                    ? $"Master fader / page {value.Page}"
                    : $"Fader {index} / page {value.Page}";
            }

            return $"{value.Type} / page {value.Page} / number {value.Number}";
        }
    }
}
