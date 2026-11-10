using System;
using System.Collections.Generic;

namespace Krab.ShapeSynth.Core
{
    /// <summary>Compiles each definition occurrence to existing signal references; owns no voices/modulators.</summary>
    public sealed class HierarchyEvaluator
    {
        public GroupDefinition Definition { get; }
        private readonly EvaluatedNode _root;

        public HierarchyEvaluator(GroupDefinition definition, IReadOnlyList<IModulator> modulators)
            : this(definition, modulators, null) { }

        internal HierarchyEvaluator(GroupDefinition definition, IReadOnlyList<IModulator> modulators,
            Func<GroupDefinition, IReadOnlyList<IModulator>, IReadOnlyList<IModulator>> resolveScope)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (modulators == null) throw new ArgumentNullException(nameof(modulators));
            _root = Compile(definition, modulators, new HashSet<INodeDefinition>(), resolveScope);
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
            Func<GroupDefinition, IReadOnlyList<IModulator>, IReadOnlyList<IModulator>> resolveScope)
        {
            if (definition == null) throw new ArgumentException("Null node.", nameof(definition));
            if (!activePath.Add(definition)) throw new ArgumentException("Group hierarchy contains a cycle.", nameof(definition));
            try
            {
                if (definition is ShapeDefinition shape) return new EvaluatedNode(shape, modulators, Array.Empty<EvaluatedNode>());
                if (!(definition is GroupDefinition group)) throw new ArgumentException("Unsupported node definition.", nameof(definition));
                if (resolveScope != null) modulators = resolveScope(group, modulators);
                var children = new EvaluatedNode[group.Children.Count];
                for (var i = 0; i < children.Length; i++) children[i] = Compile(group.Children[i], modulators, activePath, resolveScope);
                return new EvaluatedNode(group, modulators, children);
            }
            finally { activePath.Remove(definition); }
        }

        private sealed class EvaluatedNode
        {
            private readonly ParameterEvaluator[] _transform;
            private readonly ShapeDefinition _shape;
            private readonly ParameterEvaluator _width;
            private readonly ParameterEvaluator _height;
            private readonly ParameterEvaluator _opacity;
            private readonly EvaluatedNode[] _children;

            public EvaluatedNode(INodeDefinition definition, IReadOnlyList<IModulator> modulators, EvaluatedNode[] children)
            {
                var transform = definition.Transform;
                _transform = new[]
                {
                    new ParameterEvaluator(transform.PositionX, modulators),
                    new ParameterEvaluator(transform.PositionY, modulators),
                    new ParameterEvaluator(transform.RotationDegrees, modulators),
                    new ParameterEvaluator(transform.ScaleX, modulators),
                    new ParameterEvaluator(transform.ScaleY, modulators)
                };
                _children = children;
                _shape = definition as ShapeDefinition;
                if (_shape == null) return;
                _width = new ParameterEvaluator(_shape.Width, modulators);
                _height = new ParameterEvaluator(_shape.Height, modulators);
                _opacity = new ParameterEvaluator(_shape.Opacity, modulators);
            }

            public void Append(double now, ReferenceContext context, Affine2D parent, List<DrawItem> output)
            {
                var local = Affine2D.Trs(_transform[0].Evaluate(now, context), _transform[1].Evaluate(now, context),
                    _transform[2].Evaluate(now, context), _transform[3].Evaluate(now, context), _transform[4].Evaluate(now, context));
                var world = parent * local;
                if (_shape != null)
                    output.Add(new DrawItem(_shape, world, _width.Evaluate(now, context),
                        _height.Evaluate(now, context), _opacity.Evaluate(now, context)));
                foreach (var child in _children) child.Append(now, context, world, output);
            }
        }
    }
}
