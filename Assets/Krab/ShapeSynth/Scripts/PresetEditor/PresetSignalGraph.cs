using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using RosettaUI;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>Samples authoring settings through the Core signal instances. Seconds equal beats at 60 BPM.</summary>
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
                modulator = PresetCompiler.ModulatorDefinition(component).Create(OneBeatPerSecond);
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

    /// <summary>
    /// Weighted components (thin) and their sum (thick) for one Value. Curves are sampled only when the Value's
    /// settings change; the Beat cursor is a separate element moved without repainting the curves.
    /// </summary>
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
        private VisualElement _cursor;
        private string _signature;
        private (Color Color, double[] Points)[] _curves = Array.Empty<(Color, double[])>();
        private double[] _sum;
        private double _scale = 1;

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
                MoveCursor();
            });
            return box;
        }

        private void Attach(VisualElement view)
        {
            if (view == null || view == _view) return;

            if (_view != null) _view.generateVisualContent -= Draw;
            _cursor?.RemoveFromHierarchy();

            _view = view;
            _view.style.backgroundColor = Background;
            _view.generateVisualContent += Draw;
            _cursor = new VisualElement { pickingMode = PickingMode.Ignore };
            _cursor.style.position = Position.Absolute;
            _cursor.style.top = 0;
            _cursor.style.bottom = 0;
            _cursor.style.width = 1;
            _cursor.style.backgroundColor = Cursor;
            _view.Add(_cursor);
            _signature = null;
        }

        private void RepaintIfChanged()
        {
            if (_view == null) return;

            var signature = Signature();
            if (signature == _signature) return;

            _signature = signature;
            Resample();
            _view.MarkDirtyRepaint();
        }

        private void MoveCursor()
        {
            if (_cursor == null) return;

            var beat = Mathf.Clamp(_cursorBeat(), 0, PresetSignalSampler.GraphBeats);
            _cursor.style.left = beat / PresetSignalSampler.GraphBeats * _view.contentRect.width;
        }

        private string Signature()
        {
            var value = _value();
            var builder = new StringBuilder(value?.Id);
            if (value == null) return builder.ToString();

            foreach (var c in value.Components)
            {
                builder.Append('|').Append(c.Kind).Append(c.Weight).Append(',').Append(c.Attack).Append(',')
                    .Append(c.Decay).Append(',').Append(c.SustainLevel).Append(',').Append(c.SustainTime).Append(',')
                    .Append(c.Release).Append(',').Append(c.CyclesPerBeat).Append(',').Append(c.PhaseOffset)
                    .Append(c.Waveform).Append(',')
                    .Append(c.AttackCurve).Append(',').Append(c.DecayCurve).Append(',').Append(c.ReleaseCurve);
            }

            return builder.ToString();
        }

        /// <summary>Samples each weighted component once; the sum adds those points instead of resampling.</summary>
        private void Resample()
        {
            var value = _value();
            if (value == null)
            {
                _curves = Array.Empty<(Color, double[])>();
                _sum = null;
                _scale = 1;
                return;
            }

            _curves = value.Components.Select(component =>
            {
                var color = PresetEditorPalette.Modulator(component.Kind);
                var points = PresetSignalSampler.TryCreate(component, out var modulator)
                    ? SampleCurve(beat => component.Weight * modulator.Evaluate(beat))
                    : new double[Samples + 1];
                return (new Color(color.r, color.g, color.b, 0.6f), points);
            }).ToArray();

            _sum = new double[Samples + 1];
            foreach (var (_, points) in _curves)
            {
                for (var i = 0; i < _sum.Length; i++)
                {
                    _sum[i] += points[i];
                }
            }

            _scale = Scale(_curves.Select(curve => curve.Points).Append(_sum));
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

            if (_sum == null) return;

            painter.strokeColor = Zero;
            Line(painter, new Vector2(0, Y(0, _scale, rect)), new Vector2(rect.width, Y(0, _scale, rect)));
            foreach (var (color, points) in _curves)
            {
                Curve(painter, points, _scale, rect, color, 1.5f);
            }

            Curve(painter, _sum, _scale, rect, Sum, 2.5f);
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
