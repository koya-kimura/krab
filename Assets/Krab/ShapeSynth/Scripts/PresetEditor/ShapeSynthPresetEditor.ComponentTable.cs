using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>
    /// A Value component's settings as a small table, like the Inspector's Parameter rows: one heading row, then one
    /// row per stage. ADSR: Stage | Beats | Level | Curve. Ramp: Beats | Curve. LFO: Wave | Cycles / Beat | Phase.
    /// </summary>
    public sealed partial class ShapeSynthPresetEditor
    {
        private const float StageColumnWidth = 70;
        private const float CurveColumnWidth = 130;
        private const float WaveColumnWidth = 96;
        private const float CyclesColumnWidth = 92;

        private static readonly string[] WaveformNames = Enum.GetNames(typeof(LfoWaveform));
        private static readonly string[] CurveNames = Enum.GetNames(typeof(EasingCurve));

        private IEnumerable<Element> ComponentFields(PresetValue value, PresetComponent component)
        {
            yield return UI.Row(
                UI.Label("Weight").SetWidth(StageColumnWidth),
                NumberCell(() => component.Weight, weight => component.Weight = weight).SetWidth(NumberColumnWidth),
                UI.Space(),
                UI.Button("Remove", () => RemoveComponent(value, component)));
            switch (component.Kind)
            {
                case ModulatorKind.Adsr:
                    yield return TableHeader(("Stage", StageColumnWidth), ("Beats", NumberColumnWidth),
                        ("Level", NumberColumnWidth), ("Curve", CurveColumnWidth));
                    yield return StageRow("Attack",
                        BeatsCell(() => component.Attack, beats => component.Attack = beats),
                        EmptyCell(NumberColumnWidth),
                        CurveCell(() => component.AttackCurve, curve => component.AttackCurve = curve));
                    yield return StageRow("Decay", BeatsCell(() => component.Decay, beats => component.Decay = beats),
                        EmptyCell(NumberColumnWidth),
                        CurveCell(() => component.DecayCurve, curve => component.DecayCurve = curve));
                    yield return StageRow("Sustain",
                        BeatsCell(() => component.SustainTime, beats => component.SustainTime = beats),
                        NumberCell(() => component.SustainLevel, level => component.SustainLevel = level)
                            .SetWidth(NumberColumnWidth),
                        EmptyCell(CurveColumnWidth));
                    yield return StageRow("Release",
                        BeatsCell(() => component.Release, beats => component.Release = beats),
                        EmptyCell(NumberColumnWidth),
                        CurveCell(() => component.ReleaseCurve, curve => component.ReleaseCurve = curve));
                    break;
                case ModulatorKind.Ramp:
                    yield return TableHeader(("Stage", StageColumnWidth), ("Beats", NumberColumnWidth),
                        ("Curve", CurveColumnWidth));
                    yield return StageRow("Rise",
                        BeatsCell(() => component.Duration, beats => component.Duration = beats),
                        CurveCell(() => component.RampCurve, curve => component.RampCurve = curve));
                    break;
                default:
                    yield return TableHeader(("Wave", WaveColumnWidth), ("Cycles / Beat", CyclesColumnWidth),
                        ("Phase", NumberColumnWidth));
                    yield return UI.Row(
                        Choice(null, () => (int)component.Waveform, index =>
                        {
                            component.Waveform = (LfoWaveform)index;
                            Changed();
                        }, WaveformNames).SetWidth(WaveColumnWidth),
                        NumberCell(() => component.CyclesPerBeat, rate => component.CyclesPerBeat = rate)
                            .SetWidth(CyclesColumnWidth),
                        NumberCell(() => component.PhaseOffset, phase => component.PhaseOffset = phase)
                            .SetWidth(NumberColumnWidth));
                    break;
            }
        }

        private static Element TableHeader(params (string Text, float Width)[] columns)
        {
            var headings = new Element[columns.Length];
            for (var i = 0; i < columns.Length; i++)
            {
                headings[i] = UI.Label(columns[i].Text).SetWidth(columns[i].Width).SetColor(PresetEditorPalette.Title);
            }

            return UI.Row(headings);
        }

        private static Element StageRow(string stage, params Element[] cells)
        {
            var row = new Element[cells.Length + 1];
            row[0] = UI.Label(stage).SetWidth(StageColumnWidth);
            Array.Copy(cells, 0, row, 1, cells.Length);
            return UI.Row(row);
        }

        private Element BeatsCell(Func<double> read, Action<double> write) =>
            NumberCell(read, write).SetWidth(NumberColumnWidth);

        private Element CurveCell(Func<EasingCurve> read, Action<EasingCurve> write)
        {
            return Choice(null, () => (int)read(), index =>
            {
                write((EasingCurve)index);
                Changed();
            }, CurveNames).SetWidth(CurveColumnWidth);
        }

        private static Element EmptyCell(float width) =>
            UI.Label("-").SetWidth(width).SetColor(PresetEditorPalette.DragSource);
    }
}
