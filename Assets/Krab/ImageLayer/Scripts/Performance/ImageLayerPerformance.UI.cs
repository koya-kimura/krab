using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Loading;
using Krab.TypeLayout.Performance;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance
{
    /// <summary>Settings UI: deck radios, layout preset and image entry editors, library and display.</summary>
    public sealed partial class ImageLayerPerformance
    {
        private const float EditWindowWidth = 340f;
        private const float SmallButtonWidth = 28f;
        private const int DescriptionLineLength = 56;
        private const int MaxClipSlider = 40;
        private const float MaxFramesPerBeatSlider = 30f;

        private static readonly string[] LookNames = Enum.GetNames(typeof(ImageLook));

        private int _newLayoutPattern;

        public Element CreateElement()
        {
            return UI.Tabs(
                ("Decks", CreateDecksTab),
                ("Layouts", CreateLayoutsTab),
                ("Images", CreateImagesTab),
                ("Library", CreateLibraryTab),
                ("Display", CreateDisplayTab));
        }

        private Element CreateDecksTab()
        {
            var elements = new List<Element>
            {
                UI.Button("All Off", SetAllDecksOff),
                UI.Label("Deck B draws over Deck A. Pick a layout and an image on each deck.")
            };
            foreach (var deck in _decks)
                elements.Add(CreateDeckElement(deck));

            return UI.Column(elements);
        }

        private Element CreateDeckElement(ImageDeck deck)
        {
            return UI.Fold($"Deck {deck.Name}",
                UI.Label(() => DeckStatus(deck)),
                RebuildOnStructureChange(() => UI.Row(
                    UI.Column(CreateLayoutRadio(deck)).SetFlexGrow(1f),
                    UI.Column(CreateImageRadio(deck)).SetFlexGrow(1f))));
        }

        private IEnumerable<Element> CreateLayoutRadio(ImageDeck deck)
        {
            yield return UI.Label("Layout");
            yield return RadioRow(() => deck.LayoutKey == ImageDeck.OffKey, () => "Off",
                () => deck.SelectLayout(ImageDeck.OffKey), deck.LayoutBindingId(ImageDeck.OffKey),
                $"Deck {deck.Name} Layout Off");
            foreach (var preset in _layouts)
            {
                yield return RadioRow(() => deck.LayoutKey == preset.Id, () => preset.Name,
                    () => deck.SelectLayout(preset.Id), deck.LayoutBindingId(preset.Id),
                    $"Deck {deck.Name} Layout {preset.Name}");
            }
        }

        private IEnumerable<Element> CreateImageRadio(ImageDeck deck)
        {
            yield return UI.Label("Image");
            if (_imageEntries.Count == 0) yield return UI.Label("(no images: load a library)");

            foreach (var entry in _imageEntries)
            {
                yield return RadioRow(() => deck.ImageKey == entry.Id, () => entry.Name,
                    () => deck.SelectImage(entry.Id), deck.ImageBindingId(entry.Id),
                    $"Deck {deck.Name} Image {entry.Name}");
            }
        }

        private Element RadioRow(Func<bool> isSelected, Func<string> name, Action select, string bindingId,
            string midiName)
        {
            return UI.Row(
                UI.Button(UI.Label(() => (isSelected() ? "● " : "○ ") + name()), select).SetFlexGrow(1f),
                _midiUi == null ? UI.Space() : _midiUi.CreateButton(bindingId, midiName));
        }

        private string DeckStatus(ImageDeck deck)
        {
            var layout = deck.LayoutKey == ImageDeck.OffKey ? "Off" : FindLayout(deck.LayoutKey)?.Name ?? "-";
            var image = FindImage(deck.ImageKey)?.Name ?? "-";
            return deck.Error == null ? $"{layout} × {image}" : $"{layout} × {image}  — Skipped: {deck.Error}";
        }

        private Element CreateLayoutsTab()
        {
            var patternNames = _patterns.Select(pattern => pattern.Name).ToList();
            return UI.Column(
                UI.Row(
                    UI.Dropdown("New", () => _newLayoutPattern, value => _newLayoutPattern = value, patternNames),
                    UI.Button("Add", () => AddLayout(patternNames[_newLayoutPattern]))),
                SaveRow(SaveLayouts, LoadLayouts, () => _layoutsDirty, () => _layoutsStatus),
                UI.Space(),
                RebuildOnStructureChange(() => UI.Column(_layouts.Select(CreateLayoutRow))));
        }

        private Element CreateLayoutRow(LayoutPreset preset)
        {
            var pattern = FindPattern(preset.Pattern);
            var elements = new List<Element>
            {
                UI.Field("Name", () => preset.Name, value =>
                {
                    preset.Name = value;
                    MarkLayoutsChanged();
                }),
                UI.Label(pattern == null
                    ? $"Unknown pattern '{preset.Pattern}'. Its values are kept but it does not draw."
                    : UiText.Wrap($"{pattern.Name}: {pattern.Description}", DescriptionLineLength))
            };
            if (pattern != null)
                elements.AddRange(pattern.Parameters.Select(parameter => CreateParameterElement(preset, parameter)));

            var window = UI.Window($"Layout: {preset.Name}", UI.Page(elements)).SetWidth(EditWindowWidth);
            return UI.Row(
                UI.Label(() => $"{preset.Name}  ({preset.Pattern})").SetFlexGrow(1f),
                UI.WindowLauncher(UI.Label("Edit"), window),
                UI.Button("Copy", () => DuplicateLayout(preset)),
                UI.Button("↑", () => MoveLayout(preset, -1)).SetWidth(SmallButtonWidth),
                UI.Button("↓", () => MoveLayout(preset, 1)).SetWidth(SmallButtonWidth),
                UI.Button("Delete", () => DeleteLayout(preset)));
        }

        private Element CreateParameterElement(LayoutPreset preset, PatternParameter parameter)
        {
            var values = preset.Values;
            return parameter.Kind switch
            {
                ParameterKind.Bool => UI.Toggle(parameter.Label, () => values.GetBool(parameter), value =>
                {
                    values.Set(parameter, value ? 1 : 0);
                    MarkLayoutsChanged();
                }),
                ParameterKind.Int => UI.Slider(parameter.Label, () => values.GetInt(parameter), value =>
                {
                    values.Set(parameter, value);
                    MarkLayoutsChanged();
                }, (int)parameter.Min, (int)parameter.Max),
                _ => UI.Slider(parameter.Label, () => values.GetFloat(parameter), value =>
                {
                    values.Set(parameter, value);
                    MarkLayoutsChanged();
                }, (float)parameter.Min, (float)parameter.Max),
            };
        }

        private Element CreateImagesTab()
        {
            return UI.Column(
                UI.Row(
                    UI.Button("Add", AddImage),
                    UI.Button("Add Missing Sets", AddMissingImages)),
                SaveRow(SaveImages, LoadImages, () => _imagesDirty, () => _imagesStatus),
                UI.Label(() => _imagesLibrary == null ? "No library loaded." : $"Library: {_imagesLibrary}"),
                UI.Space(),
                RebuildOnStructureChange(() => UI.Column(_imageEntries.Select(CreateImageRow))));
        }

        private Element CreateImageRow(ImageEntry entry)
        {
            var setNames = new List<string>(_setNames);
            if (!setNames.Contains(entry.SetName)) setNames.Insert(0, entry.SetName);

            var window = UI.Window($"Image: {entry.Name}", UI.Page(
                UI.Field("Name", () => entry.Name, value =>
                {
                    entry.Name = value;
                    MarkImagesChanged();
                }),
                UI.Dropdown("Set", () => entry.SetName, value =>
                {
                    entry.SetName = value;
                    MarkImagesChanged();
                }, setNames),
                UI.Slider("Clip (-1 = all clips)", () => entry.Clip, value =>
                {
                    entry.Clip = value;
                    MarkImagesChanged();
                }, ImageEntry.AllClips, MaxClipSlider),
                UI.Dropdown("Look", () => (int)ParseLook(entry.Look), value =>
                {
                    entry.Look = LookNames[value];
                    MarkImagesChanged();
                }, LookNames),
                UI.Slider("Frames Per Beat", () => (float)entry.FramesPerBeat, value =>
                {
                    entry.FramesPerBeat = value;
                    MarkImagesChanged();
                }, 0f, MaxFramesPerBeatSlider))).SetWidth(EditWindowWidth);

            return UI.Row(
                UI.Label(() => $"{entry.Name}  ({ImageSummary(entry)})").SetFlexGrow(1f),
                UI.WindowLauncher(UI.Label("Edit"), window),
                UI.Button("Copy", () => DuplicateImage(entry)),
                UI.Button("↑", () => MoveImage(entry, -1)).SetWidth(SmallButtonWidth),
                UI.Button("↓", () => MoveImage(entry, 1)).SetWidth(SmallButtonWidth),
                UI.Button("Delete", () => DeleteImage(entry)));
        }

        private static string ImageSummary(ImageEntry entry)
        {
            var clip = entry.Clip == ImageEntry.AllClips ? "all" : $"clip {entry.Clip}";
            return $"{entry.SetName}, {clip}, {ParseLook(entry.Look)}, {entry.FramesPerBeat:0.##} fpb";
        }

        private static Element SaveRow(Action save, Action revert, Func<bool> isDirty, Func<string> status)
        {
            return UI.Row(
                UI.Button("Save", save),
                UI.Button("Revert", revert),
                UI.Label(() => (isDirty() ? "Unsaved changes. " : string.Empty) + status()));
        }

        /// <summary>Rows hold MIDI windows, so rebuild only when items come or go, not on every edit.</summary>
        private Element RebuildOnStructureChange(Func<Element> build)
        {
            var builtVersion = -1;
            return UI.DynamicElementOnTrigger(_ => builtVersion != _structureVersion, () =>
            {
                builtVersion = _structureVersion;
                return build();
            });
        }

        private Element CreateLibraryTab()
        {
            var libraries = new List<string>(ImageLibraryPaths.ListLibraries());
            if (!libraries.Contains(_libraryName.Get())) libraries.Insert(0, _libraryName.Get());

            return UI.Column(
                UI.Dropdown("Library", () => _libraryName.Get(), value => _libraryName.Set(value), libraries),
                _compress.CreateElement("Compress (DXT5, ~1/4 memory)"),
                UI.Button("Reload", Reload),
                UI.Label(StatusText),
                UI.DynamicElementIf(() => _issuesText.Length > 0,
                    () => UI.HelpBox(UI.Label(() => _issuesText), HelpBoxType.Warning)));
        }

        private Element CreateDisplayTab()
        {
            return UI.Column(
                _visible.CreateElement("Visible"),
                _dark.CreateElement("Dark"),
                _light.CreateElement("Light"),
                _thresholdLevel.CreateSlider("Threshold", 0f, 1f),
                UI.Label("Dark / Light / Threshold color the Duotone and Threshold looks of every image."));
        }

        private string StatusText()
        {
            if (_loadError != null) return $"Load failed: {_loadError}";
            if (_loading != null) return $"Loading {_libraryName.Get()}… {_progress:P0}";
            if (_library == null) return "No library loaded.";

            var frames = 0;
            foreach (var set in _library.Sets)
            foreach (var clip in set.Clips)
                frames += clip.FrameCount;

            return $"{_library.Name}: {_library.Sets.Count} sets, {frames} frames, " +
                   $"{_library.TextureBytes / 1048576.0:F0} MB, loaded in {_library.LoadSeconds:F1} s";
        }

        private void SetAllDecksOff()
        {
            foreach (var deck in _decks)
                deck.SelectLayout(ImageDeck.OffKey);
        }
    }
}
