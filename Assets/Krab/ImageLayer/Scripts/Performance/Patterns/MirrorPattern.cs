using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class MirrorPattern : ImagePattern
    {
        private readonly PrefsInt _clip;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _gap;
        private readonly PrefsFloat _breathe;

        public MirrorPattern() : base("Mirror",
            "One clip and its mirror image facing each other across the center line, breathing on the beat.",
            "animal", ImageLook.Duotone)
        {
            _clip = new PrefsInt(Key("Clip"), 1);
            _size = new PrefsFloat(Key("Size"), 0.8f);
            _gap = new PrefsFloat(Key("Gap"), 0);
            _breathe = new PrefsFloat(Key("Breathe"), 0.05f);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _clip.CreateSlider("Clip", 0, 20),
                _size.CreateSlider("Size (screen height)", 0.1f, 1.5f),
                _gap.CreateSlider("Gap", -400f, 800f),
                _breathe.CreateSlider("Breathe", 0f, 0.5f));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float boxHeight = context.Height * _size.Get();
            RequirePositive(boxHeight, "Size");

            var clip = set.Wrap(_clip.Get());
            var frame = Frame(clip, context);
            float boxWidth = context.Width * 0.45f;
            float gap = context.Px(_gap.Get());
            float scale = 1f + _breathe.Get() * Zigzag(context.Beats, 1);
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
