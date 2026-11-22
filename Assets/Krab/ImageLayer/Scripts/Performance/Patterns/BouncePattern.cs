using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class BouncePattern : ImagePattern
    {
        private const int MaxCount = 16;
        private const float Squash = 0.2f;

        private readonly PrefsInt _count;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _jump;
        private readonly PrefsFloat _beatsPerBounce;
        private readonly PrefsFloat _stagger;
        private readonly PrefsFloat _floor;

        public BouncePattern() : base("Bounce",
            "A row of clips hopping on the beat, squashing as they land. Stagger spreads the hops along the row.",
            "human", ImageLook.Original)
        {
            _count = new PrefsInt(Key("Count"), 6);
            _size = new PrefsFloat(Key("Size"), 320);
            _jump = new PrefsFloat(Key("Jump"), 260);
            _beatsPerBounce = new PrefsFloat(Key("Beats Per Bounce"), 1);
            _stagger = new PrefsFloat(Key("Stagger"), 0.25f);
            _floor = new PrefsFloat(Key("Floor"), 0.9f);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _count.CreateSlider("Count", 1, MaxCount),
                _size.CreateSlider("Size", 40f, 800f),
                _jump.CreateSlider("Jump", 0f, 800f),
                _beatsPerBounce.CreateSlider("Beats Per Bounce", 0.25f, 8f),
                _stagger.CreateSlider("Stagger (bounces between neighbors)", 0f, 1f),
                _floor.CreateSlider("Floor (0 top .. 1 bottom)", 0f, 1f));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");
            float beatsPerBounce = _beatsPerBounce.Get();
            RequirePositive(beatsPerBounce, "Beats Per Bounce");

            int count = Math.Clamp(_count.Get(), 1, MaxCount);
            float cellWidth = context.Width / count;
            float floor = context.Height * _floor.Get();
            float jump = context.Px(_jump.Get());
            var canvas = context.Canvas;
            for (int index = 0; index < count; index++)
            {
                double position = context.Beats / beatsPerBounce - index * _stagger.Get();
                float phase = (float)(position - Math.Floor(position));

                // Parabola: on the floor at the beat, highest halfway; squashed for a moment around landing.
                float height = 1f - (2f * phase - 1f) * (2f * phase - 1f);
                float squash = Squash * Math.Max(0f, 1f - height * 4f);
                var clip = set.Wrap(index);

                canvas.Push();
                canvas.Translate(cellWidth * (index + 0.5f), floor - jump * height);
                canvas.Scale(1f + squash, 1f - squash);
                canvas.ImageFit(clip, Frame(clip, context, index * 4), -cellWidth / 2, -size, cellWidth, size,
                    FitMode.Contain, 0.5f, 1f);
                canvas.Pop();
            }
        }
    }
}
