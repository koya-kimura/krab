using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Unity.Persistence;
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


        // Only one page is compiled and drawn at a time, so the cell RenderTexture and the per-frame evaluation
        // stay the size of one page however many presets are saved.
        private const int GalleryPageSize = 24;

        private int _galleryPage;

        private enum GallerySort
        {
            Name,
            Group,
            Rating
        }

        private static readonly string[] GallerySortNames = Enum.GetNames(typeof(GallerySort));

        private GallerySort _gallerySort;
        private Dictionary<string, int> _ratings;
        private int _ratingsRevision;

        // Ratings live in their own file and are saved on each click, apart from presets and Cmd+S.
        private Dictionary<string, int> Ratings => _ratings ??= Repository.LoadRatings();

        private PresetGridRenderer _galleryRenderer;
        private bool _galleryNeedsReload = true;
        private bool _galleryPlaying = true;
        private bool _galleryLoop = true;
        private float _galleryBeat;
        private double _galleryLastVisible = double.NegativeInfinity;
        private string _galleryError;

        private Element BuildGalleryWindow()
        {
            var content = Scrollable(
                UI.Row(
                    UI.Toggle("Play", () => _galleryPlaying),
                    UI.Toggle("Loop", () => _galleryLoop),
                    UI.Button("Restart", () => _galleryBeat = 0),
                    UI.Field("BPM", () => _bpm, value => _bpm = Mathf.Max(0, value))),
                UI.Field("Beat", () => _galleryBeat, value => _galleryBeat = Mathf.Max(0, value)),
                UI.DynamicElementIf(() => _galleryError != null,
                    () => UI.Label(() => _galleryError).SetColor(PresetEditorPalette.Error)),
                UI.Row(
                    Choice("Sort", () => (int)_gallerySort, value =>
                    {
                        _gallerySort = (GallerySort)value;
                        SetGalleryPage(0);
                    }, GallerySortNames).SetWidth(200),
                    UI.Button("◀", () => SetGalleryPage(_galleryPage - 1)),
                    UI.Label(() => $"Page {GalleryPage() + 1} / {GalleryPageCount()}  ({_savedPresets.Count} presets)"),
                    UI.Button("▶", () => SetGalleryPage(_galleryPage + 1))),
                // A page is small enough to show whole, so the grid is not put in a scroll view.
                UI.Column(
                    UI.DynamicElementOnStatusChanged(
                        () => (_savedRevision, GalleryPage(), _gallerySort,
                            _gallerySort == GallerySort.Rating ? _ratingsRevision : 0),
                        _ => BuildGalleryGrid())));
            content.RegisterUpdateCallback(_ => _galleryLastVisible = Time.unscaledTimeAsDouble);
            return content;
        }

        /// <summary>
        /// The presets of the current page. Name sorts by the part after the group (so numbers run across groups),
        /// Group by group then name, Rating by most ○ first then name.
        /// </summary>
        private PresetDocument[] GalleryPresets()
        {
            IOrderedEnumerable<PresetDocument> sorted = _gallerySort switch
            {
                GallerySort.Group => _savedPresets.OrderBy(p => PresetPath.Parse(p.Name).Group, StringComparer.Ordinal)
                    .ThenBy(p => PresetPath.Parse(p.Name).Title, StringComparer.Ordinal),
                GallerySort.Rating => _savedPresets.OrderByDescending(Rating)
                    .ThenBy(p => PresetPath.Parse(p.Name).Title, StringComparer.Ordinal),
                _ => _savedPresets.OrderBy(p => PresetPath.Parse(p.Name).Title, StringComparer.Ordinal)
            };
            return sorted.ThenBy(p => p.Id, StringComparer.Ordinal)
                .Skip(GalleryPage() * GalleryPageSize).Take(GalleryPageSize).ToArray();
        }

        private int Rating(PresetDocument preset) => Ratings.TryGetValue(preset.Id, out var rating) ? rating : 0;

        /// <summary>Pressing the current rating again clears it.</summary>
        private void SetRating(PresetDocument preset, int rating)
        {
            if (Rating(preset) == rating) Ratings.Remove(preset.Id);
            else Ratings[preset.Id] = rating;

            _ratingsRevision++;
            GuardValues(() => Repository.SaveRatings(Ratings));
        }

        /// <summary>Five ○ buttons; the first n show ● for a rating of n.</summary>
        private Element RatingRow(PresetDocument preset)
        {
            // Margins and padding are removed so the five buttons fit the width of the name button.
            return UI.Row(Enumerable.Range(1, PresetRepository.MaxRating).Select(n =>
                    (Element)Compact(UI.Button(UI.Label(() => Rating(preset) >= n ? "●" : "○"),
                        () => SetRating(preset, n))).SetWidth(GalleryCellWidth / PresetRepository.MaxRating)))
                .SetWidth(GalleryCellWidth);
        }

        private int GalleryPageCount() => Math.Max(1, (_savedPresets.Count + GalleryPageSize - 1) / GalleryPageSize);

        // Clamped on read, so a page past the end after presets are deleted shows the last page.
        private int GalleryPage() => Math.Clamp(_galleryPage, 0, GalleryPageCount() - 1);

        private void SetGalleryPage(int page)
        {
            _galleryPage = Math.Clamp(page, 0, GalleryPageCount() - 1);
            _galleryNeedsReload = true;
        }

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
            // The name button opens this preset's parameters in their own window.
            ButtonElement name = null;
            name = LeftAligned(UI.Button(UI.Label(PresetPath.Parse(preset.Name).Title),
                    () => ShowGalleryDetails(preset.Id, name)))
                .SetWidth(GalleryCellWidth);
            name.RegisterUpdateCallback(element => element.Style.BackgroundColor =
                preset.Id == _galleryDetailsId ? PresetEditorPalette.Selected : null);
            return UI.Column(image, name, RatingRow(preset)).SetWidth(GalleryCellWidth).SetFlexGrow(0);
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
            _galleryRenderer.Tick(_galleryBeat, _galleryLoop);
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
