using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using UnityEngine;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private static readonly string[] ReferenceNames = Enum.GetNames(typeof(ReferenceSource));

        private Element BuildInspectorPane()
        {
            return _inspectorPane = UI.Column(
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
            var elements = new List<Element>
            {
                UI.Field("Name", () => node.Name, value =>
                {
                    node.Name = value;
                    _dirty = true;
                }),
                UI.Fold(
                    "Transform",
                    ParameterFold("Position X", node.PositionX),
                    ParameterFold("Position Y", node.PositionY),
                    ParameterFold("Rotation", node.Rotation),
                    ParameterFold("Scale X", node.ScaleX),
                    ParameterFold("Scale Y", node.ScaleY)).Open()
            };
            if (node.Kind == PresetNodeKind.Group) return UI.Column(elements);

            elements.Add(UI.Fold("Shape", ShapeParameters(node)).Open());
            elements.Add(UI.Fold("Style", StyleFields(node)).Open());
            return UI.Column(elements);
        }

        private IEnumerable<Element> ShapeParameters(PresetNode node)
        {
            if (node.Kind == PresetNodeKind.Circle) yield return ParameterFold("Diameter", node.Width);

            if (node.Kind == PresetNodeKind.Rectangle)
            {
                yield return ParameterFold("Width", node.Width);
                yield return ParameterFold("Height", node.Height);
            }

            yield return ParameterFold("Opacity", node.Opacity);
        }

        private IEnumerable<Element> StyleFields(PresetNode node)
        {
            yield return UI.Field(
                "Color",
                () => new Color((float)node.Red, (float)node.Green, (float)node.Blue, (float)node.Alpha),
                value =>
                {
                    node.Red = value.r;
                    node.Green = value.g;
                    node.Blue = value.b;
                    node.Alpha = value.a;
                    Changed();
                });
            yield return UI.Field("Fill", () => node.Fill, value =>
            {
                node.Fill = value;
                Changed();
            });
            yield return UI.Field("Stroke", () => node.Stroke, value =>
            {
                node.Stroke = value;
                Changed();
            });
            yield return NumberField("Stroke Width", () => node.StrokeWidth, value => node.StrokeWidth = value);
            yield return UI.Field(
                "Stroke Color",
                () => new Color((float)node.StrokeRed, (float)node.StrokeGreen, (float)node.StrokeBlue, (float)node.StrokeAlpha),
                value =>
                {
                    node.StrokeRed = value.r;
                    node.StrokeGreen = value.g;
                    node.StrokeBlue = value.b;
                    node.StrokeAlpha = value.a;
                    Changed();
                });
        }

        /// <summary>Base and Reference in the Fold; the bound Value and Depth open from the Bind button.</summary>
        private Element ParameterFold(string name, PresetParameter parameter)
        {
            var header = UI.Label(() => ParameterHeader(name, parameter));
            header.RegisterUpdateCallback(element =>
                element.Style.Color = parameter.GetBinding() != null ? PresetEditorPalette.Bound : null);
            return UI.Fold(
                header,
                BindButton(name, parameter),
                new[]
                {
                    NumberField("Base", () => parameter.Base, value => parameter.Base = value),
                    Choice("Reference", () => (int)parameter.Reference, value =>
                    {
                        parameter.Reference = (ReferenceSource)value;
                        Changed();
                    }, ReferenceNames),
                    NumberField("Ref. Multiplier", () => parameter.Multiplier, value => parameter.Multiplier = value),
                    NumberField("Absolute Value", () => parameter.AbsoluteValue, value => parameter.AbsoluteValue = value)
                });
        }

        private string ParameterHeader(string name, PresetParameter parameter)
        {
            return $"{name}  {ParameterValueText(parameter)}";
        }

        private string ParameterOutput(PresetParameter parameter, double value)
        {
            try
            {
                var texture = _controller && _controller.OutputCamera ? _controller.OutputCamera.targetTexture : null;
                var context = texture
                    ? new ReferenceContext(texture.width, texture.height)
                    : new ReferenceContext(1920, 1080);
                var reference = new ReferenceDefinition(parameter.Reference, parameter.Multiplier, parameter.AbsoluteValue);
                return (reference.Evaluate(context) * value).ToString("F2");
            }
            catch (ArgumentException)
            {
                return "-";
            }
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
