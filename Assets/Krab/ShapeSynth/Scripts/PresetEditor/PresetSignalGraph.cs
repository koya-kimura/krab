using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using RosettaUI;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>Samples authoring settings through the Core ADSR/LFO instances. Seconds equal beats at 60 BPM.</summary>
    public static class PresetSignalSampler
    {
        public const float GraphBeats = 8;

        private static readonly VoiceTime OneBeatPerSecond = new(0, 60);

        /// <summary>Returns false while a draft value is invalid, e.g. a negative duration being typed.</summary>
        public static bool TryCreate(PresetComponent component, out IModulator modulator)
        {
            modulator = null;
            if (component == null) return false;

            try
            {
                if (component.Kind == ModulatorKind.Adsr)
                {
                    var adsr = new AdsrDefinition(
                        component.Attack, component.Decay, component.SustainLevel, component.SustainTime,
                        component.Release);
                    modulator = new AdsrInstance(adsr, OneBeatPerSecond);
                }
                else
                {
                    var lfo = new LfoDefinition(component.CyclesPerBeat, component.PhaseOffset, component.Waveform);
                    modulator = new LfoInstance(lfo, OneBeatPerSecond);
                }

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static double SampleOrZero(PresetComponent component, double beat)
        {
            return TryCreate(component, out var modulator) ? modulator.Evaluate(beat) : 0;
        }

        /// <summary>The Value's signal: Σ Weight × component.</summary>
        public static double SampleOrZero(PresetValue value, double beat)
        {
            return value?.Components.Sum(component => component.Weight * SampleOrZero(component, beat)) ?? 0;
        }
    }

    /// <summary>Weighted components (thin) and their sum (thick) for one Value, repainted only when inputs change.</summary>
    public sealed class PresetSignalGraph
    {
        private const int Samples = 180;
        private const float Height = 180;

        private static readonly Color Background = new(0.07f, 0.08f, 0.1f);
        private static readonly Color Grid = new(1, 1, 1, 0.06f);
        private static readonly Color Zero = new(1, 1, 1, 0.2f);
        private static readonly Color Sum = new(0.95f, 0.95f, 0.95f);
        private static readonly Color Cursor = new(1, 1, 1, 0.35f);

        private readonly Func<PresetValue> _value;
        private readonly Func<float> _cursorBeat;

        private VisualElement _view;
        private string _signature;

        public PresetSignalGraph(Func<PresetValue> value, Func<float> cursorBeat)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _cursorBeat = cursorBeat ?? throw new ArgumentNullException(nameof(cursorBeat));
        }

        public Element CreateElement()
        {
            var box = UI.Space().SetHeight(Height).SetMinHeight(Height).SetFlexShrink(0);
            box.RegisterUpdateCallback(element =>
            {
                Attach(UIToolkitBuilder.Instance.GetUIObj(element));
                RepaintIfChanged();
            });
            return box;
        }

        private void Attach(VisualElement view)
        {
            if (view == null || view == _view) return;

            if (_view != null) _view.generateVisualContent -= Draw;

            _view = view;
            _view.style.backgroundColor = Background;
            _view.generateVisualContent += Draw;
            _signature = null;
        }

        private void RepaintIfChanged()
        {
            if (_view == null) return;

            var signature = Signature();
            if (signature == _signature) return;

            _signature = signature;
            _view.MarkDirtyRepaint();
        }

        private string Signature()
        {
            var value = _value();
            var builder = new StringBuilder(value?.Id).Append('|').Append(_cursorBeat().ToString("F3"));
            if (value == null) return builder.ToString();

            foreach (var c in value.Components)
            {
                builder.Append('|').Append(c.Kind).Append(c.Weight).Append(',').Append(c.Attack).Append(',')
                    .Append(c.Decay).Append(',').Append(c.SustainLevel).Append(',').Append(c.SustainTime).Append(',')
                    .Append(c.Release).Append(',').Append(c.CyclesPerBeat).Append(',').Append(c.PhaseOffset)
                    .Append(c.Waveform);
            }

            return builder.ToString();
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = _view.contentRect;
            if (rect.width <= 2 || rect.height <= 2) return;

            var painter = context.painter2D;
            painter.lineWidth = 1;
            painter.strokeColor = Grid;
            for (var i = 1; i < 6; i++)
            {
                Line(painter, new Vector2(0, rect.height * i / 6), new Vector2(rect.width, rect.height * i / 6));
                Line(painter, new Vector2(rect.width * i / 6, 0), new Vector2(rect.width * i / 6, rect.height));
            }

            var value = _value();
            if (value != null)
            {
                var curves = value.Components
                    .Select(component => (component, points: SampleCurve(beat =>
                        component.Weight * PresetSignalSampler.SampleOrZero(component, beat))))
                    .ToArray();
                var sum = SampleCurve(beat => PresetSignalSampler.SampleOrZero(value, beat));
                var scale = Scale(curves.Select(curve => curve.points).Append(sum));

                painter.strokeColor = Zero;
                Line(painter, new Vector2(0, Y(0, scale, rect)), new Vector2(rect.width, Y(0, scale, rect)));
                foreach (var (component, points) in curves)
                {
                    var color = component.Kind == ModulatorKind.Adsr ? PresetEditorPalette.Adsr : PresetEditorPalette.Lfo;
                    Curve(painter, points, scale, rect, new Color(color.r, color.g, color.b, 0.6f), 1.5f);
                }

                Curve(painter, sum, scale, rect, Sum, 2.5f);
            }

            var cursor = Mathf.Clamp(_cursorBeat(), 0, PresetSignalSampler.GraphBeats) / PresetSignalSampler.GraphBeats
                         * rect.width;
            painter.strokeColor = Cursor;
            painter.lineWidth = 1;
            Line(painter, new Vector2(cursor, 0), new Vector2(cursor, rect.height));
        }

        private static double[] SampleCurve(Func<double, double> sample)
        {
            var points = new double[Samples + 1];
            for (var i = 0; i <= Samples; i++)
            {
                points[i] = sample(PresetSignalSampler.GraphBeats * i / Samples);
            }

            return points;
        }

        // Keeps ±1 in view and grows to fit a sum that exceeds it.
        private static double Scale(IEnumerable<double[]> curves)
        {
            var peak = curves.SelectMany(points => points)
                .Where(point => !double.IsNaN(point) && !double.IsInfinity(point))
                .Select(Math.Abs)
                .DefaultIfEmpty(1)
                .Max();
            return Math.Max(1, peak);
        }

        private static void Curve(Painter2D painter, double[] points, double scale, Rect rect, Color color, float width)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            for (var i = 0; i < points.Length; i++)
            {
                var point = new Vector2(rect.width * i / (points.Length - 1), Y(points[i], scale, rect));
                if (i == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }

            painter.Stroke();
        }

        // Zero in the middle; ±scale at 40% of the height from the center.
        private static float Y(double value, double scale, Rect rect)
        {
            return Mathf.Clamp(rect.height * 0.5f - rect.height * 0.4f * (float)(value / scale), 3, rect.height - 3);
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
