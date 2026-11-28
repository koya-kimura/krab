using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Voices;
using Krab.ShapeSynth.Unity.Rendering;
using UnityEngine;

namespace Krab.ShapeSynth.Samples
{
    /// <summary>Drawing-boundary checks only; no production Voice/Trigger ownership.</summary>
    [RequireComponent(typeof(TargetCameraCanvas))]
    public sealed class RendererSmokeTest : MonoBehaviour
    {
        [SerializeField] private Camera _outputCamera;
        [SerializeField] private bool _invertTargetY = true;
        private TargetCameraCanvas _canvas;
        private HierarchyEvaluator _evaluator;

        private void Start()
        {
            if (!_outputCamera)
            {
                Debug.LogError($"{nameof(RendererSmokeTest)} requires its output {nameof(Camera)}.", this);
                enabled = false;
                return;
            }

            _canvas = GetComponent<TargetCameraCanvas>();
            _canvas.Initialize(_outputCamera);
            _evaluator = new HierarchyEvaluator(BuildDefinition(), Array.Empty<IModulator>());
        }

        private void Update()
        {
            _canvas.EnsureInitialized();
            var width = _outputCamera.pixelWidth;
            var height = _outputCamera.pixelHeight;
            LastItems = _evaluator.Evaluate(Time.timeAsDouble, new ReferenceContext(width, height));
            UniteShapeAdapter.Draw(_canvas, LastItems, width, height, _invertTargetY);
            SubmittedFrameCount++;
        }

        public IReadOnlyList<DrawItem> LastItems { get; private set; }

        public int SubmittedFrameCount { get; private set; }

        private static GroupDefinition BuildDefinition()
        {
            var cyan = new RgbaColor(0.1, 0.85, 0.95);
            var orange = new RgbaColor(1, 0.4, 0.1);
            var pink = new RgbaColor(0.95, 0.2, 0.6);
            var triangle = new[] { new Point2(-1, -0.65), new Point2(1, -0.65), new Point2(0, 1) };
            return new GroupDefinition(null,
                ShapeDefinition.Circle(Size(0.055), At(-0.24, 0.4), color: cyan),
                ShapeDefinition.Rectangle(Size(0.07), Size(0.045), At(0, 0.4), color: orange),
                ShapeDefinition.Polygon(triangle, At(0.24, 0.4, Size(0.035), Size(0.035)), color: pink),
                // Parent non-uniform scale + child rotation keeps the resulting shear.
                new GroupDefinition(At(-0.32, -0.41, C(-1.8), C(0.7)),
                    ShapeDefinition.Circle(Size(0.035), color: cyan),
                    ShapeDefinition.Rectangle(Size(0.035), Size(0.035),
                        new TransformDefinition(positionX: Size(0.06), rotationDegrees: C(35)), color: orange)),
                // Child order is the painter order: cyan, orange, then pink.
                ShapeDefinition.Rectangle(Size(0.08), Size(0.06), At(0.07, -0.41), C(0.65), cyan),
                ShapeDefinition.Circle(Size(0.07), At(0.095, -0.41), C(0.65), orange),
                ShapeDefinition.Polygon(triangle, At(0.12, -0.41, Size(0.045), Size(0.045)), C(0.65), pink));
        }

        private static ParameterDefinition C(double value)
        {
            return ParameterDefinition.Constant(value);
        }

        private static ParameterDefinition Size(double value)
        {
            return new ParameterDefinition(new ReferenceDefinition(ReferenceSource.MinDimension), value);
        }

        private static TransformDefinition At(
            double x, double y, ParameterDefinition sx = null, ParameterDefinition sy = null)
        {
            return new TransformDefinition(
                new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasWidth), x),
                new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasHeight), y),
                scaleX: sx,
                scaleY: sy);
        }
    }
}
