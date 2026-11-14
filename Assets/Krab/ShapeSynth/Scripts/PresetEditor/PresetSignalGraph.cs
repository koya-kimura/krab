using System;
using System.Collections.Generic;
using System.Text;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Runtime;
using RosettaUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>Samples authoring settings through the Core ADSR/LFO instances. Seconds equal beats at 60 BPM.</summary>
    public static class PresetSignalSampler
    {
        public const float GraphBeats = 6;
        private static readonly VoiceTime OneBeatPerSecond = new(0, 60);

        /// <summary>Returns false while a draft value is invalid, e.g. a negative duration being typed.</summary>
        public static bool TryCreate(ModulatorSettings model, out IModulator modulator)
        {
            modulator = null;
            if (model == null) return false;

            try
            {
                if (model.Kind == ModulatorKind.Adsr)
                {
                    var adsr = new AdsrDefinition(
                        model.Attack, model.Decay, model.SustainLevel, model.SustainTime, model.Release);
                    modulator = new AdsrInstance(adsr, OneBeatPerSecond);
                }
                else
                {
                    var lfo = new LfoDefinition(model.CyclesPerBeat, model.PhaseOffset, model.Waveform);
                    modulator = new LfoInstance(lfo, OneBeatPerSecond);
                }

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static double SampleOrZero(ModulatorSettings model, double beat)
        {
            return TryCreate(model, out var modulator) ? modulator.Evaluate(beat) : 0;
        }
    }

    /// <summary>Output graph for the Values view, drawn with Painter2D and repainted only when its inputs change.</summary>
    public sealed class PresetSignalGraph
    {
        private const int Samples = 180;

        private readonly Func<IReadOnlyList<ModulatorSettings>> _models;
        private readonly Func<string> _selectedId;
        private readonly Func<float> _cursorBeat;

        private VisualElement _view;
        private string _signature;

        public PresetSignalGraph(
            Func<IReadOnlyList<ModulatorSettings>> models,
            Func<string> selectedId,
            Func<float> cursorBeat)
        {
            _models = models ?? throw new ArgumentNullException(nameof(models));
            _selectedId = selectedId ?? throw new ArgumentNullException(nameof(selectedId));
            _cursorBeat = cursorBeat ?? throw new ArgumentNullException(nameof(cursorBeat));
        }

        public int RepaintCount { get; private set; }

        public Element CreateElement()
        {
            var box = UI.Box(UI.Space()).SetHeight(180).SetMinHeight(180).SetFlexShrink(0);
            box.RegisterUpdateCallback(_ => RepaintIfChanged());
            return PresetWorkspaceLayout.BindView(box, view =>
            {
                _view = view;
                view.name = "preset-signal-graph";
                view.style.backgroundColor = PresetEditorTheme.Graph;
                PresetEditorTheme.Border(view.style, PresetEditorTheme.Hex(0x39414d));
                PresetEditorTheme.Margin(view.style, 0, 0);
                view.generateVisualContent += Draw;
                _signature = null;
                return () =>
                {
                    view.generateVisualContent -= Draw;
                    if (_view == view) _view = null;
                };
            });
        }

        private void RepaintIfChanged()
        {
            if (_view == null) return;

            var signature = Signature();
            if (signature == _signature) return;

            _signature = signature;
            RepaintCount++;
            _view.MarkDirtyRepaint();
        }

        private string Signature()
        {
            var builder = new StringBuilder(_selectedId()).Append('|').Append(_cursorBeat().ToString("F3"));
            foreach (var model in _models())
            {
                builder.Append('|').Append(model.Id).Append(model.Kind).Append(model.Attack).Append(',').Append(model.Decay)
                    .Append(',').Append(model.SustainLevel).Append(',').Append(model.SustainTime).Append(',')
                    .Append(model.Release).Append(',').Append(model.CyclesPerBeat).Append(',')
                    .Append(model.PhaseOffset).Append(model.Waveform);
            }

            return builder.ToString();
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = _view.contentRect;
            if (rect.width <= 2 || rect.height <= 2) return;

            var painter = context.painter2D;
            painter.lineWidth = 1;
            painter.strokeColor = PresetEditorTheme.GraphGrid;
            for (var i = 1; i < 6; i++)
                Line(painter, new Vector2(0, rect.height * i / 6), new Vector2(rect.width, rect.height * i / 6));

            for (var i = 1; i < 6; i++)
                Line(painter, new Vector2(rect.width * i / 6, 0), new Vector2(rect.width * i / 6, rect.height));

            painter.strokeColor = PresetEditorTheme.GraphZero;
            Line(painter, new Vector2(0, Y(0, rect)), new Vector2(rect.width, Y(0, rect)));

            var selected = _selectedId();
            ModulatorSettings highlighted = null;
            foreach (var model in _models())
            {
                if (model.Id == selected)
                {
                    highlighted = model;
                    continue;
                }

                var color = model.Kind == ModulatorKind.Adsr ? PresetEditorTheme.Adsr : PresetEditorTheme.Lfo;
                Curve(painter, model, rect, new Color(color.r, color.g, color.b, 0.45f), 1.5f);
            }

            if (highlighted != null) Curve(painter, highlighted, rect, PresetEditorTheme.Sum, 2.5f);

            var beat = Mathf.Clamp(_cursorBeat(), 0, PresetSignalSampler.GraphBeats);
            var cursor = beat / PresetSignalSampler.GraphBeats * rect.width;
            painter.strokeColor = new Color(1, 1, 1, 0.35f);
            painter.lineWidth = 1;
            Line(painter, new Vector2(cursor, 0), new Vector2(cursor, rect.height));
        }

        private static void Curve(Painter2D painter, ModulatorSettings model, Rect rect, Color color, float width)
        {
            if (!PresetSignalSampler.TryCreate(model, out var signal)) return;

            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            for (var i = 0; i <= Samples; i++)
            {
                var beat = PresetSignalSampler.GraphBeats * i / Samples;
                var point = new Vector2(rect.width * i / Samples, Y(signal.Evaluate(beat), rect));
                if (i == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.Stroke();
        }

        // Same framing as the reference graph: zero in the middle, ±1 at roughly 28% from each edge.
        private static float Y(double value, Rect rect)
        {
            return Mathf.Clamp(rect.height * 0.5f - rect.height * 0.28f * (float)value, 3, rect.height - 3);
        }

        private static void Line(Painter2D painter, Vector2 from, Vector2 to)
        {
            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(to);
            painter.Stroke();
        }
    }
}
