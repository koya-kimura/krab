using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private IReadOnlyList<PresetDocument> _savedPresets = Array.Empty<PresetDocument>();

        // Kept apart from _error so that a later successful edit does not hide it; replaced on each list refresh.
        private string _listWarning;
        private int _savedRevision;
        private int _openIndex;
        private string _presetFilesSignature;
        private double _nextPresetCheck;

        /// <summary>
        /// Two windows over the same document, opened from the shared Settings window. Visual owns the file bar and
        /// playback; Values only edits signals. The preset is drawn in the background output, so neither window has
        /// a preview image of its own. Each window scrolls both ways when it is made smaller than its content.
        /// </summary>
        public IReadOnlyList<(string Name, Element Content)> CreateWindows()
        {
            EnsureDocument();
            return new[] { ("Visual", BuildVisualWindow()), ("Values", BuildValuesWindow()) };
        }

        /// <summary>Single-window form for hosts that do not use CreateWindows: the Visual window.</summary>
        public Element CreateElement()
        {
            EnsureDocument();
            return BuildVisualWindow();
        }

        private Element BuildVisualWindow()
        {
            return Scrollable(
                BuildFileBar(),
                BuildTransportBar(),
                BuildMessages(),
                UI.Row(
                    BuildHierarchyPane().SetWidth(170),
                    BuildInspectorPane().SetWidth(620)));
        }

        private Element BuildValuesWindow()
        {
            return Scrollable(
                BuildMessages(),
                BuildValuesView());
        }

        private Element BuildMessages()
        {
            return UI.Column(
                UI.DynamicElementIf(
                    () => _error != null,
                    () => UI.Label(() => _error).SetColor(PresetEditorPalette.Error)),
                UI.DynamicElementIf(
                    () => _listWarning != null,
                    () => UI.Label(() => _listWarning).SetColor(PresetEditorPalette.Error)));
        }

        // Blocks keep their natural height; otherwise a short window squeezes the inner panes instead of scrolling.
        private static Element Scrollable(params Element[] contents) =>
            UI.ScrollViewVerticalAndHorizontal(null, null, contents.Select(content => content.SetFlexShrink(0)));

        private Element BuildFileBar()
        {
            return UI.Column(
                // "group/name" files the preset under a Performance tab named after the group.
                UI.Field("Name (group/name)", () => Document.Name, value =>
                {
                    Document.Name = value;
                    _dirty = true;
                }),
                UI.Row(
                    UI.Button("New", NewDocument),
                    // The window title belongs to UiBuilder, so the unsaved mark sits on Save.
                    UI.Button(UI.Label(() => _dirty ? "Save *" : "Save"), SaveDocument),
                    UI.DynamicElementOnStatusChanged(() => _savedRevision, _ => BuildOpenChoice())));
        }

        private Element BuildOpenChoice()
        {
            var presets = _savedPresets;
            return UI.Row(
                Choice(
                    null,
                    () => Math.Min(_openIndex, Math.Max(0, presets.Count - 1)),
                    index => _openIndex = index,
                    presets.Select(preset => preset.Name)).SetMinWidth(160),
                UI.Button("Open", () =>
                {
                    if (_openIndex < presets.Count) OpenDocument(presets[_openIndex].Id);
                }).SetInteractable(presets.Count > 0));
        }

        private static Element PaneTitle(string title)
        {
            return UI.Label(title).SetColor(PresetEditorPalette.Title);
        }

        /// <summary>Reloads the Open list; unreadable files stay out of it and their reasons go to the warning line.</summary>
        private void RefreshSavedPresets()
        {
            IReadOnlyList<string> errors = Array.Empty<string>();
            Guard(() =>
            {
                _savedPresets = Repository.List(out errors);
                _savedRevision++;
            });
            _presetFilesSignature = PresetFilesSignature();
            _listWarning = errors.Count > 0 ? "Some presets could not be read: " + string.Join("; ", errors) : null;
        }

        private void RefreshSavedPresetsWhenFilesChange()
        {
            var now = Time.unscaledTimeAsDouble;
            if (now < _nextPresetCheck) return;

            _nextPresetCheck = now + 1;
            if (PresetFilesSignature() != _presetFilesSignature) RefreshSavedPresets();
        }

        private string PresetFilesSignature()
        {
            if (!Directory.Exists(Repository.DirectoryPath)) return string.Empty;

            var files = Directory.GetFiles(Repository.DirectoryPath, "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            var signature = new StringBuilder();
            foreach (var file in files)
            {
                signature.Append(file).Append('|').Append(File.GetLastWriteTimeUtc(file).Ticks).Append('\n');
            }

            return signature.ToString();
        }

        // RosettaUI's custom dropdown menu uses a Unity internal interface removed in this Unity version.
        // Keep Rosetta's value binding and fall back to Unity's default runtime menu.
        private static DropdownElement Choice(
            LabelElement label,
            Func<int> read,
            Action<int> write,
            IEnumerable<string> options)
        {
            var element = UI.Dropdown(label, read, write, options);
            element.RegisterUpdateCallback(_ => DisableCustomMenu(element));
            return element;
        }

        private static void DisableCustomMenu(Element element)
        {
            var view = UIToolkitBuilder.Instance.GetUIObj(element);
            if (view == null || view.userData is DropdownElement) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var type = view.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField("createMenuCallback", flags);
                if (field == null) continue;

                field.SetValue(view, null);
                break;
            }

            // Marks the view as patched; RosettaUI does not use userData on dropdown views.
            view.userData = element;
        }
    }
}
