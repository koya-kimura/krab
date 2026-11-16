using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using Krab.TypeLayout.Unity.Fonts;
using Krab.TypeLayout.Unity.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Krab.TypeLayout.Samples
{
    /// <summary>
    /// Verification sketch for layout precision. Keys: 1 Grid, 2 Frame, 3 Circle, 4 Alignment, G guides.
    /// Lengths are reference pixels (1080 tall) scaled to the output.
    /// </summary>
    [RequireComponent(typeof(TypeLayoutCanvas), typeof(TypeLayoutFont))]
    public sealed class TypeLayoutSmokeTest : MonoBehaviour
    {
        private const double Margin = 40;

        private static readonly Color Pink = new(0.93f, 0.37f, 0.57f);
        private static readonly Color Blue = new(0.24f, 0.29f, 0.68f);
        private static readonly Color SafeAreaColor = new(0.2f, 0.9f, 1f, 0.5f);
        private static readonly string[] ClockLabels = { "12", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11" };

        [SerializeField] private Camera _outputCamera;
        [SerializeField] private DemoCase _case = DemoCase.Grid;
        [SerializeField] private bool _showGuides;

        private TypeLayoutCanvas _canvas;
        private TypeLayoutSketch _sketch;

        private void Start()
        {
            var font = GetComponent<TypeLayoutFont>();
            if (!_outputCamera || font.Face == null)
            {
                Debug.LogError($"{nameof(TypeLayoutSmokeTest)} requires an output {nameof(Camera)} and a loaded font.",
                    this);
                enabled = false;
                return;
            }

            _canvas = GetComponent<TypeLayoutCanvas>();
            _canvas.Initialize(_outputCamera);
            _sketch = new TypeLayoutSketch(_canvas, font.Face);
        }

        private void Update()
        {
            ReadKeys();
            _sketch.ShowGuides = _showGuides;

            var frame = new LayoutFrame(_outputCamera.pixelWidth, _outputCamera.pixelHeight);
            if (_showGuides) UniteGuideRenderer.DrawRect(_canvas, frame.SafeArea(Margin), SafeAreaColor);

            switch (_case)
            {
                case DemoCase.Grid:
                    DrawGrid(frame);
                    break;
                case DemoCase.Frame:
                    DrawFrame(frame);
                    break;
                case DemoCase.Circle:
                    DrawCircle(frame);
                    break;
                case DemoCase.Alignment:
                    DrawAlignment(frame);
                    break;
            }
        }

        public DemoCase Case
        {
            get => _case;
            set => _case = value;
        }

        public bool ShowGuides
        {
            get => _showGuides;
            set => _showGuides = value;
        }

        private void ReadKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame) _case = DemoCase.Grid;
            if (keyboard.digit2Key.wasPressedThisFrame) _case = DemoCase.Frame;
            if (keyboard.digit3Key.wasPressedThisFrame) _case = DemoCase.Circle;
            if (keyboard.digit4Key.wasPressedThisFrame) _case = DemoCase.Alignment;
            if (keyboard.gKey.wasPressedThisFrame) _showGuides = !_showGuides;
        }

        // Letters centered in vertical stripes, one per cell.
        private void DrawGrid(LayoutFrame frame)
        {
            const int columns = 10;
            const int rows = 6;
            var area = frame.Bounds;
            _canvas.Push();
            _canvas.NoStroke();
            for (var column = 0; column < columns; column++)
            {
                var cell = GridDistributor.Cell(area, columns, 1, column, 0);
                _canvas.Fill(column % 2 == 0 ? Blue : Pink);
                _canvas.Rect((float)cell.X, (float)cell.Y, (float)cell.Width, (float)cell.Height);
            }

            _canvas.Pop();

            var slots = GridDistributor.Distribute(area, columns, rows);
            var placement = new PlacementOptions(frame.ToPixels(110), HorizontalAnchor.InkCenter,
                VerticalAnchor.CapCenter, RotationMode.Upright);
            _sketch.DrawSlots(slots, "MiddleModeloridle", placement, Color.white);
        }

        // Text running around the safe area plus straight runs inside it.
        private void DrawFrame(LayoutFrame frame)
        {
            var safe = frame.SafeArea(Margin);
            var size = frame.ToPixels(52);
            var hanging = new PlacementOptions(size, vertical: VerticalAnchor.CapTop);
            var perimeter = new RectPerimeterPath(safe, cornerGap: size);
            _sketch.DrawFlow(perimeter, "Gigandect",
                new FlowOptions(frame.ToPixels(12), FlowAlignment.Justify, FlowFill.WholeText), hanging, Color.white);

            var inner = safe.Inset(frame.ToPixels(160));
            var column = new LinePath(new Point2(inner.Left + inner.Width * 0.75, inner.Top),
                new Point2(inner.Left + inner.Width * 0.75, inner.Bottom));
            _sketch.DrawFlow(column, "Gigandect", new FlowOptions(frame.ToPixels(12), fill: FlowFill.Glyphs),
                new PlacementOptions(size), Color.white);

            var row = LinePath.AlongEdge(inner, RectEdge.Top);
            _sketch.DrawFlow(row, "ectGigande", new FlowOptions(alignment: FlowAlignment.Justify, inkFlush: true),
                new PlacementOptions(size, vertical: VerticalAnchor.Baseline), Color.white);
        }

        // Two rings reading outward and inward, and a clock face of upright numbers.
        private void DrawCircle(LayoutFrame frame)
        {
            var center = frame.Bounds.Center;
            var size = frame.ToPixels(48);
            var ring = new FlowOptions(frame.ToPixels(8), FlowAlignment.Justify, FlowFill.WholeText);
            _sketch.DrawFlow(ArcPath.Circle(center, frame.ToPixels(440)), "Middle Model ", ring,
                new PlacementOptions(size), Color.white);
            _sketch.DrawFlow(ArcPath.Circle(center, frame.ToPixels(350), clockwise: false), "Gigandect ", ring,
                new PlacementOptions(size), Color.white);

            var clock = ArcPath.Circle(center, frame.ToPixels(230));
            _sketch.DrawPathGuide(clock);
            var slots = SlotDistributor.Distribute(clock, ClockLabels.Length, SlotSpacing.Ends);
            _sketch.DrawSlots(slots, ClockLabels, new PlacementOptions(size, HorizontalAnchor.InkCenter,
                VerticalAnchor.InkCenter, RotationMode.Upright), Color.white);
        }

        // Labels flush to every side of the safe area, and the four vertical anchors side by side.
        private void DrawAlignment(LayoutFrame frame)
        {
            var safe = frame.SafeArea(Margin);
            var size = frame.ToPixels(44);
            var top = LinePath.AlongEdge(safe, RectEdge.Top);
            var bottom = new LinePath(safe.PointAt(Anchor.BottomLeft), safe.PointAt(Anchor.BottomRight));
            var hang = new PlacementOptions(size, vertical: VerticalAnchor.CapTop);
            var rest = new PlacementOptions(size, vertical: VerticalAnchor.Baseline);

            _sketch.DrawFlow(top, "TOP LEFT", new FlowOptions(inkFlush: true), hang, Color.white);
            _sketch.DrawFlow(top, "TOP CENTER", new FlowOptions(alignment: FlowAlignment.Center), hang, Color.white);
            _sketch.DrawFlow(top, "TOP RIGHT", new FlowOptions(alignment: FlowAlignment.End, inkFlush: true), hang,
                Color.white);
            _sketch.DrawFlow(bottom, "BOTTOM LEFT", new FlowOptions(inkFlush: true), rest, Color.white);
            _sketch.DrawFlow(bottom, "BOTTOM RIGHT", new FlowOptions(alignment: FlowAlignment.End, inkFlush: true),
                rest, Color.white);
            _sketch.DrawFlow(LinePath.AlongEdge(safe, RectEdge.Left), "LEFT EDGE",
                new FlowOptions(alignment: FlowAlignment.Center), hang, Color.white);
            _sketch.DrawFlow(LinePath.AlongEdge(safe, RectEdge.Right), "RIGHT EDGE",
                new FlowOptions(alignment: FlowAlignment.Center), hang, Color.white);
            _sketch.DrawFlow(new LinePath(safe.PointAt(Anchor.Left), safe.PointAt(Anchor.Right)), "CENTER",
                new FlowOptions(alignment: FlowAlignment.Center), new PlacementOptions(frame.ToPixels(120)),
                Color.white);

            var guide = safe.Inset(frame.ToPixels(200), frame.ToPixels(620), frame.ToPixels(200), 0);
            var line = LinePath.AlongEdge(guide, RectEdge.Top);
            _sketch.DrawPathGuide(line);
            VerticalAnchor[] anchors =
                { VerticalAnchor.Baseline, VerticalAnchor.CapCenter, VerticalAnchor.EmCenter, VerticalAnchor.InkCenter };
            var slots = SlotDistributor.Distribute(line, anchors.Length, SlotSpacing.Cells);
            for (var i = 0; i < anchors.Length; i++)
            {
                _sketch.DrawSlots(new[] { slots[i] }, new[] { "Hxg" },
                    new PlacementOptions(frame.ToPixels(96), vertical: anchors[i]), Color.white);
            }
        }

        public enum DemoCase
        {
            Grid,
            Frame,
            Circle,
            Alignment
        }
    }
}
