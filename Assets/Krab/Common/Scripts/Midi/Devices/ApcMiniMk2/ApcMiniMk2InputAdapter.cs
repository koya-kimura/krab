using System;
using System.Collections.Generic;
using Krab.Common.Midi.Bindings;
using Krab.Common.Midi.Core;

namespace Krab.Common.Midi.Devices.ApcMiniMk2
{
    public sealed class ApcMiniMk2InputAdapter : IDisposable
    {
        private readonly MidiBindingManager _bindingManager;
        private readonly ApcMiniMk2FaderModeController _faderModes;
        private readonly Dictionary<int, int> _pressedNotePages = new();

        public ApcMiniMk2InputAdapter(MidiBindingManager bindingManager,
            ApcMiniMk2FaderModeController faderModes)
        {
            _bindingManager = bindingManager ?? throw new ArgumentNullException(nameof(bindingManager));
            _faderModes = faderModes ?? throw new ArgumentNullException(nameof(faderModes));
            _faderModes.EffectiveValueChanged += OnEffectiveValueChanged;
        }

        public int CurrentPage { get; private set; }
        public event Action<int> PageChanged;

        public float GetFaderValue(int index) => _faderModes.GetValue(index);
        public float GetRawFaderValue(int index) => _faderModes.GetRawValue(index);

        public void OnNoteOn(int note, float velocity)
        {
            if (note >= ApcMiniMk2.PageButtonFirst && note <= ApcMiniMk2.PageButtonLast)
            {
                SelectPage(note - ApcMiniMk2.PageButtonFirst);
                return;
            }

            if (note >= ApcMiniMk2.TrackButtonFirst && note <= ApcMiniMk2.TrackButtonLast)
            {
                _faderModes.ToggleAssignedMode(note - ApcMiniMk2.TrackButtonFirst);
                return;
            }

            if (ApcMiniMk2.TryGetPadPosition(note, out var x, out var y))
            {
                _pressedNotePages[note] = CurrentPage;
                ReceivePad(x, y, CurrentPage, velocity, true, false);
            }
            else if (note == ApcMiniMk2.ShiftButton)
            {
                ReceiveNote(note, ApcMiniMk2.GlobalPage, velocity, true, false);
            }
        }

        public void OnNoteOff(int note)
        {
            if (ApcMiniMk2.TryGetPadPosition(note, out var x, out var y))
                ReceivePad(x, y, TakePressedPage(note), 0f, false, true);
            else if (note == ApcMiniMk2.ShiftButton)
                ReceiveNote(note, ApcMiniMk2.GlobalPage, 0f, false, true);
        }

        public void OnControlChange(int cc, float value)
        {
            if (cc < ApcMiniMk2.FaderCcFirst || cc > ApcMiniMk2.FaderCcLast) return;
            var index = cc - ApcMiniMk2.FaderCcFirst;
            _faderModes.SetRawValue(index, value);
            PublishFader(index);
        }

        public void PublishAllFaders()
        {
            for (var i = 0; i < ApcMiniMk2.FaderCount; i++) PublishFader(i);
        }

        public void Dispose()
        {
            _faderModes.EffectiveValueChanged -= OnEffectiveValueChanged;
            _pressedNotePages.Clear();
        }

        private void OnEffectiveValueChanged(int index, float _) => PublishFader(index);

        private void PublishFader(int index)
        {
            var control = new MidiControl(MidiMessageType.ControlChange, CurrentPage,
                ApcMiniMk2.FaderCcFirst + index);
            _bindingManager.Receive(new MidiMessage(control, _faderModes.GetValue(index), false, false));
        }

        private void ReceivePad(int x, int y, int page, float value, bool isPressed, bool isReleased)
        {
            _bindingManager.Receive(new MidiMessage(ApcMiniMk2.CreatePadControl(page, x, y), value,
                isPressed, isReleased));
        }

        private void ReceiveNote(int note, int page, float value, bool isPressed, bool isReleased)
        {
            _bindingManager.Receive(new MidiMessage(new MidiControl(MidiMessageType.Note, page, note), value,
                isPressed, isReleased));
        }

        private int TakePressedPage(int note)
        {
            if (!_pressedNotePages.TryGetValue(note, out var page)) return CurrentPage;
            _pressedNotePages.Remove(note);
            return page;
        }

        private void SelectPage(int page)
        {
            if (page < 0 || page >= ApcMiniMk2.PageCount || page == CurrentPage) return;
            CurrentPage = page;
            PageChanged?.Invoke(page);
            PublishAllFaders();
        }
    }
}
