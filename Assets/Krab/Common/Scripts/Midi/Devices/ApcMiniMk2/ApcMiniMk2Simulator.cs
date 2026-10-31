using Krab.Common.Views;
using RosettaUI;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;
using UnitySimpleContainer;

namespace Krab.Common.Midi.Devices.ApcMiniMk2
{
    /// <summary>
    /// An on-screen APC mini mk2. UI input uses the same adapter as the physical device,
    /// including the current page and MIDI Learn.
    /// </summary>
    public sealed class ApcMiniMk2Simulator : MonoBehaviour, IUiTarget
    {
        private const float ButtonWidth = 43f;
        private const float ButtonHeight = 34f;
        private static readonly Color OffPadColor = new(0.13f, 0.13f, 0.13f);
        private static readonly Color OffTrackColor = new(0.23f, 0.09f, 0.09f);
        private static readonly Color OffPageColor = new(0.08f, 0.2f, 0.1f);
        private static readonly Color RandomModeColor = new Color32(243, 141, 178, 255);

        private IApcMiniMk2Surface _surface;

        private void Awake()
        {
            // RosettaUI's normal slider is horizontal. Register one vertical UI Toolkit
            // fader so the nine columns match the physical surface.
            UIToolkitBuilder.Instance.RegisterBuildBindFunc(
                typeof(FaderElement), BuildBindFunc<Slider>.Create(BindFader));
        }

        [Inject]
        public void Construct(IApcMiniMk2Surface surface)
        {
            _surface = surface;
        }

        public Element CreateElement()
        {
            if (_surface == null)
                return UI.Label("APC mini mk2 simulator: MIDI surface unavailable");

            var rows = new Element[ApcMiniMk2.GridSize + 4];
            rows[0] = UI.Label(() => $"Page {_surface.Adapter.CurrentPage} / 7");
            for (var row = 0; row < ApcMiniMk2.GridSize; row++)
            {
                var y = ApcMiniMk2.GridSize - 1 - row;
                var buttons = new Element[ApcMiniMk2.GridSize + 1];
                for (var x = 0; x < ApcMiniMk2.GridSize; x++)
                {
                    var padNote = ApcMiniMk2.GetPadNote(x, y);
                    buttons[x] = CreateSurfaceButton($"{x},{y}", padNote,
                        () => _surface.GetPadColor(padNote), OffPadColor);
                }

                var page = row;
                buttons[ApcMiniMk2.GridSize] = CreateSurfaceButton($"P{page}",
                    ApcMiniMk2.PageButtonFirst + page,
                    () => _surface.Adapter.CurrentPage == page ? Color.green : OffPageColor,
                    OffPageColor);
                rows[row + 1] = UI.Row(buttons);
            }

            var bottomButtons = new Element[ApcMiniMk2.GridSize + 1];
            for (var i = 0; i < ApcMiniMk2.GridSize; i++)
            {
                var track = i;
                bottomButtons[i] = CreateSurfaceButton($"T{i}", ApcMiniMk2.TrackButtonFirst + i,
                    () => GetTrackColor(track), OffTrackColor);
            }
            bottomButtons[ApcMiniMk2.GridSize] = CreateSurfaceButton("Shift", ApcMiniMk2.ShiftButton,
                _surface.GetShiftColor, OffPadColor);
            rows[ApcMiniMk2.GridSize + 1] = UI.Row(bottomButtons);

            var faders = new Element[ApcMiniMk2.GridSize + 1];
            for (var i = 0; i < faders.Length; i++)
            {
                var index = i;
                faders[i] = UI.Column(
                    new FaderElement(
                        () => _surface.Adapter.GetRawFaderValue(index),
                        value => _surface.Adapter.OnControlChange(ApcMiniMk2.FaderCcFirst + index, value))
                        .SetWidth(ButtonWidth).SetHeight(130f),
                    UI.Label(index == ApcMiniMk2.GridSize ? "M" : $"F{index}").SetWidth(ButtonWidth),
                    UI.Label(() => Mathf.RoundToInt(_surface.Adapter.GetFaderValue(index) * 127f).ToString())
                        .SetWidth(ButtonWidth))
                    .SetWidth(ButtonWidth);
            }
            rows[ApcMiniMk2.GridSize + 2] = UI.Row(faders);
            var settings = new Element[ApcMiniMk2.ChannelFaderCount + 2];
            settings[0] = UI.Label("Fader button assignments (saved by explicit Prefs Save)");
            for (var i = 0; i < ApcMiniMk2.ChannelFaderCount; i++)
            {
                var index = i;
                settings[i + 1] = UI.Field($"Fader {i}",
                    () => _surface.FaderModes.GetAssignment(index),
                    value => _surface.FaderModes.SetAssignment(index, value));
            }
            settings[settings.Length - 1] = UI.Label("Master fader: normal (fixed)");
            rows[ApcMiniMk2.GridSize + 3] = UI.Column(settings);
            return UI.Column(rows);
        }

        private Color GetTrackColor(int index)
        {
            if (!_surface.FaderModes.IsAssignedModeActive(index)) return OffTrackColor;
            return _surface.FaderModes.GetAssignment(index) == FaderModeAssignment.Random
                ? RandomModeColor
                : Color.red;
        }

        private ButtonElement CreateSurfaceButton(string label, int note,
            System.Func<Color> readColor, Color fallback)
        {
            var button = UI.Button(label, () =>
            {
                _surface.Adapter.OnNoteOn(note, 1f);
                _surface.Adapter.OnNoteOff(note);
            }).SetWidth(ButtonWidth).SetHeight(ButtonHeight);

            button.RegisterUpdateCallback(element =>
            {
                var color = readColor();
                if (color.r + color.g + color.b < 0.03f) color = fallback;
                element.Style.BackgroundColor = color;
                element.Style.Color = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f > 0.55f
                    ? Color.black : Color.white;
            });
            return button;
        }

        private static bool BindFader(Element element, VisualElement visualElement)
        {
            if (element is not FaderElement fader || visualElement is not Slider slider) return false;

            slider.direction = SliderDirection.Vertical;
            slider.lowValue = 0f;
            slider.highValue = 1f;
            slider.SetValueWithoutNotify(fader.ReadValue());

            void OnChanged(ChangeEvent<float> change) => fader.WriteValue(change.newValue);
            void OnUpdated(Element _) => slider.SetValueWithoutNotify(fader.ReadValue());

            slider.RegisterValueChangedCallback(OnChanged);
            fader.onUpdate += OnUpdated;
            fader.GetViewBridge().onUnsubscribe += () =>
            {
                slider.UnregisterValueChangedCallback(OnChanged);
                fader.onUpdate -= OnUpdated;
            };
            return true;
        }

        private sealed class FaderElement : Element
        {
            private readonly System.Func<float> _readValue;
            private readonly System.Action<float> _writeValue;

            public FaderElement(System.Func<float> readValue, System.Action<float> writeValue)
            {
                _readValue = readValue;
                _writeValue = writeValue;
            }

            public float ReadValue() => _readValue();
            public void WriteValue(float value) => _writeValue(value);
        }
    }
}
