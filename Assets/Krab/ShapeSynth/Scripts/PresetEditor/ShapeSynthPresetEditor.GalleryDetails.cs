using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>
    /// Read-only parameters of the preset chosen in the Gallery: what each Node is, how it is laid out and which
    /// Parameters differ from a new Node's defaults (Base, Rand and Steps, the bound Value and Depth, Flip).
    /// Editing stays in the Visual window.
    /// </summary>
    public sealed partial class ShapeSynthPresetEditor
    {
        private const float GalleryDetailsWidth = 440;

        private string _galleryDetailsId;
        private WindowElement _galleryDetailsWindow;

        private void ShowGalleryDetails(string presetId, Element anchor)
        {
            _galleryDetailsId = presetId;
            ShowPopup(PopupWindow(ref _galleryDetailsWindow, "Preset Editor - Parameters", () => Scrollable(
                UI.DynamicElementOnStatusChanged(() => (_galleryDetailsId, _savedRevision),
                    _ => BuildGalleryDetails()))), anchor);
        }

        private Element BuildGalleryDetails()
        {
            var preset = _savedPresets.FirstOrDefault(p => p.Id == _galleryDetailsId);
            if (preset == null) return UI.Label("Press a preset name to see its parameters.");

            var lines = new List<Element> { PaneTitle(preset.Name) };
            lines.AddRange(PresetSummary(preset).Select(line => (Element)UI.Label(line)));
            return UI.Column(lines).SetMinWidth(GalleryDetailsWidth);
        }

        private static IEnumerable<string> PresetSummary(PresetDocument preset)
        {
            var names = preset.AllValues().ToDictionary(value => value.Id, value => value.Name);
            foreach (var value in preset.LocalValues)
                yield return $"Local Value {value.Name}: {string.Join(" + ", value.Components.Select(Describe))}";

            foreach (var (node, depth) in Walk(preset.RootGroup, 0))
            {
                var indent = new string(' ', depth * 2);
                var header = $"{indent}{node.Kind} \"{node.Name}\"";
                if (node.Kind != PresetNodeKind.Group)
                {
                    header += $"  Anchor {node.Anchor}  Fill {node.Fill}  Stroke {node.Stroke}";
                    if (node.Layout.Kind != PresetLayoutKind.None)
                    {
                        header += $"  Layout {node.Layout.Kind} ×{node.Layout.Count}";
                        if (node.Layout.Kind == PresetLayoutKind.Grid) header += $" ({node.Layout.Columns} cols)";
                        if (node.Layout.Delay > 0) header += $" Delay {N(node.Layout.Delay)} {node.Layout.DelayOrder}";
                        if (node.Layout.FaceOutward) header += " FaceOutward";
                    }
                }

                yield return header;
                var defaults = PresetNode.Create(node.Kind);
                foreach (var (label, parameter, fresh) in NamedParameters(node).Zip(NamedParameters(defaults),
                             (a, b) => (a.Label, a.Parameter, b.Parameter)))
                {
                    var changed = parameter.Base != fresh.Base || parameter.Reference != fresh.Reference ||
                                  parameter.Random != 0 || parameter.GetBinding() != null;
                    if (changed) yield return $"{indent}  {label}: {Describe(parameter, names)}";
                }
            }
        }

        private static IEnumerable<(PresetNode Node, int Depth)> Walk(PresetNode node, int depth)
        {
            yield return (node, depth);
            foreach (var child in node.Children)
            foreach (var entry in Walk(child, depth + 1))
                yield return entry;
        }

        private static IEnumerable<(string Label, PresetParameter Parameter)> NamedParameters(PresetNode node)
        {
            yield return ("Position X", node.PositionX);
            yield return ("Position Y", node.PositionY);
            yield return ("Direction", node.Direction);
            yield return ("Distance", node.Distance);
            yield return ("Rotation", node.Rotation);
            yield return ("Scale X", node.ScaleX);
            yield return ("Scale Y", node.ScaleY);
            yield return ("Width", node.Width);
            yield return ("Height", node.Height);
            yield return ("Opacity", node.Opacity);
            yield return ("Fill Hue", node.FillHue);
            yield return ("Fill Saturation", node.FillSaturation);
            yield return ("Fill Brightness", node.FillBrightness);
            yield return ("Stroke Width", node.StrokeWidth);
            yield return ("Layout Spacing X", node.Layout.SpacingX);
            yield return ("Layout Spacing Y", node.Layout.SpacingY);
            yield return ("Layout Radius", node.Layout.Radius);
        }

        private static string Describe(PresetParameter parameter, IReadOnlyDictionary<string, string> names)
        {
            var text = N(parameter.Base) + Reference(parameter.Reference);
            if (parameter.Random != 0)
            {
                text += $"  Rand ±{N(parameter.Random)}";
                if (parameter.RandomSteps >= 2) text += $" Steps {parameter.RandomSteps}";
            }

            var binding = parameter.GetBinding();
            if (binding != null)
            {
                var value = names.TryGetValue(binding.ValueId, out var name) ? name : "(missing)";
                text += $"  + {N(binding.Depth)} × {value}";
                if (parameter.Flip != PresetFlip.None) text += $"  Flip {parameter.Flip}";
            }

            return text;
        }

        private static string Describe(PresetComponent component) => component.Kind == ModulatorKind.Adsr
            ? $"ADSR {N(component.Attack)}/{N(component.Decay)}/{N(component.SustainLevel)}/" +
              $"{N(component.SustainTime)}/{N(component.Release)}"
            : $"LFO {component.Waveform} {N(component.CyclesPerBeat)}/beat";

        private static string Reference(ReferenceSource reference) => reference switch
        {
            ReferenceSource.CanvasWidth => " xW",
            ReferenceSource.CanvasHeight => " xH",
            ReferenceSource.MinDimension => " xMin",
            ReferenceSource.MaxDimension => " xMax",
            _ => string.Empty
        };

        private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
