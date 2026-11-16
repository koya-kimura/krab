using System;
using System.Collections.Generic;
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

        /// <summary>
        /// The whole editor; UiBuilder opens it from the shared Settings window like any other target.
        /// The preset is drawn in the background output, so the editor has no preview image of its own.
        /// </summary>
        public Element CreateElement()
        {
            EnsureDocument();
            return UI.Column(
                BuildFileBar(),
                BuildTransportBar(),
                UI.DynamicElementIf(() => _error != null, () => UI.Label(() => _error).SetColor(PresetEditorPalette.Error)),
                UI.DynamicElementIf(
                    () => _listWarning != null,
                    () => UI.Label(() => _listWarning).SetColor(PresetEditorPalette.Error)),
                UI.Tabs(
                    ("Visual", UI.Row(
                        BuildHierarchyPane().SetWidth(260),
                        BuildInspectorPane().SetWidth(620))),
                    ("Values", BuildValuesView())));
        }

        private Element BuildFileBar()
        {
            return UI.Column(
                UI.Field("Name", () => Document.Name, value =>
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
            _listWarning = errors.Count > 0 ? "Some presets could not be read: " + string.Join("; ", errors) : null;
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
