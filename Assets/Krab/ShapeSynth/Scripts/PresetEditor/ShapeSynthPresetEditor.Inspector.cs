using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Geometry;
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
                    ParameterHeader(copies),
                    ParameterRow("Position X", node.PositionX, PresetQuantity.Length, copies, InitialDepth.Pixels100),
                    ParameterRow("Position Y", node.PositionY, PresetQuantity.Length, copies, InitialDepth.Pixels100),
                    ParameterRow("Direction", node.Direction, PresetQuantity.Angle, copies, InitialDepth.Degrees90),
                    ParameterRow("Distance", node.Distance, PresetQuantity.Length, copies, InitialDepth.Pixels100),
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
            // The point that Position places and Rotation/Scale turn around; Bottom grows a Shape upwards.
            yield return Choice("Anchor", () => (int)node.Anchor, value =>
            {
                node.Anchor = (PresetAnchor)value;
                Changed();
            }, AnchorNames);
            yield return ParameterHeader(node.Layout);
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

        /// <summary>
        /// Fill items appear only while Fill is on, stroke items only while Stroke is on. Both colors use the same
        /// fields: a picker for the resting color, then Hue / Saturation / Brightness rows that can be bound.
        /// </summary>
        private IEnumerable<Element> StyleFields(PresetNode node)
        {
            yield return Toggle("Fill", () => node.Fill, value => node.Fill = value);
            yield return UI.DynamicElementIf(() => node.Fill, () => UI.Column(ColorFields(node, "Fill Color",
                node.FillHue, node.FillSaturation, node.FillBrightness,
                () => node.FillAlpha, alpha => node.FillAlpha = alpha)));
            yield return Toggle("Stroke", () => node.Stroke, value => node.Stroke = value);
            yield return UI.DynamicElementIf(() => node.Stroke, () => UI.Column(StrokeFields(node)));
        }

        private IEnumerable<Element> StrokeFields(PresetNode node)
        {
            return ColorFields(node, "Stroke Color", node.StrokeHue, node.StrokeSaturation, node.StrokeBrightness,
                () => node.StrokeAlpha, alpha => node.StrokeAlpha = alpha, node.StrokeWidth);
        }

        /// <param name="strokeWidth">The stroke's Width row, placed first in the same table; null for Fill.</param>
        private IEnumerable<Element> ColorFields(PresetNode node, string label, PresetParameter hue,
            PresetParameter saturation, PresetParameter brightness, Func<double> readAlpha, Action<double> writeAlpha,
            PresetParameter strokeWidth = null)
        {
            yield return HsbColorField(label, hue, saturation, brightness, readAlpha, writeAlpha);
            yield return ParameterHeader(node.Layout);
            if (strokeWidth != null)
                yield return ParameterRow("Width", strokeWidth, PresetQuantity.Length, node.Layout);

            yield return ParameterRow("Hue", hue, PresetQuantity.Angle, node.Layout, InitialDepth.Degrees360);
            yield return ParameterRow("Saturation", saturation, PresetQuantity.Ratio, node.Layout);
            yield return ParameterRow("Brightness", brightness, PresetQuantity.Ratio, node.Layout);
        }

        private Element Toggle(string label, Func<bool> read, Action<bool> write)
        {
            return UI.Field(label, read, value =>
            {
                write(value);
                Changed();
            });
        }

        /// <summary>
        /// Shows the resting color of three HSB Parameters and writes a picked color back into their Bases. The
        /// Parameters stay the only source of the color. Picking a gray keeps the hue, and black keeps the saturation,
        /// so a later brighter pick does not jump back to red.
        /// </summary>
        private Element HsbColorField(string label, PresetParameter hue, PresetParameter saturation,
            PresetParameter brightness, Func<double> readAlpha, Action<double> writeAlpha)
        {
            return UI.Field(label, () =>
            {
                var color = ColorDefinition.FromHsb(RestingValue(hue), RestingValue(saturation),
                    RestingValue(brightness), readAlpha());
                return new Color((float)color.R, (float)color.G, (float)color.B, (float)color.A);
            }, value =>
            {
                var (h, s, b) = ColorDefinition.ToHsb(new RgbaColor(value.r, value.g, value.b, value.a));
                if (b > 0 && s > 0) SetRestingValue(hue, h);
                if (b > 0) SetRestingValue(saturation, s);
                SetRestingValue(brightness, b);
                writeAlpha(value.a);
                Changed();
            });
        }

        // Color Parameters are Absolute (deg / turn / ratio); the resting value is Base × the unit factor.
        private static double RestingValue(PresetParameter parameter) => parameter.Base * UnitFactor(parameter);

        private static void SetRestingValue(PresetParameter parameter, double value)
        {
            var factor = UnitFactor(parameter);
            if (factor != 0) parameter.Base = value / factor;
        }

        private static double UnitFactor(PresetParameter parameter) =>
            parameter.Reference == ReferenceSource.Absolute ? parameter.Multiplier * parameter.AbsoluteValue : 1;

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
