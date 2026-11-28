using System;
using Krab.Common.Views;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.Common.Midi.Devices.Mpd218
{
    /// <summary>
    /// An on-screen MPD218 using the same adapter as the hardware, including MIDI Learn.
    /// The bank selectors only mirror the hardware's Pad Bank / Ctrl Bank buttons, which send no MIDI.
    /// </summary>
    [UiTarget("MIDI/Devices", "MPD218 Simulator", 101)]
    public sealed class Mpd218Simulator : MonoBehaviour, IUiTarget
    {
        private const float PadWidth = 64f;
        private const float PadHeight = 48f;
        private const float BankButtonWidth = 48f;

        private static readonly Color OffPadColor = new(0.13f, 0.13f, 0.13f);
        private static readonly Color PressedPadColor = new(0.85f, 0.15f, 0.12f);
        private static readonly Color SelectedBankColor = new(0.85f, 0.15f, 0.12f);

        private IMpd218Surface _surface;
        private int _padBank;
        private int _knobBank;

        [Inject]
        public void Construct(IMpd218Surface surface)
        {
            _surface = surface;
        }

        public Element CreateElement()
        {
            if (_surface == null) return UI.Label("MPD218 simulator: MIDI surface unavailable");

            return UI.Column(
                CreateBankRow("Pad Bank", Mpd218.PadBankCount, Mpd218.GetPadBankName,
                    () => _padBank, bank => _padBank = bank),
                UI.DynamicElementOnStatusChanged(() => _padBank, CreatePads),
                CreateBankRow("Ctrl Bank", Mpd218.KnobBankCount, bank => (bank + 1).ToString(),
                    () => _knobBank, bank => _knobBank = bank),
                UI.DynamicElementOnStatusChanged(() => _knobBank, CreateKnobs),
                UI.Label("Pad LED colors are shown here only; the hardware lights pads by itself."));
        }

        private Element CreateBankRow(string label, int count, Func<int, string> getName,
            Func<int> readBank, Action<int> writeBank)
        {
            var elements = new Element[count + 1];
            elements[0] = UI.Label(label).SetWidth(80f);
            for (var i = 0; i < count; i++)
            {
                var bank = i;
                var button = UI.Button(getName(bank), () => writeBank(bank))
                    .SetWidth(BankButtonWidth);
                button.RegisterUpdateCallback(element =>
                    element.Style.BackgroundColor = readBank() == bank ? SelectedBankColor : OffPadColor);
                elements[i + 1] = button;
            }

            return UI.Row(elements);
        }

        private Element CreatePads(int bank)
        {
            var rows = new Element[Mpd218.PadGridSize];
            for (var row = 0; row < Mpd218.PadGridSize; row++)
            {
                var y = Mpd218.PadGridSize - 1 - row;
                var pads = new Element[Mpd218.PadGridSize];
                for (var x = 0; x < Mpd218.PadGridSize; x++)
                {
                    var index = y * Mpd218.PadGridSize + x;
                    pads[x] = CreatePad($"{Mpd218.GetPadBankName(bank)}{index + 1}", Mpd218.GetPadNote(bank, index));
                }

                rows[row] = UI.Row(pads);
            }

            return UI.Column(rows);
        }

        private ButtonElement CreatePad(string label, int note)
        {
            var button = UI.Button(label, () =>
            {
                _surface.Adapter.OnNoteOn(note, 1f);
                _surface.Adapter.OnNoteOff(note);
            }).SetWidth(PadWidth).SetHeight(PadHeight);

            button.RegisterUpdateCallback(element =>
            {
                var color = _surface.Adapter.IsNotePressed(note) ? PressedPadColor : _surface.GetPadColor(note);
                if (color.r + color.g + color.b < 0.03f) color = OffPadColor;
                element.Style.BackgroundColor = color;
                element.Style.Color = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f > 0.55f
                    ? Color.black
                    : Color.white;
            });

            return button;
        }

        private Element CreateKnobs(int bank)
        {
            var knobs = new Element[Mpd218.KnobsPerBank];
            for (var i = 0; i < knobs.Length; i++)
            {
                var cc = Mpd218.GetKnobCc(bank, i);
                knobs[i] = UI.Slider($"K{i + 1} (CC {cc})",
                    () => _surface.Adapter.GetCcValue(cc),
                    value => _surface.Adapter.OnControlChange(cc, value),
                    0f, 1f);
            }

            return UI.Column(knobs);
        }
    }
}
