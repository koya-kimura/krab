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

        /// <summary>Longest Binding delay in beats across every occurrence; a Voice lives this much longer.</summary>
        public double MaxDelayBeats { get; }

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
                new RandomContext(voiceSeed));
            MaxDelayBeats = _root.MaxDelayBeats;
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
            RandomContext context)
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
            private readonly ParameterEvaluator _strokeWidth;
            private readonly ColorEvaluator _fillColor;
            private readonly ColorEvaluator _strokeColor;
            private readonly EvaluatedNode[] _children;

            public double MaxDelayBeats { get; }

            public EvaluatedNode(INodeDefinition definition, IReadOnlyList<IModulator> modulators,
                EvaluatedNode[] children, RandomContext context)
            {
                var transform = definition.Transform;
                _transform = new[]
                {
                    new ParameterEvaluator(transform.PositionX, modulators, context, 1),
                    new ParameterEvaluator(transform.PositionY, modulators, context, 2),
                    new ParameterEvaluator(transform.RotationDegrees, modulators, context, 3),
                    new ParameterEvaluator(transform.ScaleX, modulators, context, 4),
                    new ParameterEvaluator(transform.ScaleY, modulators, context, 5),
                    new ParameterEvaluator(transform.DirectionDegrees, modulators, context, 17),
                    new ParameterEvaluator(transform.Distance, modulators, context, 18)
                };
                _children = children;
                MaxDelayBeats = MaxDelay(_transform);
                foreach (var child in children)
                {
                    MaxDelayBeats = Math.Max(MaxDelayBeats, child.MaxDelayBeats);
                }

                _shape = definition as ShapeDefinition;
                if (_shape == null) return;

                _width = new ParameterEvaluator(_shape.Width, modulators, context, 6);
                _height = new ParameterEvaluator(_shape.Height, modulators, context,
                    _shape.Kind == ShapeKind.Circle ? 6UL : 7UL);
                _opacity = new ParameterEvaluator(_shape.Opacity, modulators, context, 8);
                // Random keys: fill hue keeps 10 (the former Hue Shift); 9 (the former Color Mix) is retired; 16 is stroke
                // width; 17 and 18 are Direction and Distance.
                _fillColor = new ColorEvaluator(_shape.FillColor, modulators, context, 10, 11, 12);
                _strokeColor = ReferenceEquals(_shape.StrokeColor, _shape.FillColor)
                    ? _fillColor
                    : new ColorEvaluator(_shape.StrokeColor, modulators, context, 13, 14, 15);
                _strokeWidth = new ParameterEvaluator(_shape.StrokeWidth, modulators, context, 16);
                MaxDelayBeats = Math.Max(MaxDelayBeats, MaxDelay(_width, _height, _opacity, _strokeWidth));
                MaxDelayBeats = Math.Max(MaxDelayBeats, _fillColor.MaxDelayBeats);
                MaxDelayBeats = Math.Max(MaxDelayBeats, _strokeColor.MaxDelayBeats);
            }

            /// <summary>Distance along Direction (degrees, 0 = +X, counterclockwise); skips trig when not moving.</summary>
            private static (double X, double Y) Move(double directionDegrees, double distance)
            {
                if (distance == 0) return (0, 0);

                var radians = (directionDegrees % 360) * (Math.PI / 180);
                return (distance * Math.Cos(radians), distance * Math.Sin(radians));
            }

            private static double MaxDelay(params ParameterEvaluator[] evaluators)
            {
                var max = 0.0;
                foreach (var evaluator in evaluators)
                {
                    max = Math.Max(max, evaluator.MaxDelayBeats);
                }

                return max;
            }

            public void Append(double now, ReferenceContext context, Affine2D parent, List<DrawItem> output)
            {
                var (moveX, moveY) = Move(_transform[5].Evaluate(now, context), _transform[6].Evaluate(now, context));
                var local = Affine2D.Trs(_transform[0].Evaluate(now, context) + moveX,
                    _transform[1].Evaluate(now, context) + moveY, _transform[2].Evaluate(now, context),
                    _transform[3].Evaluate(now, context), _transform[4].Evaluate(now, context));
                var world = parent * local;
                if (_shape != null)
                {
                    var fill = _fillColor.Evaluate(now, context);
                    var stroke = ReferenceEquals(_strokeColor, _fillColor)
                        ? fill
                        : _strokeColor.Evaluate(now, context);
                    output.Add(new DrawItem(_shape, world, _width.Evaluate(now, context),
                        _height.Evaluate(now, context), _opacity.Evaluate(now, context), fill, stroke,
                        _strokeWidth.Evaluate(now, context)));
                }

                foreach (var child in _children)
                {
                    child.Append(now, context, world, output);
                }
            }
        }

        /// <summary>Evaluates one HSB color; a Constant color returns its exact RGBA without sampling.</summary>
        private sealed class ColorEvaluator
        {
            private readonly ColorDefinition _definition;
            private readonly ParameterEvaluator _hue;
            private readonly ParameterEvaluator _saturation;
            private readonly ParameterEvaluator _brightness;

            public double MaxDelayBeats { get; }

            public ColorEvaluator(ColorDefinition definition, IReadOnlyList<IModulator> modulators,
                RandomContext context, ulong hueKey, ulong saturationKey, ulong brightnessKey)
            {
                _definition = definition;
                _hue = new ParameterEvaluator(definition.Hue, modulators, context, hueKey);
                _saturation = new ParameterEvaluator(definition.Saturation, modulators, context, saturationKey);
                _brightness = new ParameterEvaluator(definition.Brightness, modulators, context, brightnessKey);
                MaxDelayBeats = Math.Max(_hue.MaxDelayBeats,
                    Math.Max(_saturation.MaxDelayBeats, _brightness.MaxDelayBeats));
            }

            public RgbaColor Evaluate(double now, ReferenceContext context)
            {
                if (_definition.ConstantColor.HasValue) return _definition.ConstantColor.Value;

                return _definition.Resolve(_hue.Evaluate(now, context), _saturation.Evaluate(now, context),
                    _brightness.Evaluate(now, context));
            }
        }
    }
}