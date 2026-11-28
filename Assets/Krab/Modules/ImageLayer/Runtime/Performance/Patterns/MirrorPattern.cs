using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class MirrorPattern : ImagePattern
    {
        private readonly PatternParameter _clip;
        private readonly PatternParameter _size;
        private readonly PatternParameter _gap;
        private readonly PatternParameter _breathe;

        public MirrorPattern() : base("Mirror",
            "One clip and its mirror image facing each other across the center line, breathing on the beat.")
        {
            _clip = Int("Clip", 1, 0, 20);
            _size = Float("Size", 0.8, 0.1, 1.5, "Size (screen height)");
            _gap = Float("Gap", 0, -400, 800);
            _breathe = Float("Breathe", 0.05, 0, 0.5);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float boxHeight = context.Height * values.GetFloat(_size);
            RequirePositive(boxHeight, "Size");

            var clip = set.Wrap(values.GetInt(_clip));
            var frame = Frame(clip, context);
            float boxWidth = context.Width * 0.45f;
            float gap = context.Px(values.GetFloat(_gap));
            float scale = 1f + values.GetFloat(_breathe) * Zigzag(context.Beats, 1);
            var canvas = context.Canvas;

            // Left copy; the right copy is the same drawing reflected across the vertical center line.
            for (int side = 0; side < 2; side++)
            {
                canvas.Push();
                if (side == 1)
                {
                    canvas.Translate(context.Width, 0f);
                    canvas.Scale(-1f, 1f);
                }

                canvas.Translate(context.Width / 2 - gap / 2, context.Height / 2);
                canvas.Scale(scale);
                canvas.ImageFit(clip, frame, -boxWidth, -boxHeight / 2, boxWidth, boxHeight, FitMode.Contain, 1f);
                canvas.Pop();
            }
        }
    }
}
