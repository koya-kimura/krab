using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Evaluation
{
    /// <summary>Compiles each definition occurrence to existing signal references; owns no voices/modulators.</summary>
    public sealed class HierarchyEvaluator
    {
        private readonly EvaluatedNode _root;

        public GroupDefinition Definition { get; }

        public HierarchyEvaluator(GroupDefinition definition, IReadOnlyList<IModulator> modulators)
            : this(definition, modulators, null, 0)
        {
        }

        public HierarchyEvaluator(GroupDefinition definition, IReadOnlyList<IModulator> modulators, int voiceSeed)
            : this(definition, modulators, null, voiceSeed)
        {
        }

        internal HierarchyEvaluator(GroupDefinition definition, IReadOnlyList<IModulator> modulators,
            Func<GroupDefinition, IReadOnlyList<IModulator>, IReadOnlyList<IModulator>> resolveScope, int voiceSeed = 0)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (modulators == null) throw new ArgumentNullException(nameof(modulators));

            _root = Compile(definition, modulators, new HashSet<INodeDefinition>(), resolveScope,
                new VariationContext(voiceSeed));
        }

        public IReadOnlyList<DrawItem> Evaluate(double nowSeconds, ReferenceContext context)
        {
            VoiceTime.RequireFinite(nowSeconds, nameof(nowSeconds));

            var items = new List<DrawItem>();
            _root.Append(nowSeconds, context, Affine2D.Identity, items);
            return items.AsReadOnly();
        }

        private static EvaluatedNode Compile(INodeDefinition definition, IReadOnlyList<IModulator> modulators,
            HashSet<INodeDefinition> activePath,
            Func<GroupDefinition, IReadOnlyList<IModulator>, IReadOnlyList<IModulator>> resolveScope,
            VariationContext context)
        {
            if (definition == null) throw new ArgumentException("Null node.", nameof(definition));
            if (!activePath.Add(definition))
                throw new ArgumentException("Group hierarchy contains a cycle.", nameof(definition));

            try
            {
                if (definition is ShapeDefinition shape)
                    return new EvaluatedNode(shape, modulators, Array.Empty<EvaluatedNode>(), context);

                if (!(definition is GroupDefinition group))
                    throw new ArgumentException("Unsupported node definition.", nameof(definition));

                if (resolveScope != null) modulators = resolveScope(group, modulators);

                var children = new EvaluatedNode[group.Children.Count];
                for (var i = 0; i < children.Length; i++)
                {
                    children[i] = Compile(group.Children[i], modulators, activePath, resolveScope, context.Child(i));
                }

                return new EvaluatedNode(group, modulators, children, context);
            }
            finally
            {
                activePath.Remove(definition);
            }
        }

        private sealed class EvaluatedNode
        {
            private readonly ParameterEvaluator[] _transform;
            private readonly ShapeDefinition _shape;
            private readonly ParameterEvaluator _width;
            private readonly ParameterEvaluator _height;
            private readonly ParameterEvaluator _opacity;
            private readonly ParameterEvaluator _colorMix;
            private readonly ParameterEvaluator _hueShift;
            private readonly EvaluatedNode[] _children;

            public EvaluatedNode(INodeDefinition definition, IReadOnlyList<IModulator> modulators,
                EvaluatedNode[] children, VariationContext context)
            {
                var transform = definition.Transform;
                _transform = new[]
                {
                    new ParameterEvaluator(transform.PositionX, modulators, context, 1),
                    new ParameterEvaluator(transform.PositionY, modulators, context, 2),
                    new ParameterEvaluator(transform.RotationDegrees, modulators, context, 3),
                    new ParameterEvaluator(transform.ScaleX, modulators, context, 4),
                    new ParameterEvaluator(transform.ScaleY, modulators, context, 5)
                };
                _children = children;
                _shape = definition as ShapeDefinition;
                if (_shape == null) return;

                _width = new ParameterEvaluator(_shape.Width, modulators, context, 6);
                _height = new ParameterEvaluator(_shape.Height, modulators, context,
                    _shape.Kind == ShapeKind.Circle ? 6UL : 7UL);
                _opacity = new ParameterEvaluator(_shape.Opacity, modulators, context, 8);
                _colorMix = new ParameterEvaluator(_shape.FillColor.Mix, modulators, context, 9);
                _hueShift = new ParameterEvaluator(_shape.FillColor.HueShiftDegrees, modulators, context, 10);
            }

            public void Append(double now, ReferenceContext context, Affine2D parent, List<DrawItem> output)
            {
                var local = Affine2D.Trs(_transform[0].Evaluate(now, context), _transform[1].Evaluate(now, context),
                    _transform[2].Evaluate(now, context), _transform[3].Evaluate(now, context),
                    _transform[4].Evaluate(now, context));
                var world = parent * local;
                if (_shape != null)
                {
                    var color = _shape.FillColor.Resolve(_colorMix.Evaluate(now, context),
                        _hueShift.Evaluate(now, context));
                    output.Add(new DrawItem(_shape, world, _width.Evaluate(now, context),
                        _height.Evaluate(now, context), _opacity.Evaluate(now, context), color));
                }

                foreach (var child in _children)
                {
                    child.Append(now, context, world, output);
                }
            }
        }
    }
}
