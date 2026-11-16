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
            if (!Components(document).Any(component => component.Kind == ModulatorKind.Adsr))
            {
                throw new ArgumentException(
                    "Preview/Trigger requires at least one ADSR Lifetime. The draft can still be saved.");
            }

            return new TriggerTarget(group);
        }

        public static GroupDefinition CompileGroup(PresetDocument document)
        {
            ValidateDocument(document);

            var slots = Components(document).Select(Slot).ToArray();
            var indices = new ValueIndices();
            var next = 0;
            foreach (var value in document.Values)
            {
                indices.Add(value.Id, (next, value));
                next += value.Components.Count;
            }

            return (GroupDefinition)CompileNode(document.RootGroup, indices, new GroupRuntimeDefinition(slots));
        }

        /// <summary>Every component in Value order; this is the Root scope slot order.</summary>
        public static IEnumerable<PresetComponent> Components(PresetDocument document)
        {
            return document.Values.SelectMany(value => value.Components);
        }

        internal static void ValidateDocument(PresetDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (document.SchemaVersion != PresetDocument.CurrentSchemaVersion)
                throw new ArgumentException($"Unsupported preset schema version {document.SchemaVersion}.");

            PresetDocument.RequireId(document.Id);
            if (string.IsNullOrWhiteSpace(document.Name)) throw new ArgumentException("Preset name is required.");
            if (document.Values == null || document.Values.Count > 2000)
                throw new ArgumentException("Invalid Value list.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var componentCount = 0;
            foreach (var value in document.Values)
            {
                if (value == null) throw new ArgumentException("Null Value.");

                PresetDocument.RequireId(value.Id);
                if (!ids.Add(value.Id)) throw new ArgumentException("Duplicate Value ID: " + value.Id);
                if (string.IsNullOrWhiteSpace(value.Name)) throw new ArgumentException("Value name is required.");
                if (value.Components == null) throw new ArgumentException("Null Value components.");

                foreach (var component in value.Components)
                {
                    if (component == null) throw new ArgumentException("Null Value component.");

                    VoiceTime.RequireFinite(component.Weight, "Component Weight");
                    Slot(component);
                }

                componentCount += value.Components.Count;
                if (componentCount > 2000) throw new ArgumentException("A preset can have at most 2000 components.");
            }

            if (document.RootGroup == null || document.RootGroup.Kind != PresetNodeKind.Group)
                throw new ArgumentException("Root Group is required.");

            ValidateNode(document.RootGroup, ids, new HashSet<string>(StringComparer.Ordinal), 0);
            if (document.Nodes().Sum(node => (long)node.Layout.GetCopyCount()) > PresetLayout.MaxTotalCopies)
                throw new ArgumentException($"A preset can draw at most {PresetLayout.MaxTotalCopies} Shapes.");
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

            if (node.Layout == null) throw new ArgumentException("Null Layout.");
            if (!Enum.IsDefined(typeof(PresetLayoutKind), node.Layout.Kind))
                throw new ArgumentException("Unknown Layout kind.");
            if (node.Kind == PresetNodeKind.Group && node.Layout.Kind != PresetLayoutKind.None)
                throw new ArgumentException("Layout is set on Shapes, not Groups.");

            PresetLayout.RequireCount(node.Layout.Count);
            PresetLayout.RequireColumns(node.Layout.Columns);
            VoiceTime.RequireFinite(node.Layout.StartAngle, "Layout Start Angle");
            VoiceTime.RequireFinite(node.Layout.Delay, "Layout Delay");
            if (node.Layout.Delay < 0) throw new ArgumentOutOfRangeException("Layout Delay", "Delay cannot be negative.");
            if (!Enum.IsDefined(typeof(PresetDelayOrder), node.Layout.DelayOrder))
                throw new ArgumentException("Unknown Delay order.");
            foreach (var parameter in node.Parameters())
            {
                if (parameter == null || parameter.Bindings == null)
                    throw new ArgumentException("Null Parameter or Bindings.");
                if (parameter.Bindings.Count > 1)
                    throw new ArgumentException("A Parameter can bind at most one Value.");

                new ReferenceDefinition(parameter.Reference, parameter.Multiplier, parameter.AbsoluteValue);
                VoiceTime.RequireFinite(parameter.Base, "Parameter Base");
                VoiceTime.RequireFinite(parameter.Random, "Parameter Random");
                if (!Enum.IsDefined(typeof(PresetFlip), parameter.Flip)) throw new ArgumentException("Unknown Flip.");
                foreach (var binding in parameter.Bindings)
                {
                    if (binding == null || !modulators.Contains(binding.ValueId ?? ""))
                        throw new ArgumentException("Binding refers to an unknown Value ID.");

                    VoiceTime.RequireFinite(binding.Depth, "Binding Depth");
                }
            }

            Color(node);
            ToColor(node);
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

        private static ModulatorSlotDefinition Slot(PresetComponent model)
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

        // A bound Value becomes one Binding per component, so the runtime sum equals Depth × Σ Weight × signal.
        // scale multiplies the whole result: Reference × (scale·Base + Σ scale·Depth × signal).
        // copy adds a Shape copy's Flip sign and Delay; Random adds a ±spread to Base per occurrence.
        private static ParameterDefinition Parameter(PresetParameter model, ValueIndices indices, double scale = 1,
            CopyModifier copy = null)
        {
            var sign = copy?.Sign(model) ?? 1;
            var bindings = model.Bindings.SelectMany(binding =>
            {
                var (first, value) = indices[binding.ValueId];
                return value.Components.Select((component, index) =>
                    new BindingDefinition(first + index, scale * sign * binding.Depth * component.Weight,
                        delayBeats: copy?.DelayBeats ?? 0, delayRandom: copy?.DelayRandom));
            });
            var parameter = new ParameterDefinition(
                new ReferenceDefinition(model.Reference, model.Multiplier, model.AbsoluteValue),
                scale * model.Base,
                bindings.ToArray());
            return model.Random == 0 ? parameter : parameter.WithBaseRandom(RandomRange.Spread(scale * model.Random));
        }

        private static RgbaColor Color(PresetNode node) =>
            new RgbaColor(node.Red, node.Green, node.Blue, node.Alpha);

        private static RgbaColor ToColor(PresetNode node) =>
            new RgbaColor(node.ToRed, node.ToGreen, node.ToBlue, node.ToAlpha);

        private static ShapeStyle Style(PresetNode node) => new ShapeStyle(node.Fill, node.Stroke, node.StrokeWidth,
            new RgbaColor(node.StrokeRed, node.StrokeGreen, node.StrokeBlue, node.StrokeAlpha));

        private static INodeDefinition CompileNode(PresetNode node, ValueIndices indices,
            GroupRuntimeDefinition runtime = null)
        {
            if (node.Kind == PresetNodeKind.Group)
            {
                var transform = new TransformDefinition(Parameter(node.PositionX, indices),
                    Parameter(node.PositionY, indices), Parameter(node.Rotation, indices),
                    Parameter(node.ScaleX, indices), Parameter(node.ScaleY, indices));
                return new GroupDefinition(transform, runtime,
                    node.Children.Select(child => CompileNode(child, indices)).ToArray());
            }

            var layout = node.Layout;
            if (layout.Kind == PresetLayoutKind.None)
                return CompileShape(node, indices, new CopyModifier(layout, 0, 1));

            // Each copy has its own Flip sign and Delay. Position stays outside the slot (and its Face Outward
            // rotation), so it moves the copy in the Shape's parent space, as for the whole arrangement.
            var copies = new INodeDefinition[layout.Count];
            for (var i = 0; i < copies.Length; i++)
            {
                var copy = new CopyModifier(layout, i, copies.Length);
                var position = new TransformDefinition(Parameter(node.PositionX, indices, copy: copy),
                    Parameter(node.PositionY, indices, copy: copy));
                var slot = Slot(layout, i, copies.Length, CompileShape(node, indices, copy, false), indices);
                copies[i] = new GroupDefinition(position, slot);
            }

            return new GroupDefinition(null, copies);
        }

        /// <summary>The Shape with Rotation/Scale and, unless a copy wrapper owns it, its Position.</summary>
        private static INodeDefinition CompileShape(PresetNode node, ValueIndices indices, CopyModifier copy,
            bool withPosition = true)
        {
            ParameterDefinition P(PresetParameter model) => Parameter(model, indices, copy: copy);

            var transform = new TransformDefinition(withPosition ? P(node.PositionX) : null,
                withPosition ? P(node.PositionY) : null, P(node.Rotation), P(node.ScaleX), P(node.ScaleY));
            var fill = new ColorDefinition(Color(node), ToColor(node), P(node.ColorMix), P(node.HueShift));
            return CompileGeometry(node, transform, fill, P);
        }

        private static ShapeDefinition CompileGeometry(PresetNode node, TransformDefinition transform,
            ColorDefinition fill, Func<PresetParameter, ParameterDefinition> parameter)
        {
            var opacity = parameter(node.Opacity);
            switch (node.Kind)
            {
                case PresetNodeKind.Circle:
                    return ShapeDefinition.Circle(parameter(node.Width), transform, opacity, style: Style(node),
                        fillColor: fill);
                case PresetNodeKind.Rectangle:
                    return ShapeDefinition.Rectangle(parameter(node.Width), parameter(node.Height),
                        transform, opacity, style: Style(node), fillColor: fill);
                case PresetNodeKind.Polygon:
                    return ShapeDefinition.Polygon(
                        node.Vertices.Select(point => new Point2(point.X, point.Y)).ToArray(), transform, opacity,
                        style: Style(node), fillColor: fill);
                default:
                    throw new ArgumentException("Unknown Node kind.");
            }
        }

        /// <summary>A scope-less wrapper Group that translates one copy to its Layout slot.</summary>
        private static INodeDefinition Slot(PresetLayout layout, int index, int count, INodeDefinition shape,
            ValueIndices indices)
        {
            var (x, y) = layout.Slot(index, count);
            // Facing outward: the copy's local +Y points away from the center.
            var rotation = layout.Kind == PresetLayoutKind.Circle && layout.FaceOutward
                ? ParameterDefinition.Constant(layout.Angle(index, count) - 90)
                : null;
            var slot = new TransformDefinition(Parameter(layout.GetAxisX(), indices, x),
                Parameter(layout.GetAxisY(), indices, y), rotation);
            return new GroupDefinition(slot, shape);
        }

        /// <summary>A copy's Flip sign per Parameter and its Delay (fixed by order, or a random 0–Delay).</summary>
        private sealed class CopyModifier
        {
            private readonly PresetLayout _layout;
            private readonly int _index;

            public CopyModifier(PresetLayout layout, int index, int count)
            {
                _layout = layout;
                _index = index;
                DelayBeats = PresetCopyPattern.DelayBeats(layout, index, count);
                if (layout.DelayOrder == PresetDelayOrder.Random && layout.Delay > 0)
                    DelayRandom = new RandomRange(0, layout.Delay);
            }

            public double DelayBeats { get; }
            public RandomRange DelayRandom { get; }

            public double Sign(PresetParameter parameter) =>
                PresetCopyPattern.FlipSign(parameter.Flip, _layout, _index);
        }

        private sealed class ValueIndices : Dictionary<string, (int First, PresetValue Value)>
        {
            public ValueIndices() : base(StringComparer.Ordinal)
            {
            }
        }
    }
}
