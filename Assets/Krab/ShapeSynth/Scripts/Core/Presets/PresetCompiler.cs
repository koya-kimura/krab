using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Presets
{
    /// <summary>Authoring DTO to immutable Core snapshot. No IO, UI, clock or runtime state.</summary>
    public static class PresetCompiler
    {
        public static TriggerTarget Compile(PresetDocument document)
        {
            var group = CompileGroup(document);
            if (!document.Modulators.Any(model => model.Kind == ModulatorKind.Adsr))
            {
                throw new ArgumentException(
                    "Preview/Trigger requires at least one ADSR Lifetime. The draft can still be saved.");
            }

            return new TriggerTarget(group);
        }

        public static GroupDefinition CompileGroup(PresetDocument document)
        {
            ValidateDocument(document);

            var slots = document.Modulators.Select(Slot).ToArray();
            var indices = document.Modulators
                .Select((model, index) => (model.Id, index))
                .ToDictionary(pair => pair.Id, pair => pair.index);
            return (GroupDefinition)CompileNode(document.RootGroup, indices, new GroupRuntimeDefinition(slots));
        }

        internal static void ValidateDocument(PresetDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (document.SchemaVersion != 1)
                throw new ArgumentException($"Unsupported preset schema version {document.SchemaVersion}.");

            PresetDocument.RequireId(document.Id);
            if (string.IsNullOrWhiteSpace(document.Name)) throw new ArgumentException("Preset name is required.");
            if (document.Modulators == null || document.Modulators.Count > 2000)
                throw new ArgumentException("Invalid Modulator list.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var model in document.Modulators)
            {
                if (model == null) throw new ArgumentException("Null Modulator.");

                PresetDocument.RequireId(model.Id);
                if (!ids.Add(model.Id)) throw new ArgumentException("Duplicate Modulator ID: " + model.Id);
                if (string.IsNullOrWhiteSpace(model.Name))
                    throw new ArgumentException("Modulator name is required.");

                Slot(model);
            }

            if (document.RootGroup == null || document.RootGroup.Kind != PresetNodeKind.Group)
                throw new ArgumentException("Root Group is required.");

            ValidateNode(document.RootGroup, ids, new HashSet<string>(StringComparer.Ordinal), 0);
        }

        private static void ValidateNode(PresetNode node, HashSet<string> modulators, HashSet<string> nodes, int depth)
        {
            if (node == null || depth > 64 || nodes.Count >= 2000)
                throw new ArgumentException("Invalid hierarchy (maximum 64 levels / 2000 Nodes).");

            PresetDocument.RequireId(node.Id);
            if (!nodes.Add(node.Id))
                throw new ArgumentException("Duplicate Node ID or hierarchy cycle: " + node.Id);
            if (string.IsNullOrWhiteSpace(node.Name)) throw new ArgumentException("Node name is required.");
            if (!Enum.IsDefined(typeof(PresetNodeKind), node.Kind)) throw new ArgumentException("Unknown Node type.");
            if (node.Children == null || (node.Kind != PresetNodeKind.Group && node.Children.Count != 0))
                throw new ArgumentException("Only Groups can contain children.");

            foreach (var parameter in node.Parameters())
            {
                if (parameter == null || parameter.Bindings == null)
                    throw new ArgumentException("Null Parameter or Bindings.");

                new ReferenceDefinition(parameter.Reference, parameter.Multiplier, parameter.AbsoluteValue);
                VoiceTime.RequireFinite(parameter.Base, "Parameter Base");
                foreach (var binding in parameter.Bindings)
                {
                    if (binding == null || !modulators.Contains(binding.ModulatorId ?? ""))
                        throw new ArgumentException("Binding refers to an unknown Modulator ID.");

                    VoiceTime.RequireFinite(binding.Depth, "Binding Depth");
                }
            }

            Color(node);
            Style(node);
            if (node.Vertices == null || node.Vertices.Count > 2000
                || (node.Kind == PresetNodeKind.Polygon && node.Vertices.Count < 3))
            {
                throw new ArgumentException("Polygon requires 3–2000 vertices.");
            }

            foreach (var point in node.Vertices)
            {
                if (point == null) throw new ArgumentException("Null vertex.");

                VoiceTime.RequireFinite(point.X, "Vertex X");
                VoiceTime.RequireFinite(point.Y, "Vertex Y");
            }

            foreach (var child in node.Children)
            {
                ValidateNode(child, modulators, nodes, depth + 1);
            }
        }

        private static ModulatorSlotDefinition Slot(ModulatorSettings model)
        {
            switch (model.Kind)
            {
                case ModulatorKind.Adsr:
                    return new ModulatorSlotDefinition(new AdsrDefinition(model.Attack, model.Decay,
                        model.SustainLevel, model.SustainTime, model.Release));
                case ModulatorKind.Lfo:
                    return new ModulatorSlotDefinition(
                        new LfoDefinition(model.CyclesPerBeat, model.PhaseOffset, model.Waveform));
                default:
                    throw new ArgumentException("Unknown Modulator kind.");
            }
        }

        private static ParameterDefinition Parameter(PresetParameter model, Dictionary<string, int> indices) =>
            new ParameterDefinition(
                new ReferenceDefinition(model.Reference, model.Multiplier, model.AbsoluteValue), model.Base,
                model.Bindings.Select(binding => new BindingDefinition(indices[binding.ModulatorId], binding.Depth))
                    .ToArray());

        private static RgbaColor Color(PresetNode node) =>
            new RgbaColor(node.Red, node.Green, node.Blue, node.Alpha);

        private static ShapeStyle Style(PresetNode node) => new ShapeStyle(node.Fill, node.Stroke, node.StrokeWidth,
            new RgbaColor(node.StrokeRed, node.StrokeGreen, node.StrokeBlue, node.StrokeAlpha));

        private static INodeDefinition CompileNode(PresetNode node, Dictionary<string, int> indices,
            GroupRuntimeDefinition runtime = null)
        {
            var transform = new TransformDefinition(Parameter(node.PositionX, indices),
                Parameter(node.PositionY, indices), Parameter(node.Rotation, indices),
                Parameter(node.ScaleX, indices), Parameter(node.ScaleY, indices));
            if (node.Kind == PresetNodeKind.Group)
            {
                return new GroupDefinition(transform, runtime,
                    node.Children.Select(child => CompileNode(child, indices)).ToArray());
            }

            var opacity = Parameter(node.Opacity, indices);
            switch (node.Kind)
            {
                case PresetNodeKind.Circle:
                    return ShapeDefinition.Circle(Parameter(node.Width, indices), transform, opacity, Color(node),
                        Style(node));
                case PresetNodeKind.Rectangle:
                    return ShapeDefinition.Rectangle(Parameter(node.Width, indices), Parameter(node.Height, indices),
                        transform, opacity, Color(node), Style(node));
                case PresetNodeKind.Polygon:
                    return ShapeDefinition.Polygon(
                        node.Vertices.Select(point => new Point2(point.X, point.Y)).ToArray(), transform, opacity,
                        Color(node), Style(node));
                default:
                    throw new ArgumentException("Unknown Node kind.");
            }
        }
    }
}
