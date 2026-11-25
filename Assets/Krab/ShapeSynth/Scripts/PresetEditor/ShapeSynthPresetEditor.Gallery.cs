using System;
using System.Linq;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private const float GalleryCellWidth = 180;
        private const float GalleryCellHeight = GalleryCellWidth * 9 / 16;

        private PresetGridRenderer _galleryRenderer;
        private bool _galleryNeedsReload = true;
        private bool _galleryPlaying = true;
        private bool _galleryLoop = true;
        private float _galleryBeat;
        private int _gallerySeed = 1234;
        private double _galleryLastVisible = double.NegativeInfinity;
        private string _galleryError;

        private Element BuildGalleryWindow()
        {
            var content = Scrollable(
                UI.Row(
                    UI.Toggle("Play", () => _galleryPlaying),
                    UI.Toggle("Loop", () => _galleryLoop),
                    UI.Button("Restart", () => _galleryBeat = 0),
                    UI.Field("BPM", () => _bpm, value => _bpm = Mathf.Max(0, value)),
                    UI.Field("Seed", () => _gallerySeed)),
                UI.Field("Beat", () => _galleryBeat, value => _galleryBeat = Mathf.Max(0, value)),
                UI.DynamicElementIf(() => _galleryError != null,
                    () => UI.Label(() => _galleryError).SetColor(PresetEditorPalette.Error)),
                UI.DynamicElementOnStatusChanged(() => _savedRevision, _ => BuildGalleryGrid()));
            content.RegisterUpdateCallback(_ => _galleryLastVisible = Time.unscaledTimeAsDouble);
            return content;
        }

        private PresetDocument[] GalleryPresets() =>
            _savedPresets.OrderBy(preset => preset.Name, StringComparer.Ordinal)
                .ThenBy(preset => preset.Id, StringComparer.Ordinal).ToArray();

        private Element BuildGalleryGrid()
        {
            var presets = GalleryPresets();
            _galleryNeedsReload = true;
            if (presets.Length == 0) return UI.Label("No saved presets.");

            var rows = (presets.Length + PresetGridRenderer.Columns - 1) / PresetGridRenderer.Columns;
            return UI.Column(Enumerable.Range(0, rows).Select(row =>
                (Element)UI.Row(presets.Skip(row * PresetGridRenderer.Columns)
                    .Take(PresetGridRenderer.Columns)
                    .Select((preset, column) => GalleryCell(preset, row, column, rows)))));
        }

        private Element GalleryCell(PresetDocument preset, int row, int column, int rows)
        {
            var image = UI.Image(() => _galleryRenderer?.Texture)
                .SetWidth(GalleryCellWidth).SetHeight(GalleryCellHeight);
            image.RegisterUpdateCallback(element =>
            {
                if (UIToolkitBuilder.Instance.GetUIObj(element) is not Image view) return;

                view.scaleMode = ScaleMode.StretchToFill;
                view.uv = new Rect((float)column / PresetGridRenderer.Columns,
                    1f - (float)(row + 1) / rows, 1f / PresetGridRenderer.Columns, 1f / rows);
            });
            // A fixed cell width: otherwise a short last row stretches its cells and leaves gaps between them.
            return UI.Column(image,
                    UI.Label(PresetPath.Parse(preset.Name).Title).SetWidth(GalleryCellWidth))
                .SetWidth(GalleryCellWidth).SetFlexGrow(0);
        }

        private void UpdateGallery()
        {
            // Window content is updated only while open. Release GPU resources once it stops being displayed.
            if (Time.unscaledTimeAsDouble - _galleryLastVisible > 0.2)
            {
                DisposeGallery();
                return;
            }

            _galleryRenderer ??= new PresetGridRenderer();
            if (_galleryNeedsReload)
            {
                _galleryRenderer.SetPresets(GalleryPresets());
                _galleryNeedsReload = false;
            }

            if (_galleryPlaying)
            {
                _galleryBeat += Time.unscaledDeltaTime * _bpm / 60f;
            }
            _galleryRenderer.Tick(_galleryBeat, _galleryLoop, _gallerySeed);
            _galleryError = string.IsNullOrEmpty(_galleryRenderer.Error) ? null : _galleryRenderer.Error;
        }

        private void DisposeGallery()
        {
            _galleryRenderer?.Dispose();
            _galleryRenderer = null;
            _galleryNeedsReload = true;
        }
    }
}
