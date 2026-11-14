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
        private const string NoValueName = "None";

        private static readonly string[] ReferenceNames = Enum.GetNames(typeof(ReferenceSource));

        private Element BuildInspectorPane()
        {
            return UI.Column(
                PaneTitle("Inspector"),
                UI.ScrollViewVertical(
                    520,
                    UI.DynamicElementOnStatusChanged(
                        () => (_documentRevision, _selectedId, _modulatorsRevision, _modulatorNamesRevision),
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

        /// <summary>Base, Reference and the single bound Value of one Parameter.</summary>
        private Element ParameterFold(string name, PresetParameter parameter)
        {
            var models = Document.Modulators.ToArray();
            var header = UI.Label(() => ParameterHeader(name, parameter));
            header.RegisterUpdateCallback(element =>
                element.Style.Color = parameter.GetBinding() != null ? PresetEditorPalette.Bound : null);
            return UI.Fold(
                header,
                NumberField("Base", () => parameter.Base, value => parameter.Base = value),
                Choice("Reference", () => (int)parameter.Reference, value =>
                {
                    parameter.Reference = (ReferenceSource)value;
                    Changed();
                }, ReferenceNames),
                Choice(
                    "Value",
                    () => Array.FindIndex(models, model => model.Id == parameter.GetBinding()?.ModulatorId) + 1,
                    index => SelectValue(name, parameter, index == 0 ? null : models.ElementAtOrDefault(index - 1)),
                    new[] { NoValueName }.Concat(models.Select(model => model.Name))),
                UI.DynamicElementIf(
                    () => parameter.GetBinding() != null,
                    () => NumberField("Depth", () => parameter.GetBinding()?.Depth ?? 0, value =>
                    {
                        var binding = parameter.GetBinding();
                        if (binding != null) binding.Depth = value;
                    })),
                NumberField("Ref. Multiplier", () => parameter.Multiplier, value => parameter.Multiplier = value),
                NumberField("Absolute Value", () => parameter.AbsoluteValue, value => parameter.AbsoluteValue = value));
        }

        private string ParameterHeader(string name, PresetParameter parameter)
        {
            var binding = parameter.GetBinding();
            var model = binding == null ? null : Document.Modulators.FirstOrDefault(entry => entry.Id == binding.ModulatorId);
            var value = binding == null ? parameter.Base : parameter.Base + binding.Depth * SignalAtBeat(binding.ModulatorId);
            var output = ParameterOutput(parameter, value);
            return model == null ? $"{name}  {output}" : $"{name}  {output}  [{model.Name}]";
        }

        private void SelectValue(string name, PresetParameter parameter, ModulatorSettings model)
        {
            if (model == null)
            {
                parameter.Unbind();
                Changed();
                return;
            }

            var current = parameter.GetBinding();
            if (current?.ModulatorId == model.Id) return;

            parameter.Bind(model.Id, current?.Depth ?? DefaultDepth(name, parameter));
            Changed();
        }

        private static double DefaultDepth(string name, PresetParameter parameter)
        {
            if (name == "Opacity") return 1;
            if (name.StartsWith("Position")) return 100;
            if (name == "Rotation") return 90;

            return parameter.Base == 0 ? 1 : parameter.Base;
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
