using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core;
using RosettaUI;

namespace Krab.ShapeSynth.Samples.Performance
{
    public sealed partial class ShapeSynthPerformance
    {
        public Element CreateElement()
        {
            EnsureSettings();
            return UI.Column(
                UI.Label("Modulators — edits apply only to the next Trigger"),
                UI.DynamicElementOnStatusChanged(() => Settings, _ => BuildModulatorEditors()),
                UI.Label("Performance / FX01"),
                UI.Field("Circle count", () => Settings.Fx01.CircleCount, value => Settings.Fx01.CircleCount = value),
                UI.Field("Initial seed", () => Settings.Fx01.Seed, value => { Settings.Fx01.Seed = value; _seedInitialized = false; }),
                UI.Label(() => $"Envelope ID: {Settings.Fx01.EnvelopeId} / LFO ID: {Settings.Fx01.OscillatorId}"),
                UI.Button("Trigger FX01 (Q)", () => TriggerFx01()),
                UI.Label(() => $"Active Voices: {VoiceCount}"),
                UI.Row(UI.Button("Save ShapeSynth", () => SaveSettings()), UI.Button("Reload ShapeSynth", () => ReloadSettings())),
                UI.Label(() => _status));
        }

        private Element BuildModulatorEditors()
        {
            var editors = new List<Element>();
            foreach (var model in Settings.Modulators)
            {
                var fields = new List<Element>
                {
                    UI.Label(() => $"ID: {model.Id} ({model.Kind})"),
                    UI.Field("Name", () => model.Name, value => model.Name = value)
                };
                if (model.Kind == ModulatorKind.Adsr)
                {
                    fields.Add(Number("Attack (beats)", () => model.Attack, value => model.Attack = value));
                    fields.Add(Number("Decay (beats)", () => model.Decay, value => model.Decay = value));
                    fields.Add(Number("Sustain level", () => model.SustainLevel, value => model.SustainLevel = value));
                    fields.Add(Number("Sustain time (beats)", () => model.SustainTime, value => model.SustainTime = value));
                    fields.Add(Number("Release (beats)", () => model.Release, value => model.Release = value));
                }
                else
                {
                    fields.Add(UI.Dropdown("Waveform", () => (int)model.Waveform, value => model.Waveform = (LfoWaveform)value,
                        Enum.GetNames(typeof(LfoWaveform))));
                    fields.Add(Number("Cycles per beat", () => model.CyclesPerBeat, value => model.CyclesPerBeat = value));
                    fields.Add(Number("Phase offset (cycles)", () => model.PhaseOffset, value => model.PhaseOffset = value));
                }
                editors.Add(UI.Fold(UI.Label(() => model.Name), UI.Column(fields)));
            }
            return UI.Column(editors);
        }

        private static Element Number(string label, Func<double> read, Action<double> write) =>
            UI.Field(label, () => (float)read(), value => write(value));
    }
}
