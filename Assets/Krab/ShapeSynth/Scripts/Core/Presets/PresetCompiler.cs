using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Presets
{
    /// <summary>Authoring DTO to immutable Core snapshot. No IO, UI, clock or runtime state.</summary>
    public static class PresetCompiler
    {
        /// <param name="autoRelease">
        /// false keeps every ADSR at Sustain until the Voice is released, e.g. while a pad is held.
        /// </param>
        public static TriggerTarget Compile(PresetDocument document, bool autoRelease = true)
        {
            var group = CompileGroup(document, autoRelease);
            if (!Components(document).Any(component => component.Kind == ModulatorKind.Adsr))
            {
                throw new ArgumentException(
                    "Preview/Trigger requires at least one bound ADSR Lifetime. The draft can still be saved.");
            }

            return new TriggerTarget(group);
        }

        public static GroupDefinition CompileGroup(PresetDocument document, bool autoRelease = true)
        {
            ValidateDocument(document);

            var slots = Components(document).Select(component => Slot(component, autoRelease)).ToArray();
            var indices = new ValueIndices();
            var next = 0;
            foreach (var value in document.BoundValues())
            {
                indices.Add(value.Id, (next, value));
                next += value.Components.Count;
            }

            return (GroupDefinition)CompileNode(document.RootGroup, indices, new GroupRuntimeDefinition(slots));
        }

        /// <summary>
        /// Every component of the bound Values, in Value order; this is the Root scope slot order. Unbound Values
        /// of the shared library add no modulators, so they do not lengthen the Voice.
        /// </summary>
        public static IEnumerable<PresetComponent> Components(PresetDocument document)
        {
            return document.BoundValues().SelectMany(value => value.Components);
        }

        internal static void ValidateDocument(PresetDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (document.SchemaVersion != PresetDocument.CurrentSchemaVersion)
                throw new ArgumentException($"Unsupported preset schema version {document.SchemaVersion}.");

            PresetDocument.RequireId(document.Id);
            if (string.IsNullOrWhiteSpace(document.Name)) throw new ArgumentException("Preset name is required.");

            if (document.Values == null || document.LocalValues == null) throw new ArgumentException("Invalid Value list.");

            var ids = ValidateValues(document.AllValues().ToList());
            if (document.RootGroup == null || document.RootGroup.Kind != PresetNodeKind.Group)
                throw new ArgumentException("Root Group is required.");

            ValidateNode(document.RootGroup, ids, new HashSet<string>(StringComparer.Ordinal), 0);
            if (document.Nodes().Sum(node => (long)node.Layout.GetCopyCount()) > PresetLayout.MaxTotalCopies)
                throw new ArgumentException($"A preset can draw at most {PresetLayout.MaxTotalCopies} Shapes.");
        }

        /// <summary>Checks a Value list (the shared library, or it with a preset's local Values) and returns its IDs.</summary>
        public static HashSet<string> ValidateValues(IReadOnlyList<PresetValue> values)
        {
            if (values == null || values.Count > 2000) throw new ArgumentException("Invalid Value list.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var componentCount = 0;
            foreach (var value in values)
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
                if (componentCount > 2000) throw new ArgumentException("Values can have at most 2000 components.");
            }

            return ids;
        }

        private static void ValidateNode(PresetNode node, HashSet<string> modulators, HashSet<string> nodes, int depth)
        {
            if (node == null || depth > PresetDocument.MaxDepth || nodes.Count >= 2000)
                throw new ArgumentException($"Invalid hierarchy (maximum {PresetDocument.MaxDepth} levels / 2000 Nodes).");

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

            VoiceTime.RequireFinite(node.FillAlpha, "Fill Alpha");
            VoiceTime.RequireFinite(node.StrokeAlpha, "Stroke Alpha");
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

        /// <summary>The Core settings of one component; throws while its values are invalid.</summary>
        public static IModulatorDefinition ModulatorDefinition(PresetComponent model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            switch (model.Kind)
            {
                case ModulatorKind.Adsr:
                    return new AdsrDefinition(model.Attack, model.Decay, model.SustainLevel, model.SustainTime,
                        model.Release, model.AttackCurve, model.DecayCurve, model.ReleaseCurve);
                case ModulatorKind.Lfo:
                    return new LfoDefinition(model.CyclesPerBeat, model.PhaseOffset, model.Waveform);
                default:
                    throw new ArgumentException("Unknown Modulator kind.");
            }
        }

        private static ModulatorSlotDefinition Slot(PresetComponent model, bool autoRelease = true)
        {
            var definition = ModulatorDefinition(model);
            return definition is AdsrDefinition adsr
                ? new ModulatorSlotDefinition(adsr, autoRelease)
                : new ModulatorSlotDefinition(definition);
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

        private static ShapeStyle Style(PresetNode node) => new ShapeStyle(node.Fill, node.Stroke);

        private static INodeDefinition CompileNode(PresetNode node, ValueIndices indices,
            GroupRuntimeDefinition runtime = null)
        {
            if (node.Kind == PresetNodeKind.Group)
            {
                var transform = new TransformDefinition(Parameter(node.PositionX, indices),
                    Parameter(node.PositionY, indices), Parameter(node.Rotation, indices),
                    Parameter(node.ScaleX, indices), Parameter(node.ScaleY, indices),
                    Parameter(node.Direction, indices), Parameter(node.Distance, indices));
                return new GroupDefinition(transform, runtime,
                    node.Children.Select(child => CompileNode(child, indices)).ToArray());
            }

            var layout = node.Layout;
            if (layout.Kind == PresetLayoutKind.None)
                return CompileShape(node, indices, new CopyModifier(layout, 0, 1));

            // Each copy has its own Flip sign and Delay. Position, Direction and Distance stay outside the slot (and
            // its Face Outward rotation), so they move the copy in the Shape's parent space, as for the whole
            // arrangement.
            var copies = new INodeDefinition[layout.Count];
            for (var i = 0; i < copies.Length; i++)
            {
                var copy = new CopyModifier(layout, i, copies.Length);
                ParameterDefinition P(PresetParameter model) => Parameter(model, indices, copy: copy);

                var position = new TransformDefinition(P(node.PositionX), P(node.PositionY),
                    directionDegrees: P(node.Direction), distance: P(node.Distance));
                var slot = Slot(layout, i, copies.Length, CompileShape(node, indices, copy, false), indices);
                copies[i] = new GroupDefinition(position, slot);
            }

            return new GroupDefinition(null, copies);
        }

        /// <summary>The Shape with Rotation/Scale and, unless a copy wrapper owns it, its Position and Move.</summary>
        private static INodeDefinition CompileShape(PresetNode node, ValueIndices indices, CopyModifier copy,
            bool withPosition = true)
        {
            ParameterDefinition P(PresetParameter model) => Parameter(model, indices, copy: copy);

            var transform = new TransformDefinition(withPosition ? P(node.PositionX) : null,
                withPosition ? P(node.PositionY) : null, P(node.Rotation), P(node.ScaleX), P(node.ScaleY),
                withPosition ? P(node.Direction) : null, withPosition ? P(node.Distance) : null);
            var fill = new ColorDefinition(P(node.FillHue), P(node.FillSaturation), P(node.FillBrightness),
                node.FillAlpha);
            var stroke = new ColorDefinition(P(node.StrokeHue), P(node.StrokeSaturation), P(node.StrokeBrightness),
                node.StrokeAlpha);
            return CompileGeometry(node, transform, fill, stroke, P);
        }

        private static ShapeDefinition CompileGeometry(PresetNode node, TransformDefinition transform,
            ColorDefinition fill, ColorDefinition stroke, Func<PresetParameter, ParameterDefinition> parameter)
        {
            var opacity = parameter(node.Opacity);
            switch (node.Kind)
            {
                case PresetNodeKind.Circle:
                    return ShapeDefinition.Circle(parameter(node.Width), transform, opacity, style: Style(node),
                        fillColor: fill, strokeColor: stroke,
                        strokeWidth: parameter(node.StrokeWidth));
                case PresetNodeKind.Rectangle:
                    return ShapeDefinition.Rectangle(parameter(node.Width), parameter(node.Height),
                        transform, opacity, style: Style(node), fillColor: fill, strokeColor: stroke,
                        strokeWidth: parameter(node.StrokeWidth));
                case PresetNodeKind.Polygon:
                    return ShapeDefinition.Polygon(
                        node.Vertices.Select(point => new Point2(point.X, point.Y)).ToArray(), transform, opacity,
                        style: Style(node), fillColor: fill, strokeColor: stroke,
                        strokeWidth: parameter(node.StrokeWidth));
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
