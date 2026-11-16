using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using UnityEngine;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private static readonly string[] LayoutNames = Enum.GetNames(typeof(PresetLayoutKind));
        private static readonly string[] DelayOrderNames = Enum.GetNames(typeof(PresetDelayOrder));

        private Element BuildInspectorPane()
        {
            return UI.Column(
                PaneTitle("Inspector"),
                UI.ScrollViewVertical(
                    520,
                    UI.DynamicElementOnStatusChanged(
                        () => (_documentRevision, _selectedId),
                        _ => BuildInspector())));
        }

        private Element BuildInspector()
        {
            var node = Selected;
            var copies = node.Kind == PresetNodeKind.Group ? null : node.Layout;
            var elements = new List<Element>
            {
                UI.Field("Name", () => node.Name, value =>
                {
                    node.Name = value;
                    _dirty = true;
                }),
                UI.Fold(
                    "Transform",
                    ParameterHeader(),
                    ParameterRow("Position X", node.PositionX, PresetQuantity.Length, copies, InitialDepth.Pixels100),
                    ParameterRow("Position Y", node.PositionY, PresetQuantity.Length, copies, InitialDepth.Pixels100),
                    ParameterRow("Rotation", node.Rotation, PresetQuantity.Angle, copies, InitialDepth.Degrees90),
                    ParameterRow("Scale X", node.ScaleX, PresetQuantity.Scale, copies),
                    ParameterRow("Scale Y", node.ScaleY, PresetQuantity.Scale, copies)).Open()
            };
            if (node.Kind == PresetNodeKind.Group) return UI.Column(elements);

            elements.Add(UI.Fold("Shape", ShapeParameters(node)).Open());
            elements.Add(UI.Fold("Layout", LayoutFields(node.Layout)).Open());
            elements.Add(UI.Fold("Style", StyleFields(node)).Open());
            return UI.Column(elements);
        }

        private IEnumerable<Element> ShapeParameters(PresetNode node)
        {
            yield return ParameterHeader();
            if (node.Kind == PresetNodeKind.Circle)
                yield return ParameterRow("Diameter", node.Width, PresetQuantity.Length, node.Layout);

            if (node.Kind == PresetNodeKind.Rectangle)
            {
                yield return ParameterRow("Width", node.Width, PresetQuantity.Length, node.Layout);
                yield return ParameterRow("Height", node.Height, PresetQuantity.Length, node.Layout);
            }

            yield return ParameterRow("Opacity", node.Opacity, PresetQuantity.Ratio, node.Layout, InitialDepth.One);
        }

        /// <summary>None draws the Shape once; other kinds draw Count copies. Only the fields in use are shown.</summary>
        private IEnumerable<Element> LayoutFields(PresetLayout layout)
        {
            yield return Choice("Kind", () => (int)layout.Kind, value =>
            {
                layout.Kind = (PresetLayoutKind)value;
                Changed();
            }, LayoutNames);
            yield return UI.DynamicElementOnStatusChanged(() => layout.Kind, kind =>
            {
                var fields = new List<Element>();
                if (kind != PresetLayoutKind.None)
                {
                    fields.Add(UI.Field("Count", () => layout.Count, value =>
                    {
                        layout.Count = Mathf.Clamp(value, 1, PresetLayout.MaxCount);
                        Changed();
                    }));
                }

                if (kind == PresetLayoutKind.Grid)
                {
                    fields.Add(UI.Field("Columns", () => layout.Columns, value =>
                    {
                        layout.Columns = Mathf.Clamp(value, 1, PresetLayout.MaxColumns);
                        Changed();
                    }));
                }

                fields.Add(NumberField("Delay (beats)", () => layout.Delay, value => layout.Delay = Math.Max(0, value)));
                // The order only matters once there is a delay to stagger.
                fields.Add(UI.DynamicElementIf(() => layout.Delay > 0, () => Choice("Delay Order",
                    () => (int)layout.DelayOrder, value =>
                    {
                        layout.DelayOrder = (PresetDelayOrder)value;
                        Changed();
                    }, DelayOrderNames)));
                if (kind != PresetLayoutKind.None) fields.Add(ParameterHeader());
                if (kind == PresetLayoutKind.Row || kind == PresetLayoutKind.Grid)
                    fields.Add(ParameterRow("Spacing X", layout.SpacingX, PresetQuantity.Length));
                if (kind == PresetLayoutKind.Column || kind == PresetLayoutKind.Grid)
                    fields.Add(ParameterRow("Spacing Y", layout.SpacingY, PresetQuantity.Length));
                if (kind == PresetLayoutKind.Circle)
                {
                    fields.Add(ParameterRow("Radius", layout.Radius, PresetQuantity.Length));
                    fields.Add(NumberField("Start Angle", () => layout.StartAngle, value => layout.StartAngle = value));
                    fields.Add(Toggle("Face Outward", () => layout.FaceOutward, value => layout.FaceOutward = value));
                }

                return UI.Column(fields);
            });
        }

        /// <summary>Color items appear only while Fill is on, stroke items only while Stroke is on.</summary>
        private IEnumerable<Element> StyleFields(PresetNode node)
        {
            yield return Toggle("Fill", () => node.Fill, value => node.Fill = value);
            yield return UI.DynamicElementIf(() => node.Fill, () => UI.Column(FillFields(node)));
            yield return Toggle("Stroke", () => node.Stroke, value => node.Stroke = value);
            yield return UI.DynamicElementIf(() => node.Stroke, () => UI.Column(
                NumberField("Stroke Width", () => node.StrokeWidth, value => node.StrokeWidth = value),
                ColorField("Stroke Color",
                    () => (node.StrokeRed, node.StrokeGreen, node.StrokeBlue, node.StrokeAlpha),
                    value => (node.StrokeRed, node.StrokeGreen, node.StrokeBlue, node.StrokeAlpha) = value)));
        }

        /// <summary>To Color only matters while Color Mix can be above 0 (non-zero Base or a bound Value).</summary>
        private IEnumerable<Element> FillFields(PresetNode node)
        {
            yield return ColorField("Color",
                () => (node.Red, node.Green, node.Blue, node.Alpha),
                value => (node.Red, node.Green, node.Blue, node.Alpha) = value);
            yield return ParameterHeader();
            yield return ParameterRow("Color Mix", node.ColorMix, PresetQuantity.Ratio, node.Layout);
            yield return UI.DynamicElementIf(
                () => node.ColorMix.Base != 0 || node.ColorMix.GetBinding() != null,
                () => ColorField("To Color",
                    () => (node.ToRed, node.ToGreen, node.ToBlue, node.ToAlpha),
                    value => (node.ToRed, node.ToGreen, node.ToBlue, node.ToAlpha) = value));
            yield return ParameterRow("Hue Shift", node.HueShift, PresetQuantity.Angle, node.Layout,
                InitialDepth.Degrees360);
        }

        private Element Toggle(string label, Func<bool> read, Action<bool> write)
        {
            return UI.Field(label, read, value =>
            {
                write(value);
                Changed();
            });
        }

        private Element ColorField(string label, Func<(double R, double G, double B, double A)> read,
            Action<(double R, double G, double B, double A)> write)
        {
            return UI.Field(label, () =>
            {
                var (r, g, b, a) = read();
                return new Color((float)r, (float)g, (float)b, (float)a);
            }, value =>
            {
                write((value.r, value.g, value.b, value.a));
                Changed();
            });
        }

        private Element NumberField(string label, Func<double> read, Action<double> write)
        {
            return UI.Field(label, () => (float)read(), value =>
            {
                write(value);
                Changed();
            });
        }
    }
}
