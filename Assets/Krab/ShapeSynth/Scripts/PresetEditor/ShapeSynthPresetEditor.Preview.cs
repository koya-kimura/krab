using RosettaUI;
using UnityEngine.UIElements;
using T = Krab.ShapeSynth.PresetEditor.PresetEditorTheme;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private static readonly string[] PreviewModeNames = { "Static · signals = 1", "Beat · BPM clock", "Animated · Voice" };

        private Element BuildPreviewPane()
        {
            return UI.Column(
                BuildPreviewToolbar(),
                _previewView.CreateElement().Styled(style => T.Padding(style, 14, 14)),
                BuildPreviewFooter());
        }

        private Element BuildPreviewToolbar()
        {
            return UI.Row(
                T.Button(() => IsPreviewRunning ? "● Playing" : "▶ Preview", Play, null, () => IsPreviewRunning),
                EnabledButton("■ Stop", StopPlayback, () => IsPreviewRunning),
                Choice(null, () => (int)_previewMode, value => SetPreviewMode((PresetPreviewMode)value), PreviewModeNames)
                    .SetWidth(160),
                UI.Space().SetFlexGrow(1),
                StateText(() => "Beat", () => _previewMode == PresetPreviewMode.Static ? T.Hex(0x5f6878) : T.Muted, 11)
                    .Styled(style => style.marginRight = 4),
                UI.Slider(null, () => _beat, SetBeat, 0f, PresetSignalSampler.GraphBeats, new SliderOption(false))
                    .SetWidth(130)
                    .RegisterUpdateCallback(element => element.Interactable = _previewMode != PresetPreviewMode.Animated),
                StateText(
                    () => _previewMode == PresetPreviewMode.Static ? "—" : _beat.ToString("F2"),
                    () => IsPreviewRunning ? T.Output : T.Hex(0x6f7a8a),
                    11).SetWidth(34),
                T.Button("Fit", () => _previewView.Fit())
            ).SetFlexWrap(true).Styled(style =>
            {
                style.minHeight = 40;
                style.alignItems = Align.Center;
                T.Padding(style, 6, 10);
                T.Border(style, T.Stroke, 0, 0, 1, 0);
                style.backgroundColor = T.Panel;
            });
        }

        private Element BuildPreviewFooter()
        {
            return UI.Row(
                T.Text(() => "Preview · " + PreviewModeLabel(), 10, T.Hex(0x788599)),
                UI.Space().SetFlexGrow(1),
                UI.Field(Caption("Auto"), () => _autoPreview, value =>
                {
                    _autoPreview = value;
                    if (value) QueuePreview();
                }).SetWidth(72),
                UI.Field(Caption("BPM"), () => _bpm, value =>
                {
                    _bpm = value;
                    QueuePreview();
                }).SetWidth(100),
                UI.Field(Caption("Seed"), () => _seed, value =>
                {
                    _seed = value;
                    QueuePreview();
                }).SetWidth(130),
                T.Text(() => _previewView.ZoomAndTextureSize(), 10, T.Hex(0x788599)).Styled(style => style.marginLeft = 10)
            ).SetFlexWrap(true).Styled(style =>
            {
                style.alignItems = Align.Center;
                style.fontSize = 10;
                T.Padding(style, 3, 10);
                T.Border(style, T.Hex(0x292f38), 1, 0, 0, 0);
                style.backgroundColor = T.Panel;
            });
        }
    }
}
