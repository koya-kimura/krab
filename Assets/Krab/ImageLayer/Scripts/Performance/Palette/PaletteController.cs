using System;
using System.Collections.Generic;
using Krab.Common.Timing;
using Krab.Common.Views;
using Krab.TypeLayout.Core.Motion;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.ImageLayer.Performance.Palette
{
    /// <summary>
    /// The palette shared by the background and the image looks. Pick a preset or the custom colors, and
    /// optionally step through the presets on the beat.
    /// </summary>
    [UiTarget("ImageLayer/Palette", "Palette", 1)]
    public sealed class PaletteController : MonoBehaviour, IUiTarget
    {
        private const float FallbackBpm = 120;
        private const string CustomName = "Custom";

        private readonly PrefsInt _index = new("ImageLayer Palette", 0);
        private readonly PrefsFloat _beatsPerChange = new("ImageLayer Palette Beats Per Change", 0);
        private readonly PrefsColor _customBackground = new("ImageLayer Palette Custom Background", Color.white);
        private readonly PrefsColor _customLine = new("ImageLayer Palette Custom Line", new Color(0f, 0f, 0f, 0.25f));
        private readonly PrefsColor _customDark = new("ImageLayer Palette Custom Dark", Color.black);
        private readonly PrefsColor _customLight = new("ImageLayer Palette Custom Light", Color.white);
        private readonly PrefsColor _customAccent = new("ImageLayer Palette Custom Accent", Color.red);

        private IBPMManager _bpmManager;

        public int PaletteCount => ColorPalette.Presets.Length + 1;

        /// <summary>The palette in effect now, including the beat-driven change.</summary>
        public ColorPalette Current
        {
            get
            {
                int index = Math.Clamp(_index.Get(), 0, PaletteCount - 1);
                float beatsPerChange = _beatsPerChange.Get();
                if (beatsPerChange > 0 && index < ColorPalette.Presets.Length)
                {
                    double beats = _bpmManager?.Beat ?? Time.timeAsDouble * FallbackBpm / 60;
                    long step = BeatSteps.Step(beats, beatsPerChange);
                    index = (int)((index + step) % ColorPalette.Presets.Length);
                }

                return index < ColorPalette.Presets.Length ? ColorPalette.Presets[index] : Custom;
            }
        }

        public int Index
        {
            get => _index.Get();
            set => _index.Set(value);
        }

        public Element CreateElement()
        {
            var names = new List<string>();
            foreach (var preset in ColorPalette.Presets)
                names.Add(preset.Name);

            names.Add(CustomName);
            return UI.Column(
                UI.Dropdown("Palette", () => Math.Clamp(_index.Get(), 0, PaletteCount - 1), value => _index.Set(value),
                    names),
                _beatsPerChange.CreateSlider("Beats Per Change (0 = hold)", 0f, 32f),
                UI.Label(() => $"Now: {Current.Name}"),
                UI.Fold("Custom",
                    _customBackground.CreateElement("Background"),
                    _customLine.CreateElement("Line"),
                    _customDark.CreateElement("Dark"),
                    _customLight.CreateElement("Light"),
                    _customAccent.CreateElement("Accent")));
        }

        [Inject]
        public void Construct(IBPMManager bpmManager)
        {
            _bpmManager = bpmManager;
        }

        private ColorPalette Custom => new(CustomName, _customBackground.Get(), _customLine.Get(), _customDark.Get(),
            _customLight.Get(), _customAccent.Get());
    }
}
