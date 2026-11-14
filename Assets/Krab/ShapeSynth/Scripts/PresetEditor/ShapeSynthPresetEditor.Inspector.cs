using System.Collections.Generic;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using UnityEngine;
using UnityEngine.UIElements;
using T = Krab.ShapeSynth.PresetEditor.PresetEditorTheme;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private Element BuildInspectorPane() => UI.Column(
            T.PaneTitle("Inspector"),
            VerticalScroll(UI.DynamicElementOnStatusChanged(() => (_documentRevision, _selectedId), _ => BuildInspector())).SetFlexGrow(1));

        private Element BuildInspector()
        {
            InspectorBuildCount++;
            var node = Selected;
            var fields = new List<Element>
            {
                UI.Field((LabelElement)null, () => node.Name, value => { node.Name = value; Changed(false); })
                    .Styled(style => { style.fontSize = 14; style.unityFontStyleAndWeight = FontStyle.Bold; }),
                T.Text(node.Kind.ToString(), 11, T.Caption).Styled(style => { style.marginTop = 2; style.marginLeft = 2; }),
                T.SectionTitle("Transform"),
                ParameterRow("Position X", node.PositionX),
                ParameterRow("Position Y", node.PositionY),
                ParameterRow("Rotation", node.Rotation),
                ParameterRow("Scale X", node.ScaleX),
                ParameterRow("Scale Y", node.ScaleY),
            };
            if (node.Kind != PresetNodeKind.Group)
            {
                fields.Add(T.SectionTitle("Shape"));
                if (node.Kind == PresetNodeKind.Circle) fields.Add(ParameterRow("Diameter", node.Width));
                if (node.Kind == PresetNodeKind.Rectangle)
                { fields.Add(ParameterRow("Width", node.Width)); fields.Add(ParameterRow("Height", node.Height)); }
                if (node.Kind == PresetNodeKind.Polygon)
                    fields.Add(T.Text("Triangle: local radius 100px. Use Scale / Rotation.", 11, T.Caption).Styled(style => style.marginTop = 6));
                fields.Add(ParameterRow("Opacity", node.Opacity));
                fields.Add(T.SectionTitle("Style"));
                fields.Add(FieldRow("Color", UI.Field((LabelElement)null, () => new Color((float)node.Red, (float)node.Green, (float)node.Blue, (float)node.Alpha), value =>
                    { node.Red = value.r; node.Green = value.g; node.Blue = value.b; node.Alpha = value.a; Changed(); })));
                fields.Add(FieldRow("Fill", UI.Field((LabelElement)null, () => node.Fill, value => { node.Fill = value; Changed(); })));
                fields.Add(FieldRow("Stroke", UI.Field((LabelElement)null, () => node.Stroke, value => { node.Stroke = value; Changed(); })));
                fields.Add(FieldRow("Stroke Width", NumberInput(() => node.StrokeWidth, value => node.StrokeWidth = value)));
                fields.Add(FieldRow("Stroke Color", UI.Field((LabelElement)null, () => new Color((float)node.StrokeRed, (float)node.StrokeGreen, (float)node.StrokeBlue, (float)node.StrokeAlpha), value =>
                    { node.StrokeRed = value.r; node.StrokeGreen = value.g; node.StrokeBlue = value.b; node.StrokeAlpha = value.a; Changed(); })));
            }
            else
                fields.Add(T.Text("Groups transform their children. Select a Shape for size, opacity and style.", 11, T.Caption)
                    .Styled(style => { style.marginTop = 10; style.whiteSpace = WhiteSpace.Normal; }));
            fields.Add(T.SectionTitle("Identity"));
            fields.Add(T.Text(node.Id, 10, T.Muted));
            return UI.Column(fields).Styled(style =>
            {
                style.paddingTop = 10; style.paddingBottom = 20; style.paddingLeft = 12; style.paddingRight = 12;
            });
        }

        /// <summary>Label on the left, fixed-width input on the right (the reference "field-row").</summary>
        private static Element FieldRow(string label, Element input) => UI.Row(
            T.Text(label, 12, T.Hex(0xc0c7d1)).SetFlexGrow(1),
            input.SetWidth(120).SetMinWidth(120).SetFlexGrow(0)
        ).Styled(style => { style.alignItems = Align.Center; style.marginTop = 4; style.marginBottom = 4; });
    }
}
